using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public class PredictionService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public PredictionService(
        HttpClient httpClient,
        AppDbContext context,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _context = context;
        _configuration = configuration;
    }

    // ─────────────────────────────────────────
    // MAIN PREDICTION FLOW
    // ─────────────────────────────────────────
    public async Task<
    (
        object risk,
        string userKey,
        int predictionId
    )>
    GetRiskAsync(
        PredictionRequestDto request)
    {
        // ─────────────────────────────────────────
        // 1. INPUT VALIDATION
        // ─────────────────────────────────────────
        if (request.Income <= 0)
        {
            throw new Exception(
                "Income must be greater than 0");
        }

        if (request.Expenses < 0)
        {
            throw new Exception(
                "Expenses cannot be negative");
        }

        if (request.Debt < 0)
        {
            throw new Exception(
                "Debt cannot be negative");
        }

        // ─────────────────────────────────────────
        // 2. IDENTIFY USER
        // ─────────────────────────────────────────
        User? user = null;

        if (!string.IsNullOrEmpty(request.UserKey))
        {
            user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.UserKey == request.UserKey);
        }

        if (user == null)
        {
            user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Email == request.Email);
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
        // 3. FETCH LAST 6 RECORDS
        // ─────────────────────────────────────────
        var history = await _context.Predictions
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .ToListAsync();

        // ─────────────────────────────────────────
        // 4. CURRENT METRICS
        // ─────────────────────────────────────────
        double currentDti = request.Income > 0
            ? request.Debt / request.Income
            : 0;

        // ─────────────────────────────────────────
        // 5. HISTORICAL FEATURES
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
        // 6. BUILD ML PAYLOAD
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
        // 7. CALL ML SERVICE
        // ─────────────────────────────────────────
        var mlUrl =
            _configuration["MLService:BaseUrl"] +
            "/predict";

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsJsonAsync(
                mlUrl,
                features
            );
        }
        catch
        {
            throw new Exception(
                "Prediction service temporarily unavailable"
            );
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                "Prediction service temporarily unavailable"
            );
        }

        Dictionary<string, JsonElement>? result;

        try
        {
            result = await response.Content
                .ReadFromJsonAsync<
                    Dictionary<string, JsonElement>>();
        }
        catch
        {
            throw new Exception(
                "Prediction service temporarily unavailable"
            );
        }

        if (result == null ||
            !result.ContainsKey("predictions"))
        {
            throw new Exception(
                "Prediction service temporarily unavailable"
            );
        }

        var predictions =
            result["predictions"];

        // ─────────────────────────────────────────
        // 8. BUILD FORECAST RESPONSE
        // ─────────────────────────────────────────
        var forecast =
            new Dictionary<string, object>();

        foreach (var month in
            predictions.EnumerateObject())
        {
            var monthData = month.Value;

            var riskScore = monthData
                .GetProperty("risk_score")
                .GetDouble();

            var riskLevel = monthData
                .GetProperty("risk_level")
                .GetString() ?? "UNKNOWN";

            forecast[month.Name] = new
            {
                risk_score = riskScore,
                risk_level = riskLevel
            };
        }

        // ─────────────────────────────────────────
        // 9. SAVE PREDICTION SNAPSHOT
        // ─────────────────────────────────────────
        var prediction = new Prediction
        {
            UserId = user.Id,

            Income = request.Income,
            Expenses = request.Expenses,
            Debt = request.Debt,

            Dti = currentDti,

            Risk = "Forecast Generated"
        };

        _context.Predictions.Add(prediction);

        await _context.SaveChangesAsync();

        // ─────────────────────────────────────────
        // 10. SAVE FORECASTS
        // ─────────────────────────────────────────
        foreach (var month in
            predictions.EnumerateObject())
        {
            var monthData = month.Value;

            var riskScore = monthData
                .GetProperty("risk_score")
                .GetDouble();

            var riskLevel = monthData
                .GetProperty("risk_level")
                .GetString() ?? "UNKNOWN";

            _context.Forecasts.Add(new Forecast
            {
                PredictionId = prediction.Id,

                ForecastMonth = month.Name,

                RiskScore = riskScore,

                RiskLevel = riskLevel,

                GeneratedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        // ─────────────────────────────────────────
        // 11. RETURN RESPONSE
        // ─────────────────────────────────────────
        return (
            forecast,
            user.UserKey,
            prediction.Id
        );
    }

    // ─────────────────────────────────────────
    // USER HISTORY
    // ─────────────────────────────────────────
    public async Task<List<PredictionHistorySummaryDto>> GetUserHistoryAsync(string userKey) {
        var user = await _context.Users
            .FirstOrDefaultAsync(
                u => u.UserKey == userKey);

        if (user == null)
        {
            return new List<
                PredictionHistorySummaryDto>();
        }

        var history = await _context.Predictions
            .Include(p => p.Forecasts)
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(
                p => p.CreatedAt)
            .ToListAsync();

        return history.Select(p =>
        {
            var forecasts = p.Forecasts
                .OrderBy(f => f.ForecastMonth)
                .ToList();

            var highestRiskScore = forecasts.Any()
                ? forecasts.Max(f => f.RiskScore)
                : 0;

            string overallRiskLevel = "LOW";

            if (highestRiskScore >= 70)
            {
                overallRiskLevel = "HIGH";
            }
            else if (highestRiskScore >= 40)
            {
                overallRiskLevel = "MEDIUM";
            }

            return new PredictionHistorySummaryDto
            {
                PredictionId = p.Id,

                UserKey = user.UserKey,

                CreatedAt = p.CreatedAt,

                Income = p.Income,

                Expenses = p.Expenses,

                Debt = p.Debt,

                Dti = p.Dti,

                ForecastMonths = forecasts.Count,

                HighestRiskScore = Math.Round(
                    highestRiskScore,
                    2),

                OverallRiskLevel =
                    overallRiskLevel
            };
        })
        .ToList();
    }

    public async Task<PredictionDetailsDto?> GetPredictionDetailsAsync(int predictionId) {
        var prediction = await _context.Predictions
            .Include(p => p.User)
            .Include(p => p.Forecasts)
            .FirstOrDefaultAsync(
                p => p.Id == predictionId);

        if (prediction == null)
        {
            return null;
        }

        var forecasts = prediction.Forecasts
            .OrderBy(f => f.ForecastMonth)
            .ToList();

        var highestRiskScore = forecasts.Any()
            ? forecasts.Max(f => f.RiskScore)
            : 0;

        string overallRiskLevel = "LOW";

        if (highestRiskScore >= 70)
        {
            overallRiskLevel = "HIGH";
        }
        else if (highestRiskScore >= 40)
        {
            overallRiskLevel = "MEDIUM";
        }

        return new PredictionDetailsDto
        {
            PredictionId = prediction.Id,

            UserKey =
                prediction.User?.UserKey ?? "",

            CreatedAt = prediction.CreatedAt,

            Income = prediction.Income,

            Expenses = prediction.Expenses,

            Debt = prediction.Debt,

            Dti = prediction.Dti,

            Summary = new PredictionSummaryDto
            {
                ForecastMonths =
                    forecasts.Count,

                HighestRiskScore =
                    Math.Round(
                        highestRiskScore,
                        2),

                OverallRiskLevel =
                    overallRiskLevel
            },

            Forecasts = forecasts
                .Select(f => new ForecastDto
                {
                    ForecastMonth =
                        f.ForecastMonth,

                    RiskScore =
                        f.RiskScore,

                    RiskLevel =
                        f.RiskLevel
                })
                .ToList()
        };
    }
}