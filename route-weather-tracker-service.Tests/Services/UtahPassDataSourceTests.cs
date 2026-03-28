using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using route_weather_tracker_service.Services;
using Xunit;

namespace route_weather_tracker_service.Tests.Services;

public class UtahPassDataSourceTests
{
    // GeoJSON with one feature placed ~0.5 km north of the Parleys summit (40.6897, -111.7437)
    // and on I-80, so it passes proximity + highway filters.
    private static string MakeParleysGeoJson(double lat = 40.694, double lon = -111.7437) => $$"""
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "geometry": { "type": "Point", "coordinates": [{{lon}}, {{lat}}] },
              "properties": {
                "Id": 90759,
                "Status": "Enabled",
                "Latitude": {{lat}},
                "Longitude": {{lon}},
                "Location": "I-80 EB @ Parleys Summit / MP 126",
                "Roadway": "I-80",
                "ALT_NAME_1A": "I-80 EB FWY",
                "ImageUrl": "https://www.udottraffic.utah.gov/map/Cctv/90759"
              }
            }
          ]
        }
        """;

    [Fact]
    public async Task GetCamerasAsync_ReturnsCameras_ForParleys_ViaGeoJson()
    {
        var handler = new FakeResponseHandler(MakeParleysGeoJson());
        var client  = new HttpClient(handler);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var loggerMock = new Mock<ILogger<UtahPassDataSource>>();

        var svc  = new UtahPassDataSource(factoryMock.Object, loggerMock.Object);
        var cams = await svc.GetCamerasAsync("parleys");

        Assert.NotNull(cams);
        Assert.Single(cams);
        Assert.Contains("udottraffic.utah.gov", cams[0].ImageUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Parleys", cams[0].Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCamerasAsync_ExcludesDisabledCameras()
    {
        // Same coordinates but Status = "Disabled" — should be excluded
        var json = """
            {
              "type": "FeatureCollection",
              "features": [
                {
                  "type": "Feature",
                  "geometry": { "type": "Point", "coordinates": [-111.7437, 40.694] },
                  "properties": {
                    "Id": 99999,
                    "Status": "Disabled",
                    "Latitude": 40.694,
                    "Longitude": -111.7437,
                    "Location": "Disabled Cam",
                    "Roadway": "I-80",
                    "ALT_NAME_1A": "I-80 EB FWY",
                    "ImageUrl": "https://www.udottraffic.utah.gov/map/Cctv/99999"
                  }
                }
              ]
            }
            """;
        var handler = new FakeResponseHandler(json);
        var client  = new HttpClient(handler);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var loggerMock = new Mock<ILogger<UtahPassDataSource>>();

        var svc  = new UtahPassDataSource(factoryMock.Object, loggerMock.Object);
        var cams = await svc.GetCamerasAsync("parleys");

        Assert.Empty(cams);
    }

    [Fact]
    public async Task GetCamerasAsync_ExcludesFarCameras()
    {
        // Place a camera 50 km away — should be excluded (threshold is 15 km)
        var json = MakeParleysGeoJson(lat: 41.10, lon: -111.74);
        var handler = new FakeResponseHandler(json);
        var client  = new HttpClient(handler);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var loggerMock = new Mock<ILogger<UtahPassDataSource>>();

        var svc  = new UtahPassDataSource(factoryMock.Object, loggerMock.Object);
        var cams = await svc.GetCamerasAsync("parleys");

        Assert.Empty(cams);
    }

    private class FakeResponseHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }
}
