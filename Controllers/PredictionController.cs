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
        var risk = await _service.GetRiskAsync(request.Dti);
        return Ok(new { risk });
    }
}