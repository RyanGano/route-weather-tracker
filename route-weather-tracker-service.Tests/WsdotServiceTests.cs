using Microsoft.Extensions.Configuration;
using route_weather_tracker_service.Services;
using route_weather_tracker_service.Tests.Helpers;

namespace route_weather_tracker_service.Tests;

public class WsdotServiceTests
{
  private static IConfiguration BuildConfig() =>
      new ConfigurationBuilder()
          .AddInMemoryCollection(new Dictionary<string, string?>
          {
            ["WsdotApiKey"] = "test-key"
          })
          .Build();

  private const string PassConditionJson = """
        {
            "PassConditionID": 1,
            "RoadCondition": "Bare and dry",
            "WeatherCondition": "Overcast",
            "TemperatureInFahrenheit": 27,
            "TravelAdvisoryActive": false,
            "DateUpdated": "2026-02-21T06:09:00"
        }
        """;

  private const string CamerasJson = """
        [
            {
                "CameraID": 1001,
                "Title": "I-90 @ MP 52: Snoqualmie Summit",
                "ImageURL": "https://images.wsdot.wa.gov/nw/090vc01373.jpg"
            },
            {
                "CameraID": 1002,
                "Title": "Stevens Pass camera",
                "ImageURL": "https://images.wsdot.wa.gov/nc/002vc00000.jpg"
            }
        ]
        """;

  [Fact]
  public async Task GetPassConditionAsync_ReturnsCondition_ForSnoqualmie()
  {
    var http = MockHttpFactory.CreateWithJson(PassConditionJson);
    var service = new WsdotService(http, BuildConfig());

    var condition = await service.GetPassConditionAsync("snoqualmie");

    Assert.NotNull(condition);
    Assert.Equal("snoqualmie", condition.PassId);
    Assert.Equal("Bare and dry", condition.RoadCondition);
    Assert.Equal("Overcast", condition.WeatherCondition);
    Assert.Equal(27, condition.TemperatureFahrenheit);
  }

  [Fact]
  public async Task GetPassConditionAsync_ReturnsNull_ForUnknownPass()
  {
    var http = MockHttpFactory.CreateWithJson(PassConditionJson);
    var service = new WsdotService(http, BuildConfig());

    var condition = await service.GetPassConditionAsync("unknown-pass");

    Assert.Null(condition);
  }

  [Fact]
  public async Task GetPassConditionAsync_ReturnsNull_OnHttpFailure()
  {
    var http = MockHttpFactory.CreateFailing();
    var service = new WsdotService(http, BuildConfig());

    var condition = await service.GetPassConditionAsync("snoqualmie");

    Assert.Null(condition);
  }

  [Fact]
  public async Task GetPassCamerasAsync_ReturnsCameras_FilteredByLocation()
  {
    var http = MockHttpFactory.CreateWithJson(CamerasJson);
    var service = new WsdotService(http, BuildConfig());

    var cameras = await service.GetPassCamerasAsync("snoqualmie");

    Assert.Single(cameras);
    Assert.Contains("Snoqualmie", cameras[0].Description);
    Assert.StartsWith("https://", cameras[0].ImageUrl);
  }

  [Fact]
  public async Task GetPassCamerasAsync_ReturnsEmpty_ForIdahoPass()
  {
    var http = MockHttpFactory.CreateWithJson(CamerasJson);
    var service = new WsdotService(http, BuildConfig());

    // Idaho passes are not served by WSDOT
    var cameras = await service.GetPassCamerasAsync("fourth-of-july");

    Assert.Empty(cameras);
  }

  [Fact]
  public async Task GetPassCamerasAsync_ReturnsEmpty_OnHttpFailure()
  {
    var http = MockHttpFactory.CreateFailing();
    var service = new WsdotService(http, BuildConfig());

    var cameras = await service.GetPassCamerasAsync("snoqualmie");

    Assert.Empty(cameras);
  }
}
