namespace Auth.Web.Configuration;

public sealed class FeatureOptions
{
    public bool EnableTokenIssuance { get; set; } = false; // Token generation disabled by default as requested
    public bool EnableTestUsers { get; set; } = false; // Must be explicitly enabled per environment
}
