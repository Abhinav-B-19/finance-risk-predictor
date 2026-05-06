using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PredictionController : ControllerBase
{
    private readonly PredictionService _service;

    public PredictionController(PredictionService service)
    {
        _service = service;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            service = "finance-risk-backend"
        });
    }

    [HttpPost]
    public async Task<IActionResult> Predict([FromBody] PredictionRequestDto request)
    {
        try
        {
            var result = await _service.GetRiskAsync(request);

            return Ok(new
            {
                status = "success",
                predictions = result.risk,
                userKey = result.userKey
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

    [HttpGet("history/{userKey}")]
    public async Task<IActionResult> GetHistory(string userKey)
    {
        var data = await _service.GetUserHistoryAsync(userKey);
        return Ok(data);
    }
}