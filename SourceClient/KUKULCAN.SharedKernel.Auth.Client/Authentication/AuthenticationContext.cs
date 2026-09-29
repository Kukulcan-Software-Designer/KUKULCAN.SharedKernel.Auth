using KUKULCAN.SharedKernel.Abstractions;
using KUKULCAN.SharedKernel.Database.Abstractions;
using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.DomainEvents.Abstractions;
using Microsoft.Extensions.Options;
using KUKULCAN.SharedKernel.Auth.Persistence;

namespace KUKULCAN.SharedKernel.Auth.Client.Authentication;

public sealed class AuthenticationContext : IDisposable
{
    private readonly ClientTenantContext _tenantContext;

    public AuthenticationContext(string provider, string connectionString, Guid activeTenantId)
    {
        _tenantContext = new ClientTenantContext(activeTenantId);

        var options = Options.Create(new KukulcanDatabaseOptions
        {
            Provider = provider switch
            {
                "SqlServer" => DatabaseProvider.SqlServer,
                "PostgreSQL" => DatabaseProvider.PostgresSql,
                "MySQL" => DatabaseProvider.MySql,
                _ => throw new ArgumentException($"Unsupported database provider: {provider}.", nameof(provider))
            },
            ConnectionString = connectionString
        });

        DbContext = new AuthDbContext(
            options,
            _tenantContext,
            new ClientClock(),
            new ClientDomainEventDispatcher());
    }

    public AuthDbContext DbContext { get; }

    public void SetActiveTenant(Guid tenantId) => _tenantContext.TenantId = tenantId;

    public void Dispose() => DbContext.Dispose();

    private sealed class ClientTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; set; } = tenantId;
    }

    private sealed class ClientClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class ClientDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}