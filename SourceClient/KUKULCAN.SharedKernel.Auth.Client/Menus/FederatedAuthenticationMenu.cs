using KUKULCAN.SharedKernel.Auth.Authentication.Federated;
using KUKULCAN.SharedKernel.Auth.Client.Authentication;
using KUKULCAN.SharedKernel.Auth.Client.Configuration;

namespace KUKULCAN.SharedKernel.Auth.Client.Menus;

public static class FederatedAuthenticationMenu
{
    public static async Task RunAsync(
        DatabaseEnvironmentConfiguration configuration,
        string providerName)
    {
        Console.Clear();
        Console.WriteLine("==================================================");
        Console.WriteLine($" {providerName} Authentication");
        Console.WriteLine("==================================================");
        Console.WriteLine();

        var tenantId = ReadGuid("Active Tenant ID");
        var clientId = ReadRequired($"{providerName} Client ID");
        var credential = ReadSecret("Credential / ID Token");

        try
        {
            using var context = new AuthenticationContext(
                configuration.Provider!,
                configuration.ConnectionString!,
                tenantId);

            using var httpClient = new HttpClient();

            IFederatedCredentialValidator validator = providerName switch
            {
                "Google" => new GoogleCredentialValidator(clientId, httpClient),
                "Microsoft" => new MicrosoftCredentialValidator(clientId, httpClient),
                "Apple" => new AppleCredentialValidator(clientId, httpClient),
                _ => throw new InvalidOperationException(
                    $"Unsupported federated provider: {providerName}.")
            };

            IFederatedAuthenticationProvider provider = providerName switch
            {
                "Google" => new GoogleFederatedAuthenticationProvider(validator),
                "Microsoft" => new MicrosoftFederatedAuthenticationProvider(validator),
                "Apple" => new AppleFederatedAuthenticationProvider(validator),
                _ => throw new InvalidOperationException(
                    $"Unsupported federated provider: {providerName}.")
            };

            var service = new FederatedAuthenticationService(
                [provider],
                new FederatedUserStore(context.DbContext));

            var result = await service.AuthenticateAsync(
                new FederatedAuthenticationRequest(providerName, credential));

            Console.WriteLine();

            if (result.IsSuccess)
            {
                Console.WriteLine("Authentication: SUCCESS");
                Console.WriteLine($"User ID: {result.Value.UserId}");
                Console.WriteLine($"Email: {result.Value.Email}");
                Console.WriteLine($"Tenant memberships: {result.Value.Tenants.Count}");

                foreach (var membership in result.Value.Tenants)
                    Console.WriteLine($"  - {membership.TenantId}");
            }
            else
            {
                Console.WriteLine("Authentication: FAILURE");
                Console.WriteLine($"Code: {result.Error.Code}");
                Console.WriteLine($"Message: {result.Error.ToString()}");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Authentication cancelled.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Invalid input: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Provider communication error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication error: {ex.GetBaseException().Message}");
        }

        Console.WriteLine();
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private static string ReadRequired(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var value = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();

            Console.WriteLine($"{label} is required.");
        }
    }

    private static Guid ReadGuid(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");

            if (Guid.TryParse(Console.ReadLine(), out var value))
                return value;

            Console.WriteLine("A valid GUID is required.");
        }
    }

    private static string ReadSecret(string label)
    {
        Console.Write($"{label}: ");
        var buffer = new List<char>();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string(buffer.ToArray());
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Count > 0)
                {
                    buffer.RemoveAt(buffer.Count - 1);
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                buffer.Add(key.KeyChar);
                Console.Write('*');
            }
        }
    }
}