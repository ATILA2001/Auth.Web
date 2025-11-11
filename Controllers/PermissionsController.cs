using Auth.Web.Domain.Dtos;
using Auth.Web.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet("{userName}")]
    public Task<UserPermissionsDto> GetAsync(string userName)
    {
        return _permissionService.GetAsync(userName);
    }
}
