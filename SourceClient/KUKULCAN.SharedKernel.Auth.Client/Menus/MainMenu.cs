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
            Console.WriteLine("5. Database Configuration");
            Console.WriteLine();
            Console.WriteLine("0. Exit");
            Console.WriteLine();
            Console.Write("Select an option: ");

            switch (Console.ReadLine())
            {
                case "5":
                    await DatabaseConfigurationMenu.RunAsync(
                        DatabaseEnvironmentConfiguration.Load());
                    configuration = DatabaseEnvironmentConfiguration.Load();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Authentication operation will be implemented next.");
                    Console.WriteLine("Press Enter to continue...");
                    Console.ReadLine();
                    break;
            }
        }
    }
}