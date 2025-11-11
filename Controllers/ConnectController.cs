using System;
using Auth.Web.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Auth.Web.Controllers;

[ApiController]
[Route("connect")]
public class ConnectController : ControllerBase
{
    private readonly IAdAuthService _adAuthService;
    private readonly IProvisioningService _provisioningService;
    private readonly ITokenService _tokenService;
    private readonly IPermissionService _permissionService;
    private readonly IClientService _clientService;
    private readonly UserManager<Domain.Entities.ApplicationUser> _userManager;

    public ConnectController(
        IAdAuthService adAuthService,
        IProvisioningService provisioningService,
        ITokenService tokenService,
        IPermissionService permissionService,
        IClientService clientService,
        UserManager<Domain.Entities.ApplicationUser> userManager)
    {
        _adAuthService = adAuthService;
        _provisioningService = provisioningService;
        _tokenService = tokenService;
        _permissionService = permissionService;
        _clientService = clientService;
        _userManager = userManager;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!await _adAuthService.ValidateAsync(request.UserName, request.Password))
        {
            return Unauthorized();
        }

        var client = await _clientService.GetAsync(request.ClientId);
        if (client is null)
        {
            return BadRequest("Cliente inválido");
        }

        if (!_clientService.IsReturnUrlAllowed(client, request.ReturnUrl))
        {
            return BadRequest("ReturnUrl inválida");
        }

        var user = await _provisioningService.EnsureUserAsync(request.UserName, request.DisplayName);
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissionService.GetAsync(request.UserName);
        var token = await _tokenService.CreateAsync(user, roles, permissions, client.Audience);

        var redirect = AppendTokenToUrl(request.ReturnUrl, token);
        return Ok(new { redirect });
    }

    private static string AppendTokenToUrl(string url, string token)
    {
        return QueryHelpers.AddQueryString(url, "token", token);
    }

    public record LoginRequest(string UserName, string Password, string ClientId, string ReturnUrl, string? DisplayName);
}
