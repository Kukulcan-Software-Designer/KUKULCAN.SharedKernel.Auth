namespace KUKULCAN.SharedKernel.Auth.SQLServer.Integration;

[TestFixture]
[NonParallelizable]
public sealed class SqlServerLocalAuthenticationIntegrationTests
{
    [Test]
    public async Task FindByEmailAsync_WithPersistedUser_ReturnsUserAndAllTenantMemberships()
    {
        // This test intentionally defines the SQL Server persistence contract before its implementation.
        await using var context = await AuthDbContextFactory.CreateAsync(
            SqlServerAuthenticationDatabase.ConnectionString);

        var userId = Guid.NewGuid();
        var firstTenantId = Guid.NewGuid();
        var secondTenantId = Guid.NewGuid();

        context.Users.Add(new AuthUserEntity
        {
            UserId = userId,
            Email = "user@example.com",
            PasswordHash = "stored-password-hash"
        });

        context.TenantMemberships.AddRange(
            new AuthTenantMembershipEntity
            {
                UserId = userId,
                TenantId = firstTenantId
            },
            new AuthTenantMembershipEntity
            {
                UserId = userId,
                TenantId = secondTenantId
            });

        await context.SaveChangesAsync();

        var store = new LocalUserStore(context);

        var user = await store.FindByEmailAsync("user@example.com");

        Assert.That(user, Is.Not.Null);
        Assert.That(user!.UserId, Is.EqualTo(userId));
        Assert.That(user.Email, Is.EqualTo("user@example.com"));
        Assert.That(user.PasswordHash, Is.EqualTo("stored-password-hash"));
        Assert.That(
            user.Tenants.Select(x => x.TenantId),
            Is.EquivalentTo(new[] { firstTenantId, secondTenantId }));
    }

    [Test]
    public async Task FindByEmailAsync_WithUsersFromDifferentTenants_ReturnsAllMembershipsForTheRequestedUser()
    {
        // Authentication must resolve the user's complete tenant membership set rather than
        // restricting the lookup to a single active tenant.
        await using var context = await AuthDbContextFactory.CreateAsync(
            SqlServerAuthenticationDatabase.ConnectionString);

        var requestedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var requestedFirstTenantId = Guid.NewGuid();
        var requestedSecondTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        context.Users.AddRange(
            new AuthUserEntity
            {
                UserId = requestedUserId,
                Email = "requested@example.com",
                PasswordHash = "requested-hash"
            },
            new AuthUserEntity
            {
                UserId = otherUserId,
                Email = "other@example.com",
                PasswordHash = "other-hash"
            });

        context.TenantMemberships.AddRange(
            new AuthTenantMembershipEntity
            {
                UserId = requestedUserId,
                TenantId = requestedFirstTenantId
            },
            new AuthTenantMembershipEntity
            {
                UserId = requestedUserId,
                TenantId = requestedSecondTenantId
            },
            new AuthTenantMembershipEntity
            {
                UserId = otherUserId,
                TenantId = otherTenantId
            });

        await context.SaveChangesAsync();

        var store = new LocalUserStore(context);

        var user = await store.FindByEmailAsync("requested@example.com");

        Assert.That(user, Is.Not.Null);
        Assert.That(user!.UserId, Is.EqualTo(requestedUserId));
        Assert.That(
            user.Tenants.Select(x => x.TenantId),
            Is.EquivalentTo(new[] { requestedFirstTenantId, requestedSecondTenantId }));
        Assert.That(
            user.Tenants.Select(x => x.TenantId),
            Does.Not.Contain(otherTenantId));
    }
}
