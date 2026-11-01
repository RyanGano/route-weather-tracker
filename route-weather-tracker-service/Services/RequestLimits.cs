using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Per-client-IP rate limits. Every request counts toward a general limit, and route computation,
/// which spends the OpenRouteService quota, has its own tighter one.
/// </summary>
public static class RequestLimits
{
  public const string RouteComputePolicy = "route-compute";

  /// <summary>Requests per client per minute across the whole API.</summary>
  public const int RequestsPerMinute = 300;

  /// <summary>Route computations per client per minute.</summary>
  public const int RouteComputesPerMinute = 20;

  public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
  {
    services.AddRateLimiter(options =>
    {
      options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

      options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
              RateLimitPartition.GetFixedWindowLimiter(
                  GetClientKey(httpContext),
                  _ => PerMinute(RequestsPerMinute)));

      options.AddPolicy(RouteComputePolicy, httpContext =>
              RateLimitPartition.GetFixedWindowLimiter(
                  GetClientKey(httpContext),
                  _ => PerMinute(RouteComputesPerMinute)));

      options.OnRejected = (context, _) =>
          {
          if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
          {
            context.HttpContext.Response.Headers.RetryAfter =
                    ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
          }
          return ValueTask.CompletedTask;
        };
    });

    return services;
  }

  /// <summary>
  /// The client address as seen by the front end that forwarded the request. Only the last
  /// X-Forwarded-For entry is used: App Service appends that one, while earlier entries are
  /// whatever the client sent. Falls back to the connection address when there is no header.
  /// </summary>
  public static string GetClientKey(HttpContext httpContext)
  {
    var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
    var last = forwardedFor
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .LastOrDefault();

    if (!string.IsNullOrEmpty(last))
    {
      // App Service appends "address:port"; drop the port so one client is one partition.
      return IPEndPoint.TryParse(last, out var endPoint) ? endPoint.Address.ToString() : last;
    }

    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
  }

  private static FixedWindowRateLimiterOptions PerMinute(int permitLimit) => new()
  {
    PermitLimit = permitLimit,
    Window = TimeSpan.FromMinutes(1),
    QueueLimit = 0,
    AutoReplenishment = true
  };
}
