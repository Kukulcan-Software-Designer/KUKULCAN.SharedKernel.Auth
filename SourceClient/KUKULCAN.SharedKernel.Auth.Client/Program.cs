using KUKULCAN.SharedKernel.Auth.Client.Configuration;
using KUKULCAN.SharedKernel.Auth.Client.Menus;

var configuration = DatabaseEnvironmentConfiguration.Load();

while (!configuration.IsComplete)
{
    Console.Clear();
    configuration = await DatabaseConfigurationMenu.RunAsync(configuration);
    if (!configuration.IsComplete)
    {
        Console.WriteLine();
        Console.WriteLine("Database configuration is required before authentication can be used.");
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }
}

await MainMenu.RunAsync(configuration);