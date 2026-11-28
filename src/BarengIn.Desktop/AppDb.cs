using System.Text.Json;
using BarengIn.Domain;
using BarengIn.Infrastructure.Persistence;
using BarengIn.Infrastructure.Services;
using Npgsql;

namespace BarengIn.Desktop;

public static class AppDb
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=barengin;Username=postgres;Password=PASSWORDHERE";

    private static readonly Dictionary<string, (string FullName, string Email, string Phone, string Password, string Faculty, string Role, string Status, string KtmFile)> OfflineRegisteredUsers =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8),
    };

    private static bool? cachedAvailable;

    public static string? ConfigFilePath { get; set; }

    public static string ConnectionString
    {
        get
        {
            string? env = Environment.GetEnvironmentVariable("BARENGIN_DB");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return Database.NormalizeConnectionString(env);
            }

            if (!string.IsNullOrWhiteSpace(ConfigFilePath))
            {
                string? explicitConfig = TryLoadConnectionStringFromAppSettings(ConfigFilePath);
                return !string.IsNullOrWhiteSpace(explicitConfig)
                    ? Database.NormalizeConnectionString(explicitConfig)
                    : DefaultConnectionString;
            }

            string? dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrWhiteSpace(dbUrl))
            {
                return Database.NormalizeConnectionString(dbUrl);
            }

            string? fromDotEnv = TryLoadConnectionStringFromDotEnv();
            if (!string.IsNullOrWhiteSpace(fromDotEnv))
            {
                return Database.NormalizeConnectionString(fromDotEnv);
            }

            string? fromConfig = TryLoadConnectionStringFromAppSettings();
            if (!string.IsNullOrWhiteSpace(fromConfig))
            {
                return Database.NormalizeConnectionString(fromConfig);
            }

            return DefaultConnectionString;
        }
    }

    public static bool IsDatabaseConfigured =>
        !string.IsNullOrWhiteSpace(ConnectionString)
        && !ConnectionString.Contains("PASSWORDHERE", StringComparison.Ordinal);

    public static bool IsAvailable
    {
        get
        {
            if (cachedAvailable.HasValue)
            {
                return cachedAvailable.Value;
            }

            if (!IsDatabaseConfigured)
            {
                cachedAvailable = false;
                return false;
            }

            try
            {
                var builder = new NpgsqlConnectionStringBuilder(ConnectionString);
                bool isLocal = string.IsNullOrWhiteSpace(builder.Host)
                            || builder.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                            || builder.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
                builder.Timeout = isLocal ? 1 : 15;
                using var conn = new NpgsqlConnection(builder.ConnectionString);
                conn.Open();
                cachedAvailable = true;
            }
            catch
            {
                cachedAvailable = false;
            }

            return cachedAvailable.Value;
        }
    }

    public static void ResetAvailabilityCache() => cachedAvailable = null;

    public static UserRepository Users => new UserRepository(new Database(ConnectionString));

    public static RideRepository Rides => new RideRepository(new Database(ConnectionString));

    public static Func<string, string, CancellationToken, Task<RouteEstimate>> RouteEstimator { get; set; } =
        EstimateRouteWithDefaultServiceAsync;

    public static Func<string, CancellationToken, Task<IReadOnlyList<PlaceSuggestion>>> PlaceSuggester { get; set; } =
        SuggestPlacesWithDefaultServiceAsync;

    private static async Task<RouteEstimate> EstimateRouteWithDefaultServiceAsync(
        string pickupArea,
        string facultyName,
        CancellationToken ct)
    {
        var service = new RouteApiService(SharedHttpClient);
        return await service.GetRouteEstimateAsync(pickupArea, facultyName, ct);
    }

    private static async Task<IReadOnlyList<PlaceSuggestion>> SuggestPlacesWithDefaultServiceAsync(
        string query,
        CancellationToken ct)
    {
        var service = new RouteApiService(SharedHttpClient);
        return await service.SearchPlaceSuggestionsAsync(query, maxResults: 6, ct);
    }

    public static User? AuthenticateOrFallback(string nim, string password, string selectedRole) =>
        AuthenticateOrFallback(nim, password, selectedRole, out _, out _, out _);

    public static User? AuthenticateOrFallback(
        string nim,
        string password,
        string selectedRole,
        out string facultyName,
        out string hobbies,
        out string bio)
    {
        facultyName = string.Empty;
        hobbies = string.Empty;
        bio = string.Empty;

        if (IsAvailable)
        {
            return Users.Authenticate(nim, password, selectedRole, out facultyName, out hobbies, out bio);
        }

        string trimmedNim = (nim ?? string.Empty).Trim();
        bool wantsAdmin = !string.IsNullOrWhiteSpace(selectedRole)
                       && selectedRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        if (wantsAdmin && !UserRepository.IsDeclaredAdminIdentifier(trimmedNim))
        {
            // Check if the user registered by email and that email is a declared admin
            bool matchedDeclaredEmail = OfflineRegisteredUsers.Values.Any(u =>
                u.Email.Equals(trimmedNim, StringComparison.OrdinalIgnoreCase)
                && UserRepository.IsDeclaredAdminIdentifier(u.Email));
            if (!matchedDeclaredEmail)
            {
                return null;
            }
        }

        // Allow login by either NIM or Email in offline registered users
        (string KeyNim, (string FullName, string Email, string Phone, string Password, string Faculty, string Role, string Status, string KtmFile) Reg)? matchedOffline = null;
        if (OfflineRegisteredUsers.TryGetValue(trimmedNim, out var byNim))
        {
            matchedOffline = (trimmedNim, byNim);
        }
        else
        {
            foreach (var kv in OfflineRegisteredUsers)
            {
                if (kv.Value.Email.Equals(trimmedNim, StringComparison.OrdinalIgnoreCase))
                {
                    matchedOffline = (kv.Key, kv.Value);
                    break;
                }
            }
        }

        if (matchedOffline.HasValue)
        {
            var reg = matchedOffline.Value.Reg;
            string effectiveNim = matchedOffline.Value.KeyNim;
            if (reg.Password != password)
            {
                return null;
            }

            if (wantsAdmin
                && !reg.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                && !UserRepository.IsDeclaredAdminIdentifier(effectiveNim)
                && !UserRepository.IsDeclaredAdminIdentifier(reg.Email))
            {
                return null;
            }

            facultyName = reg.Faculty;
            string roleToUse = !string.IsNullOrWhiteSpace(selectedRole) ? selectedRole : reg.Role;
            bool isVerified = reg.Status.Equals("Verified", StringComparison.OrdinalIgnoreCase);
            var user = UserRepository.BuildUserForRole(
                effectiveNim,
                reg.FullName,
                reg.Email,
                "offline-hash",
                reg.Phone,
                roleToUse,
                verified: isVerified);
            if (reg.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                user.SetVerificationStatus(BarengIn.Domain.Enums.VerificationStatus.Pending);
            }
            else if (reg.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                user.SetVerificationStatus(BarengIn.Domain.Enums.VerificationStatus.Rejected);
            }
            else if (reg.Status.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
            {
                user.Suspend("Suspended by Admin");
            }

            return user;
        }

        var declaredAdmin = UserRepository.TryGetDeclaredAdmin(trimmedNim);
        if (declaredAdmin.HasValue)
        {
            facultyName = "Engineering (FT)";
            return UserRepository.BuildUserForRole(
                declaredAdmin.Value.Nim,
                declaredAdmin.Value.FullName,
                declaredAdmin.Value.Email,
                "offline-hash",
                declaredAdmin.Value.Phone,
                string.IsNullOrWhiteSpace(selectedRole) ? "Admin" : selectedRole,
                verified: true);
        }

        string displayName = string.IsNullOrWhiteSpace(Models.AppSession.Name)
            ? trimmedNim
            : Models.AppSession.Name;
        string fallbackEmail = trimmedNim.Contains('@') ? trimmedNim : trimmedNim + "@mail.ugm.ac.id";
        return UserRepository.BuildUserForRole(
            trimmedNim,
            displayName,
            fallbackEmail,
            "offline-hash",
            "08123456789",
            selectedRole);
    }

    public static bool RegisterOrFallback(
        string fullName,
        string email,
        string phone,
        string password,
        string ktmNumber,
        string facultyName,
        string role,
        string ktmFile = "ktm_card.jpg")
    {
        string trimmedKtm = (ktmNumber ?? string.Empty).Trim();
        string normalizedRole = role.Trim();
        if (normalizedRole.Length > 0)
        {
            normalizedRole = char.ToUpperInvariant(normalizedRole[0]) + normalizedRole[1..].ToLowerInvariant();
        }

        OfflineRegisteredUsers[trimmedKtm] = (
            fullName.Trim(),
            email.Trim(),
            phone.Trim(),
            password,
            facultyName.Trim(),
            normalizedRole,
            "Pending",
            string.IsNullOrWhiteSpace(ktmFile) ? "ktm_card.jpg" : ktmFile.Trim());

        if (IsAvailable)
        {
            return Users.Register(fullName, email, phone, password, ktmNumber, facultyName, role);
        }

        return true;
    }

    public static IReadOnlyDictionary<string, (string FullName, string Email, string Phone, string Password, string Faculty, string Role, string Status, string KtmFile)> GetOfflineRegisteredUsers() =>
        OfflineRegisteredUsers;

    public static void UpdateOfflineUserStatus(string nim, string status)
    {
        if (string.IsNullOrWhiteSpace(nim))
        {
            return;
        }

        string trimmed = nim.Trim();
        if (OfflineRegisteredUsers.TryGetValue(trimmed, out var reg))
        {
            OfflineRegisteredUsers[trimmed] = (reg.FullName, reg.Email, reg.Phone, reg.Password, reg.Faculty, reg.Role, status, reg.KtmFile);
        }
    }

    public static void ClearOfflineUsers() => OfflineRegisteredUsers.Clear();

    public static string? TryLoadConnectionStringFromAppSettings(string? explicitPath = null)
    {
        try
        {
            string? candidate = explicitPath ?? ConfigFilePath;
            if (string.IsNullOrWhiteSpace(candidate))
            {
                string baseDirFile = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                string cwdFile = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                candidate = File.Exists(baseDirFile) ? baseDirFile : cwdFile;
            }

            if (!File.Exists(candidate))
            {
                return null;
            }

            using var stream = File.OpenRead(candidate);
            using var doc = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
            if (doc.RootElement.TryGetProperty("ConnectionStrings", out JsonElement csElem)
                && csElem.ValueKind == JsonValueKind.Object
                && csElem.TryGetProperty("Default", out JsonElement defElem)
                && defElem.ValueKind == JsonValueKind.String)
            {
                return defElem.GetString();
            }
        }
        catch
        {
            // Ignore malformed or unreadable appsettings.json and fall back cleanly.
        }

        return null;
    }

    public static string? TryLoadConnectionStringFromDotEnv()
    {
        if (!string.IsNullOrWhiteSpace(ConfigFilePath))
        {
            return null;
        }

        try
        {
            string? dir = Directory.GetCurrentDirectory();
            for (int depth = 0; depth < 6 && !string.IsNullOrWhiteSpace(dir); depth++)
            {
                foreach (string fileName in new[] { ".env.local", ".env" })
                {
                    string candidate = Path.Combine(dir, fileName);
                    if (!File.Exists(candidate))
                    {
                        continue;
                    }

                    foreach (string rawLine in File.ReadAllLines(candidate))
                    {
                        string line = rawLine.Trim();
                        if (line.Length == 0 || line.StartsWith('#'))
                        {
                            continue;
                        }

                        if (line.StartsWith("BARENGIN_DB=", StringComparison.Ordinal)
                            || line.StartsWith("DATABASE_URL=", StringComparison.Ordinal))
                        {
                            int eq = line.IndexOf('=');
                            string val = line[(eq + 1)..].Trim().Trim('"').Trim('\'');
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                return val;
                            }
                        }
                    }
                }

                dir = Directory.GetParent(dir)?.FullName;
            }
        }
        catch
        {
            // Ignore unreadable .env files cleanly.
        }

        return null;
    }
}
