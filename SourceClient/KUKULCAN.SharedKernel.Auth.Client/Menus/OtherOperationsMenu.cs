using KUKULCAN.SharedKernel.Auth.Authentication.Federated;
using KUKULCAN.SharedKernel.Auth.Authentication.Local;
using KUKULCAN.SharedKernel.Auth.Client.Authentication;
using KUKULCAN.SharedKernel.Auth.Client.Configuration;

namespace KUKULCAN.SharedKernel.Auth.Client.Menus;

public static class OtherOperationsMenu
{
    public static async Task RunAsync(DatabaseEnvironmentConfiguration configuration)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("==================================================");
            Console.WriteLine(" Other Operations");
            Console.WriteLine("==================================================");
            Console.WriteLine();
            Console.WriteLine("1. Find Local User by Email");
            Console.WriteLine("2. Find Federated User by Provider and Subject");
            Console.WriteLine("3. Validate Google Credential");
            Console.WriteLine("4. Validate Microsoft Credential");
            Console.WriteLine("5. Validate Apple Credential");
            Console.WriteLine();
            Console.WriteLine("0. Back");
            Console.WriteLine();
            Console.Write("Select an option: ");

            switch (Console.ReadLine())
            {
                case "1":
                    await FindLocalUserAsync(configuration);
                    break;
                case "2":
                    await FindFederatedUserAsync(configuration);
                    break;
                case "3":
                    await ValidateCredentialAsync(configuration, "Google");
                    break;
                case "4":
                    await ValidateCredentialAsync(configuration, "Microsoft");
                    break;
                case "5":
                    await ValidateCredentialAsync(configuration, "Apple");
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option.");
                    Pause();
                    break;
            }
        }
    }

    private static async Task FindLocalUserAsync(
        DatabaseEnvironmentConfiguration configuration)
    {
        Console.Clear();
        Console.WriteLine("Find Local User by Email");
        Console.WriteLine("========================");
        Console.WriteLine();

        var tenantId = ReadGuid("Active Tenant ID");
        var email = ReadRequired("Email");

        try
        {
            using var context = new AuthenticationContext(
                configuration.Provider!,
                configuration.ConnectionString!,
                tenantId);

            var user = await new LocalUserStore(context.DbContext)
                .FindByEmailAsync(email);

            Console.WriteLine();
            if (user is null)
            {
                Console.WriteLine("User: NOT FOUND");
            }
            else
            {
                PrintUser(user);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            Console.WriteLine();
            Console.WriteLine($"Operation error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Operation error: {ex.GetBaseException().Message}");
        }

        Pause();
    }

    private static async Task FindFederatedUserAsync(
        DatabaseEnvironmentConfiguration configuration)
    {
        Console.Clear();
        Console.WriteLine("Find Federated User");
        Console.WriteLine("===================");
        Console.WriteLine();

        var tenantId = ReadGuid("Active Tenant ID");
        var provider = ReadRequired("Provider");
        var subject = ReadRequired("External Subject");

        try
        {
            using var context = new AuthenticationContext(
                configuration.Provider!,
                configuration.ConnectionString!,
                tenantId);

            var user = await new FederatedUserStore(context.DbContext)
                .FindByFederatedIdentityAsync(provider, subject);

            Console.WriteLine();
            if (user is null)
            {
                Console.WriteLine("User: NOT FOUND");
            }
            else
            {
                PrintUser(user);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            Console.WriteLine();
            Console.WriteLine($"Operation error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Operation error: {ex.GetBaseException().Message}");
        }

        Pause();
    }

    private static async Task ValidateCredentialAsync(
        DatabaseEnvironmentConfiguration configuration,
        string providerName)
    {
        Console.Clear();
        Console.WriteLine($"{providerName} Credential Validation");
        Console.WriteLine(new string('=', providerName.Length + 21));
        Console.WriteLine();

        _ = configuration;
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
                _ => throw new InvalidOperationException(
                    $"Unsupported federated provider: {providerName}.")
            };

            var result = await validator.ValidateAsync(credential);

            Console.WriteLine();
            if (result.IsSuccess)
            {
                Console.WriteLine("Credential: VALID");
                Console.WriteLine($"Provider: {result.Value.Provider}");
                Console.WriteLine($"Subject: {result.Value.Subject}");
                Console.WriteLine($"Email: {result.Value.Email ?? "not supplied"}");
            }
            else
            {
                Console.WriteLine("Credential: INVALID");
                Console.WriteLine($"Code: {result.Error.Code}");
                Console.WriteLine($"Message: {result.Error}");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine();
            Console.WriteLine("Credential validation cancelled.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Invalid input: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Provider communication error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Validation error: {ex.GetBaseException().Message}");
        }

        Pause();
    }

    private static void PrintUser(LocalUser user)
    {
        Console.WriteLine("User: FOUND");
        Console.WriteLine($"User ID: {user.UserId}");
        Console.WriteLine($"Email: {user.Email}");
        Console.WriteLine($"Active Tenant Access: {user.HasActiveTenantAccess}");
        Console.WriteLine($"Tenant memberships: {user.Tenants.Count}");

        foreach (var membership in user.Tenants)
            Console.WriteLine($"  - {membership.TenantId}");
    }

    private static string ReadRequired(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var value = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();

            Console.WriteLine("A value is required.");
        }
    }

    private static Guid ReadGuid(string label)
    {
        while (true)
        {
            var value = ReadRequired(label);

            if (Guid.TryParse(value, out var result))
                return result;

            Console.WriteLine("A valid GUID is required.");
        }
    }

    private static string ReadSecret(string label)
    {
        Console.Write($"{label}: ");
        var characters = new List<char>();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
                break;

            if (key.Key == ConsoleKey.Backspace)
            {
                if (characters.Count > 0)
                {
                    characters.RemoveAt(characters.Count - 1);
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                characters.Add(key.KeyChar);
                Console.Write('*');
            }
        }

        Console.WriteLine();
        return new string(characters.ToArray());
    }

    private static void Pause()
    {
        Console.WriteLine();
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }
}
