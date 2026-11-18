using BarengIn.Domain;
using BarengIn.Infrastructure.Persistence;
using Npgsql;

namespace BarengIn.Desktop;

public static class AppDb
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=barengin;Username=postgres;Password=PASSWORDHERE";

    private static bool? cachedAvailable;

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("BARENGIN_DB") ?? DefaultConnectionString;

    public static bool IsDatabaseConfigured =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BARENGIN_DB"))
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
}
