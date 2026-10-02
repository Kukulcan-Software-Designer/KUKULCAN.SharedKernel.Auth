using KUKULCAN.SharedKernel.Abstractions;
using KUKULCAN.SharedKernel.Auth.Persistence;
using KUKULCAN.SharedKernel.Database.Abstractions;
using KUKULCAN.SharedKernel.Database.Configuration;
using KUKULCAN.SharedKernel.DomainEvents.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.MySQL.Integration;

[TestFixture]
[NonParallelizable]
public sealed class AuthMigrationsIntegrationTests
{
    [Test]
    public async Task Migrations_CanBeAppliedToMySqlDatabase()
    {
        await using var context = new AuthDbContext(
            Options.Create(new KukulcanDatabaseOptions
            {
                Provider = DatabaseProvider.MySql,
                ConnectionString = MySQLAuthenticationDatabase.ConnectionString
            }),
            new TestTenantContext(Guid.NewGuid()),
            new Mock<IClock>(MockBehavior.Strict).Object,
            new Mock<IDomainEventDispatcher>(MockBehavior.Strict).Object);

        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS FederatedIdentities");
        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS TenantMemberships");
        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS Users");
        await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS __EFMigrationsHistory");

        await context.Database.MigrateAsync();

        Assert.That(await context.Database.GetPendingMigrationsAsync(), Is.Empty);
        Assert.That(await context.Database.CanConnectAsync(), Is.True);
        Assert.That(await context.Users.AnyAsync(), Is.False);
        Assert.That(await context.TenantMemberships.AnyAsync(), Is.False);
        Assert.That(await context.FederatedIdentities.AnyAsync(), Is.False);
    }

    private sealed class TestTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
    }
}
