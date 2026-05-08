public class Forecast
{
    public int Id { get; set; }

    public int PredictionId { get; set; }

    public Prediction? Prediction { get; set; }

    public required string ForecastMonth { get; set; }

    public double RiskScore { get; set; }

    public string RiskLevel { get; set; } = "";

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}