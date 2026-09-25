using NUnit.Framework;
using Testcontainers.MySql;

namespace KUKULCAN.SharedKernel.Auth.MySQL.Integration;

[SetUpFixture]
public sealed class MySQLAuthenticationDatabase
{
    private static MySqlContainer? _container;

    public static string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("MySQL authentication integration container is not initialized.");

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        _container = new MySqlBuilder("mysql:8.4")
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
