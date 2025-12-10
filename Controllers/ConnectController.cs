using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Auth.Web.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;

namespace Auth.Web.Controllers;

[ApiController]
[Route("connect")]
public class ConnectController : ControllerBase
{
    private readonly IAuthFlowService _authFlowService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ConnectController(
        IAuthFlowService authFlowService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _authFlowService = authFlowService;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var outcome = await _authFlowService.LoginAsync(request, cancellationToken);

        switch (outcome.Type)
        {
            case LoginOutcomeType.SuccessAdmin:
            {
                var user = await _userManager.FindByNameAsync(request.UserNameOrEmail)
                           ?? await _userManager.FindByEmailAsync(request.UserNameOrEmail);
                if (user is null)
                {
                    return RedirectToLoginWithError("No se encontró el usuario admin.", request.ReturnUrl, request.ClientId);
                }
                await _signInManager.SignInAsync(user, isPersistent: false);
                var redirectUrl = outcome.RedirectUrl ?? "/admin";
                return Redirect(redirectUrl);
            }
            case LoginOutcomeType.SuccessExternalApp:
            {
                if (string.IsNullOrEmpty(outcome.RedirectUrl))
                {
                    return RedirectToLoginWithError("No se pudo determinar la URL de destino.", request.ReturnUrl, request.ClientId);
                }
                return Redirect(outcome.RedirectUrl);
            }
            case LoginOutcomeType.Failure:
            default:
                return RedirectToLoginWithError(outcome.ErrorMessage ?? "Usuario o contraseña inválidos.", request.ReturnUrl, request.ClientId);
        }
    }

    [HttpPost("portal-login")]
    public async Task<IActionResult> PortalLogin([FromForm] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var outcome = await _authFlowService.LoginAsync(request, cancellationToken);
        if (outcome.Type == LoginOutcomeType.SuccessAdmin)
        {
            var user = await _userManager.FindByNameAsync(request.UserNameOrEmail)
                       ?? await _userManager.FindByEmailAsync(request.UserNameOrEmail);
            if (user is null)
            {
                return RedirectToLoginWithError("No se encontró el usuario admin.", request.ReturnUrl, request.ClientId);
            }
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(outcome.RedirectUrl ?? "/admin");
        }
        return RedirectToLoginWithError(outcome.ErrorMessage ?? "Usuario o contraseña inválidos.", request.ReturnUrl, request.ClientId);
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return Redirect("/Account/Login");
    }

    [HttpGet("/Account/Logout")]
    public Task<IActionResult> AccountLogout() => Logout();

    private IActionResult RedirectToLoginWithError(string errorMessage, string? returnUrl, string? clientId)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["error"] = errorMessage,
            ["returnUrl"] = returnUrl ?? string.Empty,
            ["clientId"] = clientId ?? string.Empty
        };

        var loginUrl = QueryHelpers.AddQueryString("/Account/Login", queryParams);
        return Redirect(loginUrl);
    }
}
