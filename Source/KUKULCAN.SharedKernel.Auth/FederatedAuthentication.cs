using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.Auth;

/// <summary>Represents a federated authentication request.</summary>
public sealed record FederatedAuthenticationRequest(string Provider, string Credential);

/// <summary>Represents an identity returned by a federated authentication provider.</summary>
public sealed record FederatedIdentity(string Provider, string Subject, string? Email);

/// <summary>Authenticates credentials against an external identity provider.</summary>
public interface IFederatedAuthenticationProvider
{
    /// <summary>Gets the provider name.</summary>
    string Provider { get; }

    /// <summary>Authenticates an external credential and returns its stable identity.</summary>
    Task<Result<FederatedIdentity>> AuthenticateAsync(
        FederatedAuthenticationRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Provides access to local users linked to federated identities.</summary>
public interface IFederatedUserStore
{
    /// <summary>Finds a local user by provider and stable external subject.</summary>
    Task<LocalUser?> FindByFederatedIdentityAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken = default);
}

/// <summary>Authenticates users through registered federated identity providers.</summary>
public sealed class FederatedAuthenticationService
{
    private static readonly Error UnsupportedFederatedProvider = new(
        "Auth.UnsupportedFederatedProvider",
        "The requested federated authentication provider is not registered.");

    private static readonly Error FederatedIdentityNotLinked = new(
        "Auth.FederatedIdentityNotLinked",
        "The federated identity is not linked to a local user.");

    private static readonly Error FederatedProviderMismatch = new(
        "Auth.FederatedProviderMismatch",
        "The federated identity provider does not match the requested provider.");

    private readonly IReadOnlyCollection<IFederatedAuthenticationProvider> _providers;
    private readonly IFederatedUserStore _userStore;

    /// <summary>Initializes the federated authentication service.</summary>
    public FederatedAuthenticationService(
        IEnumerable<IFederatedAuthenticationProvider> providers,
        IFederatedUserStore userStore)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(userStore);

        _providers = providers.ToArray();
        _userStore = userStore;
    }

    /// <summary>Authenticates a federated credential and returns all tenant memberships.</summary>
    public async Task<Result<AuthenticatedUser>> AuthenticateAsync(
        FederatedAuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            throw new ArgumentException("Provider is required.", nameof(request));
        }

        var provider = _providers.FirstOrDefault(item =>
            string.Equals(item.Provider, request.Provider, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            return Result<AuthenticatedUser>.Failure(UnsupportedFederatedProvider);
        }

        var identityResult = await provider.AuthenticateAsync(request, cancellationToken);

        if (identityResult.IsFailure)
        {
            return Result<AuthenticatedUser>.Failure(identityResult.Error);
        }

        var identity = identityResult.Value;

        if (!string.Equals(identity.Provider, request.Provider, StringComparison.OrdinalIgnoreCase))
        {
            return Result<AuthenticatedUser>.Failure(FederatedProviderMismatch);
        }

        var user = await _userStore.FindByFederatedIdentityAsync(
            request.Provider,
            identity.Subject,
            cancellationToken);

        if (user is null)
        {
            return Result<AuthenticatedUser>.Failure(FederatedIdentityNotLinked);
        }

        return Result<AuthenticatedUser>.Success(
            new AuthenticatedUser(
                user.UserId,
                user.Email,
                user.Tenants.ToArray()));
    }
}
