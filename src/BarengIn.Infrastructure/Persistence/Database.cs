using Npgsql;

namespace BarengIn.Infrastructure.Persistence;

public sealed class Database
{
    private readonly string connectionString;

    public Database(string connectionString) => this.connectionString = NormalizeConnectionString(connectionString);

    public NpgsqlConnection Open()
    {
        var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        return conn;
    }

    public static string NormalizeConnectionString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        string trimmed = raw.Trim().Trim('"').Trim('\'');
        if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        try
        {
            var uri = new Uri(trimmed);
            string userInfo = uri.UserInfo;
            string username = string.Empty;
            string password = string.Empty;
            int colonIdx = userInfo.IndexOf(':');
            if (colonIdx >= 0)
            {
                username = Uri.UnescapeDataString(userInfo[..colonIdx]);
                password = Uri.UnescapeDataString(userInfo[(colonIdx + 1)..]);
            }
            else
            {
                username = Uri.UnescapeDataString(userInfo);
            }

            string database = uri.AbsolutePath.Trim('/');
            int port = uri.Port > 0 ? uri.Port : 5432;

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = port,
                Database = string.IsNullOrWhiteSpace(database) ? "neondb" : database,
                Username = username,
                Password = password,
            };

            string query = uri.Query.TrimStart('?');
            if (!string.IsNullOrWhiteSpace(query))
            {
                foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] kv = pair.Split('=', 2);
                    string k = Uri.UnescapeDataString(kv[0]).Trim().ToLowerInvariant();
                    string v = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]).Trim().ToLowerInvariant() : string.Empty;

                    if (k == "sslmode")
                    {
                        builder.SslMode = v switch
                        {
                            "disable" => SslMode.Disable,
                            "allow" => SslMode.Allow,
                            "prefer" => SslMode.Prefer,
                            "verify-ca" => SslMode.VerifyCA,
                            "verify-full" => SslMode.VerifyFull,
                            _ => SslMode.Require,
                        };
                    }
                    else if (k == "channel_binding")
                    {
                        builder.ChannelBinding = v switch
                        {
                            "require" => ChannelBinding.Require,
                            "disable" => ChannelBinding.Disable,
                            _ => ChannelBinding.Prefer,
                        };
                    }
                }
            }

            return builder.ConnectionString;
        }
        catch
        {
            return trimmed;
        }
    }
}