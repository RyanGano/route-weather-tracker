using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using route_weather_tracker_service.Controllers;

namespace route_weather_tracker_service.Tests.Controllers;

public class CamerasControllerTests
{
  private const string Snapshot = "http://ss.nmroads.com/snapshots/test.jpg";

  [Fact]
  public async Task ServesAnAllowedJpeg()
  {
    var (controller, requested) = Create(_ => Image("image/jpeg"));

    var result = await controller.Image(Snapshot, CancellationToken.None);

    var file = Assert.IsType<FileContentResult>(result);
    Assert.Equal("image/jpeg", file.ContentType);
    Assert.Single(requested);
  }

  [Fact]
  public async Task RefusesAHostNotOnTheAllowList()
  {
    var (controller, requested) = Create(_ => Image("image/jpeg"));

    var result = await controller.Image("http://example.com/a.jpg", CancellationToken.None);

    Assert.IsType<BadRequestObjectResult>(result);
    Assert.Empty(requested);
  }

  [Fact]
  public async Task DoesNotFollowARedirectToAnotherHost()
  {
    var (controller, requested) = Create(_ => Redirect("http://169.254.169.254/metadata"));

    var result = await controller.Image(Snapshot, CancellationToken.None);

    Assert.Equal(StatusCodes.Status502BadGateway, Assert.IsType<StatusCodeResult>(result).StatusCode);
    Assert.Equal([new Uri(Snapshot)], requested);
  }

  [Fact]
  public async Task FollowsARedirectThatStaysOnAnAllowedHost()
  {
    const string secure = "https://ss.nmroads.com/snapshots/test.jpg";
    var (controller, requested) = Create(uri =>
      uri.Scheme == Uri.UriSchemeHttp ? Redirect(secure) : Image("image/jpeg"));

    var result = await controller.Image(Snapshot, CancellationToken.None);

    Assert.IsType<FileContentResult>(result);
    Assert.Equal([new Uri(Snapshot), new Uri(secure)], requested);
  }

  [Fact]
  public async Task StopsAfterTooManyRedirects()
  {
    var (controller, requested) = Create(_ => Redirect(Snapshot));

    var result = await controller.Image(Snapshot, CancellationToken.None);

    Assert.Equal(StatusCodes.Status502BadGateway, Assert.IsType<StatusCodeResult>(result).StatusCode);
    Assert.Equal(CamerasController.MaxRedirects + 1, requested.Count);
  }

  [Theory]
  [InlineData("image/svg+xml")]
  [InlineData("text/html")]
  public async Task RefusesContentThatIsNotARasterImage(string contentType)
  {
    var (controller, _) = Create(_ => Image(contentType));

    var result = await controller.Image(Snapshot, CancellationToken.None);

    Assert.IsType<BadRequestObjectResult>(result);
  }

  private static HttpResponseMessage Image(string contentType)
  {
    var content = new ByteArrayContent([1, 2, 3]);
    content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
    return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
  }

  private static HttpResponseMessage Redirect(string location)
  {
    var response = new HttpResponseMessage(HttpStatusCode.Found);
    response.Headers.Location = new Uri(location);
    return response;
  }

  private static (CamerasController Controller, List<Uri> Requested) Create(Func<Uri, HttpResponseMessage> respond)
  {
    var requested = new List<Uri>();
    var client = new HttpClient(new StubHandler(uri =>
    {
      requested.Add(uri);
      return respond(uri);
    }));

    var factory = new Mock<IHttpClientFactory>();
    factory.Setup(f => f.CreateClient(CamerasController.HttpClientName)).Returns(client);

    var controller = new CamerasController(factory.Object, NullLogger<CamerasController>.Instance)
    {
      ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };
    return (controller, requested);
  }

  private sealed class StubHandler(Func<Uri, HttpResponseMessage> respond) : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
      Task.FromResult(respond(request.RequestUri!));
  }
}
