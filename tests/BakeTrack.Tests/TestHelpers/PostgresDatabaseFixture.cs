using System.Net.Sockets;
using BakeTrack.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BakeTrack.Tests.TestHelpers;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresDatabaseCollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "PostgresDatabase";
}

/// <summary>
/// Ensures the target local PostgreSQL database has all EF Core migrations applied once per test run
/// without dropping, clearing, or modifying existing data.
/// </summary>
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        ConnectionString = PostgresTestConfiguration.ResolveConnectionString();

        var options = new DbContextOptionsBuilder<BakeryDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        try
        {
            await using var db = new BakeryDbContext(options);
            await db.Database.MigrateAsync();
        }
        catch (Exception ex) when (ex is NpgsqlException or SocketException)
        {
            var builder = new NpgsqlConnectionStringBuilder(ConnectionString);
            throw new InvalidOperationException(
                $"Failed to connect or migrate local PostgreSQL at '{builder.Host}:{builder.Port}' " +
                $"(database '{builder.Database}'). Verify PostgreSQL is running (e.g. 'docker compose up -d') " +
                "and credentials in User Secrets or BAKETRACK_TEST_CONNECTION_STRING are valid.",
                ex);
        }
    }

    public Task DisposeAsync()
        => Task.CompletedTask;
}
