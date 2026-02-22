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

  private static PassWeatherForecast SampleForecast(string passId) => new()
  {
    PassId = passId,
    CurrentTempFahrenheit = 30,
    CurrentDescription = "clear sky",
    CurrentIconCode = "01d",
    DailyForecasts = []
  };

  [Fact]
  public async Task GetAllPassesAsync_ReturnsAllFourPasses()
  {
    var wsdot = new Mock<IWsdotService>();
    var idaho = new Mock<IIdahoTransportService>();
    var weather = new Mock<IOpenWeatherService>();

    wsdot.Setup(s => s.GetPassConditionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync((string id, CancellationToken _) => SampleCondition(id));
    wsdot.Setup(s => s.GetPassCamerasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync([]);
    idaho.Setup(s => s.GetPassCamerasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync([]);
    weather.Setup(s => s.GetForecastAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((string id, double _, double __, CancellationToken ___) => SampleForecast(id));

    var service = new PassAggregatorService(wsdot.Object, idaho.Object, weather.Object, BuildCache());

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
    var service = new PassAggregatorService(
        new Mock<IWsdotService>().Object,
        new Mock<IIdahoTransportService>().Object,
        new Mock<IOpenWeatherService>().Object,
        BuildCache());

    var result = await service.GetPassAsync("not-a-pass");

    Assert.Null(result);
  }

  [Fact]
  public async Task GetPassAsync_ReturnsCachedResult_OnSecondCall()
  {
    var wsdot = new Mock<IWsdotService>();
    var idaho = new Mock<IIdahoTransportService>();
    var weather = new Mock<IOpenWeatherService>();

    wsdot.Setup(s => s.GetPassConditionAsync("snoqualmie", It.IsAny<CancellationToken>()))
         .ReturnsAsync(SampleCondition("snoqualmie"));
    wsdot.Setup(s => s.GetPassCamerasAsync("snoqualmie", It.IsAny<CancellationToken>()))
         .ReturnsAsync([]);
    weather.Setup(s => s.GetForecastAsync("snoqualmie", It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SampleForecast("snoqualmie"));

    var service = new PassAggregatorService(wsdot.Object, idaho.Object, weather.Object, BuildCache());

    await service.GetPassAsync("snoqualmie");
    await service.GetPassAsync("snoqualmie");

    // WSDOT APIs should only be called once due to caching
    wsdot.Verify(s => s.GetPassConditionAsync("snoqualmie", It.IsAny<CancellationToken>()), Times.Once);
    wsdot.Verify(s => s.GetPassCamerasAsync("snoqualmie", It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task GetPassAsync_SnoqualmieSummary_HasCorrectInfo()
  {
    var wsdot = new Mock<IWsdotService>();
    var idaho = new Mock<IIdahoTransportService>();
    var weather = new Mock<IOpenWeatherService>();

    var expectedCondition = SampleCondition("snoqualmie");
    wsdot.Setup(s => s.GetPassConditionAsync("snoqualmie", It.IsAny<CancellationToken>()))
         .ReturnsAsync(expectedCondition);
    wsdot.Setup(s => s.GetPassCamerasAsync("snoqualmie", It.IsAny<CancellationToken>()))
         .ReturnsAsync([new CameraImage { CameraId = "c1", Description = "Summit", ImageUrl = "https://example.com/cam.jpg" }]);
    weather.Setup(s => s.GetForecastAsync("snoqualmie", It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SampleForecast("snoqualmie"));

    var service = new PassAggregatorService(wsdot.Object, idaho.Object, weather.Object, BuildCache());

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
}
