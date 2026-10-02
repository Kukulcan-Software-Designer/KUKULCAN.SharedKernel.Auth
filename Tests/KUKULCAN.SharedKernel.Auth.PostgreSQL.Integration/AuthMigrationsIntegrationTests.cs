using System.Data.Common;
using KUKULCAN.SharedKernel.Abstractions;
using KUKULCAN.SharedKernel.Auth.Persistence;
using KUKULCAN.SharedKernel.Database.Abstractions;
using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.DomainEvents.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.PostgreSQL.Integration;

[TestFixture]
[NonParallelizable]
public sealed class AuthMigrationsIntegrationTests
{
    [Test]
    public async Task Migrations_CanBeAppliedToPostgreSqlDatabase()
    {
        var connectionString = CreateUniqueDatabaseConnectionString(
            PostgreSQLAuthenticationDatabase.ConnectionString);

        await using var context = new AuthDbContext(
            Options.Create(new KukulcanDatabaseOptions
            {
                Provider = DatabaseProvider.PostgresSql,
                ConnectionString = connectionString
            }),
            new TestTenantContext(Guid.NewGuid()),
            new Mock<IClock>(MockBehavior.Strict).Object,
            new Mock<IDomainEventDispatcher>(MockBehavior.Strict).Object);

        await context.Database.MigrateAsync();

        Assert.That(await context.Database.GetPendingMigrationsAsync(), Is.Empty);
        Assert.That(await context.Database.CanConnectAsync(), Is.True);
        Assert.That(await context.Users.AnyAsync(), Is.False);
        Assert.That(await context.TenantMemberships.AnyAsync(), Is.False);
        Assert.That(await context.FederatedIdentities.AnyAsync(), Is.False);
    }

    [Test]
    public async Task Migrations_CreateAuthTablesInAuthSchema()
    {
        var connectionString = CreateUniqueDatabaseConnectionString(
            PostgreSQLAuthenticationDatabase.ConnectionString);

        await using var context = new AuthDbContext(
            Options.Create(new KukulcanDatabaseOptions
            {
                Provider = DatabaseProvider.PostgresSql,
                ConnectionString = connectionString
            }),
            new TestTenantContext(Guid.NewGuid()),
            new Mock<IClock>(MockBehavior.Strict).Object,
            new Mock<IDomainEventDispatcher>(MockBehavior.Strict).Object);

        await context.Database.MigrateAsync();

        await using var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = 'Auth'
              AND table_name IN ('Users', 'TenantMemberships', 'FederatedIdentities');
            """;

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());

        Assert.That(count, Is.EqualTo(3));
    }

    private static string CreateUniqueDatabaseConnectionString(string connectionString)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        builder["Database"] = $"kukulcan_auth_migration_{Guid.NewGuid():N}";
        return builder.ConnectionString;
    }

    private sealed class TestTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
    }
}
