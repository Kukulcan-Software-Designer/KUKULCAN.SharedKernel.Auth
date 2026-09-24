using KUKULCAN.SharedKernel.Abstractions;
using KUKULCAN.SharedKernel.Database.Abstractions;
using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.DomainEvents.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace KUKULCAN.SharedKernel.Auth.SQLServer.Integration;

/// <summary>Creates isolated authentication database contexts for SQL Server integration tests.</summary>
public static class AuthDbContextFactory
{
    /// <summary>Creates a fresh authentication database and its context.</summary>
    public static async Task<AuthDbContext> CreateAsync(
        string connectionString,
        Guid activeTenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var connection = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = $"AuthTest_{Guid.NewGuid():N}"
        };

        var databaseOptions = Options.Create(new KukulcanDatabaseOptions
        {
            Provider = DatabaseProvider.SqlServer,
            ConnectionString = connection.ConnectionString
        });


        var context = new AuthDbContext(
            databaseOptions,
            new TestTenantContext(activeTenantId),
            new TestClock(),
            new TestDomainEventDispatcher());

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
