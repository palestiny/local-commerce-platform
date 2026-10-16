using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

public sealed class AuthEndpointTests : IClassFixture<AuthSpikeFactory>
{
    private readonly HttpClient _client;
    public AuthEndpointTests(AuthSpikeFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_is_anonymous()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_rejects_missing_token()
    {
        var response = await _client.GetAsync("/whoami");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_rejects_malformed_token()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/whoami");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-jwt");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_accepts_valid_signed_token_and_returns_only_subject()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/whoami");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthSpikeFactory.CreateToken());
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\\"subject\\":\\"test-user-123\\"}", body);
    }

    [Theory]
    [InlineData("https://wrong-issuer.test", "local-commerce-test", false)]
    [InlineData("https://issuer.test", "wrong-audience", false)]
    [InlineData("https://issuer.test", "local-commerce-test", true)]
    public async Task Protected_endpoint_rejects_invalid_issuer_audience_or_expiry(string issuer, string audience, bool expired)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/whoami");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthSpikeFactory.CreateToken(issuer, audience, expired));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_rejects_token_signed_with_untrusted_key()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/whoami");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            AuthSpikeFactory.CreateToken(invalidSignature: true));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public sealed class AuthSpikeFactory : WebApplicationFactory<Program>
{
    private const string TestSecret = "local-test-signing-key-at-least-32-bytes-long";
    private static readonly SymmetricSecurityKey SigningKey = new(Encoding.UTF8.GetBytes(TestSecret));
    private static readonly SymmetricSecurityKey UntrustedSigningKey =
        new(Encoding.UTF8.GetBytes("different-untrusted-signing-key-32-bytes"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Authentication:Authority", "https://issuer.test");
        builder.UseSetting("Authentication:Audience", "local-commerce-test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "https://issuer.test",
                ["Authentication:Audience"] = "local-commerce-test"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.MetadataAddress = null;
                options.ConfigurationManager = null;
                options.Audience = null;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "https://issuer.test",
                    ValidateAudience = true,
                    ValidAudience = "local-commerce-test",
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    IssuerSigningKey = SigningKey,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
        });
    }

    public static string CreateToken(
        string issuer = "https://issuer.test",
        string audience = "local-commerce-test",
        bool expired = false,
        bool invalidSignature = false)
    {
        var now = DateTime.UtcNow;
        var tokenSigningKey = invalidSignature ? UntrustedSigningKey : SigningKey;
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: new[] { new Claim("sub", "test-user-123") },
            notBefore: expired ? now.AddMinutes(-10) : now.AddSeconds(-5),
            expires: expired ? now.AddMinutes(-5) : now.AddMinutes(5),
            signingCredentials: new SigningCredentials(tokenSigningKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class AuthenticationConfigurationTests
{
    [Theory]
    [InlineData(null, "local-commerce-test")]
    [InlineData("", "local-commerce-test")]
    [InlineData("not-a-url", "local-commerce-test")]
    [InlineData("http://issuer.test", "local-commerce-test")]
    [InlineData("https://issuer.test", null)]
    [InlineData("https://issuer.test", "")]
    public void Invalid_or_missing_authentication_configuration_fails_closed(string? authority, string? audience)
    {
        Assert.Throws<InvalidOperationException>(() =>
            AuthenticationConfiguration.Validate(authority, audience));
    }

    [Fact]
    public void Valid_https_authority_and_audience_are_accepted()
    {
        AuthenticationConfiguration.Validate("https://issuer.test", "local-commerce-test");
    }
}
