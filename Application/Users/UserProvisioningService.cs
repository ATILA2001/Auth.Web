using Auth.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Auth.Web.Application.Users;

public sealed class UserProvisioningService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserProvisioningService> _logger;

    public UserProvisioningService(UserManager<ApplicationUser> userManager, ILogger<UserProvisioningService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    // Intención: asegurar existencia de usuario local sin password.
    public async Task<ApplicationUser> EnsureUserAsync(string userNameOrEmail, CancellationToken ct = default)
    {
        // Intento buscar por nombre y email
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

        // Asegurar rol base Usuario
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
