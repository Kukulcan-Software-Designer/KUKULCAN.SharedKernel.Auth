using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace KUKULCAN.SharedKernel.Auth.PostgreSQL.Integration;

[SetUpFixture]
public sealed class PostgreSQLAuthenticationDatabase
{
    private static PostgreSqlContainer? _container;

    public static string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL authentication integration container is not initialized.");

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        _container = new PostgreSqlBuilder("postgres:18")
            .WithDatabase("kukulcan_auth")
            .WithUsername("kukulcan")
            .WithPassword("Kukulcan1!")
            .Build();

        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task StopAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
