using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using KUKULCAN.SharedKernel.Auth.Authentication.Federated;

namespace KUKULCAN.SharedKernel.Auth.UnitTests.ValidatorTests;

[TestFixture]
public sealed class GoogleCredentialValidatorTests
{
    private const string ClientId = "google-client-id";
    private const string Issuer = "https://accounts.google.com";

    [Test]
    public async Task ValidateAsync_WithValidToken_ReturnsSubAndEmail()
    {
        using var key = RSA.Create(2048);
        using var client = CreateClient(key, Issuer);
        var token = CreateToken(key, "google-key", Issuer, ClientId, new Dictionary<string, object>
        {
            ["sub"] = "google-subject",
            ["email"] = "user@example.com",
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        });

        var result = await new GoogleCredentialValidator(ClientId, client).ValidateAsync(token);

        result.IsSuccess.Should().BeTrue();
        result.Value.Provider.Should().Be("Google");
        result.Value.Subject.Should().Be("google-subject");
        result.Value.Email.Should().Be("user@example.com");
    }

    [TestCase("invalid-issuer")]
    [TestCase("invalid-audience")]
    [TestCase("expired")]
    [TestCase("missing-sub")]
    public async Task ValidateAsync_WithInvalidClaims_RejectsToken(string scenario)
    {
        using var key = RSA.Create(2048);
        using var client = CreateClient(key, Issuer);

        var issuer = scenario == "invalid-issuer" ? "https://evil.example.com" : Issuer;
        var audience = scenario == "invalid-audience" ? "another-client" : ClientId;
        var claims = new Dictionary<string, object>
        {
            ["exp"] = scenario == "expired"
                ? DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds()
                : DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        };
        if (scenario != "missing-sub")
            claims["sub"] = "google-subject";

        var token = CreateToken(key, "google-key", issuer, audience, claims);
        var result = await new GoogleCredentialValidator(ClientId, client).ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_WithUntrustedSignature_RejectsToken()
    {
        using var signingKey = RSA.Create(2048);
        using var trustedKey = RSA.Create(2048);
        using var client = CreateClient(trustedKey, Issuer);

        var token = CreateToken(signingKey, "google-key", Issuer, ClientId,
            new Dictionary<string, object>
            {
                ["sub"] = "google-subject",
                ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
            });

        var result = await new GoogleCredentialValidator(ClientId, client).ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
    }

    private static HttpClient CreateClient(RSA key, string issuer)
    {
        return new HttpClient(new StubHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri.Contains("openid-configuration", StringComparison.OrdinalIgnoreCase) == true)
                return Json(new { issuer, jwks_uri = "https://www.googleapis.com/oauth2/v3/certs" });

            return Json(new { keys = new[] { Jwk(key, "google-key") } });
        }));
    }

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
