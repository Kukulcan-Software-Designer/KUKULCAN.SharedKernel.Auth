using FluentAssertions;
using KUKULCAN.SharedKernel.Results;
using Moq;

namespace KUKULCAN.SharedKernel.Auth.UnitTests;

[TestFixture]
public sealed class AppleAuthenticationProviderTests
{
    [Test]
    public void Provider_ReturnsApple()
    {
        var validator = new Mock<IFederatedIdentityTokenValidator>(MockBehavior.Strict);
        var provider = new AppleAuthenticationProvider(validator.Object);

        provider.Provider.Should().Be("Apple");
    }

    [Test]
    public async Task AuthenticateAsync_WhenCredentialIsValid_ReturnsValidatedIdentity()
    {
        const string credential = "apple-id-token";
        var expectedIdentity = new FederatedIdentity("Apple", "apple-subject", "user@example.com");
        var validator = new Mock<IFederatedIdentityTokenValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync("Apple", credential, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(expectedIdentity));
        var provider = new AppleAuthenticationProvider(validator.Object);

        Result<FederatedIdentity> result = await provider.AuthenticateAsync(
            new FederatedAuthenticationRequest("Apple", credential));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedIdentity);
        validator.Verify(item => item.ValidateAsync("Apple", credential, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_WhenCredentialIsInvalid_ReturnsValidatorFailure()
    {
        const string credential = "invalid-apple-id-token";
        var failure = new Error("Auth.FederatedCredentialInvalid", "The federated credential is invalid.");
        var validator = new Mock<IFederatedIdentityTokenValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync("Apple", credential, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Failure(failure));
        var provider = new AppleAuthenticationProvider(validator.Object);

        Result<FederatedIdentity> result = await provider.AuthenticateAsync(
            new FederatedAuthenticationRequest("Apple", credential));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(failure);
    }

    [Test]
    public async Task AuthenticateAsync_PropagatesCancellationToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;
        var identity = new FederatedIdentity("Apple", "apple-subject", "user@example.com");
        var validator = new Mock<IFederatedIdentityTokenValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync("Apple", "credential", token))
            .ReturnsAsync(Result<FederatedIdentity>.Success(identity));
        var provider = new AppleAuthenticationProvider(validator.Object);

        await provider.AuthenticateAsync(new FederatedAuthenticationRequest("Apple", "credential"), token);

        validator.Verify(item => item.ValidateAsync("Apple", "credential", token), Times.Once);
    }
}
