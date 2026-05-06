public class Forecast
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public string ForecastMonth { get; set; } = "";

    public double RiskScore { get; set; }

    public string RiskLevel { get; set; } = "";

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}