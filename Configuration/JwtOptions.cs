namespace Auth.Web.Configuration;

public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 60;
}
