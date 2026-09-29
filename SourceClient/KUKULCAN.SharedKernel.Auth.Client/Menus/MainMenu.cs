using KUKULCAN.SharedKernel.Auth.Client.Authentication;
using KUKULCAN.SharedKernel.Auth.Client.Configuration;

namespace KUKULCAN.SharedKernel.Auth.Client.Menus;

public static class MainMenu
{
    public static async Task RunAsync(DatabaseEnvironmentConfiguration configuration)
    {
        var activeTenantId = ReadGuid("Initial Active Tenant ID");

        while (true)
        {
            using var context = new AuthenticationContext(
                configuration.Provider!,
                configuration.ConnectionString!,
                activeTenantId);

            var continueSession = await RunSessionAsync(context, configuration);
            if (!continueSession)
                return;

            configuration = DatabaseEnvironmentConfiguration.Load();
            if (!configuration.IsComplete)
            {
                configuration = await DatabaseConfigurationMenu.RunAsync(configuration);
            }

            activeTenantId = context.ActiveTenantId;
        }
    }

    private static async Task<bool> RunSessionAsync(
        AuthenticationContext context,
        DatabaseEnvironmentConfiguration configuration)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("==================================================");
            Console.WriteLine(" KUKULCAN.SharedKernel.Auth Client");
            Console.WriteLine("==================================================");
            Console.WriteLine();
            Console.WriteLine($"Database Provider: {configuration.Provider}");
            Console.WriteLine("Connection String: CONFIGURED");
            Console.WriteLine($"Active Tenant ID: {context.ActiveTenantId}");
            Console.WriteLine();
            Console.WriteLine("Authentication");
            Console.WriteLine("--------------");
            Console.WriteLine("1. Local Authentication");
            Console.WriteLine("2. Google Authentication");
            Console.WriteLine("3. Microsoft Authentication");
            Console.WriteLine("4. Apple Authentication");
            Console.WriteLine();
            Console.WriteLine("Client Operations");
            Console.WriteLine("-----------------");
            Console.WriteLine("5. Auth Store and Credential Operations");
            Console.WriteLine("6. Password Operations");
            Console.WriteLine("7. Authentication Data Operations");
            Console.WriteLine("8. Tenant Context");
            Console.WriteLine("9. Database Configuration");
            Console.WriteLine();
            Console.WriteLine("0. Exit");
            Console.WriteLine();
            Console.Write("Select an option: ");

            switch (Console.ReadLine())
            {
                case "1": await LocalAuthenticationMenu.RunAsync(context); break;
                case "2": await FederatedAuthenticationMenu.RunAsync(context, "Google"); break;
                case "3": await FederatedAuthenticationMenu.RunAsync(context, "Microsoft"); break;
                case "4": await FederatedAuthenticationMenu.RunAsync(context, "Apple"); break;
                case "5": await OtherOperationsMenu.RunAsync(context); break;
                case "6": await PasswordOperationsMenu.RunAsync(); break;
                case "7": await AuthenticationDataMenu.RunAsync(context); break;
                case "8": RunTenantContext(context); break;
                case "9":
                    await DatabaseConfigurationMenu.RunAsync(DatabaseEnvironmentConfiguration.Load());
                    return true;
                case "0": return false;
                default:
                    Console.WriteLine("Invalid option.");
                    Console.WriteLine("Press Enter to continue...");
                    Console.ReadLine();
                    break;
            }
        }
    }

    private static void RunTenantContext(AuthenticationContext context)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Tenant Context");
            Console.WriteLine("==============");
            Console.WriteLine($"Active Tenant ID: {context.ActiveTenantId}");
            Console.WriteLine();
            Console.WriteLine("1. Show Active Tenant");
            Console.WriteLine("2. Change Active Tenant");
            Console.WriteLine("0. Back");
            Console.Write("Select an option: ");

            switch (Console.ReadLine())
            {
                case "1": TenantContextMenu.Show(context); break;
                case "2": TenantContextMenu.Change(context); break;
                case "0": return;
                default: Console.WriteLine("Invalid option."); break;
            }
        }
    }

    private static Guid ReadGuid(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            if (Guid.TryParse(Console.ReadLine(), out var value)) return value;
            Console.WriteLine("A valid GUID is required.");
        }
    }
}