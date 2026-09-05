using FluentAssertions;
using KUKULCAN.SharedKernel.Results;
using Moq;

namespace KUKULCAN.SharedKernel.Auth.UnitTests;

[TestFixture]
public sealed class FederatedAuthenticationServiceTests
{
    [Test]
    public void AuthenticateAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        // Act
        Func<Task> act = () => service.AuthenticateAsync(null!);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>();
        provider.VerifyNoOtherCalls();
        userStore.VerifyNoOtherCalls();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void AuthenticateAsync_WhenProviderIsEmptyOrWhitespace_ThrowsArgumentException(string providerName)
    {
        // Arrange
        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        // Act
        Func<Task> act = () => service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "external-credential"));

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
        provider.VerifyNoOtherCalls();
        userStore.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_WhenProviderIsNotRegistered_ReturnsUnsupportedProviderFailure()
    {
        // Arrange
        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        provider.SetupGet(item => item.Provider).Returns("Google");
        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest("Microsoft", "external-credential"));

        // Assert
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
        // Arrange
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

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "external-credential"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(user.UserId);
        result.Value.Email.Should().Be(user.Email);
        selectedProvider.Verify(
            provider => provider.AuthenticateAsync(
                It.IsAny<FederatedAuthenticationRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
        otherProvider.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_PropagatesCancellationTokenToProviderAndUserStore()
    {
        // Arrange
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

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "external-credential"),
            cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        provider.Verify(item => item.AuthenticateAsync(It.IsAny<FederatedAuthenticationRequest>(), cancellationToken), Times.Once);
        userStore.Verify(item => item.FindByFederatedIdentityAsync(providerName, identity.Subject, cancellationToken), Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_WhenProviderRejectsCredential_ReturnsFailureAndDoesNotQueryUserStore()
    {
        // Arrange
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

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "invalid-credential"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(providerFailure);
        userStore.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_LooksUpUserByProviderAndStableSubject_NotByEmail()
    {
        // Arrange
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

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "credential"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        userStore.Verify(
            store => store.FindByFederatedIdentityAsync(providerName, subject, It.IsAny<CancellationToken>()),
            Times.Once);
        userStore.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_WhenExternalIdentityIsNotLinked_ReturnsFailureWithoutProvisioningUser()
    {
        // Arrange
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

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "valid-credential"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.FederatedIdentityNotLinked");
        userStore.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_WhenLinkedUserHasMultipleTenants_ReturnsAllTenantMemberships()
    {
        // Arrange
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

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "valid-credential"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tenants.Select(tenant => tenant.TenantId)
            .Should().BeEquivalentTo(tenants.Select(tenant => tenant.TenantId));
    }

    [Test]
    public async Task AuthenticateAsync_WhenIdentityProviderDoesNotMatchRequestedProvider_DoesNotAuthenticateUser()
    {
        // Arrange
        var provider = new Mock<IFederatedAuthenticationProvider>(MockBehavior.Strict);
        provider.SetupGet(item => item.Provider).Returns("Google");
        provider
            .Setup(item => item.AuthenticateAsync(It.IsAny<FederatedAuthenticationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(
                new FederatedIdentity("Microsoft", "provider-subject", "user@example.com")));
        var userStore = new Mock<IFederatedUserStore>(MockBehavior.Strict);
        var service = new FederatedAuthenticationService([provider.Object], userStore.Object);

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest("Google", "credential"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.FederatedProviderMismatch");
        userStore.VerifyNoOtherCalls();
    }

    [TestCase("Google")]
    [TestCase("Microsoft")]
    [TestCase("Apple")]
    public async Task AuthenticateAsync_PreservesProviderAndSubjectAsTheExternalIdentityKey(string providerName)
    {
        // Arrange
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

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "credential"));

        // Assert
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
