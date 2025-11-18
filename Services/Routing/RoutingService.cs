using System.Text.Json;
using Auth.Web.Data;
using Microsoft.EntityFrameworkCore;
using Auth.Web.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Auth.Web.Domain.Entities;
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
                // Admin se maneja fuera de este servicio
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

            var client = await db.ApplicationClients.AsNoTracking().FirstOrDefaultAsync(c => c.ClientId == rule.ClientId, ct);
            if (client is null)
            {
                _logger.LogWarning("Routing: cliente {ClientId} no encontrado", rule.ClientId);
                return null;
            }

            // Validación más permisiva de AllowedReturnUrls
            if (!IsReturnUrlAllowed(client.AllowedReturnUrlsJson, rule.ReturnUrl))
            {
                _logger.LogWarning("Routing: ReturnUrl {ReturnUrl} no permitido para cliente {ClientId}", rule.ReturnUrl, rule.ClientId);
                return null;
            }

            return (rule.ClientId, rule.ReturnUrl);
        }

        private static bool IsReturnUrlAllowed(string? allowedJson, string returnUrl)
        {
            // Si no hay lista configurada, permitir por defecto
            if (string.IsNullOrWhiteSpace(allowedJson))
            {
                return true;
            }

            string[] allowed;
            try
            {
                allowed = JsonSerializer.Deserialize<string[]>(allowedJson!) ?? Array.Empty<string>();
            }
            catch
            {
                // Si el JSON es inválido, permitir para no bloquear flujo inesperadamente
                return true;
            }

            if (allowed.Length == 0)
            {
                return true;
            }

            static string Norm(string s) => (s ?? string.Empty).Trim().TrimEnd('/');

            var r = Norm(returnUrl);
            foreach (var a in allowed)
            {
                var aa = Norm(a);
                if (aa == "*") return true;
                // Coincidencia exacta (relativa o absoluta)
                if (string.Equals(aa, r, StringComparison.OrdinalIgnoreCase)) return true;

                // Si el permitido es absoluto, permitir prefijo (base URL)
                if (Uri.TryCreate(aa, UriKind.Absolute, out var aUri))
                {
                    if (Uri.TryCreate(r, UriKind.Absolute, out var rUri))
                    {
                        // Mismo host y el path de allowed es prefijo
                        if (string.Equals(aUri.Scheme, rUri.Scheme, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(aUri.Host, rUri.Host, StringComparison.OrdinalIgnoreCase)
                            && rUri.AbsolutePath.StartsWith(aUri.AbsolutePath, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // allowed absoluto y return relativo: comparar por path
                        if (r.StartsWith(aUri.AbsolutePath, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    // allowed relativo: permitir si es prefijo del return (relativo o solo path de absoluto)
                    if (r.StartsWith(aa, StringComparison.OrdinalIgnoreCase)) return true;
                    if (Uri.TryCreate(r, UriKind.Absolute, out var rUri2))
                    {
                        if (rUri2.AbsolutePath.StartsWith(aa, StringComparison.OrdinalIgnoreCase)) return true;
                    }
                }
            }

            return false;
        }
    }
}
