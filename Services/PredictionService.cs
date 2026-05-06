using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public class PredictionService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;

    public PredictionService(HttpClient httpClient, AppDbContext context)
    {
        _httpClient = httpClient;
        _context = context;
    }

    // ─────────────────────────────────────────
    // MAIN PREDICTION FLOW
    // ─────────────────────────────────────────
    public async Task<(object risk, string userKey)> GetRiskAsync(PredictionRequestDto request)
    {
        // ─────────────────────────────────────────
        // 1. Identify user
        // ─────────────────────────────────────────
        User? user = null;

        if (!string.IsNullOrEmpty(request.UserKey))
        {
            user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserKey == request.UserKey);
        }

        if (user == null)
        {
            user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);
        }

        if (user == null)
        {
            user = new User
            {
                Name = request.Name,
                Email = request.Email
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────
        // 2. Fetch last 6 records
        // ─────────────────────────────────────────
        var history = await _context.Predictions
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .ToListAsync();

        // ─────────────────────────────────────────
        // 3. Current metrics
        // ─────────────────────────────────────────
        double currentDti = request.Income > 0
            ? request.Debt / request.Income
            : 0;

        // ─────────────────────────────────────────
        // 4. Rolling historical features
        // ─────────────────────────────────────────
        int historyCount = history.Count;

        double avgDti = historyCount > 0
            ? history.Average(p =>
                p.Income > 0
                    ? p.Debt / p.Income
                    : 0)
            : currentDti;

        double avgSavings = historyCount > 0
            ? history.Average(p =>
                p.Income > 0
                    ? (p.Income - p.Expenses) / p.Income
                    : 0)
            : 0;

        double avgDebt = historyCount > 0
            ? history.Average(p => p.Debt)
            : request.Debt;

        double debtTrend = 0;

        if (historyCount >= 2)
        {
            double latestDebt = history[0].Debt;
            double oldestDebt = history[^1].Debt;

            debtTrend = latestDebt - oldestDebt;
        }

        // ─────────────────────────────────────────
        // 5. Build ML feature payload
        // ─────────────────────────────────────────
        var features = new
        {
            income = request.Income,
            expenses = request.Expenses,
            debt = request.Debt,

            shock = "none",

            dti_lag1 = avgDti,
            dti_lag2 = avgDti,

            savings_ratio_lag1 = avgSavings,
            savings_ratio_lag2 = avgSavings,

            debt_lag1 = avgDebt,
            debt_lag2 = avgDebt,

            debt_trend = debtTrend
        };

        // ─────────────────────────────────────────
        // 6. Call ML API
        // ─────────────────────────────────────────
        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:8000/predict",
            features
        );

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("ML API request failed");
        }

        var result = await response.Content
            .ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        if (result == null || !result.ContainsKey("predictions"))
        {
            throw new Exception("Invalid ML API response");
        }

        var predictions = result["predictions"];

        // ─────────────────────────────────────────
        // 7. Parse prediction helper
        // ─────────────────────────────────────────
        object ParseMonth(string monthKey)
        {
            var month = predictions.GetProperty(monthKey);

            return new
            {
                risk_score = month
                    .GetProperty("risk_score")
                    .GetDouble(),

                risk_level = month
                    .GetProperty("risk_level")
                    .GetString()
            };
        }

        // ─────────────────────────────────────────
        // 8. Create future month mapping
        // ─────────────────────────────────────────
        var now = DateTime.UtcNow;

        string month1Key = now.AddMonths(1).ToString("yyyy-MM");
        string month2Key = now.AddMonths(2).ToString("yyyy-MM");
        string month3Key = now.AddMonths(3).ToString("yyyy-MM");

        var forecast = new Dictionary<string, object>
        {
            [month1Key] = ParseMonth("month_1"),
            [month2Key] = ParseMonth("month_2"),
            [month3Key] = ParseMonth("month_3")
        };

        // ─────────────────────────────────────────
        // 9. Save raw prediction record
        // ─────────────────────────────────────────
        var prediction = new Prediction
        {
            UserId = user.Id,
            Income = request.Income,
            Expenses = request.Expenses,
            Debt = request.Debt,
            Dti = currentDti,

            // legacy placeholder
            Risk = "Forecast Generated"
        };

        _context.Predictions.Add(prediction);

        await _context.SaveChangesAsync();

        // ─────────────────────────────────────────
        // 10. Save forecast snapshots
        // ─────────────────────────────────────────
        void AddForecast(
            string forecastMonth,
            string monthKey)
        {
            var month = predictions.GetProperty(monthKey);

            var riskScore = month
                .GetProperty("risk_score")
                .GetDouble();

            var riskLevel = month
                .GetProperty("risk_level")
                .GetString() ?? "UNKNOWN";

            _context.Forecasts.Add(new Forecast
            {
                UserId = user.Id,

                ForecastMonth = forecastMonth,

                RiskScore = riskScore,

                RiskLevel = riskLevel,

                GeneratedAt = DateTime.UtcNow
            });
        }

        AddForecast(month1Key, "month_1");
        AddForecast(month2Key, "month_2");
        AddForecast(month3Key, "month_3");

        await _context.SaveChangesAsync();

        // ─────────────────────────────────────────
        // 11. Return response
        // ─────────────────────────────────────────
        return (forecast, user.UserKey);
    }

    // ─────────────────────────────────────────
    // HISTORY
    // ─────────────────────────────────────────
    public async Task<List<PredictionResponseDto>> GetUserHistoryAsync(string userKey)
    {
        var user = await _context.Users
            .Include(u => u.Forecasts)
            .FirstOrDefaultAsync(u => u.UserKey == userKey);

        if (user == null)
        {
            return new List<PredictionResponseDto>();
        }

        var history = await _context.Predictions
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return history.Select(p => new PredictionResponseDto
        {
            Income = p.Income,

            Expenses = p.Expenses,

            Debt = p.Debt,

            Dti = p.Dti,

            CreatedAt = p.CreatedAt,

            Forecasts = user.Forecasts
                .Where(f =>
                    f.GeneratedAt.Date == p.CreatedAt.Date)
                .OrderBy(f => f.ForecastMonth)
                .Select(f => new ForecastDto
                {
                    ForecastMonth = f.ForecastMonth,

                    RiskScore = f.RiskScore,

                    RiskLevel = f.RiskLevel
                })
                .ToList()
        }).ToList();
    }
}