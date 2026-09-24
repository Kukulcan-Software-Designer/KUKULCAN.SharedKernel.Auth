namespace KUKULCAN.SharedKernel.Auth;

/// <summary>Persists and retrieves local authentication users.</summary>
public sealed class LocalUserStore : ILocalUserStore
{
    private readonly AuthDbContext _context;

    /// <summary>Initializes the local user store.</summary>
    public LocalUserStore(AuthDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task<LocalUser?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Email == email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return null;

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
