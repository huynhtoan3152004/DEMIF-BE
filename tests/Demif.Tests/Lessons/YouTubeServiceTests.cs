using System.Net;
using System.Text;
using Demif.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Demif.Tests.Lessons;

public class YouTubeServiceTests
{
    [Fact]
    public async Task GetVideoInfoAsync_WhenDataApiFails_FallsBackToPublicPage()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            var uri = request.RequestUri?.AbsoluteUri ?? string.Empty;

            if (uri.Contains("googleapis.com/youtube/v3/videos", StringComparison.OrdinalIgnoreCase))
                return new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("{\"error\":{\"message\":\"quota exceeded\"}}", Encoding.UTF8, "application/json")
                };

            if (uri.Contains("googleapis.com/youtube/v3/captions", StringComparison.OrdinalIgnoreCase))
                return new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("{\"error\":{\"message\":\"quota exceeded\"}}", Encoding.UTF8, "application/json")
                };

            if (uri.Contains("youtube.com/watch", StringComparison.OrdinalIgnoreCase))
            {
                var html = """
                    <html>
                      <head>
                        <meta property="og:title" content="Fallback Title" />
                        <meta property="og:description" content="Fallback Description" />
                        <meta property="og:image" content="https://img.youtube.com/vi/hJ7PzBD9a2g/hqdefault.jpg" />
                      </head>
                      <body>
                                                <script>var playerResponse = {"lengthSeconds":"157"};</script>
                      </body>
                    </html>
                    """;

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(html, Encoding.UTF8, "text/html")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var service = CreateService(handler);

        var info = await service.GetVideoInfoAsync("https://www.youtube.com/watch?v=hJ7PzBD9a2g");

        Assert.NotNull(info);
        Assert.Equal("hJ7PzBD9a2g", info!.VideoId);
        Assert.Equal("Fallback Title", info.Title);
        Assert.Equal("Fallback Description", info.Description);
        Assert.Equal("https://img.youtube.com/vi/hJ7PzBD9a2g/hqdefault.jpg", info.ThumbnailUrl);
        Assert.Equal(157, info.DurationSeconds);
        Assert.False(info.HasCaptions);
        Assert.Empty(info.AvailableCaptionLanguages);
    }

    private static YouTubeService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["YouTube:ApiKey"] = "test-key"
            })
            .Build();

        return new YouTubeService(httpClient, configuration, NullLogger<YouTubeService>.Instance);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}