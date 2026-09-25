using FluentAssertions;
using KUKULCAN.SharedKernel.Auth.Authentication.Local;
using KUKULCAN.SharedKernel.Results;
using Moq;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.UnitTests;

[TestFixture]
public sealed class LocalAuthenticationServiceTests
{
    [Test]
    public void Constructor_WhenUserStoreIsNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new LocalAuthenticationService(
            null!,
            new Mock<IPasswordHasher>(MockBehavior.Strict).Object);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_WhenPasswordHasherIsNull_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new LocalAuthenticationService(
            new Mock<ILocalUserStore>(MockBehavior.Strict).Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void AuthenticateAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        Func<Task> act = () => service.AuthenticateAsync(null!);

        act.Should().ThrowAsync<ArgumentNullException>();
        userStore.VerifyNoOtherCalls();
        passwordHasher.VerifyNoOtherCalls();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void AuthenticateAsync_WhenEmailIsEmptyOrWhitespace_ThrowsArgumentException(string email)
    {
        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        Func<Task> act = () => service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, "CorrectPassword!"));

        act.Should().ThrowAsync<ArgumentException>();
        userStore.VerifyNoOtherCalls();
        passwordHasher.VerifyNoOtherCalls();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void AuthenticateAsync_WhenPasswordIsEmptyOrWhitespace_ThrowsArgumentException(string password)
    {
        const string email = "user@example.com";
        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        Func<Task> act = () => service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        act.Should().ThrowAsync<ArgumentException>();
        userStore.VerifyNoOtherCalls();
        passwordHasher.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_NormalizesEmailBeforeLookingUpUser()
    {
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

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(suppliedEmail, password));

        result.IsSuccess.Should().BeTrue();
        userStore.Verify(
            store => store.FindByEmailAsync(normalizedEmail, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_PropagatesCancellationTokenToUserStore()
    {
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

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password),
            cancellationToken);

        result.IsSuccess.Should().BeFalse();
        userStore.Verify(
            store => store.FindByEmailAsync(email, cancellationToken),
            Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_WhenUserStoreIsCanceled_PropagatesOperationCanceledException()
    {
        const string email = "user@example.com";
        const string password = "CorrectPassword!";
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var cancellationException = new OperationCanceledException(cancellationToken);

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(email, cancellationToken))
            .ThrowsAsync(cancellationException);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        Func<Task> act = () => service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password),
            cancellationToken);

        var exception = await act.Should().ThrowAsync<OperationCanceledException>();
        exception.Which.CancellationToken.Should().Be(cancellationToken);
        passwordHasher.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_DoesNotMixTenantMembershipsBetweenUsers()
    {
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

        Result<AuthenticatedUser> firstResult = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(firstEmail, password));
        Result<AuthenticatedUser> secondResult = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(secondEmail, password));

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

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));
        tenantMemberships.Clear();

        result.Value.Tenants.Should().HaveCount(2);
    }

    [Test]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsAuthenticatedUserWithItsTenant()
    {
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

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

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

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        result.IsSuccess.Should().BeTrue();
        result.Value.Tenants.Select(tenant => tenant.TenantId)
            .Should().BeEquivalentTo(tenantIds);
    }

    [Test]
    public async Task AuthenticateAsync_WhenReturnedUserEmailDiffersFromLookupEmail_ReturnsStoreEmail()
    {
        const string lookupEmail = "lookup@example.com";
        const string storedEmail = "canonical@example.com";
        const string password = "CorrectPassword!";
        var user = new LocalUser(
            Guid.NewGuid(),
            storedEmail,
            "stored-password-hash",
            [new TenantMembership(Guid.NewGuid())]);

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(lookupEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwordHasher
            .Setup(hasher => hasher.Verify(password, user.PasswordHash))
            .Returns(true);

        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(lookupEmail, password));

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(storedEmail);
    }

    [Test]
    public async Task AuthenticateAsync_WhenUserHasDuplicateTenants_PreservesStoreMemberships()
    {
        const string email = "user@example.com";
        const string password = "CorrectPassword!";
        var tenantId = Guid.NewGuid();
        var memberships = new[]
        {
            new TenantMembership(tenantId),
            new TenantMembership(tenantId)
        };
        var user = new LocalUser(Guid.NewGuid(), email, "stored-password-hash", memberships);

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwordHasher
            .Setup(hasher => hasher.Verify(password, user.PasswordHash))
            .Returns(true);

        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        result.IsSuccess.Should().BeTrue();
        result.Value.Tenants.Should().HaveCount(2);
        result.Value.Tenants.Select(tenant => tenant.TenantId)
            .Should().OnlyContain(id => id == tenantId);
    }

    [Test]
    public async Task AuthenticateAsync_WhenUserHasNoTenant_ReturnsFailure()
    {
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

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.NoTenantAccess");
        result.Error.Description.Should().Be("The user does not belong to any tenant.");
    }

    [Test]
    public async Task AuthenticateAsync_WhenUserDoesNotExist_ReturnsInvalidCredentials()
    {
        const string email = "unknown@example.com";
        const string password = "AnyPassword!";

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocalUser?)null);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        result.Error.Description.Should().Be("The supplied credentials are invalid.");

        passwordHasher.Verify(
            hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task AuthenticateAsync_WhenPasswordIsIncorrect_ReturnsInvalidCredentials()
    {
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

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        result.Error.Description.Should().Be("The supplied credentials are invalid.");

        passwordHasher.Verify(
            hasher => hasher.Verify(password, user.PasswordHash),
            Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_WhenUserDoesNotExist_DoesNotVerifyPassword()
    {
        const string email = "unknown@example.com";
        const string password = "AnyPassword!";
        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocalUser?)null);
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        passwordHasher.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_WhenPasswordIsIncorrect_ReturnsSameErrorAsUnknownUser()
    {
        const string password = "IncorrectPassword!";
        var unknownStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        unknownStore
            .Setup(store => store.FindByEmailAsync("unknown@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocalUser?)null);
        var unknownHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var unknownService = new LocalAuthenticationService(unknownStore.Object, unknownHasher.Object);

        var existingUser = new LocalUser(
            Guid.NewGuid(),
            "user@example.com",
            "stored-password-hash",
            [new TenantMembership(Guid.NewGuid())]);
        var existingStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        existingStore
            .Setup(store => store.FindByEmailAsync(existingUser.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        var existingHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        existingHasher
            .Setup(hasher => hasher.Verify(password, existingUser.PasswordHash))
            .Returns(false);
        var existingService = new LocalAuthenticationService(existingStore.Object, existingHasher.Object);

        Result<AuthenticatedUser> unknownResult = await unknownService.AuthenticateAsync(
            new LocalAuthenticationRequest("unknown@example.com", password));
        Result<AuthenticatedUser> incorrectPasswordResult = await existingService.AuthenticateAsync(
            new LocalAuthenticationRequest(existingUser.Email, password));

        incorrectPasswordResult.IsFailure.Should().BeTrue();
        incorrectPasswordResult.Error.Should().Be(unknownResult.Error);
    }

    [Test]
    public async Task AuthenticateAsync_WhenSuccessful_DoesNotExposePasswordHash()
    {
        const string passwordHash = "stored-password-hash";
        var user = new LocalUser(
            Guid.NewGuid(),
            "user@example.com",
            passwordHash,
            [new TenantMembership(Guid.NewGuid())]);
        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwordHasher
            .Setup(hasher => hasher.Verify("CorrectPassword!", passwordHash))
            .Returns(true);
        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(user.Email, "CorrectPassword!"));

        result.Value.Should().NotBeNull();
        result.Value.Should().BeOfType<AuthenticatedUser>();
        result.Value.Email.Should().Be(user.Email);
        result.Value.Tenants.Should().BeEquivalentTo(user.Tenants);
        result.Value.GetType().GetProperties()
            .Select(property => property.Name)
            .Should().NotContain(nameof(LocalUser.PasswordHash));
    }

    [Test]
    public async Task AuthenticateAsync_WhenUserStoreThrows_PropagatesTheStoreException()
    {
        const string email = "user@example.com";
        const string password = "CorrectPassword!";
        var expectedException = new InvalidOperationException("Store failure.");

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        Func<Task> act = () => service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
        passwordHasher.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AuthenticateAsync_WhenPasswordHasherThrows_PropagatesTheHasherException()
    {
        const string email = "user@example.com";
        const string password = "CorrectPassword!";
        var user = new LocalUser(
            Guid.NewGuid(),
            email,
            "stored-password-hash",
            [new TenantMembership(Guid.NewGuid())]);
        var expectedException = new InvalidOperationException("Hasher failure.");

        var userStore = new Mock<ILocalUserStore>(MockBehavior.Strict);
        userStore
            .Setup(store => store.FindByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        passwordHasher
            .Setup(hasher => hasher.Verify(password, user.PasswordHash))
            .Throws(expectedException);

        var service = new LocalAuthenticationService(userStore.Object, passwordHasher.Object);

        Func<Task> act = () => service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
    }

    [Test]
    public async Task AuthenticateAsync_WhenUserHasNoTenants_DoesNotReturnAnAuthenticatedUser()
    {
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

        Result<AuthenticatedUser> result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest(email, password));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.NoTenantAccess");
    }
}
