public class PredictionDetailsDto
{
    public int PredictionId { get; set; }

    public string UserKey { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public double Income { get; set; }

    public double Expenses { get; set; }

    public double Debt { get; set; }

    public double Dti { get; set; }

    public PredictionSummaryDto Summary { get; set; }
        = new();

    public List<ForecastDto> Forecasts { get; set; }
        = new();
}