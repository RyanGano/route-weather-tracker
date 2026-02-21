using Microsoft.AspNetCore.Mvc;
using route_weather_tracker_service.Services;

namespace route_weather_tracker_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PassesController : ControllerBase
{
    private readonly IPassAggregatorService _aggregator;
    private readonly ILogger<PassesController> _logger;

    public PassesController(IPassAggregatorService aggregator, ILogger<PassesController> logger)
    {
        _aggregator = aggregator;
        _logger = logger;
    }

    /// <summary>Returns summaries for all three passes on the Stanwood → Kalispell route.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        try
        {
            var passes = await _aggregator.GetAllPassesAsync(ct);
            return Ok(passes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all pass summaries");
            return StatusCode(500, "Unable to retrieve pass data.");
        }
    }

    /// <summary>Returns the summary for a single pass by ID (snoqualmie, fourth-of-july, lookout).</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        try
        {
            var pass = await _aggregator.GetPassAsync(id, ct);
            if (pass is null)
                return NotFound($"Pass '{id}' not found.");
            return Ok(pass);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve pass summary for {PassId}", id);
            return StatusCode(500, "Unable to retrieve pass data.");
        }
    }
}
