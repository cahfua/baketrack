using BakeTrack.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BakeTrack.Tests.TestHelpers;

/// <summary>
/// Provides an isolated <see cref="IDbContextFactory{BakeryDbContext}"/> backed by a single open
/// PostgreSQL transaction that always rolls back on disposal so existing database data is never altered.
/// </summary>
public sealed class PostgresBakeryDbFactory : IDbContextFactory<BakeryDbContext>, IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    private readonly DbContextOptions<BakeryDbContext> _options;
    private bool _disposed;

    private PostgresBakeryDbFactory(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DbContextOptions<BakeryDbContext> options)
    {
        _connection = connection;
        _transaction = transaction;
        _options = options;
    }

    public static async Task<PostgresBakeryDbFactory> CreateAsync(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();
        var options = new DbContextOptionsBuilder<BakeryDbContext>()
            .UseNpgsql(connection)
            .Options;

        return new PostgresBakeryDbFactory(connection, transaction, options);
    }

    public BakeryDbContext CreateDbContext()
    {
        var context = new BakeryDbContext(_options);
        context.Database.UseTransaction(_transaction);
        return context;
    }

    public async Task<BakeryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var context = new BakeryDbContext(_options);
        await context.Database.UseTransactionAsync(_transaction, cancellationToken);
        return context;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            if (_transaction.Connection is not null)
            {
                await _transaction.RollbackAsync();
            }
        }
        finally
        {
            await _transaction.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
