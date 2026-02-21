using route_weather_tracker_service.Models;

namespace route_weather_tracker_service.Services;

/// <summary>
/// Fetches highway camera images from the Idaho 511 public REST API for
/// Fourth of July Pass and Lookout Pass on I-90.
/// No API key required — images are publicly accessible.
/// Camera IDs confirmed from https://511.idaho.gov/map/Cctv/{id} as used by
/// the official Idaho 511 camera list pages.
/// </summary>
public class IdahoTransportService : IIdahoTransportService
{
  // Camera image endpoint: returns a JPEG snapshot directly
  private const string CameraImageBaseUrl = "https://511.idaho.gov/map/Cctv/";

  // Camera IDs confirmed by cross-referencing the Idaho 511 site's DataTable
  // render function (data-lazy="/map/Cctv/{id}") with known pass camera IDs.
  // Source: https://watchidaho.net (embeds official 511 image URLs)
  private static readonly Dictionary<string, (string Label, string[] CameraIds)> PassCameras
      = new(StringComparer.OrdinalIgnoreCase)
      {
        ["fourth-of-july"] = ("4th of July Pass", ["246.C1--2", "246.C2--2"]),
        ["lookout"]        = ("Lookout Pass",      ["242.C1--2", "242.C2--2"])
      };

  public Task<List<CameraImage>> GetPassCamerasAsync(string passId, CancellationToken ct = default)
  {
    if (!PassCameras.TryGetValue(passId, out var pass))
      return Task.FromResult(new List<CameraImage>());

    var labels = new[] { "Eastbound", "Westbound" };
    var cameras = pass.CameraIds
        .Select((id, i) => new CameraImage
        {
          CameraId = id,
          Description = $"{pass.Label} – {(i < labels.Length ? labels[i] : $"Camera {i + 1}")}",
          ImageUrl = $"{CameraImageBaseUrl}{id}",
          CapturedAt = DateTime.UtcNow
        })
        .ToList();

    return Task.FromResult(cameras);
  }
}
