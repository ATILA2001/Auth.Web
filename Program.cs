using Auth.Web.Components;
using Auth.Web.Components.Account;
using Auth.Web.Configuration;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Abstractions;
using Auth.Web.Services.Auth;
using Auth.Web.Services.Clients;
using Auth.Web.Services.Permissions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AdOptions>(builder.Configuration.GetSection("ActiveDirectory"));
builder.Services.Configure<FeatureOptions>(builder.Configuration.GetSection("Features"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
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

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Registrar HttpClient para uso en componentes (llamadas a /connect/login)
builder.Services.AddHttpClient();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Registrar el IEmailSender<ApplicationUser> usado por los componentes de identidad
builder.Services.AddScoped<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddScoped<IAdAuthService, AdAuthService>();
builder.Services.AddScoped<IProvisioningService, ProvisioningService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IClientService, ClientService>();

// Routing por áreas
builder.Services.AddScoped<Auth.Web.Services.Abstractions.IRoutingService, Auth.Web.Services.Routing.RoutingService>();

// Orquestador de login
builder.Services.AddScoped<ILoginService, LoginService>();

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

// Endpoints necesarios para Logout y acciones auxiliares de Identity Components
app.MapAdditionalIdentityEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await Seed.RunAsync(app.Services);

app.Run();
