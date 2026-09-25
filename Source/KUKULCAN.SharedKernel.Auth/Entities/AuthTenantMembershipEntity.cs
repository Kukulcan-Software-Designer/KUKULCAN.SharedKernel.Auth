namespace KUKULCAN.SharedKernel.Auth.Entities;

/// <summary>Represents a persisted membership of a local user in a tenant.</summary>
public sealed class AuthTenantMembershipEntity
{
    /// <summary>Gets or sets the local user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the tenant identifier.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the associated local user.</summary>
    public AuthUserEntity User { get; set; } = null!;
}
