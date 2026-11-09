using Npgsql;

namespace BarengIn.Infrastructure.Persistence;

public sealed class Database
{
    private readonly string connectionString;

    public Database(string connectionString) => this.connectionString = connectionString;

    public NpgsqlConnection Open()
    {
        var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        return conn;
    }
}