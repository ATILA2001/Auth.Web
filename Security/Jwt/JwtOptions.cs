namespace Auth.Web.Security.Jwt;

public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;

    public int TokenLifetimeMinutes { get; set; } = 8; // valor recomendado (5-10)
}
