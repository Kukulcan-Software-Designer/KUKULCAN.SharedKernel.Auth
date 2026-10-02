using KUKULCAN.SharedKernel.Abstractions;
using KUKULCAN.SharedKernel.Auth.Persistence;
using KUKULCAN.SharedKernel.Database.Abstractions;
using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.DomainEvents.Abstractions;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace KUKULCAN.SharedKernel.Auth.Migrations.MySQL;

/// <summary>Creates AuthDbContext instances for MySQL EF Core design-time operations.</summary>
public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    /// <inheritdoc />
    public AuthDbContext CreateDbContext(string[] args)
    {
        var providerValue = Environment.GetEnvironmentVariable("KUKULCAN_DATABASE_PROVIDER");
        var connectionString = Environment.GetEnvironmentVariable("KUKULCAN_DATABASE_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(providerValue))
        {
            throw new InvalidOperationException(
                "Set KUKULCAN_DATABASE_PROVIDER before running MySQL Auth migrations.");
        }

        if (!Enum.TryParse<DatabaseProvider>(providerValue, true, out var provider) ||
            provider != DatabaseProvider.MySql)
        {
            throw new InvalidOperationException(
                "KUKULCAN_DATABASE_PROVIDER must be 'MySql' when running MySQL Auth migrations.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set KUKULCAN_DATABASE_CONNECTION_STRING before running MySQL Auth migrations.");
        }

        var options = Options.Create(new KukulcanDatabaseOptions
        {
            Provider = provider,
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
