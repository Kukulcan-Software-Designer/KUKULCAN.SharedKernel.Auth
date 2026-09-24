namespace KUKULCAN.SharedKernel.Auth;

/// <summary>Provides password hashing and verification using ASP.NET Core Identity's password hasher.</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _hasher = new();

    /// <inheritdoc />
    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return _hasher.HashPassword(new object(), password);
    }

    /// <inheritdoc />
    public bool Verify(string password, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(passwordHash);

        try
        {
            var result = _hasher.VerifyHashedPassword(new object(), passwordHash, password);

            return result is
                Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success or
                Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
