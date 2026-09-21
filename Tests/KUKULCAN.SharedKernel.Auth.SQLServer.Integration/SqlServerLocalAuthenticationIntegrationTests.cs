namespace KUKULCAN.SharedKernel.Auth.SQLServer.Integration;

[TestFixture]
[NonParallelizable]
public sealed class SqlServerLocalAuthenticationIntegrationTests
{
    [Test]
    public async Task AuthenticateAsync_WithPersistedUser_ReturnsAllTenantMemberships()
    {
        // This test intentionally defines the persistence boundary before its implementation.
        await using var context = await AuthDbContextFactory.CreateAsync(
            SqlServerAuthenticationDatabase.ConnectionString);

        var userId = Guid.NewGuid();
        var firstTenantId = Guid.NewGuid();
        var secondTenantId = Guid.NewGuid();

        context.Users.Add(new AuthUserEntity
        {
            UserId = userId,
            Email = "user@example.com",
            PasswordHash = "stored-password-hash"
        });

        context.TenantMemberships.AddRange(
            new AuthTenantMembershipEntity
            {
                UserId = userId,
                TenantId = firstTenantId
            },
            new AuthTenantMembershipEntity
            {
                UserId = userId,
                TenantId = secondTenantId
            });

        await context.SaveChangesAsync();

        var store = new LocalUserStore(context);
        var passwordHasher = new TestPasswordHasher("CorrectPassword!", "stored-password-hash");
        var service = new LocalAuthenticationService(store, passwordHasher);

        var result = await service.AuthenticateAsync(
            new LocalAuthenticationRequest("USER@EXAMPLE.COM", "CorrectPassword!"));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.UserId, Is.EqualTo(userId));
        Assert.That(result.Value.Email, Is.EqualTo("user@example.com"));
        Assert.That(
            result.Value.Tenants.Select(x => x.TenantId),
            Is.EquivalentTo(new[] { firstTenantId, secondTenantId }));
    }
}

internal sealed class TestPasswordHasher(string expectedPassword, string expectedHash) : IPasswordHasher
{
    public bool Verify(string password, string passwordHash)
        => password == expectedPassword && passwordHash == expectedHash;
}
