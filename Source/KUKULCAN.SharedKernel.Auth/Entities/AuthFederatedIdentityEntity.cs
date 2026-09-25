namespace KUKULCAN.SharedKernel.Auth;

/// <summary>Represents a persisted external identity linked to a local authentication user.</summary>
public sealed class AuthFederatedIdentityEntity
{
    /// <summary>Gets or sets the federated identity provider.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the stable subject identifier issued by the provider.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Gets or sets the associated local user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the associated local user.</summary>
    public AuthUserEntity User { get; set; } = null!;
}
