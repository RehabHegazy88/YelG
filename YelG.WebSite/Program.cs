using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using YelG.WebSite.Application;
using YelG.WebSite.Application.Abstractions;
using YelG.WebSite.Infrastructure;
using YelG.WebSite.Infrastructure.Data;
using YelG.WebSite.Infrastructure.Identity;
using YelG.WebSite.Infrastructure.Seeding;
using YelG.WebSite.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddSingleton(UiStrings.Load());

// Razor writes Arabic as is instead of &#x...; entities (markup characters are still encoded).
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

var connectionString = builder.Configuration.GetConnectionString("WebSiteDb")
    ?? throw new InvalidOperationException("Connection string 'WebSiteDb' is missing.");
builder.Services.AddWebSiteApplication();
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
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// The language comes from the first path segment (/ar, /en); everything else (admin, errors) is Arabic.
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(SiteLanguage.Culture(SiteLanguage.Arabic)),
    SupportedCultures = SiteLanguage.All.Select(SiteLanguage.Culture).ToList(),
    SupportedUICultures = SiteLanguage.All.Select(SiteLanguage.Culture).ToList(),
    RequestCultureProviders =
    [
        new CustomRequestCultureProvider(context =>
        {
            var lang = context.Request.RouteValues["lang"] as string;
            return Task.FromResult(SiteLanguage.IsSupported(lang)
                ? new ProviderCultureResult(SiteLanguage.Culture(lang!).Name)
                : null);
        })
    ]
});

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers().WithStaticAssets();

app.Run();
