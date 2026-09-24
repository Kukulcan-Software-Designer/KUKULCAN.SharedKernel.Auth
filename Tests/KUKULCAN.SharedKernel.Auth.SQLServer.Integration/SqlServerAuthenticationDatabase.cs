using NUnit.Framework;
using Testcontainers.MsSql;

namespace KUKULCAN.SharedKernel.Auth.SQLServer.Integration;

[SetUpFixture]
public sealed class SqlServerAuthenticationDatabase
{
    private static MsSqlContainer? _container;

    public static string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("SQL Server authentication integration container is not initialized.");

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
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
