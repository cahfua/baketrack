using BakeTrack.Data;
using Microsoft.Extensions.Configuration;

namespace BakeTrack.Tests.TestHelpers;

/// <summary>
/// Resolves and validates the local PostgreSQL connection string used by the test suite.
/// </summary>
public static class PostgresTestConfiguration
{
    public const string TestConnectionEnvVar = "BAKETRACK_TEST_CONNECTION_STRING";

    public static string ResolveConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(BakeryDbContext).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var rawConnection = configuration[TestConnectionEnvVar];
        if (string.IsNullOrWhiteSpace(rawConnection))
        {
            rawConnection = configuration.GetConnectionString("TestConnection");
        }

        if (string.IsNullOrWhiteSpace(rawConnection))
        {
            rawConnection = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(rawConnection))
        {
            throw new InvalidOperationException(
                "No local PostgreSQL connection string is configured for tests. " +
                $"Set environment variable '{TestConnectionEnvVar}', or configure User Secrets with:\n" +
                "  dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=localhost;Port=5432;Database=baketrack;Username=baketrack;Password=baketrack_dev\" --project src/BakeTrack\n" +
                "And ensure local PostgreSQL is running (e.g. 'docker compose up -d').");
        }

        return DatabaseConfiguration.EnsureLocalConnectionString(rawConnection, "test suite");
    }
}
