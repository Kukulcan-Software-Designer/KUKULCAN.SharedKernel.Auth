using KUKULCAN.SharedKernel.Auth.Authentication.Federated;
using KUKULCAN.SharedKernel.Auth.Authentication.Local;
using KUKULCAN.SharedKernel.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using KUKULCAN.SharedKernel.Results;

namespace KUKULCAN.SharedKernel.Auth.MySQL.Integration;

[TestFixture]
[NonParallelizable]
public sealed class FederatedAuthenticationIntegrationTests
{
    private static readonly string[] Providers = ["Google", "Microsoft", "Apple"];

    [TestCaseSource(nameof(Providers))]
    public async Task AuthenticateAsync_WithPersistedFederatedIdentity_ReturnsUserAndAllTenantMemberships(string providerName)
    {
        var firstTenantId = Guid.NewGuid();
        var secondTenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string subject = "provider-subject-001";

        await using var context = await AuthDbContextFactory.CreateAsync(
            MySQLAuthenticationDatabase.ConnectionString,
            activeTenantId: firstTenantId);

        context.Users.Add(new AuthUserEntity
        {
            UserId = userId,
            Email = "federated@example.com",
            PasswordHash = "not-used"
        });

        context.TenantMemberships.AddRange(
            new AuthTenantMembershipEntity { UserId = userId, TenantId = firstTenantId },
            new AuthTenantMembershipEntity { UserId = userId, TenantId = secondTenantId });

        context.FederatedIdentities.Add(new AuthFederatedIdentityEntity
        {
            Provider = providerName,
            Subject = subject,
            UserId = userId
        });

        await context.SaveChangesAsync();

        var store = new FederatedUserStore(context);
        var provider = new StubFederatedAuthenticationProvider(
            providerName,
            new FederatedIdentity(providerName, subject, "federated@example.com"));
        var service = new FederatedAuthenticationService([provider], store);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "credential"));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.UserId, Is.EqualTo(userId));
        Assert.That(result.Value.Email, Is.EqualTo("federated@example.com"));
        Assert.That(
            result.Value.Tenants.Select(x => x.TenantId),
            Is.EquivalentTo(new[] { firstTenantId, secondTenantId }));
    }

    [TestCaseSource(nameof(Providers))]
    public async Task AuthenticateAsync_WhenUserHasOnlyMembershipOutsideActiveTenant_ReturnsNoTenantAccess(
        string providerName)
    {
        var activeTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string subject = "outside-active-tenant-subject";

        await using var context = await AuthDbContextFactory.CreateAsync(
            MySQLAuthenticationDatabase.ConnectionString,
            activeTenantId);

        context.Users.Add(new AuthUserEntity
        {
            UserId = userId,
            Email = "federated-outside@example.com",
            PasswordHash = "not-used"
        });

        context.TenantMemberships.Add(new AuthTenantMembershipEntity
        {
            UserId = userId,
            TenantId = otherTenantId
        });

        context.FederatedIdentities.Add(new AuthFederatedIdentityEntity
        {
            Provider = providerName,
            Subject = subject,
            UserId = userId
        });

        await context.SaveChangesAsync();

        var store = new FederatedUserStore(context);
        var provider = new StubFederatedAuthenticationProvider(
            providerName,
            new FederatedIdentity(providerName, subject, "federated-outside@example.com"));
        var service = new FederatedAuthenticationService([provider], store);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "credential"));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Auth.NoTenantAccess"));
    }

    [TestCaseSource(nameof(Providers))]
    public async Task AuthenticateAsync_WhenUserHasNoTenant_ReturnsNoTenantAccess(string providerName)
    {
        var userId = Guid.NewGuid();
        const string subject = "no-tenant-subject";

        await using var context = await AuthDbContextFactory.CreateAsync(
            MySQLAuthenticationDatabase.ConnectionString,
            activeTenantId: Guid.NewGuid());

        context.Users.Add(new AuthUserEntity
        {
            UserId = userId,
            Email = "federated-no-tenant@example.com",
            PasswordHash = "not-used"
        });

        context.FederatedIdentities.Add(new AuthFederatedIdentityEntity
        {
            Provider = providerName,
            Subject = subject,
            UserId = userId
        });

        await context.SaveChangesAsync();

        var store = new FederatedUserStore(context);
        var provider = new StubFederatedAuthenticationProvider(
            providerName,
            new FederatedIdentity(providerName, subject, "federated-no-tenant@example.com"));
        var service = new FederatedAuthenticationService([provider], store);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "credential"));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error.Code, Is.EqualTo("Auth.NoTenantAccess"));
    }

    [TestCaseSource(nameof(Providers))]
    public async Task AuthenticateAsync_WhenActiveTenantChangesBetweenUserMemberships_RemainsSuccessful(
        string providerName)
    {
        var firstTenantId = Guid.NewGuid();
        var secondTenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string subject = "tenant-switch-subject";

        var tenantContext = new AuthDbContextFactory.TestTenantContext(firstTenantId);
        await using var context = await AuthDbContextFactory.CreateAsync(
            MySQLAuthenticationDatabase.ConnectionString,
            tenantContext);

        context.Users.Add(new AuthUserEntity
        {
            UserId = userId,
            Email = "federated-switch@example.com",
            PasswordHash = "not-used"
        });

        context.TenantMemberships.AddRange(
            new AuthTenantMembershipEntity { UserId = userId, TenantId = firstTenantId },
            new AuthTenantMembershipEntity { UserId = userId, TenantId = secondTenantId });

        context.FederatedIdentities.Add(new AuthFederatedIdentityEntity
        {
            Provider = providerName,
            Subject = subject,
            UserId = userId
        });

        await context.SaveChangesAsync();

        var store = new FederatedUserStore(context);
        var provider = new StubFederatedAuthenticationProvider(
            providerName,
            new FederatedIdentity(providerName, subject, "federated-switch@example.com"));
        var service = new FederatedAuthenticationService([provider], store);
        var request = new FederatedAuthenticationRequest(providerName, "credential");

        Result<AuthenticatedUser> firstResult = await service.AuthenticateAsync(request);
        tenantContext.TenantId = secondTenantId;
        Result<AuthenticatedUser> secondResult = await service.AuthenticateAsync(request);

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(firstResult.Value.Tenants.Select(x => x.TenantId),
            Is.EquivalentTo(new[] { firstTenantId, secondTenantId }));
        Assert.That(secondResult.Value.Tenants.Select(x => x.TenantId),
            Is.EquivalentTo(new[] { firstTenantId, secondTenantId }));
    }

    [Test]
    public async Task UserDeletion_CascadesToFederatedIdentities()
    {
        var userId = Guid.NewGuid();

        await using var context = await AuthDbContextFactory.CreateAsync(
            MySQLAuthenticationDatabase.ConnectionString,
            activeTenantId: Guid.NewGuid());

        context.Users.Add(new AuthUserEntity
        {
            UserId = userId,
            Email = "federated-delete@example.com",
            PasswordHash = "not-used"
        });

        context.FederatedIdentities.Add(new AuthFederatedIdentityEntity
        {
            Provider = "Google",
            Subject = "delete-subject",
            UserId = userId
        });

        await context.SaveChangesAsync();

        var user = await context.Users.SingleAsync(entity => entity.UserId == userId);
        context.Users.Remove(user);
        await context.SaveChangesAsync();

        var identityExists = await context.FederatedIdentities
            .IgnoreQueryFilters()
            .AnyAsync(identity => identity.UserId == userId);

        Assert.That(identityExists, Is.False);
    }

    [TestCaseSource(nameof(Providers))]
    public async Task FindByFederatedIdentityAsync_WhenIdentityDoesNotExist_ReturnsNull(string providerName)
    {
        await using var context = await AuthDbContextFactory.CreateAsync(
            MySQLAuthenticationDatabase.ConnectionString,
            activeTenantId: Guid.NewGuid());

        var store = new FederatedUserStore(context);

        var user = await store.FindByFederatedIdentityAsync(providerName, "unknown-subject");

        Assert.That(user, Is.Null);
    }

    [Test]
    public async Task FindByFederatedIdentityAsync_AllowsSameOidAcrossDifferentMicrosoftTenants()
    {
        await using var context = await AuthDbContextFactory.CreateAsync(
            MySQLAuthenticationDatabase.ConnectionString,
            activeTenantId: Guid.NewGuid());

        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        context.Users.AddRange(
            new AuthUserEntity
            {
                UserId = firstUserId,
                Email = "first@example.com",
                PasswordHash = "first-hash"
            },
            new AuthUserEntity
            {
                UserId = secondUserId,
                Email = "second@example.com",
                PasswordHash = "second-hash"
            });

        context.FederatedIdentities.AddRange(
            new AuthFederatedIdentityEntity
            {
                Provider = "Microsoft",
                Subject = "tenant-A:oid-X",
                UserId = firstUserId
            },
            new AuthFederatedIdentityEntity
            {
                Provider = "Microsoft",
                Subject = "tenant-B:oid-X",
                UserId = secondUserId
            });

        await context.SaveChangesAsync();

        var store = new FederatedUserStore(context);

        var firstUser = await store.FindByFederatedIdentityAsync(
            "Microsoft",
            "tenant-A:oid-X");
        var secondUser = await store.FindByFederatedIdentityAsync(
            "Microsoft",
            "tenant-B:oid-X");

        Assert.That(firstUser, Is.Not.Null);
        Assert.That(firstUser!.UserId, Is.EqualTo(firstUserId));
        Assert.That(secondUser, Is.Not.Null);
        Assert.That(secondUser!.UserId, Is.EqualTo(secondUserId));
    }

    [Test]
    public async Task FederatedIdentity_CannotBePersistedTwiceForTheSameProviderAndSubject()
    {
        var activeTenantId = Guid.NewGuid();

        await using var firstContext = await AuthDbContextFactory.CreateAsync(
            MySQLAuthenticationDatabase.ConnectionString,
            activeTenantId);

        var firstUserId = Guid.NewGuid();
        firstContext.Users.Add(new AuthUserEntity
        {
            UserId = firstUserId,
            Email = "first@example.com",
            PasswordHash = "first-hash"
        });

        firstContext.FederatedIdentities.Add(new AuthFederatedIdentityEntity
        {
            Provider = "Google",
            Subject = "same-subject",
            UserId = firstUserId
        });

        await firstContext.SaveChangesAsync();

        await using var secondContext = AuthDbContextFactory.CreateExisting(
            MySQLAuthenticationDatabase.ConnectionString,
            activeTenantId);

        var secondUserId = Guid.NewGuid();
        secondContext.Users.Add(new AuthUserEntity
        {
            UserId = secondUserId,
            Email = "second@example.com",
            PasswordHash = "second-hash"
        });

        secondContext.FederatedIdentities.Add(new AuthFederatedIdentityEntity
        {
            Provider = "Google",
            Subject = "same-subject",
            UserId = secondUserId
        });

        Assert.ThrowsAsync<DbUpdateException>(
            async () => await secondContext.SaveChangesAsync());
    }

    [Test]
    public async Task FederatedIdentity_AllowsTheSameSubjectForDifferentProviders()
    {
        await using var context = await AuthDbContextFactory.CreateAsync(
            MySQLAuthenticationDatabase.ConnectionString,
            activeTenantId: Guid.NewGuid());

        var userId = Guid.NewGuid();

        context.Users.Add(new AuthUserEntity
        {
            UserId = userId,
            Email = "multi-provider@example.com",
            PasswordHash = "stored-hash"
        });

        context.FederatedIdentities.AddRange(
            new AuthFederatedIdentityEntity
            {
                Provider = "Google",
                Subject = "same-subject",
                UserId = userId
            },
            new AuthFederatedIdentityEntity
            {
                Provider = "Microsoft",
                Subject = "same-subject",
                UserId = userId
            },
            new AuthFederatedIdentityEntity
            {
                Provider = "Apple",
                Subject = "same-subject",
                UserId = userId
            });

        await context.SaveChangesAsync();

        var count = await context.FederatedIdentities.CountAsync();

        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public async Task FederatedIdentity_MustReferenceAnExistingUser()
    {
        await using var context = await AuthDbContextFactory.CreateAsync(
            MySQLAuthenticationDatabase.ConnectionString,
            activeTenantId: Guid.NewGuid());

        context.FederatedIdentities.Add(new AuthFederatedIdentityEntity
        {
            Provider = "Google",
            Subject = "orphan-subject",
            UserId = Guid.NewGuid()
        });

        Assert.ThrowsAsync<DbUpdateException>(
            async () => await context.SaveChangesAsync());
    }

    [Test]
    public async Task FindByFederatedIdentityAsync_WhenCancellationIsRequested_PropagatesCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await using var context = await AuthDbContextFactory.CreateAsync(
            MySQLAuthenticationDatabase.ConnectionString,
            activeTenantId: Guid.NewGuid());

        var store = new FederatedUserStore(context);

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await store.FindByFederatedIdentityAsync(
                "Google",
                "subject",
                cancellationSource.Token));
    }

    private sealed class StubFederatedAuthenticationProvider : IFederatedAuthenticationProvider
    {
        private readonly FederatedIdentity _identity;

        public StubFederatedAuthenticationProvider(string provider, FederatedIdentity identity)
        {
            Provider = provider;
            _identity = identity;
        }

        public string Provider { get; }

        public Task<Result<FederatedIdentity>> AuthenticateAsync(
            FederatedAuthenticationRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result<FederatedIdentity>.Success(_identity));
    }
}
