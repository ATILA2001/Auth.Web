using Auth.Web.Application.Auth;
using Auth.Web.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Auth.Web.Domain.Entities;

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
                    return Unauthorized("No se encontró el usuario admin.");
                }
                await _signInManager.SignInAsync(user, isPersistent: false);
                var redirectUrl = outcome.RedirectUrl ?? "/admin";
                return Redirect(redirectUrl);
            }
            case LoginOutcomeType.SuccessExternalApp:
            {
                if (string.IsNullOrEmpty(outcome.RedirectUrl))
                {
                    return BadRequest("No se pudo determinar la URL de destino.");
                }
                return Redirect(outcome.RedirectUrl);
            }
            case LoginOutcomeType.Failure:
            default:
                return Unauthorized(outcome.ErrorMessage ?? "No se pudo iniciar sesión.");
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
                return Unauthorized("No se encontró el usuario admin.");
            }
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(outcome.RedirectUrl ?? "/admin");
        }
        return Unauthorized(outcome.ErrorMessage ?? "No se pudo iniciar sesión.");
    }
}
