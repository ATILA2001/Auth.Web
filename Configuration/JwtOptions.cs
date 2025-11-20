namespace Auth.Web.Configuration;

public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;

    [Obsolete("Use TokenLifetimeMinutes")] public int ExpirationMinutes { get; set; } = 60;

    // Nuevo lifetime corto para minimizar riesgo de token en query string.
    public int TokenLifetimeMinutes { get; set; } = 8; // valor recomendado (5-10)
}
