using BakeTrack.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BakeTrack.Tests.TestHelpers;

/// <summary>
/// Provides an isolated in-memory SQLite <see cref="IDbContextFactory{BakeryDbContext}"/>
/// with relational foreign-key and unique-index enforcement enabled per test instance.
/// </summary>
public sealed class SqliteBakeryDbFactory : IDbContextFactory<BakeryDbContext>, IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<BakeryDbContext> _options;

    private SqliteBakeryDbFactory(SqliteConnection connection, DbContextOptions<BakeryDbContext> options)
    {
        _connection = connection;
        _options = options;
    }

    public static async Task<SqliteBakeryDbFactory> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            await pragma.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<BakeryDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var db = new BakeryDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
        }

        return new SqliteBakeryDbFactory(connection, options);
    }

    public BakeryDbContext CreateDbContext()
        => new(_options);

    public Task<BakeryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new BakeryDbContext(_options));

    public ValueTask DisposeAsync()
        => _connection.DisposeAsync();
}
