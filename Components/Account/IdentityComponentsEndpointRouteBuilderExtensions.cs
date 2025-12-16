using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Auth.Web.Security;

namespace Microsoft.AspNetCore.Routing
{
    internal static class IdentityComponentsEndpointRouteBuilderExtensions
    {
        // Endpoints mínimos necesarios: solo Logout
        public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var accountGroup = endpoints.MapGroup("/Account");

            accountGroup.MapPost("/Logout", AccountLogoutEndpoint.HandleAsync)
                .RequireAuthorization();

            return accountGroup;
        }
    }

    internal static class AccountLogoutEndpoint
    {
        public static async Task<IResult> HandleAsync(
            HttpContext context,
            IAntiforgery antiforgery,
            [FromForm] string? returnUrl)
        {
            if (!await AntiforgeryRequestValidator.TryValidateAsync(antiforgery, context))
            {
                return Results.BadRequest();
            }

            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
            var target = LogoutRedirectHelper.Resolve(returnUrl);
            return Results.LocalRedirect(target);
        }
    }

    internal static class AntiforgeryRequestValidator
    {
        public static async Task<bool> TryValidateAsync(IAntiforgery antiforgery, HttpContext context)
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
                return true;
            }
            catch (AntiforgeryValidationException)
            {
                return false;
            }
        }
    }
}
