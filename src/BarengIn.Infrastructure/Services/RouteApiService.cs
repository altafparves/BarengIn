using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BarengIn.Infrastructure.Services;

public sealed record RouteEstimate(
    double DistanceKm,
    int DurationMinutes,
    double Co2SavedKg,
    bool IsLiveApi,
    string Source)
{
    public string SummaryText => string.Create(
        CultureInfo.InvariantCulture,
        $"~{DistanceKm:0.0} km • ~{DurationMinutes} min • {Co2SavedKg:0.00} kg CO2 saved ({Source})");
}

public sealed class RouteApiService : IDisposable
{
    public const double Co2KgPerKm = 0.12;
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(4);
    public const string DefaultUserAgent = "BarengIn-UGM-Carpool/1.0 (https://github.com/altafparvesh/BarengIn)";

    private static readonly (double Lat, double Lon) BulaksumurFallback = (-7.7702, 110.3775);
    private static readonly Regex JakalWordRegex = new(@"\bJakal\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, (double Lat, double Lon)> FacultyCoordinates =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Engineering (FT)"] = (-7.7654, 110.3723),
            ["MIPA"] = (-7.7672, 110.3762),
            ["Economics and Business (FEB)"] = (-7.7706, 110.3792),
            ["Medicine"] = (-7.7689, 110.3736),
            ["Social and Political Sciences"] = (-7.7701, 110.3774),
        };

    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;

    public RouteApiService()
        : this(CreateDefaultClient(new HttpClient()), ownsClient: true)
    {
    }

    public RouteApiService(HttpMessageHandler handler)
        : this(
            CreateDefaultClient(new HttpClient(
                handler ?? throw new ArgumentNullException(nameof(handler)),
                disposeHandler: false)),
            ownsClient: true)
    {
    }

    public RouteApiService(HttpClient httpClient)
        : this(httpClient ?? throw new ArgumentNullException(nameof(httpClient)), ownsClient: false)
    {
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", DefaultUserAgent);
        }
    }

    private RouteApiService(HttpClient httpClient, bool ownsClient)
    {
        _httpClient = httpClient;
        _ownsClient = ownsClient;
    }

    public TimeSpan Timeout => _httpClient.Timeout;

    public async Task<RouteEstimate> GetRouteEstimateAsync(
        string pickupArea,
        string facultyName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(pickupArea))
        {
            return CreateFallbackEstimate();
        }

        try
        {
            string trimmedPickup = pickupArea.Trim();
            (double Lat, double Lon)? pickupCoords = await TryGeocodeAsync(trimmedPickup, ct).ConfigureAwait(false);
            if (pickupCoords is null && JakalWordRegex.IsMatch(trimmedPickup))
            {
                string expandedPickup = JakalWordRegex.Replace(trimmedPickup, "Jalan Kaliurang");
                pickupCoords = await TryGeocodeAsync(expandedPickup, ct).ConfigureAwait(false);
            }

            if (pickupCoords is null)
            {
                return CreateFallbackEstimate();
            }

            (double destLat, double destLon) = GetFacultyCoordinates(facultyName);
            string osrmUrl = string.Create(
                CultureInfo.InvariantCulture,
                $"https://router.project-osrm.org/route/v1/driving/{pickupCoords.Value.Lon},{pickupCoords.Value.Lat};{destLon},{destLat}?overview=false");

            using HttpResponseMessage osrmResp = await _httpClient.GetAsync(osrmUrl, ct).ConfigureAwait(false);
            if (!osrmResp.IsSuccessStatusCode)
            {
                return CreateFallbackEstimate();
            }

            string osrmJson = await osrmResp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            (double DistanceKm, int DurationMinutes)? route = ParseOsrmResponse(osrmJson);
            if (route is null)
            {
                return CreateFallbackEstimate();
            }

            double co2SavedKg = Math.Round(route.Value.DistanceKm * Co2KgPerKm, 2, MidpointRounding.AwayFromZero);
            return new RouteEstimate(
                route.Value.DistanceKm,
                route.Value.DurationMinutes,
                co2SavedKg,
                IsLiveApi: true,
                Source: "OSRM + Nominatim");
        }
        catch
        {
            return CreateFallbackEstimate();
        }
    }

    public static (double Lat, double Lon) GetFacultyCoordinates(string? facultyName)
    {
        if (string.IsNullOrWhiteSpace(facultyName))
        {
            return BulaksumurFallback;
        }

        return FacultyCoordinates.TryGetValue(facultyName.Trim(), out var coords)
            ? coords
            : BulaksumurFallback;
    }

    public static (double Lat, double Lon)? ParseNominatimResponse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            {
                return null;
            }

            JsonElement first = doc.RootElement[0];
            if (!TryReadInvariantDouble(first, "lat", out double lat)
                || !TryReadInvariantDouble(first, "lon", out double lon))
            {
                return null;
            }

            if (lat is < -90 or > 90 || lon is < -180 or > 180)
            {
                return null;
            }

            return (lat, lon);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static (double DistanceKm, int DurationMinutes)? ParseOsrmResponse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (root.TryGetProperty("code", out JsonElement codeElem)
                && codeElem.ValueKind == JsonValueKind.String
                && !string.Equals(codeElem.GetString(), "Ok", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!root.TryGetProperty("routes", out JsonElement routesElem)
                || routesElem.ValueKind != JsonValueKind.Array
                || routesElem.GetArrayLength() == 0)
            {
                return null;
            }

            JsonElement firstRoute = routesElem[0];
            if (!TryReadInvariantDouble(firstRoute, "distance", out double distanceMeters)
                || !TryReadInvariantDouble(firstRoute, "duration", out double durationSeconds)
                || distanceMeters < 0
                || durationSeconds < 0)
            {
                return null;
            }

            double distanceKm = Math.Round(distanceMeters / 1000.0, 2, MidpointRounding.AwayFromZero);
            int durationMinutes = durationSeconds > 0
                ? Math.Max(1, (int)Math.Round(durationSeconds / 60.0, MidpointRounding.AwayFromZero))
                : 0;

            return (distanceKm, durationMinutes);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static RouteEstimate CreateFallbackEstimate() =>
        new(5.0, 12, 0.6, IsLiveApi: false, Source: "Offline estimate");

    public void Dispose()
    {
        if (_ownsClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<(double Lat, double Lon)?> TryGeocodeAsync(string pickupQuery, CancellationToken ct)
    {
        string queryText = pickupQuery.Contains("Yogyakarta", StringComparison.OrdinalIgnoreCase)
            ? pickupQuery
            : $"{pickupQuery}, Yogyakarta, Indonesia";
        string nominatimUrl =
            $"https://nominatim.openstreetmap.org/search?format=json&limit=1&q={Uri.EscapeDataString(queryText)}";

        using HttpResponseMessage nomResp = await _httpClient.GetAsync(nominatimUrl, ct).ConfigureAwait(false);
        if (!nomResp.IsSuccessStatusCode)
        {
            return null;
        }

        string nomJson = await nomResp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return ParseNominatimResponse(nomJson);
    }

    private static HttpClient CreateDefaultClient(HttpClient client)
    {
        client.Timeout = DefaultTimeout;
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", DefaultUserAgent);
        return client;
    }

    private static bool TryReadInvariantDouble(JsonElement obj, string propertyName, out double value)
    {
        value = 0;
        if (obj.ValueKind != JsonValueKind.Object || !obj.TryGetProperty(propertyName, out JsonElement elem))
        {
            return false;
        }

        if (elem.ValueKind == JsonValueKind.Number && elem.TryGetDouble(out double num) && double.IsFinite(num))
        {
            value = num;
            return true;
        }

        if (elem.ValueKind == JsonValueKind.String
            && double.TryParse(elem.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            && double.IsFinite(parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }
}
