using KUKULCAN.SharedKernel.Auth.Client.Configuration;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.Client.UnitTests;

[TestFixture]
public sealed class DatabaseEnvironmentConfigurationTests
{
    [Test]
    public void IsComplete_WhenProviderAndConnectionStringAreMissing_ReturnsFalse()
    {
        var configuration = new DatabaseEnvironmentConfiguration(
            provider: null,
            connectionString: null);

        Assert.That(configuration.IsComplete, Is.False);
    }
}
