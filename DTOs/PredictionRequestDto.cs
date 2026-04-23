public class PredictionRequestDto
{
    public string? UserKey { get; set; }

    public required string Name { get; set; }
    public required string Email { get; set; }

    public double Income { get; set; }
    public double Expenses { get; set; }
    public double Debt { get; set; }
}