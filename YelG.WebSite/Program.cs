using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YelG.WebSite.Application.Abstractions;
using YelG.WebSite.Infrastructure;
using YelG.WebSite.Infrastructure.Data;
using YelG.WebSite.Infrastructure.Identity;
using YelG.WebSite.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

var connectionString = builder.Configuration.GetConnectionString("WebSiteDb")
    ?? throw new InvalidOperationException("Connection string 'WebSiteDb' is missing.");
builder.Services.AddWebSiteInfrastructure(connectionString);

builder.Services
    .AddIdentity<AdminUser, AdminRole>(IdentityPolicy.Configure)
    .AddEntityFrameworkStores<WebSiteDbContext>()
    .AddDefaultTokenProviders();

var app = builder.Build();

// Development (and LocalTest) apply migrations and seed at startup. Production does neither: the reviewed,
// idempotent SQL script is applied by hand, then the seeder is run once with "--seed".
// EF tools load this program to build the model; nothing runs against a database then.
if (!EF.IsDesignTime && (app.Environment.IsDevelopment() || args.Contains("--seed")
                         || app.Configuration.GetValue<bool>("Database:MigrateAndSeedOnStartup")))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WebSiteDbContext>();
    if (!args.Contains("--seed") || app.Environment.IsDevelopment()) await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<ContentSeeder>().SeedAsync(new SeedOptions
    {
        AdminEmail = app.Configuration["Seed:AdminEmail"],
        AdminPassword = app.Configuration["Seed:AdminPassword"],
        AdminDisplayName = app.Configuration["Seed:AdminDisplayName"] ?? "YelG Admin",
        WebRootPath = app.Environment.WebRootPath
    });
    if (args.Contains("--seed")) return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
