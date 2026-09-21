using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.Auth;

/// <summary>Authenticates users through Apple federated credentials.</summary>
public sealed class AppleFederatedAuthenticationProvider : IFederatedAuthenticationProvider
{
    private static readonly Error FederatedProviderMismatch = new(
        "Auth.FederatedProviderMismatch",
        "The federated authentication request targets another provider.");

    private readonly IFederatedCredentialValidator _validator;

    /// <summary>Initializes the Apple federated authentication provider.</summary>
    public AppleFederatedAuthenticationProvider(IFederatedCredentialValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _validator = validator;
    }

    /// <inheritdoc />
    public string Provider => "Apple";

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
