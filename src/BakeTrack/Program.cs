using BakeTrack.Components;
using BakeTrack.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Render injects a dynamic PORT environment variable for containerized web services.
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Register Blazor services.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Entity Framework Core to use PostgreSQL with strict Dev/Prod isolation.
var connectionString = DatabaseConfiguration.ResolveConnectionString(
    builder.Configuration,
    builder.Environment);

builder.Services.AddDbContextFactory<BakeryDbContext>(
    options => options.UseNpgsql(connectionString));


var app = builder.Build();

// Apply pending EF Core migrations automatically on startup (required for Render Free tier without SSH).
if (!EF.IsDesignTime)
{
    using var scope = app.Services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BakeryDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

app.UseForwardedHeaders();

// Configure error handling and security for production.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

// Render terminates TLS at its edge proxy and forwards HTTP to the container.
if (string.IsNullOrWhiteSpace(renderPort))
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

// Lightweight health check endpoint for Render and smoke verification.
app.MapGet("/health", async (IDbContextFactory<BakeryDbContext> dbFactory, CancellationToken ct) =>
{
    await using var db = await dbFactory.CreateDbContextAsync(ct);
    var canConnect = await db.Database.CanConnectAsync(ct);
    return canConnect
        ? Results.Ok(new { status = "healthy", database = "connected" })
        : Results.Problem("Database connection failed.", statusCode: StatusCodes.Status503ServiceUnavailable);
});

// Serve static assets and configure Blazor components.
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Start the application.
app.Run();
