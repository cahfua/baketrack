using BakeTrack.Components;
using BakeTrack.Data;
using BakeTrack.Models;
using BakeTrack.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
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

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();

// Configure Entity Framework Core to use PostgreSQL with strict Dev/Prod isolation.
var connectionString = DatabaseConfiguration.ResolveConnectionString(
    builder.Configuration,
    builder.Environment);

builder.Services.AddDbContextFactory<BakeryDbContext>(
    options => options.UseNpgsql(connectionString));

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IDbContextFactory<BakeryDbContext>>().CreateDbContext());

// Configure ASP.NET Core Identity with Admin and Staff roles.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<BakeryDbContext>()
.AddDefaultTokenProviders()
.AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/";
    options.Cookie.Name = "BakeTrack.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);

    options.Events.OnValidatePrincipal = async context =>
    {
        await SecurityStampValidator.ValidatePrincipalAsync(context);

        if (context.Principal?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.Principal);
        if (user is null || !user.IsActive)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});

builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<IngredientService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<UserManagementService>();

var app = builder.Build();

// Apply pending EF Core migrations and seed roles/initial users on startup.
if (!EF.IsDesignTime)
{
    using var scope = app.Services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BakeryDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();

    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
    await IdentitySeeder.SeedAsync(scope.ServiceProvider, app.Configuration, app.Environment, logger);
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

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Lightweight health check endpoint for Render and smoke verification.
app.MapGet("/health", async (IDbContextFactory<BakeryDbContext> dbFactory, CancellationToken ct) =>
{
    await using var db = await dbFactory.CreateDbContextAsync(ct);
    var canConnect = await db.Database.CanConnectAsync(ct);
    return canConnect
        ? Results.Ok(new { status = "healthy", database = "connected" })
        : Results.Problem("Database connection failed.", statusCode: StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

app.MapPost("/account/logout", async (
    HttpContext httpContext,
    IAntiforgery antiforgery,
    SignInManager<ApplicationUser> signInManager) =>
{
    await antiforgery.ValidateRequestAsync(httpContext);
    await signInManager.SignOutAsync();
    return Results.LocalRedirect("/login");
}).RequireAuthorization();

// Serve static assets and configure Blazor components.
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Start the application.
app.Run();
