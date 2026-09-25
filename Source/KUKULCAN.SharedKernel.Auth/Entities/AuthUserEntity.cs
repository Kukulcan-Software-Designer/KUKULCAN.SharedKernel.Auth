namespace KUKULCAN.SharedKernel.Auth.Entities;

/// <summary>Represents the persisted local authentication identity.</summary>
public sealed class AuthUserEntity
{
    /// <summary>Gets or sets the stable user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the canonical email address used as the local login identifier.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the password hash.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's tenant memberships.</summary>
    public ICollection<AuthTenantMembershipEntity> TenantMemberships { get; set; } = [];

    /// <summary>Gets or sets the user's federated identities.</summary>
    public ICollection<AuthFederatedIdentityEntity> FederatedIdentities { get; set; } = [];
}
