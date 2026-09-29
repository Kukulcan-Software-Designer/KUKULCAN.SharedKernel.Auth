using KUKULCAN.SharedKernel.Auth.Client.Configuration;

namespace KUKULCAN.SharedKernel.Auth.Client.Menus;

public static class DatabaseConfigurationMenu
{
    public static async Task<DatabaseEnvironmentConfiguration> RunAsync(
        DatabaseEnvironmentConfiguration current)
    {
        while (!current.IsComplete)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" KUKULCAN.SharedKernel.Auth Client");
            Console.WriteLine("==================================================");
            Console.WriteLine();
            Console.WriteLine($"AUTH_DB_PROVIDER: {(string.IsNullOrWhiteSpace(current.Provider) ? "NOT CONFIGURED" : current.Provider)}");
            Console.WriteLine($"AUTH_DB_CONNECTION_STRING: {(string.IsNullOrWhiteSpace(current.ConnectionString) ? "NOT CONFIGURED" : "CONFIGURED")}");
            Console.WriteLine();
            Console.WriteLine("1. Configure Database Provider");
            if (!string.IsNullOrWhiteSpace(current.Provider))
                Console.WriteLine("2. Configure Database Connection String");
            Console.WriteLine("0. Exit");
            Console.WriteLine();
            Console.Write("Select an option: ");

            switch (Console.ReadLine())
            {
                case "1":
                    current = current with { Provider = SelectProvider() };
                    break;
                case "2" when !string.IsNullOrWhiteSpace(current.Provider):
                    current = current with { ConnectionString = BuildConnectionString(current.Provider!) };
                    break;
                case "0":
                    Environment.Exit(0);
                    return current;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }

            if (current.IsComplete)
                current.SetEnvironmentVariables();
        }

        await Task.CompletedTask;
        return current;
    }

    private static string SelectProvider()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Database Provider");
            Console.WriteLine("-----------------");
            Console.WriteLine("1. SQL Server");
            Console.WriteLine("2. PostgreSQL");
            Console.WriteLine("3. MySQL");
            Console.WriteLine("0. Cancel");
            Console.Write("Select an option: ");

            switch (Console.ReadLine())
            {
                case "1": return "SqlServer";
                case "2": return "PostgreSQL";
                case "3": return "MySQL";
                case "0": return string.Empty;
                default: Console.WriteLine("Invalid option."); break;
            }
        }
    }

    private static string BuildConnectionString(string provider)
    {
        Console.Clear();
        Console.WriteLine($"Connection configuration: {provider}");
        Console.WriteLine("----------------------------------------");

        var host = ReadRequired(provider == "SqlServer" ? "Server" : provider == "MySQL" ? "Server" : "Host");
        var port = ReadOptional("Port", provider == "SqlServer" ? "1433" : provider == "PostgreSQL" ? "5432" : "3306");
        var database = ReadRequired("Database");
        var user = ReadRequired(provider == "PostgreSQL" ? "Username" : "User");
        var password = ReadRequiredSecret("Password");

        return provider switch
        {
            "SqlServer" => $"Server={host},{port};Database={database};User Id={user};Password={password};TrustServerCertificate=True",
            "PostgreSQL" => $"Host={host};Port={port};Database={database};Username={user};Password={password}",
            "MySQL" => $"Server={host};Port={port};Database={database};User={user};Password={password}",
            _ => throw new InvalidOperationException($"Unsupported provider: {provider}")
        };
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

    private static string ReadOptional(string label, string defaultValue)
    {
        Console.Write($"{label} [{defaultValue}]: ");
        var value = Console.ReadLine();
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    private static string ReadRequiredSecret(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var value = ReadHiddenInput();
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            Console.WriteLine($"{label} is required.");
        }
    }

    private static string ReadHiddenInput()
    {
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
                    Console.Write(" ");
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