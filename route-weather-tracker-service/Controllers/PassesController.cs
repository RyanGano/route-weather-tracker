using Microsoft.AspNetCore.Mvc;
using route_weather_tracker_service.Data;
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

  /// <summary>
  /// Returns pass summaries. Pass <c>from</c> and <c>to</c> endpoint IDs to filter
  /// to only the passes that fall between those two locations (in trip order).
  /// Optionally pass <c>highway</c> (e.g. "I-90" or "US-2") to restrict results
  /// to a specific highway corridor. Omit all params to get every known pass.
  /// </summary>
  [HttpGet]
  public async Task<IActionResult> GetAll(
      [FromQuery] string? from,
      [FromQuery] string? to,
      [FromQuery] string? highway,
      CancellationToken ct)
  {
    try
    {
      List<string> passIds;

      // Start with all passes, optionally pre-filtered by highway
      var candidates = string.IsNullOrWhiteSpace(highway)
          ? PassRegistry.Passes
          : PassRegistry.Passes.Where(p => p.Highway.Equals(highway, StringComparison.OrdinalIgnoreCase)).ToList();

      if (from is not null && to is not null)
      {
        var fromEp = RouteEndpointRegistry.GetById(from);
        var toEp = RouteEndpointRegistry.GetById(to);
        if (fromEp is null || toEp is null)
          return BadRequest($"Unknown endpoint id. Valid ids: {string.Join(", ", RouteEndpointRegistry.Endpoints.Select(e => e.Id))}");

        var minLon = Math.Min(fromEp.Longitude, toEp.Longitude);
        var maxLon = Math.Max(fromEp.Longitude, toEp.Longitude);
        var eastward = toEp.Longitude > fromEp.Longitude;

        passIds = candidates
            .Where(p => p.Longitude > minLon && p.Longitude < maxLon)
            .OrderBy(p => eastward ? p.Longitude : -p.Longitude)
            .Select(p => p.Id)
            .ToList();
      }
      else
      {
        passIds = candidates.Select(p => p.Id).ToList();
      }

      var passes = await _aggregator.GetPassesAsync(passIds, ct);
      return Ok(passes);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to retrieve pass summaries (from={From}, to={To})", from, to);
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
