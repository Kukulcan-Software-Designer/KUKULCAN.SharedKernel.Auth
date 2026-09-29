using KUKULCAN.SharedKernel.Auth.Client.Configuration;

namespace KUKULCAN.SharedKernel.Auth.Client.Menus;

public static class DatabaseConfigurationMenu
{
    public static async Task<DatabaseEnvironmentConfiguration> RunAsync(
        DatabaseEnvironmentConfiguration current)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("==================================================");
            Console.WriteLine(" KUKULCAN.SharedKernel.Auth Client");
            Console.WriteLine("==================================================");
            Console.WriteLine();
            Console.WriteLine($"AUTH_DB_PROVIDER: {(string.IsNullOrWhiteSpace(current.Provider) ? "NOT CONFIGURED" : current.Provider)}");
            Console.WriteLine($"AUTH_DB_CONNECTION_STRING: {(string.IsNullOrWhiteSpace(current.ConnectionString) ? "NOT CONFIGURED" : "CONFIGURED")}");
            Console.WriteLine();
            Console.WriteLine("1. Configure Database Provider");
            Console.WriteLine("2. Configure Database Connection String");
            Console.WriteLine("0. Continue");
            Console.WriteLine();
            Console.Write("Select an option: ");

            switch (Console.ReadLine())
            {
                case "1":
                {
                    var provider = SelectProvider();
                    if (!string.IsNullOrWhiteSpace(provider))
                    {
                        current = current with
                        {
                            Provider = provider,
                            ConnectionString = null
                        };

                        Environment.SetEnvironmentVariable("AUTH_DB_PROVIDER", provider);
                        Environment.SetEnvironmentVariable("AUTH_DB_CONNECTION_STRING", null);
                    }

                    break;
                }
                case "2" when !string.IsNullOrWhiteSpace(current.Provider):
                {
                    var connectionString = BuildConnectionString(current.Provider!);
                    current = current with { ConnectionString = connectionString };

                    Environment.SetEnvironmentVariable(
                        "AUTH_DB_CONNECTION_STRING",
                        connectionString);

                    break;
                }
                case "0":
                    if (current.IsComplete)
                        return current;

                    Console.WriteLine("Both database environment variables are required.");
                    Console.WriteLine("Press Enter to continue...");
                    Console.ReadLine();
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
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
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    private static string BuildConnectionString(string provider)
    {
        Console.Clear();
        Console.WriteLine($"Connection configuration: {provider}");
        Console.WriteLine("----------------------------------------");

        var host = ReadRequired(provider == "PostgreSQL" ? "Host" : "Server");
        var port = ReadOptional(
            "Port",
            provider switch
            {
                "SqlServer" => "1433",
                "PostgreSQL" => "5432",
                "MySQL" => "3306",
                _ => throw new InvalidOperationException($"Unsupported provider: {provider}")
            });
        var database = ReadRequired("Database");
        var user = ReadRequired(provider == "PostgreSQL" ? "Username" : "User");
        var password = ReadRequiredSecret("Password");

        return provider switch
        {
            "SqlServer" =>
                $"Server={host},{port};Database={database};User Id={user};Password={password};TrustServerCertificate=True",
            "PostgreSQL" =>
                $"Host={host};Port={port};Database={database};Username={user};Password={password}",
            "MySQL" =>
                $"Server={host};Port={port};Database={database};User={user};Password={password}",
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