using Auth.Web.Domain.Entities;
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

    public async Task<ApplicationUser> EnsureUserAsync(string userNameOrEmail, CancellationToken ct = default)
    {
        var user = await _userManager.FindByNameAsync(userNameOrEmail)
                   ?? await _userManager.FindByEmailAsync(userNameOrEmail);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = userNameOrEmail,
                Email = userNameOrEmail.Contains('@') ? userNameOrEmail : string.Empty,
                Nombre = userNameOrEmail
            };
            var create = await _userManager.CreateAsync(user);
            if (!create.Succeeded)
            {
                var errors = string.Join(",", create.Errors.Select(e => e.Description));
                _logger.LogError("No se pudo crear usuario {User}: {Errors}", userNameOrEmail, errors);
                throw new InvalidOperationException($"No se pudo crear usuario: {errors}");
            }
        }

        if (!await _userManager.IsInRoleAsync(user, "Usuario"))
        {
            var add = await _userManager.AddToRoleAsync(user, "Usuario");
            if (!add.Succeeded)
            {
                var errors = string.Join(",", add.Errors.Select(e => e.Description));
                _logger.LogWarning("No se pudo asignar rol Usuario a {User}: {Errors}", userNameOrEmail, errors);
            }
        }

        return user;
    }
}
