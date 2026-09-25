using KUKULCAN.SharedKernel.Abstractions;
using KUKULCAN.SharedKernel.Auth.Persistence;
using KUKULCAN.SharedKernel.Database.Abstractions;
using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.DomainEvents.Abstractions;
using Microsoft.Extensions.Options;

namespace KUKULCAN.SharedKernel.Auth.PostgreSQL.Integration;

/// <summary>Creates authentication database contexts for integration tests.</summary>
public static class AuthDbContextFactory
{
    /// <summary>Creates a fresh authentication schema for the test.</summary>
    public static async Task<AuthDbContext> CreateAsync(
        string connectionString,
        Guid activeTenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var options = Options.Create(new KukulcanDatabaseOptions
        {
            Provider = DatabaseProvider.PostgresSql,
            ConnectionString = connectionString
        });

        var context = new AuthDbContext(
            options,
            new TestTenantContext(activeTenantId),
            new TestClock(),
            new TestDomainEventDispatcher());

        await context.Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);
        await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        return context;
    }

    private sealed class TestTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class TestDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
