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

    [HttpPost]
    public async Task<IActionResult> Predict([FromBody] PredictionRequestDto request)
    {
        var result = await _service.GetRiskAsync(request);

        return Ok(new
        {
            risk = result.risk,
            userKey = result.userKey
        });
    }

    [HttpGet("history/{userKey}")]
    public async Task<IActionResult> GetHistory(string userKey)
    {
        var data = await _service.GetUserHistoryAsync(userKey);
        return Ok(data);
    }
}