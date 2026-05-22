public class HistoryAnalyticsResponse
{
    public string UserKey { get; set; } = string.Empty;

    public int TotalPredictions { get; set; }

    public double AverageRiskScore { get; set; }

    public double HighestRiskScore { get; set; }

    public string LatestRiskLevel { get; set; } = string.Empty;

    public List<HistoryTrendPointDto> HistoricalTrend { get; set; } = new();

    public List<FutureRiskForecastDto> FutureTrendForecast { get; set; } = new();
}