using System.Text.Json;
using Auth.Web.Data;
using Microsoft.EntityFrameworkCore;
using Auth.Web.Services.Abstractions;

namespace Auth.Web.Services.Routing
{
    public sealed class RoutingService : IRoutingService
    {
        private readonly AuthDbContext _db;
        public RoutingService(AuthDbContext db) => _db = db;

        public async Task<(string ClientId, string ReturnUrl)?> ResolveForUserAsync(string userId, CancellationToken ct = default)
        {
            var areaIds = await _db.UserAreas
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.AreaId)
                .ToListAsync(ct);

            if (areaIds.Count == 0)
            {
                return null;
            }

            var rule = await _db.AreaRoutes
                .Where(r => r.IsActive && areaIds.Contains(r.AreaId))
                .OrderBy(r => r.Priority)
                .FirstOrDefaultAsync(ct);

            if (rule is null)
            {
                return null;
            }

            var client = await _db.ApplicationClients.FirstOrDefaultAsync(c => c.ClientId == rule.ClientId, ct);
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
