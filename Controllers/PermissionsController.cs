using Auth.Web.Application.Permissions.Dtos;
using Auth.Web.Services.Abstractions.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet("{userName}")]
    [Authorize(Roles = "Admin")]
    public Task<UserPermissionsDto> GetAsync(string userName)
    {
        return _permissionService.GetAsync(userName);
    }

    [HttpGet("version")]
    public async Task<ActionResult<PermissionVersionDto>> GetVersionAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var version = await _permissionService.GetVersionByUserIdAsync(userId);
        return new PermissionVersionDto { Version = version };
    }
}
