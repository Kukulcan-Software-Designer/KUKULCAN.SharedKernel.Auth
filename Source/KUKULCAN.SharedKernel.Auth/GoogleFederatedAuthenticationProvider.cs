using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.Auth;

/// <summary>Authenticates users through Google federated credentials.</summary>
public sealed class GoogleFederatedAuthenticationProvider : IFederatedAuthenticationProvider
{
    private static readonly Error FederatedProviderMismatch = new(
        "Auth.FederatedProviderMismatch",
        "The federated authentication request targets another provider.");

    private readonly IFederatedCredentialValidator _validator;

    /// <summary>Initializes the Google federated authentication provider.</summary>
    public GoogleFederatedAuthenticationProvider(IFederatedCredentialValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _validator = validator;
    }

    /// <inheritdoc />
    public string Provider => "Google";

    /// <inheritdoc />
    public Task<Result<FederatedIdentity>> AuthenticateAsync(
        FederatedAuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(request.Provider, Provider, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(Result<FederatedIdentity>.Failure(FederatedProviderMismatch));
        }

        if (string.IsNullOrWhiteSpace(request.Credential))
        {
            throw new ArgumentException("Credential is required.", nameof(request));
        }

        return _validator.ValidateAsync(request.Credential, cancellationToken);
    }
}
