using Microsoft.AspNetCore.Mvc;

namespace FinanceRiskAPI.Controllers;

[ApiController]
[Route("analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IHistoryAnalyticsService _historyAnalyticsService;

    public AnalyticsController(IHistoryAnalyticsService historyAnalyticsService)
    {
        _historyAnalyticsService = historyAnalyticsService;
    }

    [HttpGet("history/{userKey}")]
    public async Task<IActionResult> GetHistoryAnalytics(string userKey)
    {
        if (string.IsNullOrWhiteSpace(userKey))
        {
            return BadRequest(new
            {
                message = "User key is required"
            });
        }

        var analytics = await _historyAnalyticsService
            .GetHistoryAnalyticsAsync(userKey);

        if (analytics is null)
        {
            return BadRequest(new
            {
                message = "Unable to load history analytics"
            });
        }

        return Ok(analytics);
    }
}