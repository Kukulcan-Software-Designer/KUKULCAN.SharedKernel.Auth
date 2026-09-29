using KUKULCAN.SharedKernel.Auth.Client.Configuration;

namespace KUKULCAN.SharedKernel.Auth.Client.Menus;

public static class MainMenu
{
    public static async Task RunAsync(DatabaseEnvironmentConfiguration configuration)
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
            Console.WriteLine();
            Console.WriteLine("Authentication");
            Console.WriteLine("--------------");
            Console.WriteLine("1. Local Authentication");
            Console.WriteLine("2. Google Authentication");
            Console.WriteLine("3. Microsoft Authentication");
            Console.WriteLine("4. Apple Authentication");
            Console.WriteLine();
            Console.WriteLine("Other Operations");
            Console.WriteLine("----------------");
            Console.WriteLine("5. Auth Store and Credential Operations");
            Console.WriteLine("6. Database Configuration");
            Console.WriteLine();
            Console.WriteLine("0. Exit");
            Console.WriteLine();
            Console.Write("Select an option: ");

            switch (Console.ReadLine())
            {
                case "1":
                    await LocalAuthenticationMenu.RunAsync(configuration);
                    break;
                case "2":
                    await FederatedAuthenticationMenu.RunAsync(configuration, "Google");
                    break;
                case "3":
                    await FederatedAuthenticationMenu.RunAsync(configuration, "Microsoft");
                    break;
                case "4":
                    await FederatedAuthenticationMenu.RunAsync(configuration, "Apple");
                    break;
                case "5":
                    await OtherOperationsMenu.RunAsync(configuration);
                    break;
                case "6":
                    await DatabaseConfigurationMenu.RunAsync(
                        DatabaseEnvironmentConfiguration.Load());
                    configuration = DatabaseEnvironmentConfiguration.Load();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option.");
                    Console.WriteLine("Press Enter to continue...");
                    Console.ReadLine();
                    break;
            }
        }
    }
}