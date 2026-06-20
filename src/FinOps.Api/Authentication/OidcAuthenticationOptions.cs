namespace FinOps.Api.Authentication;

public sealed class OidcAuthenticationOptions
{
    public const string SectionName = "Authentication:Oidc";

    public bool Enabled { get; init; }

    public string Authority { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public bool RequireHttpsMetadata { get; init; } = true;

    public int ClockSkewSeconds { get; init; } = 60;
}
