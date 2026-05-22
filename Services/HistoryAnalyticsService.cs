using Microsoft.EntityFrameworkCore;

public class HistoryAnalyticsService : IHistoryAnalyticsService
{
    private readonly AppDbContext _dbContext;

    public HistoryAnalyticsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HistoryAnalyticsResponse?> GetHistoryAnalyticsAsync(string userKey)
    {
        if (string.IsNullOrWhiteSpace(userKey))
        {
            return null;
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Predictions)
                .ThenInclude(p => p.Forecasts)
            .FirstOrDefaultAsync(u => u.UserKey == userKey);

        if (user is null)
        {
            return new HistoryAnalyticsResponse
            {
                UserKey = userKey,
                TotalPredictions = 0,
                AverageRiskScore = 0,
                HighestRiskScore = 0,
                LatestRiskLevel = "LOW",
                HistoricalTrend = new List<HistoryTrendPointDto>(),
                FutureTrendForecast = new List<FutureRiskForecastDto>()
            };
        }

        var predictions = user.Predictions
            .OrderBy(p => p.CreatedAt)
            .ToList();

        if (predictions.Count == 0)
        {
            return new HistoryAnalyticsResponse
            {
                UserKey = userKey,
                TotalPredictions = 0,
                AverageRiskScore = 0,
                HighestRiskScore = 0,
                LatestRiskLevel = "LOW",
                HistoricalTrend = new List<HistoryTrendPointDto>(),
                FutureTrendForecast = new List<FutureRiskForecastDto>()
            };
        }

        var historicalTrend = predictions
            .Select(prediction =>
            {
                var highestForecast = prediction.Forecasts
                    .OrderByDescending(f => f.RiskScore)
                    .FirstOrDefault();

                var highestRiskScore = highestForecast?.RiskScore ?? 0;

                var overallRiskLevel =
                    !string.IsNullOrWhiteSpace(highestForecast?.RiskLevel)
                        ? highestForecast.RiskLevel
                        : prediction.Risk;

                return new HistoryTrendPointDto
                {
                    PredictionId = prediction.Id,
                    CreatedAt = prediction.CreatedAt,
                    Income = prediction.Income,
                    Expenses = prediction.Expenses,
                    Debt = prediction.Debt,
                    Dti = prediction.Dti,
                    HighestRiskScore = Math.Round(highestRiskScore, 2),
                    OverallRiskLevel = overallRiskLevel
                };
            })
            .ToList();

        var latestPrediction = historicalTrend
            .OrderByDescending(item => item.CreatedAt)
            .First();

        var averageRiskScore = historicalTrend
            .Average(item => item.HighestRiskScore);

        var highestRiskScoreOverall = historicalTrend
            .Max(item => item.HighestRiskScore);

        var futureTrendForecast = BuildFutureTrendForecast(historicalTrend);

        return new HistoryAnalyticsResponse
        {
            UserKey = userKey,
            TotalPredictions = historicalTrend.Count,
            AverageRiskScore = Math.Round(averageRiskScore, 2),
            HighestRiskScore = Math.Round(highestRiskScoreOverall, 2),
            LatestRiskLevel = latestPrediction.OverallRiskLevel,
            HistoricalTrend = historicalTrend,
            FutureTrendForecast = futureTrendForecast
        };
    }

    private static List<FutureRiskForecastDto> BuildFutureTrendForecast(
        List<HistoryTrendPointDto> historicalTrend
    )
    {
        if (historicalTrend.Count == 0)
        {
            return new List<FutureRiskForecastDto>();
        }

        var latestRiskScore = historicalTrend
            .Last()
            .HighestRiskScore;

        double monthlyChange = 0;

        if (historicalTrend.Count >= 2)
        {
            var previousRiskScore = historicalTrend[^2].HighestRiskScore;

            monthlyChange = latestRiskScore - previousRiskScore;
        }

        var forecast = new List<FutureRiskForecastDto>();

        for (var month = 1; month <= 3; month++)
        {
            var predictedRiskScore = latestRiskScore + monthlyChange * month;

            predictedRiskScore = Math.Max(0, predictedRiskScore);

            var roundedRiskScore = Math.Round(predictedRiskScore, 2);

            forecast.Add(new FutureRiskForecastDto
            {
                ForecastMonth = $"Next Month {month}",
                PredictedRiskScore = roundedRiskScore,
                PredictedRiskLevel = GetRiskLevel(roundedRiskScore)
            });
        }

        return forecast;
    }

   private static string GetRiskLevel(double riskScore)
    {
        string riskLevel = "LOW";

        if (riskScore >= 70)
        {
            riskLevel = "HIGH";
        }
        else if (riskScore >= 40)
        {
            riskLevel = "MEDIUM";
        }

        return riskLevel;
    }
}