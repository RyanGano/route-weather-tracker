using Microsoft.AspNetCore.Mvc;
using route_weather_tracker_service.Data;
using route_weather_tracker_service.Services;

namespace route_weather_tracker_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoutesController : ControllerBase
{
  private readonly IRoutingService _routing;

  public RoutesController(IRoutingService routing) => _routing = routing;

  /// <summary>Returns all known highway routes (I-90, US-2, etc.).</summary>
  [HttpGet]
  public IActionResult GetAll() => Ok(RouteRegistry.Routes);

  /// <summary>
  /// Computes up to three driving route options between two named endpoints,
  /// including alternate routes where OSRM finds them. Each route includes
  /// the mountain pass IDs matched geometrically along its polyline.
  /// </summary>
  /// <param name="from">Origin endpoint ID (e.g. "seattle").</param>
  /// <param name="to">Destination endpoint ID (e.g. "denver").</param>
  [HttpGet("compute")]
  public async Task<IActionResult> Compute(
      [FromQuery] string from,
      [FromQuery] string to,
      CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
      return BadRequest("Both 'from' and 'to' endpoint IDs are required.");

    var fromEp = RouteEndpointRegistry.GetById(from);
    var toEp = RouteEndpointRegistry.GetById(to);

    if (fromEp is null)
      return BadRequest($"Unknown origin '{from}'. Valid ids: {string.Join(", ", RouteEndpointRegistry.Endpoints.Select(e => e.Id))}");
    if (toEp is null)
      return BadRequest($"Unknown destination '{to}'. Valid ids: {string.Join(", ", RouteEndpointRegistry.Endpoints.Select(e => e.Id))}");

    var routes = await _routing.GetRoutesAsync(fromEp, toEp, ct);
    return Ok(routes);
  }
}
