public class Prediction
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public double Income { get; set; }

    public double Expenses { get; set; }

    public double Debt { get; set; }

    public double Dti { get; set; }

    public required string Risk { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Forecast> Forecasts { get; set; } = new();
}