using KUKULCAN.SharedKernel.Abstractions;
using KUKULCAN.SharedKernel.Auth.Entities;
using KUKULCAN.SharedKernel.Database;
using KUKULCAN.SharedKernel.Database.Abstractions;
using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.Database.Interceptors;
using KUKULCAN.SharedKernel.DomainEvents.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KUKULCAN.SharedKernel.Auth.Persistence;

/// <summary>Database context for authentication persistence.</summary>
public sealed class AuthDbContext : KukulcanDbContextBase
{
    /// <summary>Initializes the authentication database context.</summary>
    public AuthDbContext(
        IOptions<KukulcanDatabaseOptions> options,
        ITenantContext tenantContext,
        IClock clock,
        IDomainEventDispatcher domainEventDispatcher,
        SlowQueryInterceptor? slowQueryInterceptor = null)
        : base(options, tenantContext, clock, domainEventDispatcher, slowQueryInterceptor)
    {
    }

    /// <summary>Gets the persisted local users.</summary>
    public DbSet<AuthUserEntity> Users => Set<AuthUserEntity>();

    /// <summary>Gets the persisted tenant memberships for local users.</summary>
    public DbSet<AuthTenantMembershipEntity> TenantMemberships => Set<AuthTenantMembershipEntity>();

    /// <summary>Gets the persisted federated identities.</summary>
    public DbSet<AuthFederatedIdentityEntity> FederatedIdentities => Set<AuthFederatedIdentityEntity>();

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        CanonicalizeUserEmails();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override int SaveChanges()
        => SaveChanges(acceptAllChangesOnSuccess: true);

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        CanonicalizeUserEmails();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    private void CanonicalizeUserEmails()
    {
        foreach (var entry in ChangeTracker.Entries<AuthUserEntity>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.Email = entry.Entity.Email.Trim().ToLowerInvariant();
        }
    }
}
