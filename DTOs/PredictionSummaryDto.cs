public class PredictionSummaryDto
{
    public int ForecastMonths { get; set; }

    public double HighestRiskScore { get; set; }

    public string OverallRiskLevel { get; set; } = "";
}