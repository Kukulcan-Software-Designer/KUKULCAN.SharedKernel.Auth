using KUKULCAN.SharedKernel.Abstractions;
using KUKULCAN.SharedKernel.Auth.Persistence;
using KUKULCAN.SharedKernel.Database.Abstractions;
using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.DomainEvents.Abstractions;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace KUKULCAN.SharedKernel.Auth.Migrations.SQLServer;

/// <summary>Creates AuthDbContext instances for SQL Server EF Core design-time operations.</summary>
public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    /// <inheritdoc />
    public AuthDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "KUKULCAN_AUTH_SQLSERVER_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set KUKULCAN_AUTH_SQLSERVER_CONNECTION_STRING before running SQL Server Auth migrations.");
        }

        var options = Options.Create(new KukulcanDatabaseOptions
        {
            Provider = DatabaseProvider.SqlServer,
            ConnectionString = connectionString
        });

        return new AuthDbContext(
            options,
            new DesignTimeTenantContext(),
            new DesignTimeClock(),
            new DesignTimeDomainEventDispatcher());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
    }

    private sealed class DesignTimeClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class DesignTimeDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
