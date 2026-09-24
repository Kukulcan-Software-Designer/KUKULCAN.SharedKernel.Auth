using FluentAssertions;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.UnitTests;

[TestFixture]
public sealed class PasswordHasherTests
{
    private PasswordHasher _hasher = null!;

    [SetUp]
    public void SetUp()
    {
        _hasher = new PasswordHasher();
    }

    [Test]
    public void Hash_WithSamePassword_ProducesDifferentHashes()
    {
        const string password = "CorrectPassword!";

        var firstHash = _hasher.Hash(password);
        var secondHash = _hasher.Hash(password);

        firstHash.Should().NotBeNullOrWhiteSpace();
        secondHash.Should().NotBeNullOrWhiteSpace();
        firstHash.Should().NotBe(secondHash);
    }

    [Test]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        const string password = "CorrectPassword!";
        var hash = _hasher.Hash(password);

        _hasher.Verify(password, hash).Should().BeTrue();
    }

    [Test]
    public void Verify_WithIncorrectPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("CorrectPassword!");

        _hasher.Verify("IncorrectPassword!", hash).Should().BeFalse();
    }

    [Test]
    public void Verify_WithMalformedHash_ReturnsFalse()
    {
        _hasher.Verify("AnyPassword!", "not-a-valid-password-hash").Should().BeFalse();
    }

    [Test]
    public void Hash_WhenPasswordIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _hasher.Hash(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Verify_WhenPasswordIsNull_ThrowsArgumentNullException()
    {
        var hash = _hasher.Hash("CorrectPassword!");

        Action act = () => _hasher.Verify(null!, hash);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Verify_WhenHashIsNull_ThrowsArgumentNullException()
    {
        Action act = () => _hasher.Verify("CorrectPassword!", null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
