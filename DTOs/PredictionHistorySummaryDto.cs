public class PredictionHistorySummaryDto
{
    public int PredictionId { get; set; }

    public string UserKey { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public double Income { get; set; }

    public double Expenses { get; set; }

    public double Debt { get; set; }

    public double Dti { get; set; }

    public int ForecastMonths { get; set; }

    public double HighestRiskScore { get; set; }

    public string OverallRiskLevel { get; set; } = "";
}