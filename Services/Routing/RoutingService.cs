using System.Text.Json;
using Auth.Web.Data;
using Microsoft.EntityFrameworkCore;
using Auth.Web.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Auth.Web.Domain.Entities;

namespace Auth.Web.Services.Routing
{
    public sealed class RoutingService : IRoutingService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        public RoutingService(IServiceScopeFactory scopeFactory, UserManager<ApplicationUser> userManager)
        {
            _scopeFactory = scopeFactory;
            _userManager = userManager;
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
                return null;
            }

            var rule = await db.AreaRoutes
                .AsNoTracking()
                .Where(r => r.IsActive && areaIds.Contains(r.AreaId))
                .OrderBy(r => r.Priority)
                .FirstOrDefaultAsync(ct);

            if (rule is null)
            {
                return null;
            }

            var client = await db.ApplicationClients.AsNoTracking().FirstOrDefaultAsync(c => c.ClientId == rule.ClientId, ct);
            if (client is null)
            {
                return null;
            }

            try
            {
                var allowed = JsonSerializer.Deserialize<string[]>(client.AllowedReturnUrlsJson) ?? Array.Empty<string>();
                if (!allowed.Contains(rule.ReturnUrl, StringComparer.OrdinalIgnoreCase))
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }

            return (rule.ClientId, rule.ReturnUrl);
        }
    }
}
