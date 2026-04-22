using System.Net.Http.Json;

public class PredictionService
{
    private readonly HttpClient _httpClient;

    public PredictionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetRiskAsync(double dti)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:8000/predict",
            new { dti = dti }
        );

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

       return result?["risk"] ?? "Unknown";
    }
}