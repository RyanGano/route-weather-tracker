using System.Net;
using System.Text;
using Moq;
using Moq.Protected;

namespace route_weather_tracker_service.Tests.Helpers;

/// <summary>
/// Builds a mock HttpMessageHandler that returns a canned JSON response.
/// </summary>
public static class MockHttpFactory
{
    public static HttpClient CreateWithJson(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        return new HttpClient(handler.Object);
    }

    public static HttpClient CreateFailing(HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode });

        return new HttpClient(handler.Object);
    }
}
