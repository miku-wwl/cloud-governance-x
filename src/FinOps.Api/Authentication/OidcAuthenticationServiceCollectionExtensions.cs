using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FinOps.Api.Authentication;

public static class OidcAuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddFinOpsOidcAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(OidcAuthenticationOptions.SectionName);
        var settings = section.Get<OidcAuthenticationOptions>() ?? new();

        services
            .AddOptions<OidcAuthenticationOptions>()
            .Bind(section)
            .Validate(
                options => !options.Enabled ||
                    (!string.IsNullOrWhiteSpace(options.Authority) &&
                     Uri.TryCreate(options.Authority, UriKind.Absolute, out _)),
                "OIDC Authority must be an absolute URI when authentication is enabled.")
            .Validate(
                options => !options.Enabled ||
                    !options.RequireHttpsMetadata ||
                    (Uri.TryCreate(options.Authority, UriKind.Absolute, out var authority) &&
                     authority.Scheme == Uri.UriSchemeHttps),
                "OIDC Authority must use HTTPS when RequireHttpsMetadata is enabled.")
            .Validate(
                options => !options.Enabled ||
                    !string.IsNullOrWhiteSpace(options.Audience),
                "OIDC Audience is required when authentication is enabled.")
            .Validate(
                options => options.ClockSkewSeconds is >= 0 and <= 300,
                "OIDC ClockSkewSeconds must be between 0 and 300.")
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = EmptyToNull(settings.Authority);
                options.Audience = EmptyToNull(settings.Audience);
                options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromSeconds(settings.ClockSkewSeconds)
                };

                if (!settings.Enabled)
                {
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            context.NoResult();
                            return Task.CompletedTask;
                        }
                    };
                }
            });

        return services;
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
