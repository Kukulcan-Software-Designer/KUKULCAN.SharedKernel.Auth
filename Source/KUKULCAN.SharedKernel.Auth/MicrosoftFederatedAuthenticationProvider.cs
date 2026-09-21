using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.Auth;

/// <summary>Authenticates users through Microsoft federated credentials.</summary>
public sealed class MicrosoftFederatedAuthenticationProvider : IFederatedAuthenticationProvider
{
    private static readonly Error FederatedProviderMismatch = new(
        "Auth.FederatedProviderMismatch",
        "The federated authentication request targets another provider.");

    private readonly IFederatedCredentialValidator _validator;

    /// <summary>Initializes the Microsoft federated authentication provider.</summary>
    public MicrosoftFederatedAuthenticationProvider(IFederatedCredentialValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _validator = validator;
    }

    /// <inheritdoc />
    public string Provider => "Microsoft";

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
