using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using KUKULCAN.SharedKernel.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace KUKULCAN.SharedKernel.Auth.UnitTests.Api;

[TestFixture]
public sealed class AuthenticationApiTests
{
    [Test]
    public async Task AuthenticationApi_WhenValidLocalCredentialsArePosted_ReturnsAuthenticatedUser()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var user = new LocalUser(
            Guid.NewGuid(),
            "user@example.com",
            "stored-password-hash",
            [new TenantMembership(tenantId)]);

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.AddSingleton<ILocalUserStore>(new StubLocalUserStore(user));
                services.AddSingleton<IPasswordHasher>(new StubPasswordHasher());
            }));

        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/local", new
        {
            email = "user@example.com",
            password = "correct-password"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authenticatedUser = await response.Content.ReadFromJsonAsync<AuthenticatedUser>();
        authenticatedUser.Should().NotBeNull();
        authenticatedUser!.UserId.Should().Be(user.UserId);
        authenticatedUser.Email.Should().Be(user.Email);
        authenticatedUser.Tenants.Should().ContainSingle(tenant => tenant.TenantId == tenantId);
    }

    private sealed class StubLocalUserStore(LocalUser user) : ILocalUserStore
    {
        public Task<LocalUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult<LocalUser?>(user);
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public bool Verify(string password, string passwordHash)
            => password == "correct-password" && passwordHash == "stored-password-hash";
    }
}
