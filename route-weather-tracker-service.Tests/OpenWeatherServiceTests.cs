using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using route_weather_tracker_service.Services;

namespace route_weather_tracker_service.Tests;

public class OpenWeatherServiceTests
{
  private static IConfiguration BuildConfig() =>
      new ConfigurationBuilder()
          .AddInMemoryCollection(new Dictionary<string, string?>
          {
            ["OpenWeatherApiKey"] = "test-key"
          })
          .Build();

  private const string CurrentWeatherJson = """
        {
            "main": { "temp": 32.5, "temp_min": 28.0, "temp_max": 35.0 },
            "weather": [ { "description": "light snow", "icon": "13d" } ],
            "wind": { "speed": 8.5 }
        }
        """;

  private const string ForecastJson = """
        {
            "list": [
                {
                    "dt": 1740139200,
                    "main": { "temp": 31.0, "temp_min": 27.0, "temp_max": 34.0 },
                    "weather": [ { "description": "snow", "icon": "13d" } ],
                    "wind": { "speed": 7.0 },
                    "rain": { "3h": 2.5 }
                },
                {
                    "dt": 1740150000,
                    "main": { "temp": 30.0, "temp_min": 26.0, "temp_max": 33.0 },
                    "weather": [ { "description": "snow", "icon": "13n" } ],
                    "wind": { "speed": 6.5 }
                }
            ]
        }
        """;

  private static HttpClient BuildMultiResponseClient()
  {
    var handler = new Mock<HttpMessageHandler>();
    handler.Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.Contains("weather")
                                               && !r.RequestUri.AbsolutePath.Contains("forecast")),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(CurrentWeatherJson, Encoding.UTF8, "application/json")
        });

    handler.Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.Contains("forecast")),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(ForecastJson, Encoding.UTF8, "application/json")
        });

    return new HttpClient(handler.Object);
  }

  [Fact]
  public async Task GetForecastAsync_ReturnsCurrentConditions()
  {
    var service = new OpenWeatherService(BuildMultiResponseClient(), BuildConfig(), NullLogger<OpenWeatherService>.Instance);

    var forecast = await service.GetForecastAsync("snoqualmie", 47.4245, -121.4116);

    Assert.NotNull(forecast);
    Assert.Equal(32.5, forecast.CurrentTempFahrenheit);
    Assert.Equal("light snow", forecast.CurrentDescription);
    Assert.Equal("13d", forecast.CurrentIconCode);
  }

  [Fact]
  public async Task GetForecastAsync_ReturnsDailyForecasts()
  {
    var service = new OpenWeatherService(BuildMultiResponseClient(), BuildConfig(), NullLogger<OpenWeatherService>.Instance);

    var forecast = await service.GetForecastAsync("snoqualmie", 47.4245, -121.4116);

    Assert.NotNull(forecast);
    Assert.NotEmpty(forecast.DailyForecasts);
    var day = forecast.DailyForecasts[0];
    Assert.True(day.HighFahrenheit >= day.LowFahrenheit);
    Assert.False(string.IsNullOrEmpty(day.Description));
  }

  [Fact]
  public async Task GetForecastAsync_ReturnsNull_OnHttpFailure()
  {
    var handler = new Mock<HttpMessageHandler>();
    handler.Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized });

    var http = new HttpClient(handler.Object);
    var service = new OpenWeatherService(http, BuildConfig(), NullLogger<OpenWeatherService>.Instance);

    var forecast = await service.GetForecastAsync("snoqualmie", 47.4245, -121.4116);

    Assert.Null(forecast);
  }
}
