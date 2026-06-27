using Microsoft.AspNetCore.Mvc;

namespace route_weather_tracker_service.Controllers;

/// <summary>
/// Proxies roadside camera snapshots that are only served over plain HTTP (e.g.
/// New Mexico's ss.nmroads.com) so the HTTPS frontend can display them without
/// tripping mixed-content blocking. A strict host allowlist keeps this from being
/// an open proxy / SSRF vector.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CamerasController : ControllerBase
{
    // Only hosts known to serve camera snapshots over HTTP-only may be proxied.
    private static readonly HashSet<string> AllowedHosts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ss.nmroads.com",
        };

    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<CamerasController> _logger;

    public CamerasController(IHttpClientFactory httpFactory, ILogger<CamerasController> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    /// <summary>GET /api/cameras/image?url={absolute-snapshot-url}</summary>
    [HttpGet("image")]
    public async Task<IActionResult> Image([FromQuery] string url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return BadRequest("Invalid url.");

        if (!AllowedHosts.Contains(uri.Host))
            return BadRequest("Host not allowed.");

        try
        {
            var client = _httpFactory.CreateClient("camera-proxy");
            client.Timeout = TimeSpan.FromSeconds(15);
            using var resp = await client.GetAsync(uri, ct);
            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode);

            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Not an image.");

            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
            // Match the upstream snapshot cadence; the frontend also cache-busts.
            Response.Headers.CacheControl = "public, max-age=60";
            return File(bytes, contentType);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Camera proxy failed for {Url}", uri);
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }
}
