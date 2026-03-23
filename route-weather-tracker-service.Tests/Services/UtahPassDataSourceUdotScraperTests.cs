using System.Net;
using System.Net.Http;
using System.Text;
using Moq;
using Moq.Protected;
using route_weather_tracker_service.Services;
using route_weather_tracker_service.Models;
using Xunit;

namespace route_weather_tracker_service.Tests.Services;

public class UtahPassDataSourceUdotScraperTests
{
    [Fact]
    public async Task GetCamerasAsync_FallsBackToUdotCameras_WhenArcGisEmpty()
    {
        // Arrange: ArcGIS response is empty features array
        var arcJson = "{ \"features\": [] }";

        // First handler: ArcGIS call
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(arcJson, Encoding.UTF8, "application/json")
            })
            // Second call: udotcameras HTML
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(string.Empty, Encoding.UTF8, "text/plain")
            })
            // Third call: udotcameras HTML
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("<html>Parleys <img src=\"/images/parleys_cam.jpg\"></html>", Encoding.UTF8, "text/html")
            });

        var client = new HttpClient(handler.Object);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<UtahPassDataSource>>();
        var svc = new UtahPassDataSource(factoryMock.Object, loggerMock.Object);

        // Act
        var cams = await svc.GetCamerasAsync("parleys");

        // Assert
        Assert.NotNull(cams);
        Assert.Single(cams);
        Assert.Contains("parleys_cam.jpg", cams[0].ImageUrl, StringComparison.OrdinalIgnoreCase);
    }
}
