using Radzen;
using Auth.Web.Components;
using Auth.Web.Components.Account;
using Auth.Web.Configuration;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Services.Abstractions.Users;
using Auth.Web.Services.Abstractions.Admin;
using Auth.Web.Application.Permissions;
using Auth.Web.Services.Abstractions.Permissions;
using Auth.Web.Services.Abstractions.Routing;
using Auth.Web.Services.Abstractions.Clients;
using Auth.Web.Security.Auth;
using Auth.Web.Services.Implementations.Auth;
using Auth.Web.Services.Implementations.Clients;
using Auth.Web.Services.Implementations.Permissions;
using Auth.Web.Services.Implementations.Routing;
using Auth.Web.Services.Implementations.Admin;
using Auth.Web.Services.Implementations.Users;
using Auth.Web.Repositories.Abstractions;
using Auth.Web.Repositories.Abstractions.Admin;
using Auth.Web.Repositories.Implementations;
using Auth.Web.Repositories.Implementations.Admin;
using Auth.Web.Repositories.Abstractions.Permissions;
using Auth.Web.Repositories.Implementations.Permissions;
using Auth.Web.Repositories.Abstractions.Routing;
using Auth.Web.Repositories.Implementations.Routing;
using Auth.Web.Repositories.Abstractions.Clients;
using Auth.Web.Repositories.Implementations.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

builder.Services.Configure<AdOptions>(builder.Configuration.GetSection("ActiveDirectory"));
builder.Services.Configure<FeatureOptions>(builder.Configuration.GetSection("Features"));
builder.Services.Configure<TestUsersOptions>(builder.Configuration.GetSection("TestUsers"));

var sharedCookieName = builder.Configuration["SharedCookie:Name"] ?? ".Auth.Shared";
var sharedCookieDomain = builder.Configuration["SharedCookie:Domain"];
var dataProtectionAppName = builder.Configuration["SharedCookie:ApplicationName"] ?? "Auth.SharedCookie";

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContextFactory<AuthDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContext<AuthDbContext>(
    options => options.UseSqlServer(connectionString),
    optionsLifetime: ServiceLifetime.Singleton);

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AuthDbContext>()
    .SetApplicationName(dataProtectionAppName);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = sharedCookieName;
    options.Cookie.Domain = string.IsNullOrWhiteSpace(sharedCookieDomain) ? null : sharedCookieDomain;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// Use a shared, net48-compatible ticket format for the shared cookie (run after ConfigureApplicationCookie)
builder.Services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
    .PostConfigure<IDataProtectionProvider>((options, dp) =>
    {
        var protector = dp.CreateProtector(
            "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
            IdentityConstants.ApplicationScheme,
            "v2");
        options.TicketDataFormat = new Auth.Web.Security.SharedCookieTicketDataFormat(protector);
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Services
if (OperatingSystem.IsWindows())
{
    builder.Services.AddScoped<IActiveDirectoryAuthService, AdAuthService>();
}
else
{
    builder.Services.AddScoped<IActiveDirectoryAuthService, NoopAdAuthService>();
}
builder.Services.AddScoped<IAdminSignInService, AdminSignInService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IPermissionAuditService, PermissionAuditService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IRoutingService, RoutingService>();

// Orchestrators and user-related services
builder.Services.AddScoped<IAuthFlowService, AuthFlowService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IUserProvisioningService, UserProvisioningService>();
builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
builder.Services.AddScoped<UserPermissionsAssembler>();

// Admin services
builder.Services.AddScoped<IAdminUserService, UserAdminService>();
builder.Services.AddScoped<IAdminRoleService, RoleAdminService>();
builder.Services.AddScoped<IAdminAreaService, AreaAdminService>();
builder.Services.AddScoped<IAdminClientService, ClientAdminService>();
builder.Services.AddScoped<IAdminRoutingService, RoutingAdminService>();
builder.Services.AddScoped<IAdminPageService, PageAdminService>();
builder.Services.AddScoped<IAdminActionPermissionService, ActionPermissionAdminService>();
builder.Services.AddScoped<IAdminRolePagePermissionService, RolePagePermissionAdminService>();
builder.Services.AddScoped<IAdminAreaPagePermissionService, AreaPagePermissionAdminService>();
builder.Services.AddScoped<IAdminUserPageOverrideService, UserPageOverrideAdminService>();

// Admin repositories
builder.Services.AddScoped<IAreaAdminRepository, AreaAdminRepository>();
builder.Services.AddScoped<IClientAdminRepository, ClientAdminRepository>();
builder.Services.AddScoped<IRoutingAdminRepository, RoutingAdminRepository>();
builder.Services.AddScoped<IRoleAdminRepository, RoleAdminRepository>();
builder.Services.AddScoped<IUserAdminRepository, UserAdminRepository>();
builder.Services.AddScoped<IPageAdminRepository, PageAdminRepository>();
builder.Services.AddScoped<IActionPermissionAdminRepository, ActionPermissionAdminRepository>();
builder.Services.AddScoped<IRolePagePermissionAdminRepository, RolePagePermissionAdminRepository>();
builder.Services.AddScoped<IAreaPagePermissionAdminRepository, AreaPagePermissionAdminRepository>();
builder.Services.AddScoped<IUserPageOverrideAdminRepository, UserPageOverrideAdminRepository>();

// Non-admin repositories
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IRoutingRepository, RoutingRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IAreaRepository, AreaRepository>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"));

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = false; // set true only after registering in the HSTS preload list
});

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
});

var app = builder.Build();


// Optional: delete DataProtectionKeys to force regeneration under the new protector (disabled by default)
if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("DataProtection:CleanupKeysOnStartup"))
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var loggerFactory = scope.ServiceProvider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("DataProtectionStartup");

            var keys = db.DataProtectionKeys.ToList();
            if (keys.Count > 0)
            {
                db.DataProtectionKeys.RemoveRange(keys);
                db.SaveChanges();
                logger?.LogInformation("Deleted {Count} existing DataProtectionKeys to force regeneration under machine DPAPI.", keys.Count);
            }
            else
            {
                logger?.LogInformation("No DataProtectionKeys found to delete.");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()?.CreateLogger("DataProtectionStartup");
        logger?.LogWarning(ex, "Failed to clean DataProtectionKeys during startup: {Message}", ex.Message);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["X-XSS-Protection"] = "0"; // rely on CSP, not legacy IE filter
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Redirección inmediata del root a /Account/Login (HTTP 302) para evitar delay en cliente
app.MapGet("/", (HttpContext ctx) =>
{
    var returnUrl = ctx.Request.Query["returnUrl"].ToString();
    var target = string.IsNullOrEmpty(returnUrl)
        ? "/Account/Login"
        : $"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";
    return Results.Redirect(target, permanent: false);
});

app.MapControllers();
app.MapAdditionalIdentityEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
