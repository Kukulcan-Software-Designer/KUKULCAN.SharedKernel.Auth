using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.Auth;

/// <summary>Represents a local authentication request.</summary>
public sealed record LocalAuthenticationRequest(string Email, string Password);

/// <summary>Represents a tenant to which an authenticated user belongs.</summary>
public sealed record TenantMembership(Guid TenantId);

/// <summary>Represents a local user credential record and its tenant memberships.</summary>
public sealed record LocalUser(
    Guid UserId,
    string Email,
    string PasswordHash,
    IReadOnlyCollection<TenantMembership> Tenants);

/// <summary>Represents an authenticated user and all tenants to which the user belongs.</summary>
public sealed record AuthenticatedUser(
    Guid UserId,
    string Email,
    IReadOnlyCollection<TenantMembership> Tenants);

/// <summary>Provides access to users authenticated with local credentials.</summary>
public interface ILocalUserStore
{
    /// <summary>Finds a user by email address.</summary>
    Task<LocalUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
}

/// <summary>Verifies password values against stored password hashes.</summary>
public interface IPasswordHasher
{
    /// <summary>Determines whether a password matches a stored hash.</summary>
    bool Verify(string password, string passwordHash);
}

/// <summary>Authenticates users with credentials stored by the application.</summary>
public sealed class LocalAuthenticationService
{
    private static readonly Error InvalidCredentials = new(
        "Auth.InvalidCredentials",
        "The supplied credentials are invalid.");

    private static readonly Error NoTenantAccess = new(
        "Auth.NoTenantAccess",
        "The user does not belong to any tenant.");

    private readonly ILocalUserStore _userStore;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>Initializes the local authentication service.</summary>
    public LocalAuthenticationService(ILocalUserStore userStore, IPasswordHasher passwordHasher)
    {
        ArgumentNullException.ThrowIfNull(userStore);
        ArgumentNullException.ThrowIfNull(passwordHasher);

        _userStore = userStore;
        _passwordHasher = passwordHasher;
    }

    /// <summary>Authenticates a user using local credentials and returns all tenant memberships.</summary>
    public async Task<Result<AuthenticatedUser>> AuthenticateAsync(
        LocalAuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userStore.FindByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<AuthenticatedUser>.Failure(InvalidCredentials);
        }

        if (user.Tenants.Count == 0)
        {
            return Result<AuthenticatedUser>.Failure(NoTenantAccess);
        }

        return Result<AuthenticatedUser>.Success(
            new AuthenticatedUser(
                user.UserId,
                user.Email,
                user.Tenants.ToArray()));
    }

    private static void ValidateRequest(LocalAuthenticationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required.", nameof(request));
        }
    }
}
