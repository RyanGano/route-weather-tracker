using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using route_weather_tracker_service.Models;
using route_weather_tracker_service.Services;

namespace route_weather_tracker_service.Tests;

public class PassAggregatorServiceTests
{
  private static IMemoryCache BuildCache() =>
      new MemoryCache(Options.Create(new MemoryCacheOptions()));

  private static PassCondition SampleCondition(string passId) => new()
  {
    PassId = passId,
    RoadCondition = "Bare and dry",
    WeatherCondition = "Clear",
    TemperatureFahrenheit = 30,
    LastUpdated = DateTime.UtcNow
  };

  private static PassWeatherForecast SampleForecast() => new()
  {
    CurrentTempFahrenheit = 30,
    CurrentDescription = "clear sky",
    CurrentIconCode = "01d",
    DailyForecasts = []
  };

  /// <summary>Creates an IPassDataSource mock that handles the given passIds.</summary>
  private static Mock<IPassDataSource> BuildSource(
      IReadOnlySet<string> passIds,
      Func<string, PassCondition?>? conditionFactory = null,
      List<CameraImage>? cameras = null)
  {
    var mock = new Mock<IPassDataSource>();
    mock.Setup(s => s.SupportedPassIds).Returns(passIds);
    mock.Setup(s => s.GetConditionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((string id, CancellationToken _) => conditionFactory?.Invoke(id));
    mock.Setup(s => s.GetCamerasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(cameras ?? []);
    return mock;
  }

  private static readonly IReadOnlySet<string> WaPassIds =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "snoqualmie", "stevens-pass" };

  private static readonly IReadOnlySet<string> IdahoPassIds =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fourth-of-july", "lookout" };

  [Fact]
  public async Task GetAllPassesAsync_ReturnsAllFourPasses()
  {
    var waSource = BuildSource(WaPassIds, conditionFactory: id => SampleCondition(id));
    var idSource = BuildSource(IdahoPassIds);
    var weather = new Mock<IOpenWeatherService>();
    weather.Setup(s => s.GetForecastAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((string _, double __, double ___, CancellationToken ____) => SampleForecast());

    var service = new PassAggregatorService([waSource.Object, idSource.Object], weather.Object, BuildCache());

    var passes = await service.GetAllPassesAsync();

    Assert.Equal(4, passes.Count);
    Assert.Contains(passes, p => p.Info.Id == "snoqualmie");
    Assert.Contains(passes, p => p.Info.Id == "stevens-pass");
    Assert.Contains(passes, p => p.Info.Id == "fourth-of-july");
    Assert.Contains(passes, p => p.Info.Id == "lookout");
  }

  [Fact]
  public async Task GetPassAsync_ReturnsNull_ForUnknownId()
  {
    var waSource = BuildSource(WaPassIds);
    var idSource = BuildSource(IdahoPassIds);
    var service = new PassAggregatorService(
        [waSource.Object, idSource.Object],
        new Mock<IOpenWeatherService>().Object,
        BuildCache());

    var result = await service.GetPassAsync("not-a-pass");

    Assert.Null(result);
  }

  [Fact]
  public async Task GetPassAsync_ReturnsCachedResult_OnSecondCall()
  {
    var waSource = new Mock<IPassDataSource>();
    waSource.Setup(s => s.SupportedPassIds).Returns(WaPassIds);
    waSource.Setup(s => s.GetConditionAsync("snoqualmie", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleCondition("snoqualmie"));
    waSource.Setup(s => s.GetCamerasAsync("snoqualmie", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

    var weather = new Mock<IOpenWeatherService>();
    weather.Setup(s => s.GetForecastAsync("snoqualmie", It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SampleForecast());

    var service = new PassAggregatorService([waSource.Object], weather.Object, BuildCache());

    await service.GetPassAsync("snoqualmie");
    await service.GetPassAsync("snoqualmie");

    // Data source APIs should only be called once due to caching
    waSource.Verify(s => s.GetConditionAsync("snoqualmie", It.IsAny<CancellationToken>()), Times.Once);
    waSource.Verify(s => s.GetCamerasAsync("snoqualmie", It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task GetPassAsync_SnoqualmieSummary_HasCorrectInfo()
  {
    var expectedCondition = SampleCondition("snoqualmie");
    var cam = new CameraImage { CameraId = "c1", Description = "Summit", ImageUrl = "https://example.com/cam.jpg" };
    var waSource = BuildSource(WaPassIds, conditionFactory: _ => expectedCondition, cameras: [cam]);

    var weather = new Mock<IOpenWeatherService>();
    weather.Setup(s => s.GetForecastAsync("snoqualmie", It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SampleForecast());

    var service = new PassAggregatorService([waSource.Object], weather.Object, BuildCache());

    var summary = await service.GetPassAsync("snoqualmie");

    Assert.NotNull(summary);
    Assert.Equal("snoqualmie", summary.Info.Id);
    Assert.Equal("Snoqualmie Pass", summary.Info.Name);
    Assert.Equal(3022, summary.Info.ElevationFeet);
    Assert.NotNull(summary.Condition);
    Assert.Equal("Bare and dry", summary.Condition.RoadCondition);
    Assert.Single(summary.Cameras);
    Assert.NotNull(summary.Weather);
  }

  // ---------------------------------------------------------------------------
  // InferRoadCondition is private, so we test it via GetPassAsync with an Idaho
  // pass that has no official condition source (GetConditionAsync returns null),
  // which causes PassAggregatorService to call DeriveCondition / InferRoadCondition.
  // ---------------------------------------------------------------------------
  public static TheoryData<string, double, string> InferRoadConditionCases() => new()
  {
    // blizzard/heavy-snow branch
    { "blizzard",    25, "Icy / Snow packed" }, // < 28 → Icy / Snow packed
    { "heavy snow",  30, "Heavy snow"         }, // >= 28 → Heavy snow
    // snow/sleet branch
    { "light snow",  28, "Snow packed / Icy"  }, // < 30 → Snow packed / Icy
    { "snow showers",32, "Snow covered"       }, // >= 30 → Snow covered
    { "sleet",       20, "Snow packed / Icy"  },
    // freezing/ice branch
    { "freezing rain", 28, "Icy / Freezing"   },
    { "ice storm",     20, "Icy / Freezing"   },
    // rain/drizzle/shower branch
    { "light rain",    35, "Bare and wet"     }, // >= 32 → Bare and wet
    { "drizzle",       28, "Freezing rain"    }, // < 32 → Freezing rain
    { "shower rain",   25, "Freezing rain"    },
    // mist/fog branch
    { "fog",           40, "Bare and wet"     },
    { "mist",          38, "Bare and wet"     },
    // default
    { "clear sky",     55, "Bare and dry"     },
  };

  [Theory]
  [MemberData(nameof(InferRoadConditionCases))]
  public async Task InferRoadCondition_ProducesExpectedRoadCondition(
      string weatherDescription, double tempF, string expectedRoadCondition)
  {
    // Use fourth-of-july: Idaho source returns null condition, triggering DeriveCondition
    var idSource = new Mock<IPassDataSource>();
    idSource.Setup(s => s.SupportedPassIds)
            .Returns(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fourth-of-july", "lookout" });
    idSource.Setup(s => s.GetConditionAsync("fourth-of-july", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PassCondition?)null);
    idSource.Setup(s => s.GetCamerasAsync("fourth-of-july", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

    var weather = new Mock<IOpenWeatherService>();
    weather.Setup(s => s.GetForecastAsync("fourth-of-july", It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new PassWeatherForecast
           {
             CurrentTempFahrenheit = tempF,
             CurrentDescription = weatherDescription,
             CurrentIconCode = "01d",
             DailyForecasts = []
           });

    var service = new PassAggregatorService([idSource.Object], weather.Object, BuildCache());

    var summary = await service.GetPassAsync("fourth-of-july");

    Assert.NotNull(summary);
    Assert.NotNull(summary.Condition);
    Assert.Equal(expectedRoadCondition, summary.Condition.RoadCondition);
  }

  [Fact]
  public async Task GetPassAsync_CacheIsIsolatedPerPass()
  {
    // Arrange: WA source for snoqualmie, Idaho source for fourth-of-july
    var waSource = new Mock<IPassDataSource>();
    waSource.Setup(s => s.SupportedPassIds).Returns(WaPassIds);
    waSource.Setup(s => s.GetConditionAsync("snoqualmie", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleCondition("snoqualmie"));
    waSource.Setup(s => s.GetCamerasAsync("snoqualmie", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

    var idSource = new Mock<IPassDataSource>();
    idSource.Setup(s => s.SupportedPassIds).Returns(IdahoPassIds);
    idSource.Setup(s => s.GetConditionAsync("fourth-of-july", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PassCondition?)null);
    idSource.Setup(s => s.GetCamerasAsync("fourth-of-july", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

    var weather = new Mock<IOpenWeatherService>();
    weather.Setup(s => s.GetForecastAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((string _, double __, double ___, CancellationToken ____) => SampleForecast());

    var service = new PassAggregatorService([waSource.Object, idSource.Object], weather.Object, BuildCache());

    // Act: fetch snoqualmie twice and fourth-of-july once
    await service.GetPassAsync("snoqualmie");
    await service.GetPassAsync("fourth-of-july");
    await service.GetPassAsync("snoqualmie");

    // Assert: each source is called exactly once (second snoqualmie served from cache,
    // fourth-of-july is NOT served from snoqualmie cache)
    waSource.Verify(s => s.GetConditionAsync("snoqualmie", It.IsAny<CancellationToken>()), Times.Once);
    waSource.Verify(s => s.GetCamerasAsync("snoqualmie", It.IsAny<CancellationToken>()), Times.Once);
    idSource.Verify(s => s.GetCamerasAsync("fourth-of-july", It.IsAny<CancellationToken>()), Times.Once);
    waSource.Verify(s => s.GetCamerasAsync("fourth-of-july", It.IsAny<CancellationToken>()), Times.Never);
  }
}
