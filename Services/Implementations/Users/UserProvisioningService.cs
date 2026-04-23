using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Auth.Web.Services.Implementations.Users;

public sealed class UserProvisioningService : IUserProvisioningService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserProvisioningService> _logger;

    public UserProvisioningService(UserManager<ApplicationUser> userManager, ILogger<UserProvisioningService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<ApplicationUser> EnsureUserAsync(string userName, string? email = null, string? nombre = null, CancellationToken ct = default)
    {
        var user = await _userManager.FindByNameAsync(userName)
                   ?? (email is not null ? await _userManager.FindByEmailAsync(email) : null);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = email ?? string.Empty,
                Nombre = nombre ?? userName
            };
            var create = await _userManager.CreateAsync(user);
            if (!create.Succeeded)
            {
                var errors = string.Join(",", create.Errors.Select(e => e.Description));
                _logger.LogError("No se pudo crear usuario {User}: {Errors}", userName, errors);
                throw new InvalidOperationException($"No se pudo crear usuario: {errors}");
            }
        }

        if (!await _userManager.IsInRoleAsync(user, "Usuario"))
        {
            var add = await _userManager.AddToRoleAsync(user, "Usuario");
            if (!add.Succeeded)
            {
                var errors = string.Join(",", add.Errors.Select(e => e.Description));
                _logger.LogWarning("No se pudo asignar rol Usuario a {User}: {Errors}", userName, errors);
            }
        }

        return user;
    }
}
