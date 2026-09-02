using FluentAssertions;
using KUKULCAN.SharedKernel.Results;
using Moq;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.UnitTests;

[TestFixture]
public sealed class LocalAuthenticationServiceTests
{
    [Test]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsAuthenticatedUser()
    {
        // Arrange
        const string email = "user@example.com";
        const string password = "CorrectPassword!";
        var user = new LocalUser(Guid.NewGuid(), email, "stored-password-hash");

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

        userStore.Verify(
            store => store.FindByEmailAsync(email, It.IsAny<CancellationToken>()),
            Times.Once);
        passwordHasher.Verify(
            hasher => hasher.Verify(password, user.PasswordHash),
            Times.Once);
    }
}
