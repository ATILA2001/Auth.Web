using Auth.Web.Services.Abstractions.Auth;
using Auth.Web.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Auth.Web.Data.Entities;
using Microsoft.AspNetCore.Authentication;

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
    public async Task<IActionResult> Login([FromForm] LoginRequestDto dto)
    {
        var result = await _authFlowService.LoginAsync(dto);

        if (result.SignInAdmin)
        {
            var user = await _userManager.FindByIdAsync(result.AdminUserId!);
            if (user != null)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
            }
        }

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
