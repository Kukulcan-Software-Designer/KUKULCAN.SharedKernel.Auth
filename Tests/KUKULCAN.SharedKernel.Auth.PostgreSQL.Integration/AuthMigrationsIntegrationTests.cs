using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.PostgreSQL.Integration;

[TestFixture]
[NonParallelizable]
public sealed class AuthMigrationsIntegrationTests
{
    [Test]
    public async Task MigrateAsync_OnFreshDatabase_CreatesAuthSchema()
    {
        await using var context = AuthDbContextFactory.CreateExisting(
            PostgreSQLAuthenticationDatabase.ConnectionString,
            Guid.NewGuid());

        await context.Database.MigrateAsync();

        Assert.That(await context.Database.CanConnectAsync(), Is.True);
        Assert.That(await context.Users.CountAsync(), Is.EqualTo(0));
        Assert.That(await context.TenantMemberships.CountAsync(), Is.EqualTo(0));
        Assert.That(await context.FederatedIdentities.CountAsync(), Is.EqualTo(0));
    }
}
