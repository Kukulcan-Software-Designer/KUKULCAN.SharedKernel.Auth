using FluentAssertions;
using KUKULCAN.SharedKernel.Results;
using Moq;

namespace KUKULCAN.SharedKernel.Auth.UnitTests;

[TestFixture]
public sealed class MicrosoftAuthenticationProviderTests
{
    [Test]
    public void Provider_ReturnsMicrosoft()
    {
        var validator = new Mock<IFederatedIdentityTokenValidator>(MockBehavior.Strict);
        var provider = new MicrosoftAuthenticationProvider(validator.Object);

        provider.Provider.Should().Be("Microsoft");
    }

    [Test]
    public async Task AuthenticateAsync_WhenCredentialIsValid_ReturnsValidatedIdentity()
    {
        const string credential = "microsoft-id-token";
        var expectedIdentity = new FederatedIdentity("Microsoft", "microsoft-subject", "user@example.com");
        var validator = new Mock<IFederatedIdentityTokenValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync("Microsoft", credential, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(expectedIdentity));
        var provider = new MicrosoftAuthenticationProvider(validator.Object);

        Result<FederatedIdentity> result = await provider.AuthenticateAsync(
            new FederatedAuthenticationRequest("Microsoft", credential));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedIdentity);
        validator.Verify(item => item.ValidateAsync("Microsoft", credential, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_WhenCredentialIsInvalid_ReturnsValidatorFailure()
    {
        const string credential = "invalid-microsoft-id-token";
        var failure = new Error("Auth.FederatedCredentialInvalid", "The federated credential is invalid.");
        var validator = new Mock<IFederatedIdentityTokenValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync("Microsoft", credential, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Failure(failure));
        var provider = new MicrosoftAuthenticationProvider(validator.Object);

        Result<FederatedIdentity> result = await provider.AuthenticateAsync(
            new FederatedAuthenticationRequest("Microsoft", credential));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(failure);
    }

    [Test]
    public async Task AuthenticateAsync_PropagatesCancellationToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;
        var identity = new FederatedIdentity("Microsoft", "microsoft-subject", "user@example.com");
        var validator = new Mock<IFederatedIdentityTokenValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync("Microsoft", "credential", token))
            .ReturnsAsync(Result<FederatedIdentity>.Success(identity));
        var provider = new MicrosoftAuthenticationProvider(validator.Object);

        await provider.AuthenticateAsync(new FederatedAuthenticationRequest("Microsoft", "credential"), token);

        validator.Verify(item => item.ValidateAsync("Microsoft", "credential", token), Times.Once);
    }
}
