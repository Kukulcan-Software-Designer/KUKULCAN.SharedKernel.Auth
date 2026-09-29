using KUKULCAN.SharedKernel.Auth.Authentication.Federated;
using KUKULCAN.SharedKernel.Auth.Authentication.Local;
using KUKULCAN.SharedKernel.Auth.Client.Authentication;

namespace KUKULCAN.SharedKernel.Auth.Client.Menus;

public static class OtherOperationsMenu
{
    public static async Task RunAsync(AuthenticationContext context)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Other Operations");
            Console.WriteLine("================");
            Console.WriteLine($"Active Tenant ID: {context.ActiveTenantId}");
            Console.WriteLine();
            Console.WriteLine("1. Find Local User by Email");
            Console.WriteLine("2. Find Federated User by Provider and Subject");
            Console.WriteLine("3. Validate Google Credential");
            Console.WriteLine("4. Validate Microsoft Credential");
            Console.WriteLine("5. Validate Apple Credential");
            Console.WriteLine("0. Back");
            Console.Write("Select an option: ");

            switch (Console.ReadLine())
            {
                case "1": await FindLocalUserAsync(context); break;
                case "2": await FindFederatedUserAsync(context); break;
                case "3": await ValidateCredentialAsync("Google"); break;
                case "4": await ValidateCredentialAsync("Microsoft"); break;
                case "5": await ValidateCredentialAsync("Apple"); break;
                case "0": return;
                default: Console.WriteLine("Invalid option."); Pause(); break;
            }
        }
    }

    private static async Task FindLocalUserAsync(AuthenticationContext context)
    {
        Console.Clear();
        var user = await new LocalUserStore(context.DbContext).FindByEmailAsync(ReadRequired("Email"));
        Console.WriteLine();
        if (user is null) Console.WriteLine("User: NOT FOUND");
        else PrintUser(user);
        Pause();
    }

    private static async Task FindFederatedUserAsync(AuthenticationContext context)
    {
        Console.Clear();
        var provider = ReadRequired("Provider");
        var subject = ReadRequired("External Subject");
        var user = await new FederatedUserStore(context.DbContext)
            .FindByFederatedIdentityAsync(provider, subject);
        Console.WriteLine();
        if (user is null) Console.WriteLine("User: NOT FOUND");
        else PrintUser(user);
        Pause();
    }

    private static async Task ValidateCredentialAsync(string providerName)
    {
        Console.Clear();
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
        catch (Exception ex) { Console.WriteLine($"Validation error: {ex.GetBaseException().Message}"); }

        Pause();
    }

    private static void PrintUser(LocalUser user)
    {
        Console.WriteLine("User: FOUND");
        Console.WriteLine($"User ID: {user.UserId}");
        Console.WriteLine($"Email: {user.Email}");
        Console.WriteLine($"Active Tenant Access: {user.HasActiveTenantAccess}");
        Console.WriteLine($"Tenant memberships: {user.Tenants.Count}");
        foreach (var membership in user.Tenants) Console.WriteLine($"  - {membership.TenantId}");
    }

    private static string ReadRequired(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var value = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            Console.WriteLine("A value is required.");
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