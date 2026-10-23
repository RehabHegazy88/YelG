using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using YelG.AdminPortal.Infrastructure;
using YelG.BLL;
using YelG.BLL.Common;
using YelG.DAL;
using YelG.DAL.Abstractions;
using YelG.DAL.Data;
using YelG.DAL.Seeding;
using YelG.Model.Auth;

var builder = WebApplication.CreateBuilder(args);

// `dotnet run` serves static files from the build output only in Development by default;
// other non-production environments (e.g. LocalTest) need it switched on explicitly.
if (!builder.Environment.IsProduction()) builder.WebHost.UseStaticWebAssets();

// Add services to the container.
builder.Services
    .AddControllersWithViews(options =>
    {
        options.Filters.Add(new AuthorizeFilter());                       // every page needs a signed-in user
        options.Filters.Add(new MustChangePasswordFilter());              // temporary passwords are replaced first
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); // every POST, forms and AJAX
        options.Filters.Add<BusinessExceptionFilter>();
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// AJAX calls send the token in this header (see site.js).
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

var connectionString = builder.Configuration.GetConnectionString("YelgDb")
    ?? throw new InvalidOperationException("Connection string 'YelgDb' is missing.");
builder.Services.AddYelgData(connectionString);
builder.Services.AddYelgBusiness();
builder.Services.AddYelgEncryption(builder.Configuration["Encryption:Key"]);

// Failed sign-ins per user name and per IP (see LoginThrottle); limits can be tuned in "LoginThrottle".
builder.Services.AddSingleton(builder.Configuration.GetSection("LoginThrottle").Get<LoginThrottleOptions>() ?? new LoginThrottleOptions());
builder.Services.AddSingleton<LoginThrottle>();

builder.Services
    .AddIdentity<User, Role>(IdentityPolicy.Configure)
    .AddEntityFrameworkStores<YelgDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>();

// Deactivating a user or changing their role updates the security stamp; open sessions end within a minute.
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(6); // same as sessionSeconds in Apps Script
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    // AJAX calls get 401/403 instead of an HTML login page.
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.IsAjax()) context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.IsAjax()) context.Response.StatusCode = StatusCodes.Status403Forbidden;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

var app = builder.Build();

if (string.IsNullOrWhiteSpace(app.Configuration["Encryption:Key"]))
    app.Logger.LogWarning("Encryption:Key is not set; anything that encrypts data will fail. Set Encryption__Key to 32 random bytes in Base64.");

// Apply migrations and seed roles / question bank / first Admin (replaces setupYELG).
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync(app.Configuration["Seed:AdminUserName"], app.Configuration["Seed:AdminPassword"]);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
