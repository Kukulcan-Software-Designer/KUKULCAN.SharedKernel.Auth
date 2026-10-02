using KUKULCAN.SharedKernel.Auth.Persistence;
using KUKULCAN.SharedKernel.Database.Configuration;
using NUnit.Framework;

namespace KUKULCAN.SharedKernel.Auth.UnitTests.Persistence;

[TestFixture]
public sealed class AuthMigrationsTests
{
    [TestCase(DatabaseProvider.PostgresSql, "KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL")]
    [TestCase(DatabaseProvider.SqlServer, "KUKULCAN.SharedKernel.Auth.Migrations.SQLServer")]
    [TestCase(DatabaseProvider.MySql, "KUKULCAN.SharedKernel.Auth.Migrations.MySQL")]
    public void GetAssemblyName_ReturnsProviderSpecificMigrationAssembly(
        DatabaseProvider provider,
        string expectedAssemblyName)
    {
        AuthMigrations.GetAssemblyName(provider).Should().Be(expectedAssemblyName);
    }

    [Test]
    public void GetAssemblyName_RejectsUnsupportedProvider()
    {
        var action = () => AuthMigrations.GetAssemblyName((DatabaseProvider)999);

        action.Should().Throw<NotSupportedException>();
    }
}
