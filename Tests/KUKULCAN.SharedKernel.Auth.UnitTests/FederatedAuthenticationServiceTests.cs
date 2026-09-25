using FluentAssertions;
using KUKULCAN.SharedKernel.Auth.Authentication.Federated;
using KUKULCAN.SharedKernel.Auth.Authentication.Local;
using KUKULCAN.SharedKernel.Results;
using Moq;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.UnitTests;

[TestFixture]
public sealed class FederatedAuthenticationServiceTests
{
    [Test]
    public void Constructor_WhenProvidersIsNull_ThrowsArgumentNullException()
    {
        Action act = () => new FederatedAuthenticationService(
            null!,
            new Mock<IFederatedUserStore>(MockBehavior.Strict).Object);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_WhenUserStoreIsNull_ThrowsArgumentNullException()
    {
        Action act = () => new FederatedAuthenticationService(
            Array.Empty<IFederatedAuthenticationProvider>(),
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_WhenProvidersIsEmpty_CreatesServiceWithoutThrowing()
    {
        Action act = () => new FederatedAuthenticationService(
            Array.Empty<IFederatedAuthenticationProvider>(),
            new Mock<IFederatedUserStore>(MockBehavior.Strict).Object);

        act.Should().NotThrow();
    }

    [Test]
    public void AuthenticateAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        Func<Task> act = () => service.AuthenticateAsync(null!);

        act.Should().ThrowAsync<ArgumentNullException>();
        provider.VerifyNoOtherCalls();
        userStore.VerifyNoOtherCalls();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void AuthenticateAsync_WhenProviderIsEmptyOrWhitespace_ThrowsArgumentException(string providerName)
    {
        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        Func<Task> act = () => service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "external-credential"));

        act.Should().ThrowAsync<ArgumentException>();
        provider.VerifyNoOtherCalls();
        userStore.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_WhenProviderIsNotRegistered_ReturnsUnsupportedProviderFailure()
    {
        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        provider.SetupGet(item => item.Provider).Returns("Google");
        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest("Microsoft", "external-credential"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.UnsupportedFederatedProvider");
        provider.Verify(item => item.AuthenticateAsync(It.IsAny<FederatedAuthenticationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        userStore.VerifyNoOtherCalls();
    }

    [TestCase("Google")]
    [TestCase("Microsoft")]
    [TestCase("Apple")]
    public async Task AuthenticateAsync_UsesTheProviderMatchingTheRequestedProvider(string providerName)
    {
        var expectedIdentity = new FederatedIdentity(
            providerName,
            "provider-subject",
            "user@example.com");
        var selectedProvider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        selectedProvider.SetupGet(provider => provider.Provider).Returns(providerName);
        selectedProvider
            .Setup(provider => provider.AuthenticateAsync(
                It.Is<FederatedAuthenticationRequest>(request =>
                    request.Provider == providerName && request.Credential == "external-credential"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(expectedIdentity));

        var otherProvider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        otherProvider.SetupGet(provider => provider.Provider).Returns("Other");
        var user = CreateUser([new TenantMembership(Guid.NewGuid())]);
        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByFederatedIdentityAsync(
                providerName,
                expectedIdentity.Subject,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new FederatedAuthenticationService(
            [otherProvider.Object, selectedProvider.Object],
            userStore.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "external-credential"));

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(user.UserId);
        result.Value.Email.Should().Be(user.Email);
        selectedProvider.Verify(
            provider => provider.AuthenticateAsync(
                It.IsAny<FederatedAuthenticationRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
        otherProvider.VerifyNoOtherCalls();
    }

    [TestCase("Google")]
    [TestCase("Microsoft")]
    [TestCase("Apple")]
    public async Task AuthenticateAsync_SelectsProviderCaseInsensitively(string providerName)
    {
        var identity = new FederatedIdentity(providerName, "provider-subject", "user@example.com");
        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        provider.SetupGet(item => item.Provider).Returns(providerName);
        provider
            .Setup(item => item.AuthenticateAsync(
                It.Is<FederatedAuthenticationRequest>(request =>
                    string.Equals(request.Provider, providerName, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(identity));

        var user = CreateUser([new TenantMembership(Guid.NewGuid())]);
        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByFederatedIdentityAsync(providerName, identity.Subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName.ToUpperInvariant(), "credential"));

        result.IsSuccess.Should().BeTrue();
        provider.Verify(item => item.AuthenticateAsync(
            It.IsAny<FederatedAuthenticationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_PropagatesCancellationTokenToProviderAndUserStore()
    {
        const string providerName = "Google";
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var identity = new FederatedIdentity(providerName, "provider-subject", "user@example.com");
        var user = CreateUser([new TenantMembership(Guid.NewGuid())]);

        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        provider.SetupGet(item => item.Provider).Returns(providerName);
        provider
            .Setup(item => item.AuthenticateAsync(
                It.IsAny<FederatedAuthenticationRequest>(), cancellationToken))
            .ReturnsAsync(Result<FederatedIdentity>.Success(identity));

        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByFederatedIdentityAsync(
                providerName, identity.Subject, cancellationToken))
            .ReturnsAsync(user);

        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "external-credential"),
            cancellationToken);

        result.IsSuccess.Should().BeTrue();
        provider.Verify(item => item.AuthenticateAsync(It.IsAny<FederatedAuthenticationRequest>(), cancellationToken), Times.Once);
        userStore.Verify(item => item.FindByFederatedIdentityAsync(providerName, identity.Subject, cancellationToken), Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_WhenProviderRejectsCredential_ReturnsFailureAndDoesNotQueryUserStore()
    {
        const string providerName = "Google";
        var providerFailure = new Error("Auth.FederatedCredentialInvalid", "The federated credential is invalid.");
        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        provider.SetupGet(item => item.Provider).Returns(providerName);
        provider
            .Setup(item => item.AuthenticateAsync(
                It.IsAny<FederatedAuthenticationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Failure(providerFailure));
        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "invalid-credential"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(providerFailure);
        userStore.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_LooksUpUserByProviderAndStableSubject_NotByEmail()
    {
        const string providerName = "Microsoft";
        const string subject = "stable-provider-subject";
        const string email = "changed@example.com";
        var identity = new FederatedIdentity(providerName, subject, email);
        var user = CreateUser([new TenantMembership(Guid.NewGuid())]);

        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        provider.SetupGet(item => item.Provider).Returns(providerName);
        provider
            .Setup(item => item.AuthenticateAsync(It.IsAny<FederatedAuthenticationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(identity));

        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByFederatedIdentityAsync(providerName, subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "credential"));

        result.IsSuccess.Should().BeTrue();
        userStore.Verify(
            store => store.FindByFederatedIdentityAsync(providerName, subject, It.IsAny<CancellationToken>()),
            Times.Once);
        userStore.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_WhenExternalIdentityIsNotLinked_ReturnsFailureWithoutProvisioningUser()
    {
        const string providerName = "Apple";
        var identity = new FederatedIdentity(providerName, "unlinked-subject", "user@example.com");
        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        provider.SetupGet(item => item.Provider).Returns(providerName);
        provider
            .Setup(item => item.AuthenticateAsync(It.IsAny<FederatedAuthenticationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(identity));

        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByFederatedIdentityAsync(providerName, identity.Subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocalUser?)null);

        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "valid-credential"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.FederatedIdentityNotLinked");
        userStore.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_WhenLinkedUserHasMultipleTenants_ReturnsAllTenantMemberships()
    {
        const string providerName = "Google";
        var tenants = new[]
        {
            new TenantMembership(Guid.NewGuid()),
            new TenantMembership(Guid.NewGuid()),
            new TenantMembership(Guid.NewGuid())
        };
        var identity = new FederatedIdentity(providerName, "provider-subject", "user@example.com");
        var user = CreateUser(tenants);
        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        provider.SetupGet(item => item.Provider).Returns(providerName);
        provider
            .Setup(item => item.AuthenticateAsync(It.IsAny<FederatedAuthenticationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(identity));
        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByFederatedIdentityAsync(providerName, identity.Subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "valid-credential"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Tenants.Select(tenant => tenant.TenantId)
            .Should().BeEquivalentTo(tenants.Select(tenant => tenant.TenantId));
    }

    [Test]
    public async Task AuthenticateAsync_DoesNotMixTenantMembershipsBetweenUsers()
    {
        const string providerName = "Google";
        var firstTenants = new[] { new TenantMembership(Guid.NewGuid()) };
        var secondTenants = new[] { new TenantMembership(Guid.NewGuid()) };
        var firstUser = CreateUser(firstTenants);
        var secondUser = CreateUser(secondTenants);

        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        provider.SetupGet(item => item.Provider).Returns(providerName);
        provider
            .SetupSequence(item => item.AuthenticateAsync(
                It.IsAny<FederatedAuthenticationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(
                new FederatedIdentity(providerName, "first-subject", "first@example.com")))
            .ReturnsAsync(Result<FederatedIdentity>.Success(
                new FederatedIdentity(providerName, "second-subject", "second@example.com")));

        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByFederatedIdentityAsync(providerName, "first-subject", It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstUser);
        userStore
            .Setup(store => store.FindByFederatedIdentityAsync(providerName, "second-subject", It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondUser);

        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        Result<AuthenticatedUser> firstResult = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "credential"));
        Result<AuthenticatedUser> secondResult = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "credential"));

        firstResult.Value.Tenants.Should().BeEquivalentTo(firstTenants);
        firstResult.Value.Tenants.Should().NotContain(secondTenants);
        secondResult.Value.Tenants.Should().BeEquivalentTo(secondTenants);
        secondResult.Value.Tenants.Should().NotContain(firstTenants);
    }

    [Test]
    public async Task AuthenticateAsync_DoesNotExposeTheUserStoreTenantCollection()
    {
        const string providerName = "Microsoft";
        var tenants = new List<TenantMembership>
        {
            new(Guid.NewGuid()),
            new(Guid.NewGuid())
        };
        var user = CreateUser(tenants);

        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        provider.SetupGet(item => item.Provider).Returns(providerName);
        provider
            .Setup(item => item.AuthenticateAsync(It.IsAny<FederatedAuthenticationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(
                new FederatedIdentity(providerName, "subject", "user@example.com")));

        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByFederatedIdentityAsync(providerName, "subject", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "credential"));
        tenants.Clear();

        result.Value.Tenants.Should().HaveCount(2);
    }

    [Test]
    public async Task AuthenticateAsync_WhenIdentityProviderDoesNotMatchRequestedProvider_DoesNotAuthenticateUser()
    {
        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        provider.SetupGet(item => item.Provider).Returns("Google");
        provider
            .Setup(item => item.AuthenticateAsync(It.IsAny<FederatedAuthenticationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(
                new FederatedIdentity("Microsoft", "provider-subject", "user@example.com")));
        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest("Google", "credential"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.FederatedProviderMismatch");
        userStore.VerifyNoOtherCalls();
    }

    [TestCase("Google")]
    [TestCase("Microsoft")]
    [TestCase("Apple")]
    public async Task AuthenticateAsync_PreservesProviderAndSubjectAsTheExternalIdentityKey(string providerName)
    {
        var identity = new FederatedIdentity(providerName, "subject-123", "user@example.com");
        var user = CreateUser([new TenantMembership(Guid.NewGuid())]);
        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        provider.SetupGet(item => item.Provider).Returns(providerName);
        provider
            .Setup(item => item.AuthenticateAsync(It.IsAny<FederatedAuthenticationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(identity));
        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByFederatedIdentityAsync(providerName, identity.Subject, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "credential"));

        result.IsSuccess.Should().BeTrue();
        userStore.Verify(
            store => store.FindByFederatedIdentityAsync(providerName, "subject-123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static LocalUser CreateUser(IReadOnlyCollection<TenantMembership> tenants)
        => new(
            Guid.NewGuid(),
            "user@example.com",
            "stored-password-hash",
            tenants);
}
