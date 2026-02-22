using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using route_weather_tracker_service.Models;
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
    var service = new WsdotService(http, BuildConfig(), NullLogger<WsdotService>.Instance);

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
    var service = new WsdotService(http, BuildConfig(), NullLogger<WsdotService>.Instance);

    var condition = await service.GetPassConditionAsync("unknown-pass");

    Assert.Null(condition);
  }

  [Fact]
  public async Task GetPassConditionAsync_ReturnsNull_OnHttpFailure()
  {
    var http = MockHttpFactory.CreateFailing();
    var service = new WsdotService(http, BuildConfig(), NullLogger<WsdotService>.Instance);

    var condition = await service.GetPassConditionAsync("snoqualmie");

    Assert.Null(condition);
  }

  [Fact]
  public async Task GetPassCamerasAsync_ReturnsCameras_FilteredByLocation()
  {
    var http = MockHttpFactory.CreateWithJson(CamerasJson);
    var service = new WsdotService(http, BuildConfig(), NullLogger<WsdotService>.Instance);

    var cameras = await service.GetPassCamerasAsync("snoqualmie");

    Assert.Single(cameras);
    Assert.Contains("Snoqualmie", cameras[0].Description);
    Assert.StartsWith("https://", cameras[0].ImageUrl);
  }

  [Fact]
  public async Task GetPassCamerasAsync_ReturnsEmpty_ForIdahoPass()
  {
    var http = MockHttpFactory.CreateWithJson(CamerasJson);
    var service = new WsdotService(http, BuildConfig(), NullLogger<WsdotService>.Instance);

    // Idaho passes are not served by WSDOT
    var cameras = await service.GetPassCamerasAsync("fourth-of-july");

    Assert.Empty(cameras);
  }

  [Fact]
  public async Task GetPassCamerasAsync_ReturnsEmpty_OnHttpFailure()
  {
    var http = MockHttpFactory.CreateFailing();
    var service = new WsdotService(http, BuildConfig(), NullLogger<WsdotService>.Instance);

    var cameras = await service.GetPassCamerasAsync("snoqualmie");

    Assert.Empty(cameras);
  }

  [Fact]
  public async Task GetPassConditionAsync_ReturnsNull_OnNetworkError()
  {
    var service = new WsdotService(MockHttpFactory.CreateThrowingNetworkError(), BuildConfig(), NullLogger<WsdotService>.Instance);

    var condition = await service.GetPassConditionAsync("snoqualmie");

    Assert.Null(condition);
  }

  [Fact]
  public async Task GetPassCamerasAsync_ReturnsEmpty_OnNetworkError()
  {
    var service = new WsdotService(MockHttpFactory.CreateThrowingNetworkError(), BuildConfig(), NullLogger<WsdotService>.Instance);

    var cameras = await service.GetPassCamerasAsync("snoqualmie");

    Assert.Empty(cameras);
  }

  // ---------------------------------------------------------------------------
  // Restriction parsing tests — exercised via GetPassConditionAsync responses
  // ---------------------------------------------------------------------------

  [Fact]
  public async Task ParseRestriction_ReturnsNone_WhenAdvisoryInactive()
  {
    const string json = """
        {
            "PassConditionID": 11,
            "RoadCondition": "Bare and dry",
            "WeatherCondition": "Clear",
            "TemperatureInFahrenheit": 40,
            "TravelAdvisoryActive": false
        }
        """;

    var service = new WsdotService(MockHttpFactory.CreateWithJson(json), BuildConfig(), NullLogger<WsdotService>.Instance);
    var condition = await service.GetPassConditionAsync("snoqualmie");

    Assert.NotNull(condition);
    Assert.Equal(TravelRestriction.None, condition.EastboundRestriction);
    Assert.Equal(TravelRestriction.None, condition.WestboundRestriction);
    Assert.Equal(string.Empty, condition.EastboundRestrictionText);
  }

  public static TheoryData<string, TravelRestriction> RestrictionTextCases() => new()
  {
    { "Chains required on all vehicles",         TravelRestriction.ChainsRequired  },
    { "Traction tires required",                 TravelRestriction.TiresOrTraction },
    { "Snow tires required",                     TravelRestriction.TiresOrTraction },
    { "Road is closed to all traffic",           TravelRestriction.Closed          },
    { "Winter driving advisory — drive slowly",  TravelRestriction.None            },
  };

  [Theory]
  [MemberData(nameof(RestrictionTextCases))]
  public async Task ParseRestriction_MapsRestrictionText_ToCorrectEnum(
      string restrictionText, TravelRestriction expectedRestriction)
  {
    var json = $$"""
        {
            "PassConditionID": 11,
            "RoadCondition": "Snow covered",
            "WeatherCondition": "Snowing",
            "TemperatureInFahrenheit": 28,
            "TravelAdvisoryActive": true,
            "RestrictionOne": { "RestrictionText": "{{restrictionText}}" },
            "RestrictionTwo": { "RestrictionText": "" }
        }
        """;

    var service = new WsdotService(MockHttpFactory.CreateWithJson(json), BuildConfig(), NullLogger<WsdotService>.Instance);
    var condition = await service.GetPassConditionAsync("snoqualmie");

    Assert.NotNull(condition);
    Assert.Equal(expectedRestriction, condition.EastboundRestriction);
    Assert.Equal(restrictionText, condition.EastboundRestrictionText);
  }
}
