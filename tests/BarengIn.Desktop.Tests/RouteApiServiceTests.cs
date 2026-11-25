using System.Globalization;
using System.Net;
using System.Text;
using BarengIn.Desktop.Models;
using BarengIn.Infrastructure.Services;

namespace BarengIn.Desktop.Tests;

public class RouteApiServiceTests
{
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string?> UserAgents { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            UserAgents.Add(request.Headers.UserAgent.ToString());
            return _responder(request, cancellationToken);
        }
    }

    [Fact]
    public void GetFacultyCoordinates_ReturnsDistinctCoordinatesForAllKnownFaculties_AndBulaksumurFallback()
    {
        var seen = new HashSet<(double, double)>();
        foreach (string faculty in Faculties.All)
        {
            var coords = RouteApiService.GetFacultyCoordinates(faculty);
            Assert.InRange(coords.Lat, -8.0, -7.5);
            Assert.InRange(coords.Lon, 110.0, 110.6);
            seen.Add(coords);
        }

        Assert.Equal(Faculties.All.Length, seen.Count);

        var fallback = RouteApiService.GetFacultyCoordinates("Unknown Faculty");
        Assert.Equal((-7.7702, 110.3775), fallback);
        Assert.Equal(fallback, RouteApiService.GetFacultyCoordinates(null));
        Assert.Equal(fallback, RouteApiService.GetFacultyCoordinates("   "));
    }

    [Fact]
    public void ParseNominatimResponse_ParsesValidArray_AndRejectsInvalidPayloads()
    {
        const string validJson = """[{"lat":"-7.7523","lon":"110.3812","display_name":"Jakal Km 5"}]""";
        var parsed = RouteApiService.ParseNominatimResponse(validJson);
        Assert.NotNull(parsed);
        Assert.Equal(-7.7523, parsed!.Value.Lat, 4);
        Assert.Equal(110.3812, parsed.Value.Lon, 4);

        Assert.Null(RouteApiService.ParseNominatimResponse("[]"));
        Assert.Null(RouteApiService.ParseNominatimResponse("{}"));
        Assert.Null(RouteApiService.ParseNominatimResponse("{ invalid json"));
        Assert.Null(RouteApiService.ParseNominatimResponse(""));
        Assert.Null(RouteApiService.ParseNominatimResponse(null));
        Assert.Null(RouteApiService.ParseNominatimResponse("""[{"lat":"95.0","lon":"110.38"}]"""));
    }

    [Fact]
    public void ParseOsrmResponse_ParsesValidRoute_AndRejectsInvalidPayloads()
    {
        const string validJson = """{"code":"Ok","routes":[{"distance":6250.4,"duration":900.2}]}""";
        var parsed = RouteApiService.ParseOsrmResponse(validJson);
        Assert.NotNull(parsed);
        Assert.Equal(6.25, parsed!.Value.DistanceKm, 2);
        Assert.Equal(15, parsed.Value.DurationMinutes);

        Assert.Null(RouteApiService.ParseOsrmResponse("""{"code":"NoRoute","routes":[]}"""));
        Assert.Null(RouteApiService.ParseOsrmResponse("""{"code":"Ok","routes":[]}"""));
        Assert.Null(RouteApiService.ParseOsrmResponse("{ bad json"));
        Assert.Null(RouteApiService.ParseOsrmResponse(null));
    }

    [Fact]
    public async Task GetRouteEstimateAsync_LiveSuccess_SetsUserAgentTimeoutInvariantUrlsAndCalculatesCo2()
    {
        using var handler = new StubHttpMessageHandler((req, _) =>
        {
            string url = req.RequestUri!.ToString();
            if (url.StartsWith("https://nominatim.openstreetmap.org/", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""[{"lat":"-7.7523","lon":"110.3812"}]""", Encoding.UTF8, "application/json"),
                });
            }

            if (url.StartsWith("https://router.project-osrm.org/", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"code":"Ok","routes":[{"distance":8500.0,"duration":1080.0}]}""", Encoding.UTF8, "application/json"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        using var service = new RouteApiService(handler);
        Assert.Equal(TimeSpan.FromSeconds(4), service.Timeout);

        CultureInfo prevCulture = CultureInfo.CurrentCulture;
        CultureInfo prevUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var deDe = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentCulture = deDe;
            CultureInfo.CurrentUICulture = deDe;

            RouteEstimate estimate = await service.GetRouteEstimateAsync("Jakal Km 5", "Engineering (FT)", TestContext.Current.CancellationToken);

            Assert.True(estimate.IsLiveApi);
            Assert.Equal("OSRM + Nominatim", estimate.Source);
            Assert.Equal(8.5, estimate.DistanceKm, 2);
            Assert.Equal(18, estimate.DurationMinutes);
            Assert.Equal(1.02, estimate.Co2SavedKg, 2); // 8.5 * 0.12 = 1.02
            Assert.Contains("8.5 km", estimate.SummaryText);
            Assert.Contains("1.02 kg CO2 saved", estimate.SummaryText);
        }
        finally
        {
            CultureInfo.CurrentCulture = prevCulture;
            CultureInfo.CurrentUICulture = prevUiCulture;
        }

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.UserAgents, ua => Assert.Contains("BarengIn-UGM-Carpool", ua));

        string nomUrl = handler.Requests[0].RequestUri!.ToString();
        Assert.StartsWith("https://nominatim.openstreetmap.org/search?format=json&limit=1&q=", nomUrl);
        Assert.Contains("Jakal", nomUrl);

        string osrmUrl = handler.Requests[1].RequestUri!.ToString();
        Assert.StartsWith("https://router.project-osrm.org/route/v1/driving/110.3812,-7.7523;110.3723,-7.7654?overview=false", osrmUrl);
        Assert.DoesNotContain(",", osrmUrl.Split('?')[0].Replace("110.3812,-7.7523;110.3723,-7.7654", string.Empty));
    }

    [Fact]
    public async Task GetRouteEstimateAsync_WithBlankPickup_ReturnsFallbackWithoutCallingNetwork()
    {
        using var handler = new StubHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("Network should not be called for blank pickup area."));
        using var service = new RouteApiService(handler);

        RouteEstimate estimate = await service.GetRouteEstimateAsync("   ", "MIPA", TestContext.Current.CancellationToken);

        Assert.Empty(handler.Requests);
        AssertFallback(estimate);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetRouteEstimateAsync_OnHttpErrorStatus_ReturnsFallbackEstimate(HttpStatusCode status)
    {
        using var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(status)));
        using var service = new RouteApiService(handler);

        RouteEstimate estimate = await service.GetRouteEstimateAsync("Seturan", "MIPA", TestContext.Current.CancellationToken);
        AssertFallback(estimate);
    }

    [Fact]
    public async Task GetRouteEstimateAsync_OnEmptyNominatimResult_ReturnsFallbackEstimate()
    {
        using var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json"),
            }));
        using var service = new RouteApiService(handler);

        RouteEstimate estimate = await service.GetRouteEstimateAsync("Nowhere Place", "MIPA", TestContext.Current.CancellationToken);
        Assert.Single(handler.Requests);
        AssertFallback(estimate);
    }

    [Fact]
    public async Task GetRouteEstimateAsync_OnOsrmHttpError_ReturnsFallbackEstimate()
    {
        using var handler = new StubHttpMessageHandler((req, _) =>
        {
            if (req.RequestUri!.Host.Contains("nominatim", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""[{"lat":"-7.75","lon":"110.38"}]""", Encoding.UTF8, "application/json"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });
        using var service = new RouteApiService(handler);

        RouteEstimate estimate = await service.GetRouteEstimateAsync("Seturan", "Medicine", TestContext.Current.CancellationToken);
        Assert.Equal(2, handler.Requests.Count);
        AssertFallback(estimate);
    }

    [Fact]
    public async Task GetRouteEstimateAsync_OnHttpRequestExceptionOrTimeout_ReturnsFallbackEstimate()
    {
        using var networkErrorHandler = new StubHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Network unreachable"));
        using var netService = new RouteApiService(networkErrorHandler);
        AssertFallback(await netService.GetRouteEstimateAsync("Seturan", "MIPA", TestContext.Current.CancellationToken));

        using var timeoutHandler = new StubHttpMessageHandler((_, _) =>
            throw new TaskCanceledException("Request timed out"));
        using var timeoutService = new RouteApiService(timeoutHandler);
        AssertFallback(await timeoutService.GetRouteEstimateAsync("Seturan", "MIPA", TestContext.Current.CancellationToken));
    }

    [Fact]
    public void DefaultConstructor_SetsFourSecondTimeout()
    {
        using var service = new RouteApiService();
        Assert.Equal(TimeSpan.FromSeconds(4), service.Timeout);
    }

    [Fact]
    public async Task GetRouteEstimateAsync_JakalAbbreviation_RetriesWithJalanKaliurangWhenFirstLookupEmpty()
    {
        int nominatimCalls = 0;
        using var handler = new StubHttpMessageHandler((req, _) =>
        {
            string url = req.RequestUri!.ToString();
            if (url.StartsWith("https://nominatim.openstreetmap.org/", StringComparison.Ordinal))
            {
                nominatimCalls++;
                string body = nominatimCalls == 1
                    ? "[]"
                    : """[{"lat":"-7.7523","lon":"110.3812"}]""";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                });
            }

            if (url.StartsWith("https://router.project-osrm.org/", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"code":"Ok","routes":[{"distance":6000.0,"duration":720.0}]}""", Encoding.UTF8, "application/json"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        using var service = new RouteApiService(handler);
        RouteEstimate estimate = await service.GetRouteEstimateAsync("Jakal Km 5", "Engineering (FT)", TestContext.Current.CancellationToken);

        Assert.Equal(3, handler.Requests.Count);
        Assert.Contains("Jakal", handler.Requests[0].RequestUri!.AbsoluteUri);
        Assert.Contains("Jalan%20Kaliurang", handler.Requests[1].RequestUri!.AbsoluteUri);
        Assert.True(estimate.IsLiveApi);
        Assert.Equal(6.0, estimate.DistanceKm, 2);
        Assert.Equal(12, estimate.DurationMinutes);
        Assert.Equal(0.72, estimate.Co2SavedKg, 2);
    }

    private static void AssertFallback(RouteEstimate estimate)
    {
        Assert.False(estimate.IsLiveApi);
        Assert.Equal("Offline estimate", estimate.Source);
        Assert.Equal(5.0, estimate.DistanceKm);
        Assert.Equal(12, estimate.DurationMinutes);
        Assert.Equal(0.6, estimate.Co2SavedKg);
    }
}
