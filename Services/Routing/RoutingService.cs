using Auth.Web.Data;
using Auth.Web.Domain.Entities;
using Auth.Web.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Auth.Web.Services.Routing
{
    public sealed class RoutingService : IRoutingService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RoutingService> _logger;

        public RoutingService(IServiceScopeFactory scopeFactory, UserManager<ApplicationUser> userManager, ILogger<RoutingService> logger)
        {
            _scopeFactory = scopeFactory;
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

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            var areaIds = await db.UserAreas
                .AsNoTracking()
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.AreaId)
                .ToListAsync(ct);

            if (areaIds.Count == 0)
            {
                _logger.LogInformation("Routing: usuario {UserId} sin áreas asignadas", userId);
                return null;
            }

            var rule = await db.AreaRoutes
                .AsNoTracking()
                .Where(r => r.IsActive && areaIds.Contains(r.AreaId))
                .OrderBy(r => r.Priority)
                .FirstOrDefaultAsync(ct);

            if (rule is null)
            {
                _logger.LogInformation("Routing: sin regla activa para usuario {UserId}", userId);
                return null;
            }

            return (rule.ClientId, rule.ReturnUrl);
        }
    }
}
