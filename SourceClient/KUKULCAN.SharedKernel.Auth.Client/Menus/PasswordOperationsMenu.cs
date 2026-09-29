using KUKULCAN.SharedKernel.Auth.Authentication.Local;

namespace KUKULCAN.SharedKernel.Auth.Client.Menus;

public static class PasswordOperationsMenu
{
    public static Task RunAsync()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Password Operations");
            Console.WriteLine("===================");
            Console.WriteLine("1. Hash Password");
            Console.WriteLine("2. Verify Password");
            Console.WriteLine("0. Back");
            Console.Write("Select an option: ");

            switch (Console.ReadLine())
            {
                case "1": HashPassword(); break;
                case "2": VerifyPassword(); break;
                case "0": return Task.CompletedTask;
                default: Console.WriteLine("Invalid option."); Pause(); break;
            }
        }
    }

    private static void HashPassword()
    {
        Console.Clear();
        var password = ReadSecret("Password");
        Console.WriteLine();
        Console.WriteLine("Password hash:");
        Console.WriteLine(new PasswordHasher().Hash(password));
        Pause();
    }

    private static void VerifyPassword()
    {
        Console.Clear();
        var password = ReadSecret("Password");
        var hash = ReadRequired("Password Hash");
        Console.WriteLine();
        Console.WriteLine(new PasswordHasher().Verify(password, hash) ? "Password: VALID" : "Password: INVALID");
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