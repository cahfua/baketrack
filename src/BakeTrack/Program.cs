using BakeTrack.Data;
using Microsoft.EntityFrameworkCore;
using BakeTrack.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<BakeryDbContext>(
    options => options.UseNpgsql(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"
        )
    )
);

var app = builder.Build();

// Temporary database connection test
try
{
    await using var db = await app.Services
        .GetRequiredService<IDbContextFactory<BakeryDbContext>>()
        .CreateDbContextAsync();

    await db.Database.OpenConnectionAsync();

    Console.WriteLine("SUCCESS: Connected to Render PostgreSQL!");

    await db.Database.CloseConnectionAsync();
}
catch (Exception ex)
{
    Console.WriteLine("DATABASE CONNECTION FAILED:");
    Console.WriteLine(ex.ToString());
}
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
