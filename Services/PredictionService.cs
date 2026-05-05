using System.Net.Http.Json;
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

    public async Task<(string risk, string userKey)> GetRiskAsync(PredictionRequestDto request)
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
        // 2. Fetch history (for lag features)
        // ─────────────────────────────────────────
        var history = await _context.Predictions
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Take(2)
            .ToListAsync();

        // ─────────────────────────────────────────
        // 3. Build features
        // ─────────────────────────────────────────
        double currentDti = request.Debt / request.Income;

        double GetDti(Prediction p) =>
            p.Income > 0 ? p.Debt / p.Income : 0;

        var prev1 = history.ElementAtOrDefault(0);
        var prev2 = history.ElementAtOrDefault(1);

        var prev1Dti = prev1 != null ? GetDti(prev1) : currentDti;
        var prev2Dti = prev2 != null ? GetDti(prev2) : prev1Dti;

        var prev1Savings = prev1 != null
            ? (prev1.Income - prev1.Expenses) / prev1.Income
            : 0;

        var prev2Savings = prev2 != null
            ? (prev2.Income - prev2.Expenses) / prev2.Income
            : 0;

        var prev1Debt = prev1?.Debt ?? request.Debt;
        var prev2Debt = prev2?.Debt ?? prev1Debt;

        var features = new
        {
            income = request.Income,
            expenses = request.Expenses,
            debt = request.Debt,
            shock = "none",

            dti_lag1 = prev1Dti,
            dti_lag2 = prev2Dti,
            savings_ratio_lag1 = prev1Savings,
            savings_ratio_lag2 = prev2Savings,
            debt_lag1 = prev1Debt,
            debt_lag2 = prev2Debt
        };

        // ─────────────────────────────────────────
        // 4. Call ML API
        // ─────────────────────────────────────────
        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:8000/predict",
            features
        );

        if (!response.IsSuccessStatusCode)
        {
            return ("Error", user.UserKey);
        }

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        var risk = result != null && result.ContainsKey("risk_level")
            ? result["risk_level"]?.ToString() ?? "Unknown"
            : "Unknown";

        // ─────────────────────────────────────────
        // 5. Save prediction
        // ─────────────────────────────────────────
        var prediction = new Prediction
        {
            UserId = user.Id,
            Income = request.Income,
            Expenses = request.Expenses,
            Debt = request.Debt,
            Dti = currentDti,
            Risk = risk
        };

        _context.Predictions.Add(prediction);
        await _context.SaveChangesAsync();

        // ─────────────────────────────────────────
        // 6. Return result
        // ─────────────────────────────────────────
        return (risk, user.UserKey);
    }

    public async Task<List<PredictionResponseDto>> GetUserHistoryAsync(string userKey)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserKey == userKey);

        if (user == null)
            return new List<PredictionResponseDto>();

        return await _context.Predictions
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PredictionResponseDto
            {
                Income = p.Income,
                Expenses = p.Expenses,
                Debt = p.Debt,
                Dti = p.Dti,
                Risk = p.Risk,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }
}