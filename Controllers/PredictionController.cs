using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PredictionController : ControllerBase
{
    private readonly PredictionService
        _predictionService;

    public PredictionController(
        PredictionService predictionService)
    {
        _predictionService =
            predictionService;
    }

    // ROOT

    [HttpGet("/")]
    public IActionResult Root()
    {
        return Ok(new
        {
            service =
                "finance-risk-backend",

            status = "running"
        });
    }

    // HEALTH

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            service =
                "finance-risk-backend",

            status = "ok"
        });
    }

    // PREDICT

    [HttpPost("predict")]
    public async Task<IActionResult>
        Predict(
            [FromBody]
            PredictionRequestDto request)
    {
        var (
            forecast,
            userKey,
            predictionId
        ) =
            await _predictionService
                .GetRiskAsync(request);

        return Ok(new
        {
            status = "success",

            userKey,

            predictionId,

            predictions = forecast
        });
    }

    // HISTORY

    [HttpGet("history/{userKey}")]
    public async Task<IActionResult>
        GetHistory(string userKey)
    {
        var history =
            await _predictionService
                .GetUserHistoryAsync(
                    userKey);

        return Ok(history);
    }

    // PREDICTION DETAILS

    [HttpGet(
        "/prediction/{predictionId}")]
    public async Task<IActionResult>
        GetPredictionDetails(
            int predictionId)
    {
        var data =
            await _predictionService
                .GetPredictionDetailsAsync(
                    predictionId);

        if (data == null)
        {
            return NotFound(new
            {
                status = "error",

                message =
                    "Prediction not found"
            });
        }

        return Ok(data);
    }
}