using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BarengIn.Infrastructure.Services;

public sealed record PlaceSuggestion(
    string Title,
    string Subtitle,
    double Lat,
    double Lon)
{
    public string DisplayLabel => string.IsNullOrWhiteSpace(Subtitle) ? Title : $"{Title} — {Subtitle}";
    public override string ToString() => Title;
}

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

    private static readonly PlaceSuggestion[] CuratedPlaces =
    [
        new("Pogung Lor", "Sinduadi, Mlati, Sleman Regency, Special Region of Yogyakarta", -7.7582, 110.3738),
        new("Pogung Kidul", "Sinduadi, Mlati, Sleman Regency, Special Region of Yogyakarta", -7.7625, 110.3718),
        new("Jalan Pogung Baru", "Pogung Kidul, Sinduadi, Mlati, Sleman Regency", -7.7598, 110.3741),
        new("Pogung Baru Blok F", "Jalan Pogung Baru, Sinduadi, Mlati, Sleman Regency", -7.7591, 110.3748),
        new("Pogung Dalangan", "Sinduadi, Mlati, Sleman Regency, Special Region of Yogyakarta", -7.7604, 110.3711),
        new("Pogung Rejo", "Sinduadi, Mlati, Sleman Regency, Special Region of Yogyakarta", -7.7568, 110.3724),
        new("Jakal Km 4.5 (Panti Rapih / MM UGM)", "Caturtunggal, Depok, Sleman Regency", -7.7575, 110.3820),
        new("Jakal Km 5 (Jalan Kaliurang Km 5)", "Caturtunggal, Depok, Sleman Regency", -7.7523, 110.3812),
        new("Jakal Km 5.5 (Pandega Marta)", "Caturtunggal, Depok, Sleman Regency", -7.7485, 110.3834),
        new("Jakal Km 6 (Kentungan)", "Condongcatur, Depok, Sleman Regency", -7.7435, 110.3865),
        new("Jakal Km 7 (Banteng / Kayen)", "Condongcatur, Depok, Sleman Regency", -7.7348, 110.3914),
        new("Jakal Km 8 (Prujakan)", "Sinduharjo, Ngaglik, Sleman Regency", -7.7260, 110.3968),
        new("Jakal Km 9 (Gondangan)", "Sardonoharjo, Ngaglik, Sleman Regency", -7.7172, 110.4015),
        new("Jakal Km 10 (Pasar Gentan)", "Sardonoharjo, Ngaglik, Sleman Regency", -7.7085, 110.4063),
        new("Sendowo", "Sinduadi, Mlati, Sleman Regency, Special Region of Yogyakarta", -7.7720, 110.3705),
        new("Blimbingsari", "Caturtunggal, Depok, Sleman Regency, Special Region of Yogyakarta", -7.7745, 110.3748),
        new("Klebengan", "Caturtunggal, Depok, Sleman Regency, Special Region of Yogyakarta", -7.7675, 110.3868),
        new("Karanggayam", "Caturtunggal, Depok, Sleman Regency, Special Region of Yogyakarta", -7.7634, 110.3845),
        new("Karangmalang", "Caturtunggal, Depok, Sleman Regency, Special Region of Yogyakarta", -7.7721, 110.3862),
        new("Condongcatur (Terminal / Hartono Mall)", "Depok, Sleman Regency, Special Region of Yogyakarta", -7.7574, 110.4085),
        new("Seturan Raya", "Caturtunggal, Depok, Sleman Regency, Special Region of Yogyakarta", -7.7702, 110.4096),
        new("Babarsari", "Caturtunggal, Depok, Sleman Regency, Special Region of Yogyakarta", -7.7796, 110.4148),
        new("Gejayan (Jalan Affandi)", "Caturtunggal, Depok, Sleman Regency", -7.7734, 110.3897),
        new("Demangan Baru", "Caturtunggal, Depok, Sleman Regency, Special Region of Yogyakarta", -7.7815, 110.3895),
        new("Monjali (Monumen Jogja Kembali)", "Sariharjo, Ngaglik, Sleman Regency", -7.7495, 110.3694),
        new("Terminal Jombor", "Sinduadi, Mlati, Sleman Regency, Special Region of Yogyakarta", -7.7471, 110.3612),
        new("Jalan Magelang Km 5 (TVRI / Kutu Tegal)", "Sinduadi, Mlati, Sleman Regency", -7.7556, 110.3618),
        new("Stasiun Tugu Yogyakarta", "Sosromenduran, Gedong Tengen, Yogyakarta City", -7.7892, 110.3635),
        new("Stasiun Lempuyangan", "Bausasran, Danurejan, Yogyakarta City", -7.7902, 110.3754),
        new("Malioboro", "Suryatmajan, Danurejan, Yogyakarta City", -7.7926, 110.3658),
        new("Tugu Jogja (Jetis)", "Cokrodiningratan, Jetis, Yogyakarta City", -7.7829, 110.3671),
        new("Kotabaru", "Gondokusuman, Yogyakarta City, Special Region of Yogyakarta", -7.7865, 110.3742),
        new("Godean (Jalan Godean Km 5)", "Banyuraden, Gamping, Sleman Regency", -7.7688, 110.2936),
        new("Kasihan (Ringroad Selatan / UMY)", "Tamantirto, Kasihan, Bantul Regency", -7.8256, 110.3392),
        new("Kotagede", "Prenggan, Kotagede, Yogyakarta City", -7.8282, 110.3996),
        new("Terminal Giwangan", "Giwangan, Umbulharjo, Yogyakarta City", -7.8344, 110.3923),
        new("Maguwoharjo (Stadion / Lotte Mart)", "Depok, Sleman Regency, Special Region of Yogyakarta", -7.7676, 110.4331),
        new("Balairung UGM (Gedung Pusat)", "Bulaksumur, Caturtunggal, Depok, Sleman Regency", -7.7686, 110.3785),
        new("Grha Sabha Pramana (GSP) UGM", "Bulaksumur, Caturtunggal, Depok, Sleman Regency", -7.7704, 110.3777),
        new("RSUP Dr. Sardjito", "Jalan Kesehatan No.1, Sendowo, Mlati, Sleman Regency", -7.7686, 110.3732),
        new("Masjid Kampus UGM (Maskam)", "Bulaksumur, Caturtunggal, Depok, Sleman Regency", -7.7732, 110.3797),
        new("Gelanggang Inovasi dan Kreativitas (GIK) UGM", "Jalan Persatuan, Bulaksumur, Sleman Regency", -7.7745, 110.3746),
        new("Perpustakaan Pusat UGM", "Bulaksumur, Caturtunggal, Depok, Sleman Regency", -7.7697, 110.3782),
        new("Wisdom Park UGM", "Karangmalang, Caturtunggal, Depok, Sleman Regency", -7.7712, 110.3822),
    ];

    private static readonly Dictionary<string, (double Lat, double Lon)> KnownAreaCoordinates =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Jakal"] = (-7.7539, 110.3839),
            ["Jakal Km 4"] = (-7.7610, 110.3802),
            ["Jakal Km 4.5"] = (-7.7575, 110.3820),
            ["Jakal Km 5"] = (-7.7523, 110.3812),
            ["Jakal Km 6"] = (-7.7435, 110.3865),
            ["Jakal Km 7"] = (-7.7348, 110.3914),
            ["Jakal Km 8"] = (-7.7260, 110.3968),
            ["Jakal Km 9"] = (-7.7172, 110.4015),
            ["Jakal Km 10"] = (-7.7085, 110.4063),
            ["Jalan Kaliurang"] = (-7.7523, 110.3812),
            ["Pogung"] = (-7.7618, 110.3732),
            ["Pogung Baru"] = (-7.7598, 110.3741),
            ["Pogung Kidul"] = (-7.7625, 110.3718),
            ["Pogung Lor"] = (-7.7582, 110.3738),
            ["Sendowo"] = (-7.7720, 110.3705),
            ["Klebengan"] = (-7.7675, 110.3868),
            ["Karanggayam"] = (-7.7634, 110.3845),
            ["Condongcatur"] = (-7.7574, 110.4085),
            ["Concat"] = (-7.7574, 110.4085),
            ["Seturan"] = (-7.7702, 110.4096),
            ["Babarsari"] = (-7.7796, 110.4148),
            ["Gejayan"] = (-7.7734, 110.3897),
            ["Demangan"] = (-7.7815, 110.3895),
            ["Monjali"] = (-7.7495, 110.3694),
            ["Mlati"] = (-7.7418, 110.3526),
            ["Sleman"] = (-7.7162, 110.3556),
            ["Godean"] = (-7.7688, 110.2936),
            ["Kasihan"] = (-7.8256, 110.3392),
            ["Bantul"] = (-7.8879, 110.3289),
            ["Kotagede"] = (-7.8282, 110.3996),
            ["Malioboro"] = (-7.7926, 110.3658),
            ["Stasiun Tugu"] = (-7.7892, 110.3635),
            ["Tugu"] = (-7.7829, 110.3671),
            ["Lempuyangan"] = (-7.7902, 110.3754),
            ["Jombor"] = (-7.7471, 110.3612),
            ["Giwangan"] = (-7.8344, 110.3923),
            ["Maguwoharjo"] = (-7.7676, 110.4331),
            ["Depok"] = (-7.7689, 110.4018),
        };

    private static readonly Dictionary<string, (double Lat, double Lon)> FacultyCoordinates =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Legacy / English short keys
            ["Engineering (FT)"] = (-7.7654, 110.3723),
            ["MIPA"] = (-7.7672, 110.3762),
            ["Economics and Business (FEB)"] = (-7.7706, 110.3792),
            ["Medicine"] = (-7.7689, 110.3736),
            ["Social and Political Sciences"] = (-7.7701, 110.3774),

            // 18 Official UGM Faculties + Schools
            ["Fakultas Biologi"] = (-7.7655, 110.3768),
            ["Fakultas Ekonomika dan Bisnis (FEB)"] = (-7.7708, 110.3794),
            ["Fakultas Farmasi"] = (-7.7679, 110.3752),
            ["Fakultas Filsafat"] = (-7.7715, 110.3798),
            ["Fakultas Geografi"] = (-7.7664, 110.3766),
            ["Fakultas Hukum"] = (-7.7709, 110.3806),
            ["Fakultas Ilmu Budaya (FIB)"] = (-7.7718, 110.3782),
            ["Fakultas Ilmu Sosial dan Ilmu Politik (FISIPOL)"] = (-7.7702, 110.3788),
            ["Fakultas Kedokteran, Kesehatan Masyarakat, dan Keperawatan (FK-KMK)"] = (-7.7691, 110.3738),
            ["Fakultas Kedokteran Gigi (FKG)"] = (-7.7696, 110.3749),
            ["Fakultas Kedokteran Hewan (FKH)"] = (-7.7681, 110.3834),
            ["Fakultas Kehutanan"] = (-7.7674, 110.3805),
            ["Fakultas Matematika dan Ilmu Pengetahuan Alam (FMIPA)"] = (-7.7670, 110.3764),
            ["Fakultas Pertanian"] = (-7.7695, 110.3818),
            ["Fakultas Peternakan"] = (-7.7672, 110.3852),
            ["Fakultas Psikologi"] = (-7.7724, 110.3789),
            ["Fakultas Teknik"] = (-7.7652, 110.3721),
            ["Fakultas Teknik (FT)"] = (-7.7653, 110.3725),
            ["Fakultas Teknologi Pertanian (FTP)"] = (-7.7688, 110.3802),
            ["Sekolah Vokasi (SV)"] = (-7.7748, 110.3742),
            ["Sekolah Pascasarjana"] = (-7.7644, 110.3755),

            // Major UGM Campus Landmarks & Destinations
            ["Balairung UGM (Gedung Pusat)"] = (-7.7686, 110.3785),
            ["Grha Sabha Pramana (GSP)"] = (-7.7704, 110.3777),
            ["RSUP Dr. Sardjito"] = (-7.7686, 110.3732),
            ["Rumah Sakit Akademik (RSA) UGM"] = (-7.7426, 110.3508),
            ["Perpustakaan Pusat UGM"] = (-7.7697, 110.3782),
            ["Masjid Kampus UGM (Maskam)"] = (-7.7732, 110.3797),
            ["Gelanggang Inovasi dan Kreativitas (GIK)"] = (-7.7745, 110.3746),
            ["Wisdom Park UGM"] = (-7.7712, 110.3822),
            ["Stadion Pancasila UGM"] = (-7.7705, 110.3831),
            ["Asrama Mahasiswa (Darmaputera / Ratnaningsih)"] = (-7.7629, 110.3795),
        };

    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;
    private readonly bool _allowCoordinateFallback;

    public RouteApiService()
        : this(CreateDefaultClient(new HttpClient()), ownsClient: true, allowCoordinateFallback: true)
    {
    }

    public RouteApiService(HttpMessageHandler handler)
        : this(
            CreateDefaultClient(new HttpClient(
                handler ?? throw new ArgumentNullException(nameof(handler)),
                disposeHandler: false)),
            ownsClient: true,
            allowCoordinateFallback: false)
    {
    }

    public RouteApiService(HttpClient httpClient)
        : this(httpClient ?? throw new ArgumentNullException(nameof(httpClient)), ownsClient: false, allowCoordinateFallback: true)
    {
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", DefaultUserAgent);
        }
    }

    private RouteApiService(HttpClient httpClient, bool ownsClient, bool allowCoordinateFallback)
    {
        _httpClient = httpClient;
        _ownsClient = ownsClient;
        _allowCoordinateFallback = allowCoordinateFallback;
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
            string expandedPickup = JakalWordRegex.IsMatch(trimmedPickup)
                ? JakalWordRegex.Replace(trimmedPickup, "Jalan Kaliurang")
                : trimmedPickup;

            bool usedLocalLookup = false;
            (double Lat, double Lon)? pickupCoords = await TryGeocodeAsync(trimmedPickup, ct).ConfigureAwait(false);
            if (pickupCoords is null && !string.Equals(expandedPickup, trimmedPickup, StringComparison.Ordinal))
            {
                pickupCoords = await TryGeocodeAsync(expandedPickup, ct).ConfigureAwait(false);
            }

            if (pickupCoords is null && _allowCoordinateFallback)
            {
                pickupCoords = await TryGeocodeBroadAsync(trimmedPickup, ct).ConfigureAwait(false);
            }

            if (pickupCoords is null)
            {
                if (_allowCoordinateFallback && TryGetKnownAreaCoordinates(trimmedPickup, out var localCoords))
                {
                    pickupCoords = localCoords;
                    usedLocalLookup = true;
                }
                else
                {
                    return CreateFallbackEstimate();
                }
            }

            (double destLat, double destLon) = GetFacultyCoordinates(facultyName);
            string osrmUrl = string.Create(
                CultureInfo.InvariantCulture,
                $"https://router.project-osrm.org/route/v1/driving/{pickupCoords.Value.Lon},{pickupCoords.Value.Lat};{destLon},{destLat}?overview=false");

            try
            {
                using HttpResponseMessage osrmResp = await _httpClient.GetAsync(osrmUrl, ct).ConfigureAwait(false);
                if (osrmResp.IsSuccessStatusCode)
                {
                    string osrmJson = await osrmResp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    (double DistanceKm, int DurationMinutes)? route = ParseOsrmResponse(osrmJson);
                    if (route is not null)
                    {
                        double co2SavedKg = Math.Round(route.Value.DistanceKm * Co2KgPerKm, 2, MidpointRounding.AwayFromZero);
                        return new RouteEstimate(
                            route.Value.DistanceKm,
                            route.Value.DurationMinutes,
                            co2SavedKg,
                            IsLiveApi: true,
                            Source: usedLocalLookup ? "OSRM + UGM Map" : "OSRM + Nominatim");
                    }
                }
            }
            catch
            {
                if (!_allowCoordinateFallback)
                {
                    return CreateFallbackEstimate();
                }
            }

            if (_allowCoordinateFallback)
            {
                return EstimateFromCoordinates(pickupCoords.Value, (destLat, destLon), "Coordinate estimate");
            }

            return CreateFallbackEstimate();
        }
        catch
        {
            return CreateFallbackEstimate();
        }
    }

    public static bool TryGetKnownAreaCoordinates(string? pickupArea, out (double Lat, double Lon) coords)
    {
        coords = default;
        if (string.IsNullOrWhiteSpace(pickupArea))
        {
            return false;
        }

        string trimmed = pickupArea.Trim();
        if (KnownAreaCoordinates.TryGetValue(trimmed, out coords))
        {
            return true;
        }

        foreach (PlaceSuggestion p in CuratedPlaces)
        {
            if (p.Title.Equals(trimmed, StringComparison.OrdinalIgnoreCase)
                || p.DisplayLabel.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                coords = (p.Lat, p.Lon);
                return true;
            }
        }

        foreach (PlaceSuggestion p in CuratedPlaces)
        {
            if (p.Title.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains(p.Title, StringComparison.OrdinalIgnoreCase))
            {
                coords = (p.Lat, p.Lon);
                return true;
            }
        }

        foreach (var kv in KnownAreaCoordinates)
        {
            if (trimmed.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
            {
                coords = kv.Value;
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<PlaceSuggestion> SearchLocalPlaceSuggestions(string? query, int maxResults = 6)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return CuratedPlaces.Take(maxResults).ToList();
        }

        string trimmed = query.Trim();
        var startsWith = new List<PlaceSuggestion>();
        var containsTitle = new List<PlaceSuggestion>();
        var containsSub = new List<PlaceSuggestion>();

        foreach (PlaceSuggestion p in CuratedPlaces)
        {
            if (p.Title.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                startsWith.Add(p);
            }
            else if (p.Title.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                containsTitle.Add(p);
            }
            else if (p.Subtitle.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                containsSub.Add(p);
            }
        }

        return startsWith
            .Concat(containsTitle)
            .Concat(containsSub)
            .Take(maxResults)
            .ToList();
    }

    public async Task<IReadOnlyList<PlaceSuggestion>> SearchPlaceSuggestionsAsync(
        string? query,
        int maxResults = 6,
        CancellationToken ct = default)
    {
        var results = SearchLocalPlaceSuggestions(query, maxResults).ToList();
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3 || results.Count >= maxResults)
        {
            return results;
        }

        try
        {
            string trimmed = query.Trim();
            string expanded = JakalWordRegex.IsMatch(trimmed)
                ? JakalWordRegex.Replace(trimmed, "Jalan Kaliurang")
                : trimmed;
            string queryText = expanded.Contains("Yogyakarta", StringComparison.OrdinalIgnoreCase)
                ? expanded
                : $"{expanded}, Yogyakarta, Indonesia";
            string url = $"https://nominatim.openstreetmap.org/search?format=json&limit=5&q={Uri.EscapeDataString(queryText)}";

            using HttpResponseMessage resp = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return results;
            }

            string json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            foreach (PlaceSuggestion live in ParseNominatimSuggestions(json))
            {
                if (results.Count >= maxResults)
                {
                    break;
                }

                if (!results.Any(r => r.Title.Equals(live.Title, StringComparison.OrdinalIgnoreCase)))
                {
                    results.Add(live);
                }
            }
        }
        catch
        {
            // Keep instant local suggestions if live Nominatim autocomplete fails or is cancelled.
        }

        return results;
    }

    public static IReadOnlyList<PlaceSuggestion> ParseNominatimSuggestions(string? json)
    {
        var list = new List<PlaceSuggestion>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return list;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            foreach (JsonElement item in doc.RootElement.EnumerateArray())
            {
                if (!TryReadInvariantDouble(item, "lat", out double lat)
                    || !TryReadInvariantDouble(item, "lon", out double lon)
                    || lat is < -90 or > 90
                    || lon is < -180 or > 180)
                {
                    continue;
                }

                string displayName = item.TryGetProperty("display_name", out JsonElement dn) && dn.ValueKind == JsonValueKind.String
                    ? dn.GetString() ?? string.Empty
                    : string.Empty;
                string name = item.TryGetProperty("name", out JsonElement nm) && nm.ValueKind == JsonValueKind.String
                    ? nm.GetString() ?? string.Empty
                    : string.Empty;

                if (string.IsNullOrWhiteSpace(displayName) && string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                string[] parts = displayName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                string title = !string.IsNullOrWhiteSpace(name) ? name : (parts.Length > 0 ? parts[0] : displayName);
                string subtitle = parts.Length > 1 ? string.Join(", ", parts.Skip(1).Take(4)) : string.Empty;

                KnownAreaCoordinates[title] = (lat, lon);
                list.Add(new PlaceSuggestion(title, subtitle, lat, lon));
            }
        }
        catch
        {
            // Return whatever was parsed so far.
        }

        return list;
    }

    public static RouteEstimate EstimateFromCoordinates(
        (double Lat, double Lon) origin,
        (double Lat, double Lon) destination,
        string source = "Coordinate estimate")
    {
        const double EarthRadiusKm = 6371.0;
        double dLat = (destination.Lat - origin.Lat) * Math.PI / 180.0;
        double dLon = (destination.Lon - origin.Lon) * Math.PI / 180.0;
        double lat1 = origin.Lat * Math.PI / 180.0;
        double lat2 = destination.Lat * Math.PI / 180.0;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1) * Math.Cos(lat2) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double straightKm = EarthRadiusKm * c;
        double roadKm = Math.Max(0.5, Math.Round(straightKm * 1.3, 2, MidpointRounding.AwayFromZero));
        int durationMinutes = Math.Max(2, (int)Math.Round(roadKm / 25.0 * 60.0, MidpointRounding.AwayFromZero));
        double co2SavedKg = Math.Round(roadKm * Co2KgPerKm, 2, MidpointRounding.AwayFromZero);

        return new RouteEstimate(roadKm, durationMinutes, co2SavedKg, IsLiveApi: true, Source: source);
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

    private async Task<(double Lat, double Lon)?> TryGeocodeBroadAsync(string pickupQuery, CancellationToken ct)
    {
        try
        {
            string queryText = pickupQuery.Contains("Indonesia", StringComparison.OrdinalIgnoreCase)
                ? pickupQuery
                : $"{pickupQuery}, Indonesia";
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
        catch
        {
            return null;
        }
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
