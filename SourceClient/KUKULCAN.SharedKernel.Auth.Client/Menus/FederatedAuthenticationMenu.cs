using KUKULCAN.SharedKernel.Auth.Authentication.Federated;
using KUKULCAN.SharedKernel.Auth.Client.Authentication;

namespace KUKULCAN.SharedKernel.Auth.Client.Menus;

public static class FederatedAuthenticationMenu
{
    public static async Task RunAsync(AuthenticationContext context, string providerName)
    {
        Console.Clear();
        Console.WriteLine($"{providerName} Authentication");
        Console.WriteLine(new string('=', providerName.Length + 15));
        Console.WriteLine($"Active Tenant ID: {context.ActiveTenantId}");
        Console.WriteLine();

        var clientId = ReadRequired($"{providerName} Client ID");
        var credential = ReadSecret("Credential / ID Token");

        try
        {
            using var httpClient = new HttpClient();
            IFederatedCredentialValidator validator = providerName switch
            {
                "Google" => new GoogleCredentialValidator(clientId, httpClient),
                "Microsoft" => new MicrosoftCredentialValidator(clientId, httpClient),
                "Apple" => new AppleCredentialValidator(clientId, httpClient),
                _ => throw new InvalidOperationException($"Unsupported federated provider: {providerName}.")
            };

            IFederatedAuthenticationProvider provider = providerName switch
            {
                "Google" => new GoogleFederatedAuthenticationProvider(validator),
                "Microsoft" => new MicrosoftFederatedAuthenticationProvider(validator),
                "Apple" => new AppleFederatedAuthenticationProvider(validator),
                _ => throw new InvalidOperationException($"Unsupported federated provider: {providerName}.")
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
                Console.WriteLine($"Message: {result.Error}");
            }
        }
        catch (OperationCanceledException) { Console.WriteLine("Authentication cancelled."); }
        catch (ArgumentException ex) { Console.WriteLine($"Invalid input: {ex.Message}"); }
        catch (HttpRequestException ex) { Console.WriteLine($"Provider communication error: {ex.Message}"); }
        catch (Exception ex) { Console.WriteLine($"Authentication error: {ex.GetBaseException().Message}"); }

        Pause();
    }

    private static string ReadRequired(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var value = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            Console.WriteLine($"{label} is required.");
        }
    }

    private static string ReadSecret(string label)
    {
        Console.Write($"{label}: ");
        var buffer = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return new string(buffer.ToArray()); }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Count > 0) { buffer.RemoveAt(buffer.Count - 1); Console.Write("\b \b"); }
                continue;
            }
            if (!char.IsControl(key.KeyChar)) { buffer.Add(key.KeyChar); Console.Write('*'); }
        }
    }

    private static void Pause()
    {
        Console.WriteLine();
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }
}