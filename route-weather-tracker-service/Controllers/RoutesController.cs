using Microsoft.AspNetCore.Mvc;
using route_weather_tracker_service.Data;

namespace route_weather_tracker_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoutesController : ControllerBase
{
  /// <summary>Returns all known highway routes (I-90, US-2, etc.).</summary>
  [HttpGet]
  public IActionResult GetAll() => Ok(RouteRegistry.Routes);
}
