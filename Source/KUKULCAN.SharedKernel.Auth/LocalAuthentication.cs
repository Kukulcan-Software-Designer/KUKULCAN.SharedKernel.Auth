using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.Auth;

/// <summary>Represents a local authentication request.</summary>
public sealed record LocalAuthenticationRequest(string Email, string Password);

/// <summary>Represents a local user credential record.</summary>
public sealed record LocalUser(Guid UserId, string Email, string PasswordHash);

/// <summary>Represents an authenticated user.</summary>
public sealed record AuthenticatedUser(Guid UserId, string Email);

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

    /// <summary>Authenticates a user using local credentials.</summary>
    public async Task<Result<AuthenticatedUser>> AuthenticateAsync(
        LocalAuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userStore.FindByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<AuthenticatedUser>.Failure(InvalidCredentials);
        }

        return Result<AuthenticatedUser>.Success(new AuthenticatedUser(user.UserId, user.Email));
    }
}
