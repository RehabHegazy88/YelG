using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;
using YelG.WebSite.Application;
using YelG.WebSite.Application.Abstractions;
using YelG.WebSite.Application.Common;
using YelG.WebSite.Application.Media;
using YelG.WebSite.Areas.Admin.Controllers;
using YelG.WebSite.Areas.Admin.Editing;
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
    .AddSignInManager<AdminSignInManager>()
    .AddClaimsPrincipalFactory<AdminClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

// Admin panel sign-in: the cookie is sent to /admin only (the preview lives there too), never to the public pages.
var secureCookies = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "yelg.admin";
    o.Cookie.Path = "/admin";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = secureCookies;
    o.LoginPath = "/admin/login";
    o.LogoutPath = "/admin/logout";
    o.AccessDeniedPath = "/admin/denied";
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
});
// a deactivated account or a changed role ends open sessions within this time
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(5));
builder.Services.AddAntiforgery(o =>
{
    o.Cookie.Name = "yelg.af";
    o.Cookie.SecurePolicy = secureCookies;
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AdminPolicies.Content, p => p.RequireRole(AdminRoles.Admin, AdminRoles.Editor))
    .AddPolicy(AdminPolicies.AdminOnly, p => p.RequireRole(AdminRoles.Admin));

// Media library: uploads live outside the deployed files (App_Data/media by default) and are served under /media.
builder.Services.Configure<MediaOptions>(builder.Configuration.GetSection(MediaOptions.SectionName));
builder.Services.PostConfigure<MediaOptions>(o => o.StoragePath = Path.GetFullPath(o.StoragePath, builder.Environment.ContentRootPath));

builder.Services.AddSingleton(AdminScreens.Build());

var app = builder.Build();

// the editors take their maximum lengths from the database model
using (var scope = app.Services.CreateScope())
    app.Services.GetRequiredService<EditorRegistry>().ApplyModel(scope.ServiceProvider.GetRequiredService<WebSiteDbContext>().Model);

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

if (!EF.IsDesignTime && args.Contains(AdminCommand.Flag))
{
    Environment.ExitCode = await AdminCommand.RunAsync(app.Services, args);
    return;
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

var media = app.Services.GetRequiredService<IOptions<MediaOptions>>().Value;
Directory.CreateDirectory(media.StoragePath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(media.StoragePath),
    RequestPath = media.RequestPath,
    // only the formats the library accepts are served
    ContentTypeProvider = new FileExtensionContentTypeProvider(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp", [".gif"] = "image/gif",
        [".mp4"] = "video/mp4", [".webm"] = "video/webm"
    }),
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.XContentTypeOptions = "nosniff";
        ctx.Context.Response.Headers.CacheControl = "public, max-age=2592000";   // file names never change
    }
});

app.MapStaticAssets();
app.MapControllers().WithStaticAssets();

app.Run();
