using Npgsql;

namespace BakeTrack.Data;

/// <summary>
/// Resolves PostgreSQL connection strings safely across local development and Render production.
/// </summary>
public static class DatabaseConfiguration
{
    public static string ResolveConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            var localConnection = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(localConnection))
            {
                throw new InvalidOperationException(
                    "No local PostgreSQL connection string is configured for Development. " +
                    "Configure your personal database with:\n" +
                    "  dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=localhost;Port=5432;Database=baketrack;Username=YOUR_USER;Password=YOUR_PASSWORD\" --project src/BakeTrack\n" +
                    "Or start the optional local Docker database with: docker compose up -d");
            }

            return EnsureLocalConnectionString(localConnection, "Development environment");
        }

        // Production / non-Development: prefer Render's DATABASE_URL, then ConnectionStrings:DefaultConnection.
        var rawProdConnection = configuration["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(rawProdConnection))
        {
            rawProdConnection = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(rawProdConnection))
        {
            throw new InvalidOperationException(
                "No PostgreSQL connection string found. Set DATABASE_URL or ConnectionStrings__DefaultConnection.");
        }

        return NormalizeConnectionString(rawProdConnection, isProduction: true);
    }

    public static string EnsureLocalConnectionString(string rawValue, string contextLabel = "Development environment")
    {
        var normalized = NormalizeConnectionString(rawValue, isProduction: false);
        var builder = new NpgsqlConnectionStringBuilder(normalized);

        if (IsRenderHost(builder.Host))
        {
            throw new InvalidOperationException(
                $"Blocked attempt to connect {contextLabel} to a Render PostgreSQL host ('{builder.Host}'). " +
                "Update your local configuration to point to your local PostgreSQL instance.");
        }

        return builder.ConnectionString;
    }

    public static string NormalizeConnectionString(string rawValue, bool isProduction)
    {
        var trimmed = rawValue.Trim();
        if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return new NpgsqlConnectionStringBuilder(trimmed).ConnectionString;
        }

        var uri = new Uri(trimmed);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty,
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty
        };

        ApplyQueryParameters(builder, uri.Query);

        if (isProduction && !HasQueryParameter(uri.Query, "sslmode"))
        {
            // Render internal hosts (e.g. dpg-xxx-a) have no dots and may not require SSL,
            // whereas external Render hosts (*.render.com) require SSL.
            builder.SslMode = builder.Host?.Contains('.') == true
                ? SslMode.Require
                : SslMode.Prefer;
        }

        return builder.ConnectionString;
    }

    private static bool IsRenderHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        return host.Contains("render.com", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("dpg-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasQueryParameter(string query, string key)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv[0].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void ApplyQueryParameters(NpgsqlConnectionStringBuilder builder, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            var key = Uri.UnescapeDataString(kv[0]);
            var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;

            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase) &&
                Enum.TryParse<SslMode>(value, ignoreCase: true, out var sslMode))
            {
                builder.SslMode = sslMode;
            }
        }
    }
}
