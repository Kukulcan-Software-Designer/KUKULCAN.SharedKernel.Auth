using Microsoft.EntityFrameworkCore;

namespace KUKULCAN.SharedKernel.Auth;

/// <summary>Persists and retrieves local users linked to federated identities.</summary>
public sealed class FederatedUserStore : IFederatedUserStore
{
    private readonly AuthDbContext _context;

    /// <summary>Initializes the federated user store.</summary>
    public FederatedUserStore(AuthDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task<LocalUser?> FindByFederatedIdentityAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken = default)
    {
        var identity = await _context.FederatedIdentities
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.Provider == provider && entity.Subject == subject,
                cancellationToken)
            .ConfigureAwait(false);

        if (identity is null)
            return null;

        var user = await _context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(entity => entity.UserId == identity.UserId, cancellationToken)
            .ConfigureAwait(false);

        var tenantMemberships = await _context.TenantMemberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.UserId == user.UserId)
            .Select(entity => entity.TenantId)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return new LocalUser(
            user.UserId,
            user.Email,
            user.PasswordHash,
            tenantMemberships.Select(tenantId => new TenantMembership(tenantId)).ToArray());
    }
}
