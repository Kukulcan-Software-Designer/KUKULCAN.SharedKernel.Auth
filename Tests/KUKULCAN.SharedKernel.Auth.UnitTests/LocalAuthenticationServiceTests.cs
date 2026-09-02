using FluentAssertions;
using KUKULCAN.SharedKernel.Results;
using Moq;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.UnitTests;

[TestFixture]
public sealed class LocalAuthenticationServiceTests
{
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
}
