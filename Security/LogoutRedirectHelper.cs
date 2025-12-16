using System;

namespace Auth.Web.Security;

internal static class LogoutRedirectHelper
{
    private const string DefaultRedirect = "/Account/Login";

    public static string Resolve(string? returnUrl, Func<string?, bool>? isLocalUrl = null)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return DefaultRedirect;
        }

        var validator = isLocalUrl ?? IsSafeLocalUrl;
        return validator(returnUrl) ? returnUrl : DefaultRedirect;
    }

    private static bool IsSafeLocalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (url.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
        {
            return false;
        }

        if (url[0] == '~')
        {
            if (url.Length == 1)
            {
                return false;
            }

            if (url[1] == '/')
            {
                return true;
            }

            return false;
        }

        if (url[0] == '/')
        {
            return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
        }

        return false;
    }
}
