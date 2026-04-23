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

    public async Task<(string risk, string userKey)> GetRiskAsync(PredictionRequestDto request){
        // Identify user
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

        // Calculate DTI
        var dti = request.Debt / request.Income;

        // Call ML API
        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:8000/predict",
            new { dti = dti }
        );

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        var risk = result?["risk"] ?? "Unknown";

        // Save prediction
        var prediction = new Prediction
        {
            UserId = user.Id,
            Income = request.Income,
            Expenses = request.Expenses,
            Debt = request.Debt,
            Dti = dti,
            Risk = risk
        };

        _context.Predictions.Add(prediction);
        await _context.SaveChangesAsync();

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