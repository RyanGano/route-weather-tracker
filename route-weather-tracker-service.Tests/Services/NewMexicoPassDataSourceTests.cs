using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using route_weather_tracker_service.Services;
using route_weather_tracker_service.Tests.Helpers;

namespace route_weather_tracker_service.Tests.Services;

public class NewMexicoPassDataSourceTests
{
    // Glorieta summit: 35.5218, -105.7700
    // Place a camera ~1 km north so it clearly passes the 50 km threshold.
    private static string MakeSingleCameraJson(
        double lat = 35.530, double lon = -105.770,
        bool enabled = true,
        string name = "I-25@Glorieta",
        string title = "I-25 @ Glorieta",
        string snapshot = "http://ss.nmroads.com/snapshots/i25_glorietapass.jpg") => $$"""
        [
          {
            "name": "{{name}}",
            "title": "{{title}}",
            "lat": {{lat}},
            "lon": {{lon}},
            "snapshotFile": "{{snapshot}}",
            "enabled": {{(enabled ? "true" : "false")}},
            "cameraType": "iDome"
          }
        ]
        """;

    private static NewMexicoPassDataSource BuildSut(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var client = MockHttpFactory.CreateWithJson(json, statusCode);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var loggerMock = new Mock<ILogger<NewMexicoPassDataSource>>();
        return new NewMexicoPassDataSource(factoryMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task SupportedPassIds_ContainsAllNmPasses()
    {
        var sut = BuildSut("[]");
        Assert.Contains("glorieta",      sut.SupportedPassIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("tijeras",       sut.SupportedPassIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("raton",         sut.SupportedPassIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("apache-summit", sut.SupportedPassIds, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("emory",         sut.SupportedPassIds, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetConditionAsync_ReturnsNull()
    {
        var sut = BuildSut("[]");
        var result = await sut.GetConditionAsync("glorieta");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsCamera_WhenNearGlorieta()
    {
        var sut = BuildSut(MakeSingleCameraJson());
        var cams = await sut.GetCamerasAsync("glorieta");

        Assert.Single(cams);
        Assert.Equal("I-25@Glorieta", cams[0].CameraId);
        Assert.Contains("Glorieta", cams[0].Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ss.nmroads.com", cams[0].ImageUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCamerasAsync_ExcludesDisabledCameras()
    {
        var sut = BuildSut(MakeSingleCameraJson(enabled: false));
        var cams = await sut.GetCamerasAsync("glorieta");
        Assert.Empty(cams);
    }

    [Fact]
    public async Task GetCamerasAsync_ExcludesCamerasBeyond50Km()
    {
        // 60 km north of Glorieta
        var sut = BuildSut(MakeSingleCameraJson(lat: 36.065, lon: -105.770));
        var cams = await sut.GetCamerasAsync("glorieta");
        Assert.Empty(cams);
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsAtMostTwoCameras()
    {
        // 7 cameras all placed ~1 km from Glorieta
        var cameras = Enumerable.Range(1, 7).Select(i => $$"""
            {
              "name": "cam{{i}}",
              "title": "Camera {{i}}",
              "lat": {{35.530 + i * 0.001}},
              "lon": -105.770,
              "snapshotFile": "http://ss.nmroads.com/snapshots/cam{{i}}.jpg",
              "enabled": true
            }
            """);
        var json = "[" + string.Join(",", cameras) + "]";

        var sut = BuildSut(json);
        var cams = await sut.GetCamerasAsync("glorieta");

        Assert.Equal(2, cams.Count);
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsCamerasOrderedByDistance()
    {
        // Two cameras near Glorieta: far one listed first in the JSON
        var json = """
            [
              { "name": "far",  "title": "Far Cam",  "lat": 35.560, "lon": -105.770,
                "snapshotFile": "http://ss.nmroads.com/snapshots/far.jpg",  "enabled": true },
              { "name": "near", "title": "Near Cam", "lat": 35.522, "lon": -105.770,
                "snapshotFile": "http://ss.nmroads.com/snapshots/near.jpg", "enabled": true }
            ]
            """;

        var sut = BuildSut(json);
        var cams = await sut.GetCamerasAsync("glorieta");

        Assert.Equal(2, cams.Count);
        Assert.Equal("near", cams[0].CameraId);
        Assert.Equal("far",  cams[1].CameraId);
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsEmpty_ForUnknownPass()
    {
        var sut = BuildSut(MakeSingleCameraJson());
        var cams = await sut.GetCamerasAsync("unknown-pass");
        Assert.Empty(cams);
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsEmpty_OnHttpError()
    {
        var sut = BuildSut("{}", HttpStatusCode.InternalServerError);
        var cams = await sut.GetCamerasAsync("glorieta");
        Assert.Empty(cams);
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsEmpty_OnNetworkError()
    {
        var client = MockHttpFactory.CreateThrowingNetworkError();
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var loggerMock = new Mock<ILogger<NewMexicoPassDataSource>>();

        var sut = new NewMexicoPassDataSource(factoryMock.Object, loggerMock.Object);
        var cams = await sut.GetCamerasAsync("raton");
        Assert.Empty(cams);
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsCamera_ForRatonPass()
    {
        // Raton pass: 36.9977, -104.5076 — place camera 2 km north
        var json = """
            [
              { "name": "I-25@RatonPass", "title": "I-25 @ Raton Pass",
                "lat": 37.015, "lon": -104.508,
                "snapshotFile": "http://ss.nmroads.com/snapshots/i25_ratonpass.jpg",
                "enabled": true }
            ]
            """;
        var sut = BuildSut(json);
        var cams = await sut.GetCamerasAsync("raton");

        Assert.Single(cams);
        Assert.Contains("ss.nmroads.com", cams[0].ImageUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsEmpty_WhenCameraHasNoSnapshotFile()
    {
        var json = """
            [
              { "name": "cam1", "title": "No Snapshot",
                "lat": 35.522, "lon": -105.770,
                "enabled": true }
            ]
            """;
        var sut = BuildSut(json);
        var cams = await sut.GetCamerasAsync("glorieta");
        Assert.Empty(cams);
    }
}
