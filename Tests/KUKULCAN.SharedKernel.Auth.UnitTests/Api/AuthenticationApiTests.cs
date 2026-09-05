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
        var user = CreateUser([new TenantMembership(tenantId)]);
        using var client = await CreateClientAsync(user);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/local", new
        {
            email = user.Email,
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

    [Test]
    public async Task AuthenticationApi_WhenUserHasMultipleTenants_ReturnsAllTenantMemberships()
    {
        // Arrange
        var tenantOne = new TenantMembership(Guid.NewGuid());
        var tenantTwo = new TenantMembership(Guid.NewGuid());
        var user = CreateUser([tenantOne, tenantTwo]);
        using var client = await CreateClientAsync(user);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/local", new
        {
            email = user.Email,
            password = "correct-password"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authenticatedUser = await response.Content.ReadFromJsonAsync<AuthenticatedUser>();
        authenticatedUser.Should().NotBeNull();
        authenticatedUser!.Tenants.Should().HaveCount(2);
        authenticatedUser.Tenants.Should().Contain(tenantOne);
        authenticatedUser.Tenants.Should().Contain(tenantTwo);
    }

    [Test]
    public async Task AuthenticationApi_WhenUserDoesNotExist_ReturnsUnauthorized()
    {
        // Arrange
        using var client = await CreateClientAsync(null);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/local", new
        {
            email = "missing@example.com",
            password = "correct-password"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AuthenticationApi_WhenPasswordIsIncorrect_ReturnsUnauthorized()
    {
        // Arrange
        var user = CreateUser([new TenantMembership(Guid.NewGuid())]);
        using var client = await CreateClientAsync(user);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/local", new
        {
            email = user.Email,
            password = "incorrect-password"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AuthenticationApi_WhenUserHasNoTenant_ReturnsForbidden()
    {
        // Arrange
        var user = CreateUser([]);
        using var client = await CreateClientAsync(user);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/local", new
        {
            email = user.Email,
            password = "correct-password"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task AuthenticationApi_WhenEmailIsEmptyOrWhitespace_ReturnsBadRequest(string email)
    {
        // Arrange
        using var client = await CreateClientAsync(null);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/local", new
        {
            email,
            password = "correct-password"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task AuthenticationApi_WhenPasswordIsEmptyOrWhitespace_ReturnsBadRequest(string password)
    {
        // Arrange
        using var client = await CreateClientAsync(null);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/local", new
        {
            email = "user@example.com",
            password
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AuthenticationApi_WhenRequestBodyIsMissing_ReturnsBadRequest()
    {
        // Arrange
        using var client = await CreateClientAsync(null);

        // Act
        using var response = await client.PostAsync("/api/auth/local", content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static LocalUser CreateUser(IReadOnlyCollection<TenantMembership> tenants)
        => new(
            Guid.NewGuid(),
            "user@example.com",
            "stored-password-hash",
            tenants);

    private static async Task<HttpClient> CreateClientAsync(LocalUser? user)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.AddSingleton<ILocalUserStore>(new StubLocalUserStore(user));
                services.AddSingleton<IPasswordHasher>(new StubPasswordHasher());
            }));

        return factory.CreateClient();
    }

    private sealed class StubLocalUserStore(LocalUser? user) : ILocalUserStore
    {
        public Task<LocalUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(user);
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public bool Verify(string password, string passwordHash)
            => password == "correct-password" && passwordHash == "stored-password-hash";
    }
}
