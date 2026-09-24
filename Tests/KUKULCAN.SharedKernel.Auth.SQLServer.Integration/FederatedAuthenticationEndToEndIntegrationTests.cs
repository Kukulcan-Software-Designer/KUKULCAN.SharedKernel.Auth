using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KUKULCAN.SharedKernel.Auth;
using KUKULCAN.SharedKernel.Results;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.SQLServer.Integration;

[TestFixture]
[NonParallelizable]
public sealed class FederatedAuthenticationEndToEndIntegrationTests
{
    private const string GoogleClientId = "google-client-id";
    private const string MicrosoftClientId = "microsoft-client-id";
    private const string MicrosoftTenantId = "11111111-2222-3333-4444-555555555555";
    private const string MicrosoftObjectId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
    private const string AppleClientId = "com.kukulcan.signin";

    private static readonly string[] Providers = ["Google", "Microsoft", "Apple"];

    [TestCaseSource(nameof(Providers))]
    public async Task AuthenticateAsync_WithRealCredentialValidatorAndPersistedIdentity_ReturnsUserAndAllTenantMemberships(
        string providerName)
    {
        using var key = RSA.Create(2048);

        var firstTenantId = Guid.NewGuid();
        var secondTenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var context = await AuthDbContextFactory.CreateAsync(
            SqlServerAuthenticationDatabase.ConnectionString,
            activeTenantId: firstTenantId);

        context.Users.Add(new AuthUserEntity
        {
            UserId = userId,
            Email = "federated-e2e@example.com",
            PasswordHash = "not-used"
        });

        context.TenantMemberships.AddRange(
            new AuthTenantMembershipEntity { UserId = userId, TenantId = firstTenantId },
            new AuthTenantMembershipEntity { UserId = userId, TenantId = secondTenantId });

        var subject = SubjectFor(providerName);
        context.FederatedIdentities.Add(new AuthFederatedIdentityEntity
        {
            Provider = providerName,
            Subject = subject,
            UserId = userId
        });

        await context.SaveChangesAsync();

        var client = CreateClient(providerName, key);
        var credential = CreateToken(providerName, key);

        IFederatedAuthenticationProvider provider = providerName switch
        {
            "Google" => new GoogleFederatedAuthenticationProvider(
                new GoogleCredentialValidator(GoogleClientId, client)),
            "Microsoft" => new MicrosoftFederatedAuthenticationProvider(
                new MicrosoftCredentialValidator(MicrosoftClientId, client)),
            "Apple" => new AppleFederatedAuthenticationProvider(
                new AppleCredentialValidator(AppleClientId, client)),
            _ => throw new ArgumentOutOfRangeException(nameof(providerName))
        };

        var service = new FederatedAuthenticationService(
            [provider],
            new FederatedUserStore(context));

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, credential));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.UserId, Is.EqualTo(userId));
        Assert.That(result.Value.Email, Is.EqualTo("federated-e2e@example.com"));
        Assert.That(
            result.Value.Tenants.Select(x => x.TenantId),
            Is.EquivalentTo(new[] { firstTenantId, secondTenantId }));
    }

    private static string SubjectFor(string providerName) =>
        providerName switch
        {
            "Google" => "google-subject",
            "Microsoft" => MicrosoftTenantId + ":" + MicrosoftObjectId,
            "Apple" => "apple-subject",
            _ => throw new ArgumentOutOfRangeException(nameof(providerName))
        };

    private static HttpClient CreateClient(string providerName, RSA key) =>
        new(new StubHandler(request =>
        {
            if (request.RequestUri?.AbsoluteUri.Contains("openid-configuration", StringComparison.OrdinalIgnoreCase) == true)
            {
                var issuer = providerName switch
                {
                    "Google" => "https://accounts.google.com",
                    "Microsoft" => "https://login.microsoftonline.com/" + MicrosoftTenantId + "/v2.0",
                    "Apple" => "https://appleid.apple.com",
                    _ => throw new ArgumentOutOfRangeException(nameof(providerName))
                };

                var jwksUri = providerName switch
                {
                    "Google" => "https://www.googleapis.com/oauth2/v3/certs",
                    "Microsoft" => "https://login.microsoftonline.com/common/discovery/v2.0/keys",
                    "Apple" => "https://appleid.apple.com/auth/keys",
                    _ => throw new ArgumentOutOfRangeException(nameof(providerName))
                };

                return Json(new { issuer, jwks_uri = jwksUri });
            }

            return Json(new { keys = new[] { Jwk(key, providerName + "-key") } });
        }));

    private static string CreateToken(string providerName, RSA key)
    {
        var claims = new Dictionary<string, object>
        {
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        };

        switch (providerName)
        {
            case "Google":
                claims["sub"] = "google-subject";
                claims["email"] = "federated-e2e@example.com";
                break;
            case "Microsoft":
                claims["sub"] = "pairwise-subject";
                claims["tid"] = MicrosoftTenantId;
                claims["oid"] = MicrosoftObjectId;
                claims["email"] = "federated-e2e@example.com";
                break;
            case "Apple":
                claims["sub"] = "apple-subject";
                claims["email"] = "federated-e2e@example.com";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(providerName));
        }

        var issuer = providerName switch
        {
            "Google" => "https://accounts.google.com",
            "Microsoft" => "https://login.microsoftonline.com/" + MicrosoftTenantId + "/v2.0",
            "Apple" => "https://appleid.apple.com",
            _ => throw new ArgumentOutOfRangeException(nameof(providerName))
        };

        var audience = providerName switch
        {
            "Google" => GoogleClientId,
            "Microsoft" => MicrosoftClientId,
            "Apple" => AppleClientId,
            _ => throw new ArgumentOutOfRangeException(nameof(providerName))
        };

        claims["iss"] = issuer;
        claims["aud"] = audience;
        claims["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var kid = providerName + "-key";
        var header = B64(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { alg = "RS256", kid, typ = "JWT" })));
        var payload = B64(JsonSerializer.SerializeToUtf8Bytes(claims));
        var data = Encoding.ASCII.GetBytes(header + "." + payload);
        var signature = key.SignData(
            data,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return header + "." + payload + "." + B64(signature);
    }

    private static object Jwk(RSA key, string kid)
    {
        var parameters = key.ExportParameters(false);

        return new
        {
            kty = "RSA",
            use = "sig",
            alg = "RS256",
            kid,
            n = B64(parameters.Modulus!),
            e = B64(parameters.Exponent!)
        };
    }

    private static string B64(byte[] value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static HttpResponseMessage Json(object value) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(value),
                Encoding.UTF8,
                "application/json")
        };

    private sealed class StubHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
