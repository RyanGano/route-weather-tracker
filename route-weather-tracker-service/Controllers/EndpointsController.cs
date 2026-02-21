using Microsoft.AspNetCore.Mvc;
using route_weather_tracker_service.Data;

namespace route_weather_tracker_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EndpointsController : ControllerBase
{
  /// <summary>Returns all known route endpoints (cities) ordered west to east.</summary>
  [HttpGet]
  public IActionResult GetAll() => Ok(RouteEndpointRegistry.Endpoints);
}
