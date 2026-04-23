using Auth.Web.Data.Entities;
using Auth.Web.Services.Abstractions.Routing;
using Auth.Web.Services.Abstractions.Clients;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Auth.Web.Repositories.Abstractions.Routing;

namespace Auth.Web.Services.Implementations.Routing;

public sealed class RoutingService : IRoutingService
{
    private readonly IRoutingRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RoutingService> _logger;
    private readonly IClientService _clientService;

    public RoutingService(IRoutingRepository repository, UserManager<ApplicationUser> userManager, ILogger<RoutingService> logger, IClientService clientService)
    {
        _repository = repository;
        _userManager = userManager;
        _logger = logger;
        _clientService = clientService;
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

        if (rule.Client is null || string.IsNullOrWhiteSpace(rule.Client.ClientId))
        {
            _logger.LogInformation("Routing: regla sin cliente válido para usuario {UserId}", userId);
            return null;
        }

        var returnUrl = _clientService.GetDefaultReturnUrl(rule.Client);
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            _logger.LogInformation("Routing: cliente {ClientId} sin ReturnUrl configurada", rule.Client.ClientId);
            return null;
        }

        return (rule.Client.ClientId, returnUrl);
    }

    public async Task<IReadOnlyList<(string ClientId, string ReturnUrl)>> ResolveAllForUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
            return [];

        var areaIds = await _repository.GetUserAreaIdsAsync(userId, ct);
        if (areaIds.Count == 0)
            return [];

        var routes = await _repository.GetDistinctActiveRoutesForAreasAsync(areaIds, ct);

        var result = new List<(string ClientId, string ReturnUrl)>();
        foreach (var route in routes)
        {
            if (route.Client is null || string.IsNullOrWhiteSpace(route.Client.ClientId))
                continue;

            var returnUrl = _clientService.GetDefaultReturnUrl(route.Client);
            if (string.IsNullOrWhiteSpace(returnUrl))
                continue;

            result.Add((route.Client.ClientId, returnUrl));
        }

        return result;
    }
}
