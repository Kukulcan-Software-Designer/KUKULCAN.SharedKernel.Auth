
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using KUKULCAN.SharedKernel.Auth;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.UnitTests;

[TestFixture]
public sealed class AppleCredentialValidatorTests
{
    private const string ClientId = "com.kukulcan.signin";
    private const string Issuer = "https://appleid.apple.com";

    [Test]
    public async Task ValidateAsync_WithValidIdentityToken_ReturnsSubAndEmail()
    {
        using var key = RSA.Create(2048);
        using var client = CreateClient(key);
        var token = CreateToken(key, "apple-key", Issuer, ClientId, new Dictionary<string, object>
        {
            ["sub"] = "apple-subject",
            ["email"] = "user@privaterelay.appleid.com",
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        });

        var result = await new AppleCredentialValidator(ClientId, client).ValidateAsync(token);

        result.IsSuccess.Should().BeTrue();
        result.Value.Provider.Should().Be("Apple");
        result.Value.Subject.Should().Be("apple-subject");
        result.Value.Email.Should().Be("user@privaterelay.appleid.com");
    }

    [TestCase("invalid-issuer")]
    [TestCase("invalid-audience")]
    [TestCase("expired")]
    [TestCase("missing-sub")]
    public async Task ValidateAsync_WithInvalidClaims_RejectsToken(string scenario)
    {
        using var key = RSA.Create(2048);
        using var client = CreateClient(key);
        var issuer = scenario == "invalid-issuer" ? "https://evil.example.com" : Issuer;
        var audience = scenario == "invalid-audience" ? "another-client" : ClientId;
        var claims = new Dictionary<string, object>
        {
            ["exp"] = scenario == "expired"
                ? DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds()
                : DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        };
        if (scenario != "missing-sub")
            claims["sub"] = "apple-subject";

        var token = CreateToken(key, "apple-key", issuer, audience, claims);
        var result = await new AppleCredentialValidator(ClientId, client).ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_WithUntrustedSignature_RejectsToken()
    {
        using var signingKey = RSA.Create(2048);
        using var trustedKey = RSA.Create(2048);
        using var client = CreateClient(trustedKey);

        var token = CreateToken(signingKey, "apple-key", Issuer, ClientId,
            new Dictionary<string, object>
            {
                ["sub"] = "apple-subject",
                ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
            });

        var result = await new AppleCredentialValidator(ClientId, client).ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
    }

    private static HttpClient CreateClient(RSA key) =>
        new(new StubHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri.Contains("openid-configuration", StringComparison.OrdinalIgnoreCase) == true)
                return Json(new { issuer = Issuer, jwks_uri = "https://appleid.apple.com/auth/keys" });

            return Json(new { keys = new[] { Jwk(key, "apple-key") } });
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

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
