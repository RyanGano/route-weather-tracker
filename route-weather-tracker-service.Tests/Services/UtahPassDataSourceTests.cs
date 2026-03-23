using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using route_weather_tracker_service.Services;
using Xunit;

namespace route_weather_tracker_service.Tests.Services;

public class UtahPassDataSourceTests
{
    private class FakeHandler : HttpMessageHandler
    {
        private readonly string _json;
        public FakeHandler(string json) => _json = json;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(resp);
        }
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsCameras_ForParleys()
    {
        var json = "{\"features\":[{\"attributes\":{\"OBJECTID\":1,\"CAMERA_NAME\":\"Parleys Camera\",\"IMAGE_URL\":\"https://example.com/cameras/parleys.jpg\",\"NAME\":\"Parleys\"}}]}";
        var handler = new FakeHandler(json);
        var client = new HttpClient(handler);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var loggerMock = new Mock<ILogger<UtahPassDataSource>>();

        var svc = new UtahPassDataSource(factoryMock.Object, loggerMock.Object);
        var cams = await svc.GetCamerasAsync("parleys");

        Assert.NotNull(cams);
        Assert.Single(cams);
        Assert.Contains("parleys.jpg", cams[0].ImageUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Parleys", cams[0].Description, StringComparison.OrdinalIgnoreCase);
    }
}
