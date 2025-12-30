using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Auth.Web.Repositories.Abstractions.Routing;

namespace Auth.Web.Services.Implementations.Routing;

public sealed class RoutingService : IRoutingService
{
    private readonly IRoutingRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RoutingService> _logger;

    public RoutingService(IRoutingRepository repository, UserManager<ApplicationUser> userManager, ILogger<RoutingService> logger)
    {
        _repository = repository;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<(string ClientId, string ReturnUrl)?> ResolveForUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return null;
        }

        var areaIds = await _repository.GetUserAreaIdsAsync(userId, ct);

        if (areaIds.Count == 0)
        {
            _logger.LogInformation("Routing: usuario {UserId} sin áreas asignadas", userId);
            return null;
        }

        var rule = await _repository.GetFirstActiveRouteForAreasAsync(areaIds, ct);

        if (rule is null)
        {
            _logger.LogInformation("Routing: sin regla activa para usuario {UserId}", userId);
            return null;
        }

        return (rule.ClientId, rule.ReturnUrl);
    }
}
