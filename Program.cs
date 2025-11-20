using Radzen;
using Auth.Web.Components;
using Auth.Web.Components.Account;
using Auth.Web.Configuration;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Auth.Web.Services.Admin;
using Auth.Web.Application.Abstractions;
using Auth.Web.Application.Auth;
using Auth.Web.Application.Users;
using Auth.Web.Application.Permissions;
using Auth.Web.Application.Admin.Abstractions; // new admin interfaces
using LegacyRoutingService = Auth.Web.Services.Abstractions.IRoutingService;
using LegacyPermissionService = Auth.Web.Services.Abstractions.IPermissionService;
using LegacyClientService = Auth.Web.Services.Abstractions.IClientService;
using LegacyAdAuthService = Auth.Web.Services.Abstractions.IAdAuthService;
using Auth.Web.Services.Auth; // AdAuthService
using Auth.Web.Services.Clients;
using Auth.Web.Services.Permissions;
using Auth.Web.Services.Routing;
using InfraClientAdminService = Auth.Web.Infrastructure.Admin.ClientAdminService;
using InfraRoutingAdminService = Auth.Web.Infrastructure.Admin.RoutingAdminService;

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

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddScoped<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// AD service implements both legacy and new interface
builder.Services.AddScoped<LegacyAdAuthService, AdAuthService>();
builder.Services.AddScoped<IActiveDirectoryAuthService, AdAuthService>();

// JWT token service
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Permission service implements both interfaces
builder.Services.AddScoped<LegacyPermissionService, PermissionService>();
builder.Services.AddScoped<Auth.Web.Application.Abstractions.IPermissionService, PermissionService>();

// Client service implements both
builder.Services.AddScoped<LegacyClientService, ClientService>();
builder.Services.AddScoped<Auth.Web.Application.Abstractions.IClientService, ClientService>();

// Routing service implements both
builder.Services.AddScoped<LegacyRoutingService, RoutingService>();
builder.Services.AddScoped<Auth.Web.Application.Abstractions.IRoutingService, RoutingService>();

// Application layer orchestrators
builder.Services.AddScoped<IAuthFlowService, AuthFlowService>(); // register via interface
builder.Services.AddScoped<UserProvisioningService>();
builder.Services.AddScoped<UserPermissionsAssembler>();

// New admin interfaces mapping
builder.Services.AddScoped<IAdminUserService, UserAdminService>();
builder.Services.AddScoped<IAdminRoleService, RoleAdminService>();
builder.Services.AddScoped<IAdminAreaService, AreaAdminService>();
builder.Services.AddScoped<IAdminClientService, InfraClientAdminService>();
builder.Services.AddScoped<IAdminRoutingService, InfraRoutingAdminService>();

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

await Seed.RunAsync(app.Services);

app.Run();
