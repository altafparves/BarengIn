using System.Text.Json;
using BarengIn.Domain;
using BarengIn.Infrastructure.Persistence;
using Npgsql;

namespace BarengIn.Desktop;

public static class AppDb
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=barengin;Username=postgres;Password=PASSWORDHERE";

    private static bool? cachedAvailable;

    public static string? ConfigFilePath { get; set; }

    public static string ConnectionString
    {
        get
        {
            string? env = Environment.GetEnvironmentVariable("BARENGIN_DB");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env;
            }

            string? fromConfig = TryLoadConnectionStringFromAppSettings();
            if (!string.IsNullOrWhiteSpace(fromConfig))
            {
                return fromConfig;
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
                var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
                {
                    Timeout = 1,
                };
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

    public static User? AuthenticateOrFallback(string nim, string password, string selectedRole)
    {
        if (IsAvailable)
        {
            return Users.Authenticate(nim, password);
        }

        return UserRepository.BuildUserForRole(
            nim,
            nim,
            nim + "@mail.ugm.ac.id",
            "offline-hash",
            nim,
            selectedRole);
    }

    public static bool RegisterOrFallback(
        string fullName,
        string email,
        string phone,
        string password,
        string ktmNumber,
        string facultyName,
        string role)
    {
        if (IsAvailable)
        {
            return Users.Register(fullName, email, phone, password, ktmNumber, facultyName, role);
        }

        return true;
    }

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
}
