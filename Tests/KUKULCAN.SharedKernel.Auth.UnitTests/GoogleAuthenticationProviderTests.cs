using FluentAssertions;
using KUKULCAN.SharedKernel.Results;
using Moq;

namespace KUKULCAN.SharedKernel.Auth.UnitTests;

[TestFixture]
public sealed class GoogleAuthenticationProviderTests
{
    [Test]
    public void Provider_ReturnsGoogle()
    {
        var validator = new Mock<IFederatedIdentityTokenValidator>(MockBehavior.Strict);
        var provider = new GoogleAuthenticationProvider(validator.Object);

        provider.Provider.Should().Be("Google");
    }

    [Test]
    public async Task AuthenticateAsync_WhenCredentialIsValid_ReturnsValidatedIdentity()
    {
        const string credential = "google-id-token";
        var expectedIdentity = new FederatedIdentity("Google", "google-subject", "user@example.com");
        var validator = new Mock<IFederatedIdentityTokenValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync("Google", credential, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(expectedIdentity));
        var provider = new GoogleAuthenticationProvider(validator.Object);

        Result<FederatedIdentity> result = await provider.AuthenticateAsync(
            new FederatedAuthenticationRequest("Google", credential));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedIdentity);
        validator.Verify(item => item.ValidateAsync("Google", credential, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_WhenCredentialIsInvalid_ReturnsValidatorFailure()
    {
        const string credential = "invalid-google-id-token";
        var failure = new Error("Auth.FederatedCredentialInvalid", "The federated credential is invalid.");
        var validator = new Mock<IFederatedIdentityTokenValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync("Google", credential, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Failure(failure));
        var provider = new GoogleAuthenticationProvider(validator.Object);

        Result<FederatedIdentity> result = await provider.AuthenticateAsync(
            new FederatedAuthenticationRequest("Google", credential));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(failure);
    }

    [Test]
    public async Task AuthenticateAsync_PropagatesCancellationToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;
        var identity = new FederatedIdentity("Google", "google-subject", "user@example.com");
        var validator = new Mock<IFederatedIdentityTokenValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync("Google", "credential", token))
            .ReturnsAsync(Result<FederatedIdentity>.Success(identity));
        var provider = new GoogleAuthenticationProvider(validator.Object);

        await provider.AuthenticateAsync(new FederatedAuthenticationRequest("Google", "credential"), token);

        validator.Verify(item => item.ValidateAsync("Google", "credential", token), Times.Once);
    }
}
