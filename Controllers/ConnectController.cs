using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Auth.Web.Controllers;

[ApiController]
[Route("connect")]
public class ConnectController : ControllerBase
{
    private readonly IAuthFlowService _authFlowService;

    public ConnectController(IAuthFlowService authFlowService)
    {
        _authFlowService = authFlowService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] LoginRequestDto dto)
    {
        var result = await _authFlowService.LoginAsync(dto);
        return Redirect(result.RedirectUrl);
    }

    [HttpPost("portal-login")]
    public Task<IActionResult> PortalLogin([FromForm] LoginRequestDto dto)
        => Login(dto);

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return Redirect("/Account/Login");
    }
}
