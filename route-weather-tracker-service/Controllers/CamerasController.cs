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
public class CamerasController(IHttpClientFactory httpFactory, ILogger<CamerasController> logger) : ControllerBase
{
  /// <summary>Name of the HttpClient registered for this proxy (redirects off, body capped).</summary>
  public const string HttpClientName = "camera-proxy";

  /// <summary>Largest snapshot the proxy will pass through.</summary>
  public const long MaxImageBytes = 5 * 1024 * 1024;

  /// <summary>Redirects followed per request; each target must also be an allowed host.</summary>
  public const int MaxRedirects = 3;

  // Only hosts known to serve camera snapshots over HTTP-only may be proxied.
  private static readonly HashSet<string> AllowedHosts =
      new(StringComparer.OrdinalIgnoreCase)
      {
            "ss.nmroads.com",
      };

  // Raster formats only. SVG in particular is excluded: it can carry script and would be served
  // from the API's own origin.
  private static readonly HashSet<string> AllowedContentTypes =
      new(StringComparer.OrdinalIgnoreCase)
      {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp",
      };

  private readonly IHttpClientFactory _httpFactory = httpFactory;
  private readonly ILogger<CamerasController> _logger = logger;

  /// <summary>GET /api/cameras/image?url={absolute-snapshot-url}</summary>
  [HttpGet("image")]
  public async Task<IActionResult> Image([FromQuery] string url, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(url) ||
        !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
        !IsAllowed(uri))
      return BadRequest("Host not allowed.");

    try
    {
      var client = _httpFactory.CreateClient(HttpClientName);

      // The client does not follow redirects, so each hop is checked here before it is requested.
      var current = uri;
      for (var redirects = 0; ; redirects++)
      {
        using var resp = await client.GetAsync(current, HttpCompletionOption.ResponseHeadersRead, ct);

        if (IsRedirect(resp.StatusCode))
        {
          var location = resp.Headers.Location;
          if (location is null || redirects >= MaxRedirects)
            return StatusCode(StatusCodes.Status502BadGateway);

          var next = location.IsAbsoluteUri ? location : new Uri(current, location);
          if (!IsAllowed(next))
          {
            _logger.LogWarning("Camera proxy refused a redirect from {Url} to another host", current);
            return StatusCode(StatusCodes.Status502BadGateway);
          }

          current = next;
          continue;
        }

        if (!resp.IsSuccessStatusCode)
          return StatusCode((int)resp.StatusCode);

        var contentType = resp.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        if (!AllowedContentTypes.Contains(contentType))
          return BadRequest("Not a supported image.");

        if (resp.Content.Headers.ContentLength > MaxImageBytes)
          return StatusCode(StatusCodes.Status502BadGateway);

        // The client's MaxResponseContentBufferSize rejects a body over the cap that has no
        // declared length.
        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
        // Match the upstream snapshot cadence; the frontend also cache-busts.
        Response.Headers.CacheControl = "public, max-age=60";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(bytes, contentType);
      }
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

  internal static bool IsAllowed(Uri uri) =>
      (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
      AllowedHosts.Contains(uri.Host);

  private static bool IsRedirect(System.Net.HttpStatusCode status) =>
      (int)status is >= 300 and < 400;
}
