using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.Auth.Authentication.Federated;

public sealed class AppleCredentialValidator : IFederatedCredentialValidator
{
    private const string OpenIdConfigurationUrl =
        "https://appleid.apple.com/.well-known/openid-configuration";

    private static readonly Error InvalidFederatedCredential = new(
        "Auth.FederatedCredentialInvalid",
        "The federated credential is invalid.");

    private readonly string _clientId;
    private readonly HttpClient _httpClient;
    private readonly JwtSecurityTokenHandler _tokenHandler = new()
    {
        MapInboundClaims = false
    };

    public AppleCredentialValidator(string clientId, HttpClient httpClient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNull(httpClient);

        _clientId = clientId;
        _httpClient = httpClient;
    }

    public async Task<Result<FederatedIdentity>> ValidateAsync(
        string credential,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credential);

        try
        {
            var configuration = await GetOpenIdConfigurationAsync(cancellationToken);
            var signingKeys = await GetSigningKeysAsync(configuration.JwksUri, cancellationToken);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration.Issuer,
                ValidateAudience = true,
                ValidAudience = _clientId,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                IssuerSigningKeys = signingKeys,
                ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 }
            };

            var principal = _tokenHandler.ValidateToken(
                credential,
                validationParameters,
                out _);

            var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(subject))
                return Result<FederatedIdentity>.Failure(InvalidFederatedCredential);

            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email);

            return Result<FederatedIdentity>.Success(
                new FederatedIdentity("Apple", subject, email));
        }
        catch (SecurityTokenException)
        {
            return Result<FederatedIdentity>.Failure(InvalidFederatedCredential);
        }
        catch (ArgumentException)
        {
            return Result<FederatedIdentity>.Failure(InvalidFederatedCredential);
        }
        catch (InvalidOperationException)
        {
            return Result<FederatedIdentity>.Failure(InvalidFederatedCredential);
        }
        catch (JsonException)
        {
            return Result<FederatedIdentity>.Failure(InvalidFederatedCredential);
        }
    }

    private async Task<OpenIdConfiguration> GetOpenIdConfigurationAsync(
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            OpenIdConfigurationUrl,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var configuration = await JsonSerializer.DeserializeAsync<OpenIdConfiguration>(
            stream,
            cancellationToken: cancellationToken);

        if (configuration is null ||
            string.IsNullOrWhiteSpace(configuration.Issuer) ||
            string.IsNullOrWhiteSpace(configuration.JwksUri))
        {
            throw new JsonException("Apple OpenID Connect configuration is invalid.");
        }

        return configuration;
    }

    private async Task<IReadOnlyCollection<SecurityKey>> GetSigningKeysAsync(
        string jwksUri,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            jwksUri,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var jwks = await JsonSerializer.DeserializeAsync<JsonWebKeySetResponse>(
            stream,
            cancellationToken: cancellationToken);

        if (jwks?.Keys is null || jwks.Keys.Count == 0)
            throw new JsonException("Apple JWKS does not contain signing keys.");

        var keys = new List<SecurityKey>();

        foreach (var key in jwks.Keys)
        {
            if (!string.Equals(key.Kty, "RSA", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(key.Alg, SecurityAlgorithms.RsaSha256, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(key.Kid) ||
                string.IsNullOrWhiteSpace(key.N) ||
                string.IsNullOrWhiteSpace(key.E))
                continue;

            var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = Base64UrlDecode(key.N),
                Exponent = Base64UrlDecode(key.E)
            });

            keys.Add(new RsaSecurityKey(rsa) { KeyId = key.Kid });
        }

        if (keys.Count == 0)
            throw new SecurityTokenException("No trusted RSA signing keys were found.");

        return keys;
    }

    private sealed record OpenIdConfiguration(
        [property: JsonPropertyName("issuer")] string Issuer,
        [property: JsonPropertyName("jwks_uri")] string JwksUri);

    private sealed record JsonWebKeySetResponse(
        [property: JsonPropertyName("keys")] List<JsonWebKey> Keys);

    private sealed record JsonWebKey(
        [property: JsonPropertyName("kty")] string Kty,
        [property: JsonPropertyName("use")] string Use,
        [property: JsonPropertyName("alg")] string Alg,
        [property: JsonPropertyName("kid")] string Kid,
        [property: JsonPropertyName("n")] string N,
        [property: JsonPropertyName("e")] string E);

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');

        padded += padded.Length % 4 switch
        {
            0 => string.Empty,
            2 => "==",
            3 => "=",
            _ => throw new FormatException("Invalid Base64Url value.")
        };

        return Convert.FromBase64String(padded);
    }
}
