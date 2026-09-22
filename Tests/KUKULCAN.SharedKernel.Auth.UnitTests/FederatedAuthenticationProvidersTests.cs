using FluentAssertions;
using KUKULCAN.SharedKernel.Results;
using Moq;

namespace KUKULCAN.SharedKernel.Auth.UnitTests;

[TestFixture]
public sealed class FederatedAuthenticationProvidersTests
{
    [TestCase("Google")]
    [TestCase("Microsoft")]
    [TestCase("Apple")]
    public void Provider_ExposesExpectedProviderName(string providerName)
    {
        // Arrange
        var validator = new Mock<IFederatedCredentialValidator>(MockBehavior.Strict);
        var provider = CreateProvider(providerName, validator.Object);

        // Act
        string result = provider.Provider;

        // Assert
        result.Should().Be(providerName);
    }

    [TestCase("Google")]
    [TestCase("Microsoft")]
    [TestCase("Apple")]
    public async Task AuthenticateAsync_DelegatesCredentialValidationAndReturnsIdentity(string providerName)
    {
        // Arrange
        const string credential = "external-credential";
        var identity = new FederatedIdentity(providerName, "stable-subject", "user@example.com");
        var validator = new Mock<IFederatedCredentialValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync(
                credential,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(identity));
        var provider = CreateProvider(providerName, validator.Object);

        // Act
        Result<FederatedIdentity> result = await provider.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, credential));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(identity);
        validator.Verify(
            item => item.ValidateAsync(credential, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestCase("Google")]
    [TestCase("Microsoft")]
    [TestCase("Apple")]
    public async Task AuthenticateAsync_WhenCredentialIsRejected_ReturnsValidatorFailure(string providerName)
    {
        // Arrange
        const string credential = "invalid-credential";
        var expectedError = new Error(
            $"Auth.{providerName}CredentialInvalid",
            "The federated credential is invalid.");
        var validator = new Mock<IFederatedCredentialValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync(credential, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Failure(expectedError));
        var provider = CreateProvider(providerName, validator.Object);

        // Act
        Result<FederatedIdentity> result = await provider.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, credential));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(expectedError);
    }

    [TestCase("Google")]
    [TestCase("Microsoft")]
    [TestCase("Apple")]
    public async Task AuthenticateAsync_PropagatesCancellationToken(string providerName)
    {
        // Arrange
        const string credential = "external-credential";
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var identity = new FederatedIdentity(providerName, "stable-subject", "user@example.com");
        var validator = new Mock<IFederatedCredentialValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync(credential, cancellationToken))
            .ReturnsAsync(Result<FederatedIdentity>.Success(identity));
        var provider = CreateProvider(providerName, validator.Object);

        // Act
        Result<FederatedIdentity> result = await provider.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, credential),
            cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        validator.Verify(
            item => item.ValidateAsync(credential, cancellationToken),
            Times.Once);
    }

    [TestCase("Google")]
    [TestCase("Microsoft")]
    [TestCase("Apple")]
    public async Task AuthenticateAsync_WhenRequestTargetsAnotherProvider_DoesNotValidateCredential(string providerName)
    {
        // Arrange
        var validator = new Mock<IFederatedCredentialValidator>(MockBehavior.Strict);
        var provider = CreateProvider(providerName, validator.Object);

        // Act
        Result<FederatedIdentity> result = await provider.AuthenticateAsync(
            new FederatedAuthenticationRequest("Other", "external-credential"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.FederatedProviderMismatch");
        validator.VerifyNoOtherCalls();
    }

    [TestCase("Google")]
    [TestCase("Microsoft")]
    [TestCase("Apple")]
    public async Task AuthenticateAsync_WhenCredentialIsEmpty_ThrowsArgumentException(string providerName)
    {
        // Arrange
        var validator = new Mock<IFederatedCredentialValidator>(MockBehavior.Strict);
        var provider = CreateProvider(providerName, validator.Object);

        // Act
        Func<Task> act = () => provider.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, "   "));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        validator.VerifyNoOtherCalls();
    }


    [TestCase("Google")]
    [TestCase("Microsoft")]
    [TestCase("Apple")]
    public void Constructor_WhenCredentialValidatorIsNull_ThrowsArgumentNullException(string providerName)
    {
        Action act = () => CreateProvider(providerName, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [TestCase("Google")]
    [TestCase("Microsoft")]
    [TestCase("Apple")]
    public async Task AuthenticateAsync_WhenValidatorThrows_PropagatesTheValidatorException(string providerName)
    {
        const string credential = "external-credential";
        var expectedException = new InvalidOperationException("Validator failure.");
        var validator = new Mock<IFederatedCredentialValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync(credential, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);
        var provider = CreateProvider(providerName, validator.Object);

        Func<Task> act = () => provider.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName, credential));

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
    }

    [TestCase("Google")]
    [TestCase("Microsoft")]
    [TestCase("Apple")]
    public async Task AuthenticateAsync_WhenRequestedProviderDiffersOnlyByCase_DelegatesToValidator(string providerName)
    {
        const string credential = "external-credential";
        var identity = new FederatedIdentity(providerName, "stable-subject", "user@example.com");
        var validator = new Mock<IFederatedCredentialValidator>(MockBehavior.Strict);
        validator
            .Setup(item => item.ValidateAsync(credential, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FederatedIdentity>.Success(identity));
        var provider = CreateProvider(providerName, validator.Object);

        Result<FederatedIdentity> result = await provider.AuthenticateAsync(
            new FederatedAuthenticationRequest(providerName.ToUpperInvariant(), credential));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(identity);
        validator.Verify(
            item => item.ValidateAsync(credential, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static IFederatedAuthenticationProvider CreateProvider(
        string providerName,
        IFederatedCredentialValidator validator)
        => providerName switch
        {
            "Google" => new GoogleFederatedAuthenticationProvider(validator),
            "Microsoft" => new MicrosoftFederatedAuthenticationProvider(validator),
            "Apple" => new AppleFederatedAuthenticationProvider(validator),
            _ => throw new ArgumentOutOfRangeException(nameof(providerName), providerName, null)
        };
}
