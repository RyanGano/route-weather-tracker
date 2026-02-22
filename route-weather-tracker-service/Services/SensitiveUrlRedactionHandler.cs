using System.Diagnostics;
using System.Text.RegularExpressions;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Redacts sensitive query-string parameters (API keys) from outgoing HTTP request URLs
/// recorded by OpenTelemetry, so they do not appear in Application Insights logs.
/// The actual request URI is unchanged — only the telemetry span attributes are sanitized.
/// </summary>
public class SensitiveUrlRedactionHandler : DelegatingHandler
{
  // Matches the value portion of ?appid=... or &appid=... (OpenWeatherMap)
  // and ?AccessCode=... or &AccessCode=... (WSDOT)
  private static readonly Regex[] Patterns =
  [
    new(@"(?<=[?&]appid=)[^&]+",       RegexOptions.Compiled),
    new(@"(?<=[?&]AccessCode=)[^&]+",  RegexOptions.Compiled | RegexOptions.IgnoreCase),
  ];

  protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request, CancellationToken cancellationToken)
  {
    var response = await base.SendAsync(request, cancellationToken);

    // Redact the URL in the current OpenTelemetry activity (span)
    var activity = Activity.Current;
    if (activity is not null)
    {
      foreach (var tag in new[] { "url.full", "http.url" })
      {
        if (activity.GetTagItem(tag) is string url)
        {
          var redacted = url;
          foreach (var pattern in Patterns)
            redacted = pattern.Replace(redacted, "[REDACTED]");
          activity.SetTag(tag, redacted);
        }
      }
    }

    return response;
  }
}
