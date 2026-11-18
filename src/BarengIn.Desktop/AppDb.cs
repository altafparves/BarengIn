using BarengIn.Domain;
using BarengIn.Infrastructure.Persistence;

namespace BarengIn.Desktop;

public static class AppDb
{
    // Reads BARENGIN_DB if set; otherwise uses default local PostgreSQL connection string.
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=barengin;Username=postgres;Password=PASSWORDHERE";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("BARENGIN_DB") ?? DefaultConnectionString;

    public static bool IsDatabaseConfigured =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BARENGIN_DB"))
        && !ConnectionString.Contains("PASSWORDHERE", StringComparison.Ordinal);

    public static UserRepository Users => new UserRepository(new Database(ConnectionString));

    public static RideRepository Rides => new RideRepository(new Database(ConnectionString));

    public static User? AuthenticateOrFallback(string nim, string password, string selectedRole)
    {
        if (IsDatabaseConfigured)
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
        if (IsDatabaseConfigured)
        {
            return Users.Register(fullName, email, phone, password, ktmNumber, facultyName, role);
        }

        return true;
    }
}
