using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using route_weather_tracker_service.Services;
using route_weather_tracker_service.Tests.Helpers;

namespace route_weather_tracker_service.Tests.Services;

public class CaliforniaPassDataSourceTests
{
    // Builds a Caltrans cctvStatus-shaped feed with a single camera.
    private static string MakeSingleCameraJson(
        double lat = 39.330, double lon = -120.330,   // ~1 km from Donner summit (39.3224, -120.3287)
        bool inService = true,
        string index = "42",
        string locationName = "Hwy 80 at Castle Peak",
        string nearbyPlace = "Truckee",
        string route = "I-80",
        string imageUrl = "https://cwwp2.dot.ca.gov/data/d3/cctv/image/hwy80atcastlepeak/hwy80atcastlepeak.jpg") => $$"""
        {
          "data": [
            {
              "cctv": {
                "index": "{{index}}",
                "location": {
                  "district": "3",
                  "locationName": "{{locationName}}",
                  "nearbyPlace": "{{nearbyPlace}}",
                  "longitude": "{{lon}}",
                  "latitude": "{{lat}}",
                  "route": "{{route}}"
                },
                "inService": "{{(inService ? "true" : "false")}}",
                "imageData": {
                  "static": {
                    "currentImageURL": "{{imageUrl}}"
                  }
                }
              }
            }
          ]
        }
        """;

    private static CaliforniaPassDataSource BuildSut(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var client = MockHttpFactory.CreateWithJson(json, statusCode);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var loggerMock = new Mock<ILogger<CaliforniaPassDataSource>>();
        return new CaliforniaPassDataSource(factoryMock.Object, loggerMock.Object);
    }

    [Fact]
    public void SupportedPassIds_ContainsAllCaPasses()
    {
        var sut = BuildSut("{}");
        foreach (var id in new[] { "mt-shasta", "donner", "echo-summit", "cajon", "tehachapi", "monitor", "tioga", "sonora" })
            Assert.Contains(id, sut.SupportedPassIds, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetConditionAsync_ReturnsNull()
    {
        var sut = BuildSut("{}");
        Assert.Null(await sut.GetConditionAsync("donner"));
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsCamera_WhenNearPass()
    {
        var sut = BuildSut(MakeSingleCameraJson());
        var cams = await sut.GetCamerasAsync("donner");

        Assert.Single(cams);
        Assert.Equal("ca-d03-42", cams[0].CameraId);
        Assert.Contains("Castle Peak", cams[0].Description);
        Assert.Contains("Truckee", cams[0].Description);
        Assert.Contains("cwwp2.dot.ca.gov", cams[0].ImageUrl);
    }

    [Fact]
    public async Task GetCamerasAsync_ExcludesOutOfServiceCameras()
    {
        var sut = BuildSut(MakeSingleCameraJson(inService: false));
        Assert.Empty(await sut.GetCamerasAsync("donner"));
    }

    [Fact]
    public async Task GetCamerasAsync_ExcludesCamerasBeyondRadius()
    {
        // ~50 km north of Donner summit — beyond the 25 km radius
        var sut = BuildSut(MakeSingleCameraJson(lat: 39.770, lon: -120.330));
        Assert.Empty(await sut.GetCamerasAsync("donner"));
    }

    [Fact]
    public async Task GetCamerasAsync_ExcludesCameraWithNoImageUrl()
    {
        var json = """
            { "data": [ { "cctv": {
                "index": "1",
                "location": { "locationName": "X", "latitude": "39.330", "longitude": "-120.330" },
                "inService": "true",
                "imageData": { "static": { } }
            } } ] }
            """;
        var sut = BuildSut(json);
        Assert.Empty(await sut.GetCamerasAsync("donner"));
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsAtMostTwoCameras_OrderedByDistance()
    {
        // Three cameras at increasing distance from Donner summit; nearest two win, in order.
        var json = """
            { "data": [
              { "cctv": { "index": "far",  "location": { "locationName": "Far",  "latitude": "39.430", "longitude": "-120.330" },
                "inService": "true", "imageData": { "static": { "currentImageURL": "https://cwwp2.dot.ca.gov/far.jpg" } } } },
              { "cctv": { "index": "near", "location": { "locationName": "Near", "latitude": "39.325", "longitude": "-120.329" },
                "inService": "true", "imageData": { "static": { "currentImageURL": "https://cwwp2.dot.ca.gov/near.jpg" } } } },
              { "cctv": { "index": "mid",  "location": { "locationName": "Mid",  "latitude": "39.360", "longitude": "-120.330" },
                "inService": "true", "imageData": { "static": { "currentImageURL": "https://cwwp2.dot.ca.gov/mid.jpg" } } } }
            ] }
            """;
        var sut = BuildSut(json);
        var cams = await sut.GetCamerasAsync("donner");

        Assert.Equal(2, cams.Count);
        Assert.Equal("ca-d03-near", cams[0].CameraId);
        Assert.Equal("ca-d03-mid", cams[1].CameraId);
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsEmpty_ForUnknownPass()
    {
        var sut = BuildSut(MakeSingleCameraJson());
        Assert.Empty(await sut.GetCamerasAsync("snoqualmie"));
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsEmpty_OnHttpError()
    {
        var sut = BuildSut("{}", HttpStatusCode.InternalServerError);
        Assert.Empty(await sut.GetCamerasAsync("donner"));
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsEmpty_OnNetworkError()
    {
        var client = MockHttpFactory.CreateThrowingNetworkError();
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var loggerMock = new Mock<ILogger<CaliforniaPassDataSource>>();

        var sut = new CaliforniaPassDataSource(factoryMock.Object, loggerMock.Object);
        Assert.Empty(await sut.GetCamerasAsync("donner"));
    }

    [Fact]
    public async Task GetCamerasAsync_DeduplicatesAcrossDistrictFeeds()
    {
        // Sonora queries two district feeds (D10 + D9); the mock returns the same
        // camera for both, so the identical image URL must be de-duplicated.
        var json = MakeSingleCameraJson(
            lat: 38.330, lon: -119.630,   // ~1 km from Sonora summit (38.3294, -119.6264)
            index: "7",
            locationName: "US-395 at Sonora Jct",
            route: "US-395",
            imageUrl: "https://cwwp2.dot.ca.gov/data/d10/cctv/image/sonorajct/sonorajct.jpg");
        var sut = BuildSut(json);

        var cams = await sut.GetCamerasAsync("sonora");
        Assert.Single(cams);
    }
}
