using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PredictionController : ControllerBase
{
    private readonly PredictionService _predictionService;

    public PredictionController(
        PredictionService predictionService)
    {
        _predictionService = predictionService;
    }

    // ─────────────────────────────────────────
    // ROOT
    // ─────────────────────────────────────────

    [HttpGet("/")]
    public IActionResult Root()
    {
        return Ok(new
        {
            service = "finance-risk-backend",
            status = "running"
        });
    }

    // ─────────────────────────────────────────
    // HEALTH
    // ─────────────────────────────────────────

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            service = "finance-risk-backend",
            status = "ok"
        });
    }

    // ─────────────────────────────────────────
    // PREDICT
    // ─────────────────────────────────────────

    [HttpPost("predict")]
    public async Task<IActionResult> Predict(
        [FromBody] PredictionRequestDto request)
    {
        try
        {
            var (forecast, userKey) =
                await _predictionService
                    .GetRiskAsync(request);

            return Ok(new
            {
                status = "success",
                userKey,
                predictions = forecast
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                status = "error",
                message = ex.Message
            });
        }
    }

    // ─────────────────────────────────────────
    // HISTORY
    // ─────────────────────────────────────────

    [HttpGet("history/{userKey}")]
    public async Task<IActionResult>
        GetHistory(string userKey)
    {
        try
        {
            var history =
                await _predictionService
                    .GetUserHistoryAsync(userKey);

            return Ok(history);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                status = "error",
                message = ex.Message
            });
        }
    }

    // ─────────────────────────────────────────
    // PREDICTION
    // ─────────────────────────────────────────

    [HttpGet("/prediction/{predictionId}")]
    public async Task<IActionResult>
        GetPredictionDetails(int predictionId)
    {
        try
        {
            var data = await _predictionService
                .GetPredictionDetailsAsync(
                    predictionId);

            if (data == null)
            {
                return NotFound(new
                {
                    status = "error",
                    message = "Prediction not found"
                });
            }

            return Ok(data);
        }
        catch
        {
            return StatusCode(500, new
            {
                status = "error",
                message =
                    "Prediction service temporarily unavailable"
            });
        }
    }
}