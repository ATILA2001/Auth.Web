using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;

namespace Auth.Web.Controllers;

[ApiController]
[Route("connect")]
public class ConnectController : ControllerBase
{
    private readonly IAuthFlowService _authFlowService;
    private readonly IAntiforgery _antiforgery;

    public ConnectController(IAuthFlowService authFlowService, IAntiforgery antiforgery)
    {
        _authFlowService = authFlowService;
        _antiforgery = antiforgery;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] LoginRequestDto dto)
    {
        if (!await TryValidateAntiforgeryAsync())
        {
            return BadRequest();
        }

        var result = await _authFlowService.LoginAsync(dto);

        if (result.ShowAppPicker)
        {
            return Redirect("/Account/SelectApp");
        }

        return Redirect(result.RedirectUrl);
    }

    [HttpPost("portal-login")]
    public Task<IActionResult> PortalLogin([FromForm] LoginRequestDto dto)
        => Login(dto);

    [Authorize]
    [HttpGet("switch-app")]
    public async Task<IActionResult> SwitchApp([FromQuery] string? clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Redirect("/Account/SelectApp");
        }

        var redirectUrl = await _authFlowService.SelectAppAsync(clientId);
        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            return Redirect("/Account/SelectApp?error=access_denied");
        }

        return Redirect(redirectUrl);
    }

    [Authorize]
    [HttpPost("select-app")]
    public async Task<IActionResult> SelectApp([FromForm] string clientId)
    {
        if (!await TryValidateAntiforgeryAsync())
        {
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Redirect("/Account/SelectApp");
        }

        var redirectUrl = await _authFlowService.SelectAppAsync(clientId);
        if (string.IsNullOrWhiteSpace(redirectUrl))
        {
            return Redirect("/Account/SelectApp?error=access_denied");
        }

        return Redirect(redirectUrl);
    }

    [HttpGet("logout")]
    public IActionResult Logout()
        => StatusCode(StatusCodes.Status405MethodNotAllowed);

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutPost()
    {
        if (!await TryValidateAntiforgeryAsync())
        {
            return BadRequest();
        }

        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return Redirect("/Account/Login");
    }

    private async Task<bool> TryValidateAntiforgeryAsync()
    {
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }
}
