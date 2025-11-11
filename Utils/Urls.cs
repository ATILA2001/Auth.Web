namespace Auth.Web.Utils;

public static class Urls
{
    public static string NormalizePagePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var trimmed = path.Trim();
        if (!trimmed.StartsWith('/'))
        {
            trimmed = $"/{trimmed}";
        }

        return trimmed.Replace("//", "/");
    }
}
