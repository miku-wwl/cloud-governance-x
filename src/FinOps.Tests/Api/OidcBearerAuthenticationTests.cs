using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using FinOps.Api.Authentication;
using FinOps.Api.Tenancy;
using FinOps.Application.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace FinOps.Tests.Api;

public sealed class OidcBearerAuthenticationTests
{
    private const string Issuer = "https://issuer.finops.test";
    private const string Audience = "api://finops-api";
    private const string Subject = "operator-a";
    private static readonly Guid TenantId =
        Guid.Parse("20000000-0000-0000-0000-000000000025");

    [Fact]
    public async Task Protected_endpoint_rejects_missing_token()
    {
        using var rsa = RSA.Create(2048);
        await using var application = await CreateApplicationAsync(CreateSigningKey(rsa));

        using var response = await application.GetTestClient().GetAsync("/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Valid_token_preserves_raw_issuer_and_subject_claims()
    {
        using var rsa = RSA.Create(2048);
        var signingKey = CreateSigningKey(rsa);
        await using var application = await CreateApplicationAsync(signingKey);
        using var request = CreateAuthenticatedRequest(
            CreateToken(signingKey, Issuer, Audience, DateTime.UtcNow.AddMinutes(5)));

        using var response = await application.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            $"{Issuer}|{Subject}",
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Expired_token_is_rejected()
    {
        using var rsa = RSA.Create(2048);
        var signingKey = CreateSigningKey(rsa);
        await using var application = await CreateApplicationAsync(signingKey);
        using var request = CreateAuthenticatedRequest(
            CreateToken(signingKey, Issuer, Audience, DateTime.UtcNow.AddMinutes(-5)));

        using var response = await application.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_with_wrong_issuer_is_rejected()
    {
        using var rsa = RSA.Create(2048);
        var signingKey = CreateSigningKey(rsa);
        await using var application = await CreateApplicationAsync(signingKey);
        using var request = CreateAuthenticatedRequest(
            CreateToken(
                signingKey,
                "https://other-issuer.finops.test",
                Audience,
                DateTime.UtcNow.AddMinutes(5)));

        using var response = await application.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_with_wrong_audience_is_rejected()
    {
        using var rsa = RSA.Create(2048);
        var signingKey = CreateSigningKey(rsa);
        await using var application = await CreateApplicationAsync(signingKey);
        using var request = CreateAuthenticatedRequest(
            CreateToken(
                signingKey,
                Issuer,
                "api://other-api",
                DateTime.UtcNow.AddMinutes(5)));

        using var response = await application.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_with_untrusted_signature_is_rejected()
    {
        using var trustedRsa = RSA.Create(2048);
        using var untrustedRsa = RSA.Create(2048);
        var trustedKey = CreateSigningKey(trustedRsa);
        var untrustedKey = CreateSigningKey(untrustedRsa);
        await using var application = await CreateApplicationAsync(trustedKey);
        using var request = CreateAuthenticatedRequest(
            CreateToken(
                untrustedKey,
                Issuer,
                Audience,
                DateTime.UtcNow.AddMinutes(5)));

        using var response = await application.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_signing_key_refreshes_configuration_for_next_request()
    {
        using var oldRsa = RSA.Create(2048);
        using var rotatedRsa = RSA.Create(2048);
        var oldKey = CreateSigningKey(oldRsa);
        var rotatedKey = CreateSigningKey(rotatedRsa);
        var configurationManager = new SigningKeyRefreshConfigurationManager(
            CreateOidcConfiguration(oldKey),
            CreateOidcConfiguration(rotatedKey));
        await using var application = await CreateApplicationAsync(
            oldKey,
            configurationManager: configurationManager);
        using var firstRequest = CreateAuthenticatedRequest(
            CreateToken(
                rotatedKey,
                Issuer,
                Audience,
                DateTime.UtcNow.AddMinutes(5)));

        using var firstResponse = await application.GetTestClient().SendAsync(firstRequest);
        using var secondRequest = CreateAuthenticatedRequest(
            CreateToken(
                rotatedKey,
                Issuer,
                Audience,
                DateTime.UtcNow.AddMinutes(5)));
        using var secondResponse = await application.GetTestClient().SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, firstResponse.StatusCode);
        Assert.True(configurationManager.RefreshCount > 0);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Unavailable_oidc_metadata_fails_authentication_closed()
    {
        using var rsa = RSA.Create(2048);
        var signingKey = CreateSigningKey(rsa);
        var configurationManager = new UnavailableConfigurationManager();
        await using var application = await CreateApplicationAsync(
            signingKey,
            configurationManager: configurationManager);
        using var request = CreateAuthenticatedRequest(
            CreateToken(
                signingKey,
                Issuer,
                Audience,
                DateTime.UtcNow.AddMinutes(5)));

        using var response = await application.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(configurationManager.RequestCount > 0);
    }

    [Fact]
    public async Task Health_endpoint_remains_anonymous()
    {
        using var rsa = RSA.Create(2048);
        await using var application = await CreateApplicationAsync(CreateSigningKey(rsa));

        using var response = await application.GetTestClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Disabled_authentication_does_not_accept_otherwise_valid_token()
    {
        using var rsa = RSA.Create(2048);
        var signingKey = CreateSigningKey(rsa);
        await using var application = await CreateApplicationAsync(
            signingKey,
            enabled: false);
        using var request = CreateAuthenticatedRequest(
            CreateToken(signingKey, Issuer, Audience, DateTime.UtcNow.AddMinutes(5)));

        using var response = await application.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Valid_token_establishes_trusted_tenant_context_after_membership_check()
    {
        using var rsa = RSA.Create(2048);
        var signingKey = CreateSigningKey(rsa);
        await using var application = await CreateApplicationAsync(signingKey);
        using var request = CreateAuthenticatedRequest(
            CreateToken(signingKey, Issuer, Audience, DateTime.UtcNow.AddMinutes(5)),
            "/tenant");
        request.Headers.Add(
            HttpTenantContextMiddleware.TenantSelectionHeader,
            TenantId.ToString());

        using var response = await application.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            $"{TenantId}|{Issuer}|{Subject}",
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public void Enabled_authentication_requires_absolute_authority_and_audience()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{OidcAuthenticationOptions.SectionName}:Enabled"] = "true",
                [$"{OidcAuthenticationOptions.SectionName}:Authority"] = "not-a-uri",
                [$"{OidcAuthenticationOptions.SectionName}:Audience"] = ""
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFinOpsOidcAuthentication(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<OidcAuthenticationOptions>>().Value);
    }

    [Fact]
    public void Https_metadata_rejects_http_authority_during_configuration_validation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{OidcAuthenticationOptions.SectionName}:Enabled"] = "true",
                [$"{OidcAuthenticationOptions.SectionName}:Authority"] =
                    "http://issuer.finops.test",
                [$"{OidcAuthenticationOptions.SectionName}:Audience"] = Audience,
                [$"{OidcAuthenticationOptions.SectionName}:RequireHttpsMetadata"] = "true"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFinOpsOidcAuthentication(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<OidcAuthenticationOptions>>().Value);
    }

    private static async Task<WebApplication> CreateApplicationAsync(
        SecurityKey trustedSigningKey,
        bool enabled = true,
        IConfigurationManager<OpenIdConnectConfiguration>? configurationManager = null)
    {
        var configurationValues = new Dictionary<string, string?>
        {
            [$"{OidcAuthenticationOptions.SectionName}:Enabled"] = enabled.ToString(),
            [$"{OidcAuthenticationOptions.SectionName}:Authority"] = Issuer,
            [$"{OidcAuthenticationOptions.SectionName}:Audience"] = Audience,
            [$"{OidcAuthenticationOptions.SectionName}:RequireHttpsMetadata"] = "true",
            [$"{OidcAuthenticationOptions.SectionName}:ClockSkewSeconds"] = "0"
        };

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(configurationValues);
        builder.Services.AddFinOpsOidcAuthentication(builder.Configuration);
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<TenantContext>();
        builder.Services.AddScoped<ITenantContext>(
            provider => provider.GetRequiredService<TenantContext>());
        builder.Services.AddScoped<ITenantContextInitializer>(
            provider => provider.GetRequiredService<TenantContext>());
        builder.Services.AddScoped<ITenantMembershipResolver, AcceptingMembershipResolver>();
        builder.Services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                var oidcConfiguration = new OpenIdConnectConfiguration
                {
                    Issuer = Issuer
                };
                oidcConfiguration.SigningKeys.Add(trustedSigningKey);
                options.ConfigurationManager =
                    configurationManager ??
                    new StaticConfigurationManager<OpenIdConnectConfiguration>(
                        oidcConfiguration);
            });

        var application = builder.Build();
        application.UseAuthentication();
        application.UseHttpTenantContext();
        application.UseAuthorization();
        application.MapGet("/protected", async context =>
            {
                var issuer = context.User.FindFirst("iss")?.Value;
                var subject = context.User.FindFirst("sub")?.Value;
                await context.Response.WriteAsync($"{issuer}|{subject}");
            })
            .RequireAuthorization();
        application.MapGet("/health", () => Results.Ok())
            .AllowAnonymous();
        application.MapGet("/tenant", async context =>
            {
                var tenant = context.RequestServices
                    .GetRequiredService<ITenantContext>()
                    .RequireCurrent();
                await context.Response.WriteAsync(
                    $"{tenant.TenantId}|{tenant.Issuer}|{tenant.Subject}");
            })
            .RequireAuthorization();
        await application.StartAsync();

        return application;
    }

    private static RsaSecurityKey CreateSigningKey(RSA rsa) =>
        new(rsa)
        {
            KeyId = Guid.NewGuid().ToString("N")
        };

    private static OpenIdConnectConfiguration CreateOidcConfiguration(
        SecurityKey signingKey)
    {
        var configuration = new OpenIdConnectConfiguration
        {
            Issuer = Issuer
        };
        configuration.SigningKeys.Add(signingKey);
        return configuration;
    }

    private static string CreateToken(
        SecurityKey signingKey,
        string issuer,
        string audience,
        DateTime expires)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity([new Claim("sub", Subject)]),
            IssuedAt = now.AddMinutes(-10),
            NotBefore = now.AddMinutes(-10),
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.RsaSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        string token,
        string path = "/protected")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization =
            new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);
        return request;
    }

    private sealed class AcceptingMembershipResolver : ITenantMembershipResolver
    {
        public Task<bool> IsActiveTenantAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(tenantId == TenantId);

        public Task<bool> HasActiveMembershipAsync(
            Guid tenantId,
            string issuer,
            string subject,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                tenantId == TenantId &&
                issuer == Issuer &&
                subject == Subject);
    }

    private sealed class SigningKeyRefreshConfigurationManager(
        OpenIdConnectConfiguration initialConfiguration,
        OpenIdConnectConfiguration refreshedConfiguration) :
        IConfigurationManager<OpenIdConnectConfiguration>
    {
        private OpenIdConnectConfiguration currentConfiguration =
            initialConfiguration;

        public int RefreshCount { get; private set; }

        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(
            CancellationToken cancel) =>
            Task.FromResult(currentConfiguration);

        public void RequestRefresh()
        {
            RefreshCount++;
            currentConfiguration = refreshedConfiguration;
        }
    }

    private sealed class UnavailableConfigurationManager :
        IConfigurationManager<OpenIdConnectConfiguration>
    {
        public int RequestCount { get; private set; }

        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(
            CancellationToken cancel)
        {
            RequestCount++;
            return Task.FromException<OpenIdConnectConfiguration>(
                new IOException("OIDC metadata endpoint is unavailable."));
        }

        public void RequestRefresh()
        {
        }
    }
}
