using Radzen;
using Auth.Web.Components;
using Auth.Web.Components.Account;
using Auth.Web.Configuration;
using Auth.Web.Data;
using Auth.Web.Data.Entities;
using Auth.Web.Security.Jwt;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AdOptions>(builder.Configuration.GetSection("ActiveDirectory"));
builder.Services.Configure<FeatureOptions>(builder.Configuration.GetSection("Features"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

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
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAdminSignInService, AdminSignInService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
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

// Admin repositories
builder.Services.AddScoped<IAreaAdminRepository, AreaAdminRepository>();
builder.Services.AddScoped<IClientAdminRepository, ClientAdminRepository>();
builder.Services.AddScoped<IRoutingAdminRepository, RoutingAdminRepository>();
builder.Services.AddScoped<IRoleAdminRepository, RoleAdminRepository>();
builder.Services.AddScoped<IUserAdminRepository, UserAdminRepository>();

// Non-admin repositories
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IRoutingRepository, RoutingRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IAreaRepository, AreaRepository>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"));

var app = builder.Build();

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
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Redireccion inmediata del root a /Account/Login (HTTP 302) para evitar delay en cliente
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

await Seed.RunAsync(app.Services);

app.Run();
