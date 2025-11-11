using System.Linq;
using Auth.Web.Domain.Entities;
using Auth.Web.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Auth.Web.Services.Auth;

public class ProvisioningService : IProvisioningService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ProvisioningService> _logger;

    public ProvisioningService(UserManager<ApplicationUser> userManager, ILogger<ProvisioningService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<ApplicationUser> EnsureUserAsync(string userName, string? displayName = null)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = string.Empty,
                Nombre = displayName
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(",", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to provision user {User}: {Errors}", userName, errors);
                throw new InvalidOperationException($"No se pudo crear el usuario {userName}: {errors}");
            }
        }

        if (!await _userManager.IsInRoleAsync(user, "Usuario"))
        {
            var addRoleResult = await _userManager.AddToRoleAsync(user, "Usuario");
            if (!addRoleResult.Succeeded)
            {
                var errors = string.Join(",", addRoleResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign role Usuario to {User}: {Errors}", userName, errors);
                throw new InvalidOperationException($"No se pudo asignar el rol Usuario a {userName}: {errors}");
            }
        }

        return user;
    }
}
