using KUKULCAN.SharedKernel.Abstractions;
using KUKULCAN.SharedKernel.Database;
using KUKULCAN.SharedKernel.Database.Abstractions;
using KUKULCAN.SharedKernel.DomainEvents.Abstractions;
using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.Database.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KUKULCAN.SharedKernel.Auth;

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
}
