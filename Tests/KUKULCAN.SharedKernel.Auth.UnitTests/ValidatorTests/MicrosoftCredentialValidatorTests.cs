using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.UnitTests.ValidatorTests;

[TestFixture]
public sealed class MicrosoftCredentialValidatorTests
{
    private const string ClientId = "microsoft-client-id";
    private const string TenantId = "11111111-2222-3333-4444-555555555555";
    private const string Issuer = "https://login.microsoftonline.com/" + TenantId + "/v2.0";
    private const string ObjectId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

    [Test]
    public async Task ValidateAsync_WithValidToken_UsesTidAndOidAsSubject()
    {
        using var key = RSA.Create(2048);
        using var client = CreateClient(key);
        var token = CreateToken(key, "microsoft-key", Issuer, ClientId, new Dictionary<string, object>
        {
            ["sub"] = "pairwise-subject",
            ["tid"] = TenantId,
            ["oid"] = ObjectId,
            ["email"] = "user@example.com",
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        });

        var result = await new MicrosoftCredentialValidator(ClientId, client).ValidateAsync(token);

        result.IsSuccess.Should().BeTrue();
        result.Value.Provider.Should().Be("Microsoft");
        result.Value.Subject.Should().Be(TenantId + ":" + ObjectId);
        result.Value.Email.Should().Be("user@example.com");
    }

    [TestCase("invalid-issuer")]
    [TestCase("invalid-audience")]
    [TestCase("invalid-tid")]
    [TestCase("missing-oid")]
    [TestCase("expired")]
    public async Task ValidateAsync_WithInvalidClaims_RejectsToken(string scenario)
    {
        using var key = RSA.Create(2048);
        using var client = CreateClient(key);

        var issuer = scenario == "invalid-issuer"
            ? "https://login.microsoftonline.com/99999999-8888-7777-6666-555555555555/v2.0"
            : Issuer;
        var audience = scenario == "invalid-audience" ? "another-client" : ClientId;
        var tid = scenario == "invalid-tid"
            ? "99999999-8888-7777-6666-555555555555"
            : TenantId;

        var claims = new Dictionary<string, object>
        {
            ["tid"] = tid,
            ["exp"] = scenario == "expired"
                ? DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds()
                : DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        };
        if (scenario != "missing-oid")
            claims["oid"] = ObjectId;

        var token = CreateToken(key, "microsoft-key", issuer, audience, claims);
        var result = await new MicrosoftCredentialValidator(ClientId, client).ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_WithUntrustedSignature_RejectsToken()
    {
        using var signingKey = RSA.Create(2048);
        using var trustedKey = RSA.Create(2048);
        using var client = CreateClient(trustedKey);

        var token = CreateToken(signingKey, "microsoft-key", Issuer, ClientId,
            new Dictionary<string, object>
            {
                ["tid"] = TenantId,
                ["oid"] = ObjectId,
                ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
            });

        var result = await new MicrosoftCredentialValidator(ClientId, client).ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
    }

    [TestCase("missing-configuration")]
    [TestCase("missing-issuer")]
    [TestCase("missing-jwks-uri")]
    public async Task ValidateAsync_WithInvalidOpenIdConfiguration_RejectsToken(string scenario)
    {
        using var key = RSA.Create(2048);
        using var client = CreateClient(
            key,
            configuration: scenario == "missing-configuration"
                ? null
                : scenario switch
                {
                    "missing-issuer" => new { issuer = "", jwks_uri = "https://login.microsoftonline.com/common/discovery/v2.0/keys" },
                    "missing-jwks-uri" => new { issuer = "https://login.microsoftonline.com/{tenantid}/v2.0", jwks_uri = "" },
                    _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
                },
            configurationIsExplicit: true);

        var token = CreateToken(key, "microsoft-key", Issuer, ClientId, new Dictionary<string, object>
        {
            ["tid"] = TenantId,
            ["oid"] = ObjectId,
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        });

        var result = await new MicrosoftCredentialValidator(ClientId, client).ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_WithEmptyJwks_RejectsToken()
    {
        using var key = RSA.Create(2048);
        using var client = CreateClient(key, jwks: new { keys = Array.Empty<object>() });

        var token = CreateToken(key, "microsoft-key", Issuer, ClientId, new Dictionary<string, object>
        {
            ["tid"] = TenantId,
            ["oid"] = ObjectId,
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        });

        var result = await new MicrosoftCredentialValidator(ClientId, client).ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_WithNoUsableJwksKeys_RejectsToken()
    {
        using var key = RSA.Create(2048);
        using var client = CreateClient(key, jwks: new
        {
            keys = new[]
            {
                new
                {
                    kty = "EC",
                    use = "sig",
                    alg = "ES256",
                    kid = "unsupported-key",
                    n = "",
                    e = ""
                }
            }
        });

        var token = CreateToken(key, "microsoft-key", Issuer, ClientId, new Dictionary<string, object>
        {
            ["tid"] = TenantId,
            ["oid"] = ObjectId,
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        });

        var result = await new MicrosoftCredentialValidator(ClientId, client).ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
    }

    private static HttpClient CreateClient(
        RSA key,
        object? configuration = null,
        object? jwks = null,
        bool configurationIsExplicit = false) =>
        new(new StubHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri.Contains("openid-configuration", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (configurationIsExplicit)
                {
                    if (configuration is null)
                        return JsonNull();

                    return Json(configuration);
                }

                return Json(new
                {
                    issuer = "https://login.microsoftonline.com/{tenantid}/v2.0",
                    jwks_uri = "https://login.microsoftonline.com/common/discovery/v2.0/keys"
                });
            }

            return Json(jwks ?? new { keys = new[] { Jwk(key, "microsoft-key") } });
        }));

    private static object Jwk(RSA key, string kid)
    {
        var p = key.ExportParameters(false);
        return new { kty = "RSA", use = "sig", alg = "RS256", kid, n = B64(p.Modulus!), e = B64(p.Exponent!) };
    }

    private static string CreateToken(RSA key, string kid, string issuer, string audience, IDictionary<string, object> claims)
    {
        claims["iss"] = issuer;
        claims["aud"] = audience;
        claims["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = B64(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { alg = "RS256", kid, typ = "JWT" })));
        var payload = B64(JsonSerializer.SerializeToUtf8Bytes(claims));
        var data = Encoding.ASCII.GetBytes(header + "." + payload);
        return header + "." + payload + "." + B64(key.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    private static string B64(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static HttpResponseMessage Json(object value) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };

    private static HttpResponseMessage JsonNull() =>
        new(HttpStatusCode.OK) { Content = new StringContent("null", Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
