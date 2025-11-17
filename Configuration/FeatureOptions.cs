namespace Auth.Web.Configuration;

public sealed class FeatureOptions
{
    public bool EnableTokenIssuance { get; set; } = false; // Token generation disabled by default as requested
}
