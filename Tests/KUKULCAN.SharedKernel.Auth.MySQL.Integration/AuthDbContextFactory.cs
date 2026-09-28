using KUKULCAN.SharedKernel.Abstractions;
using KUKULCAN.SharedKernel.Auth.Persistence;
using KUKULCAN.SharedKernel.Database.Abstractions;
using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.DomainEvents.Abstractions;
using Microsoft.Extensions.Options;

namespace KUKULCAN.SharedKernel.Auth.MySQL.Integration;

/// <summary>Creates authentication database contexts for integration tests.</summary>
public static class AuthDbContextFactory
{
    /// <summary>Creates a fresh authentication schema for the test.</summary>
    public static Task<AuthDbContext> CreateAsync(
        string connectionString,
        Guid activeTenantId,
        CancellationToken cancellationToken = default)
        => CreateAsync(
            connectionString,
            new TestTenantContext(activeTenantId),
            cancellationToken);

    internal static async Task<AuthDbContext> CreateAsync(
        string connectionString,
        TestTenantContext tenantContext,
        CancellationToken cancellationToken = default)
    {
        var context = CreateContext(connectionString, tenantContext);

        await context.Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);
        await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        return context;
    }

    internal static AuthDbContext CreateExisting(
        string connectionString,
        Guid activeTenantId)
        => CreateContext(
            connectionString,
            new TestTenantContext(activeTenantId));

    private static AuthDbContext CreateContext(
        string connectionString,
        TestTenantContext tenantContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var options = Options.Create(new KukulcanDatabaseOptions
        {
            Provider = DatabaseProvider.MySql,
            ConnectionString = connectionString
        });

        return new AuthDbContext(
            options,
            tenantContext,
            new TestClock(),
            new TestDomainEventDispatcher());
    }

    internal sealed class TestTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; set; } = tenantId;
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
