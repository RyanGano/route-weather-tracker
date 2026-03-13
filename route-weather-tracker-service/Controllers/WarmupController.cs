using Microsoft.AspNetCore.Mvc;
using route_weather_tracker_service.Services;

namespace route_weather_tracker_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarmupController : ControllerBase
{
  private readonly IPassAggregatorService _aggregator;
  private readonly ILogger<WarmupController> _logger;

  public WarmupController(IPassAggregatorService aggregator, ILogger<WarmupController> logger)
  {
    _aggregator = aggregator;
    _logger = logger;
  }

  /// <summary>
  /// Triggers a background warmup of cached pass data. Returns quickly with
  /// an Accepted result while warming continues in the background.
  /// </summary>
  [HttpGet]
  public IActionResult Get()
  {
    // Fire-and-forget: populate aggregator caches without delaying the client.
    _ = Task.Run(async () =>
    {
      try
      {
        await _aggregator.GetAllPassesAsync();
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Warmup task failed");
      }
    });

    return Accepted();
  }
}
