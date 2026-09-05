using FluentAssertions;
using KUKULCAN.SharedKernel.Results;
using Moq;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.UnitTests;

[TestFixture]
public sealed class LocalAuthenticationServiceTests
{
    [Test]
    public void AuthenticateAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        // Act
        Func<Task> act = () => service.AuthenticateAsync(null!);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>();
        userStore.VerifyNoOtherCalls();
        passwordHasher.VerifyNoOtherCalls();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void AuthenticateAsync_WhenEmailIsEmptyOrWhitespace_ThrowsArgumentException(string email)
    {
        // Arrange
        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        // Act
        Func<Task> act = () => service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, "CorrectPassword!"));

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
        userStore.VerifyNoOtherCalls();
        passwordHasher.VerifyNoOtherCalls();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void AuthenticateAsync_WhenPasswordIsEmptyOrWhitespace_ThrowsArgumentException(string password)
    {
        // Arrange
        const string email = "user@example.com";
        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        // Act
        Func<Task> act = () => service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
        userStore.VerifyNoOtherCalls();
        passwordHasher.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_NormalizesEmailBeforeLookingUpUser()
    {
        // Arrange
        const string suppliedEmail = "  USER@Example.COM  ";
        const string normalizedEmail = "user@example.com";
        const string password = "CorrectPassword!";
        var user = new LocalUser(
            Guid.NewGuid(),
            normalizedEmail,
            "stored-password-hash",
            [new TenantMembership(Guid.NewGuid())]);

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(normalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwordHasher
            .Setup(hasher => hasher.Verify(password, user.PasswordHash))
            .Returns(true);

        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(suppliedEmail, password));

        // Assert
        result.IsSuccess.Should().BeTrue();
        userStore.Verify(
            store => store.FindByEmailAsync(normalizedEmail, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_PropagatesCancellationTokenToUserStore()
    {
        // Arrange
        const string email = "user@example.com";
        const string password = "CorrectPassword!";
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(email, cancellationToken))
            .ReturnsAsync((LocalUser?)null);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password),
            cancellationToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        userStore.Verify(
            store => store.FindByEmailAsync(email, cancellationToken),
            Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_DoesNotMixTenantMembershipsBetweenUsers()
    {
        // Arrange
        const string firstEmail = "first@example.com";
        const string secondEmail = "second@example.com";
        const string password = "CorrectPassword!";
        var firstTenantIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var secondTenantIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var firstUser = new LocalUser(
            Guid.NewGuid(),
            firstEmail,
            "first-password-hash",
            [new TenantMembership(firstTenantIds[0]), new TenantMembership(firstTenantIds[1])]);
        var secondUser = new LocalUser(
            Guid.NewGuid(),
            secondEmail,
            "second-password-hash",
            [new TenantMembership(secondTenantIds[0]), new TenantMembership(secondTenantIds[1])]);

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(firstEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstUser);
        userStore
            .Setup(store => store.FindByEmailAsync(secondEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondUser);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwordHasher
            .Setup(hasher => hasher.Verify(password, firstUser.PasswordHash))
            .Returns(true);
        passwordHasher
            .Setup(hasher => hasher.Verify(password, secondUser.PasswordHash))
            .Returns(true);

        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        // Act
        Result<AuthenticatedUser> firstResult = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(firstEmail, password));
        Result<AuthenticatedUser> secondResult = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(secondEmail, password));

        // Assert
        firstResult.Value.Tenants.Select(tenant => tenant.TenantId)
            .Should().BeEquivalentTo(firstTenantIds);
        firstResult.Value.Tenants.Select(tenant => tenant.TenantId)
            .Should().NotContain(secondTenantIds);
        secondResult.Value.Tenants.Select(tenant => tenant.TenantId)
            .Should().BeEquivalentTo(secondTenantIds);
        secondResult.Value.Tenants.Select(tenant => tenant.TenantId)
            .Should().NotContain(firstTenantIds);
    }

    [Test]
    public async Task AuthenticateAsync_ReturnsTenantMembershipsWithoutExposingTheUserStoreCollection()
    {
        // Arrange
        const string email = "user@example.com";
        const string password = "CorrectPassword!";
        var tenantMemberships = new List<TenantMembership>
        {
            new(Guid.NewGuid()),
            new(Guid.NewGuid())
        };
        var user = new LocalUser(
            Guid.NewGuid(),
            email,
            "stored-password-hash",
            tenantMemberships);

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwordHasher
            .Setup(hasher => hasher.Verify(password, user.PasswordHash))
            .Returns(true);

        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));
        tenantMemberships.Clear();

        // Assert
        result.Value.Tenants.Should().HaveCount(2);
    }

    [Test]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsAuthenticatedUserWithItsTenant()
    {
        // Arrange
        const string email = "user@example.com";
        const string password = "CorrectPassword!";
        var tenantId = Guid.NewGuid();
        var user = new LocalUser(
            Guid.NewGuid(),
            email,
            "stored-password-hash",
            [new TenantMembership(tenantId)]);

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwordHasher
            .Setup(hasher => hasher.Verify(password, user.PasswordHash))
            .Returns(true);

        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(user.UserId);
        result.Value.Email.Should().Be(user.Email);
        result.Value.Tenants.Should().ContainSingle()
            .Which.TenantId.Should().Be(tenantId);

        userStore.Verify(
            store => store.FindByEmailAsync(email, It.IsAny<CancellationToken>()),
            Times.Once);
        passwordHasher.Verify(
            hasher => hasher.Verify(password, user.PasswordHash),
            Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsAllUserTenants()
    {
        // Arrange
        const string email = "user@example.com";
        const string password = "CorrectPassword!";
        var tenantIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var user = new LocalUser(
            Guid.NewGuid(),
            email,
            "stored-password-hash",
            [new TenantMembership(tenantIds[0]), new TenantMembership(tenantIds[1])]);

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwordHasher
            .Setup(hasher => hasher.Verify(password, user.PasswordHash))
            .Returns(true);

        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tenants.Select(tenant => tenant.TenantId)
            .Should().BeEquivalentTo(tenantIds);
    }

    [Test]
    public async Task AuthenticateAsync_WhenUserHasNoTenant_ReturnsFailure()
    {
        // Arrange
        const string email = "user@example.com";
        const string password = "CorrectPassword!";
        var user = new LocalUser(
            Guid.NewGuid(),
            email,
            "stored-password-hash",
            []);

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwordHasher
            .Setup(hasher => hasher.Verify(password, user.PasswordHash))
            .Returns(true);

        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.NoTenantAccess");
        result.Value.Should().BeNull();
    }

    [Test]
    public async Task AuthenticateAsync_WhenUserDoesNotExist_ReturnsInvalidCredentials()
    {
        // Arrange
        const string email = "unknown@example.com";
        const string password = "AnyPassword!";

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocalUser?)null);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        result.Value.Should().BeNull();

        passwordHasher.Verify(
            hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task AuthenticateAsync_WhenPasswordIsIncorrect_ReturnsInvalidCredentials()
    {
        // Arrange
        const string email = "user@example.com";
        const string password = "IncorrectPassword!";
        var user = new LocalUser(
            Guid.NewGuid(),
            email,
            "stored-password-hash",
            [new TenantMembership(Guid.NewGuid())]);

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwordHasher
            .Setup(hasher => hasher.Verify(password, user.PasswordHash))
            .Returns(false);

        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        // Act
        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        result.Value.Should().BeNull();

        passwordHasher.Verify(
            hasher => hasher.Verify(password, user.PasswordHash),
            Times.Once);
    }
}
