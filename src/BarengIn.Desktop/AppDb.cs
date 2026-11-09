using BarengIn.Infrastructure.Persistence;

namespace BarengIn.Desktop;

public static class AppDb
{
    // CHANGE YOUR_PASSWORD to the password you use for PostgreSQL / pgAdmin.
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=barengin;Username=postgres;Password=PASSWORDHERE";

    public static UserRepository Users => new UserRepository(new Database(ConnectionString));
}