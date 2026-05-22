public class FutureRiskForecastDto
{
    public string ForecastMonth { get; set; } = string.Empty;

    public double PredictedRiskScore { get; set; }

    public string PredictedRiskLevel { get; set; } = string.Empty;
}