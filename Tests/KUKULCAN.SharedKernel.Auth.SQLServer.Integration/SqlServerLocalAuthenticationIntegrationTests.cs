namespace KUKULCAN.SharedKernel.Auth.SQLServer.Integration;

[TestFixture]
[NonParallelizable]
public sealed class SqlServerLocalAuthenticationIntegrationTests
{
    [Test]
    public async Task FindByEmailAsync_WithPersistedUser_ReturnsUserAndAllTenantMemberships()
    {
        // This test intentionally defines the SQL Server persistence contract before its implementation.
        var firstTenantId = Guid.NewGuid();
        var secondTenantId = Guid.NewGuid();

        await using var context = await AuthDbContextFactory.CreateAsync(
            SqlServerAuthenticationDatabase.ConnectionString,
            activeTenantId: firstTenantId);

        var userId = Guid.NewGuid();

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
        var requestedFirstTenantId = Guid.NewGuid();
        var requestedSecondTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        await using var context = await AuthDbContextFactory.CreateAsync(
            SqlServerAuthenticationDatabase.ConnectionString,
            activeTenantId: requestedFirstTenantId);

        var requestedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

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

    [Test]
    public async Task FindByEmailAsync_WithPersistedUserWithoutTenantMemberships_ReturnsUserWithEmptyTenantCollection()
    {
        var activeTenantId = Guid.NewGuid();

        await using var context = await AuthDbContextFactory.CreateAsync(
            SqlServerAuthenticationDatabase.ConnectionString,
            activeTenantId);

        var userId = Guid.NewGuid();

        context.Users.Add(new AuthUserEntity
        {
            UserId = userId,
            Email = "without-tenants@example.com",
            PasswordHash = "stored-password-hash"
        });

        await context.SaveChangesAsync();

        var store = new LocalUserStore(context);

        var user = await store.FindByEmailAsync("without-tenants@example.com");

        Assert.That(user, Is.Not.Null);
        Assert.That(user!.UserId, Is.EqualTo(userId));
        Assert.That(user.Tenants, Is.Empty);
    }

    [Test]
    public async Task FindByEmailAsync_WithUnknownEmail_ReturnsNull()
    {
        await using var context = await AuthDbContextFactory.CreateAsync(
            SqlServerAuthenticationDatabase.ConnectionString,
            Guid.NewGuid());

        var store = new LocalUserStore(context);

        var user = await store.FindByEmailAsync("unknown@example.com");

        Assert.That(user, Is.Null);
    }


    [Test]
    public async Task UserIdentity_CannotBePersistedTwiceWithTheSameUserId()
    {
        await using var context = await AuthDbContextFactory.CreateAsync(
            SqlServerAuthenticationDatabase.ConnectionString,
            Guid.NewGuid());

        var userId = Guid.NewGuid();

        context.Users.AddRange(
            new AuthUserEntity
            {
                UserId = userId,
                Email = "first@example.com",
                PasswordHash = "first-hash"
            },
            new AuthUserEntity
            {
                UserId = userId,
                Email = "second@example.com",
                PasswordHash = "second-hash"
            });

        Assert.ThrowsAsync<DbUpdateException>(
            async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task UserEmail_MustBeUniqueAcrossLocalUsers()
    {
        await using var context = await AuthDbContextFactory.CreateAsync(
            SqlServerAuthenticationDatabase.ConnectionString,
            Guid.NewGuid());

        context.Users.AddRange(
            new AuthUserEntity
            {
                UserId = Guid.NewGuid(),
                Email = "duplicate@example.com",
                PasswordHash = "first-hash"
            },
            new AuthUserEntity
            {
                UserId = Guid.NewGuid(),
                Email = "duplicate@example.com",
                PasswordHash = "second-hash"
            });

        Assert.ThrowsAsync<DbUpdateException>(
            async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task TenantMembership_CannotBePersistedTwiceForTheSameUserAndTenant()
    {
        await using var context = await AuthDbContextFactory.CreateAsync(
            SqlServerAuthenticationDatabase.ConnectionString,
            Guid.NewGuid());

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        context.Users.Add(new AuthUserEntity
        {
            UserId = userId,
            Email = "duplicate-membership@example.com",
            PasswordHash = "stored-password-hash"
        });

        context.TenantMemberships.AddRange(
            new AuthTenantMembershipEntity
            {
                UserId = userId,
                TenantId = tenantId
            },
            new AuthTenantMembershipEntity
            {
                UserId = userId,
                TenantId = tenantId
            });

        Assert.ThrowsAsync<DbUpdateException>(
            async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task TenantMembership_MustReferenceAnExistingUser()
    {
        await using var context = await AuthDbContextFactory.CreateAsync(
            SqlServerAuthenticationDatabase.ConnectionString,
            Guid.NewGuid());

        context.TenantMemberships.Add(new AuthTenantMembershipEntity
        {
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        });

        Assert.ThrowsAsync<DbUpdateException>(
            async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task FindByEmailAsync_WhenCancellationIsRequested_PropagatesCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await using var context = await AuthDbContextFactory.CreateAsync(
            SqlServerAuthenticationDatabase.ConnectionString,
            Guid.NewGuid());

        var store = new LocalUserStore(context);

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await store.FindByEmailAsync(
                "user@example.com",
                cancellationSource.Token));
    }
}
