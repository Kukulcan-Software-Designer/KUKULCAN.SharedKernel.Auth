using KUKULCAN.SharedKernel.Auth.Authentication.Local;
using KUKULCAN.SharedKernel.Auth.Client.Authentication;
using KUKULCAN.SharedKernel.Auth.Entities;
using Microsoft.EntityFrameworkCore;

namespace KUKULCAN.SharedKernel.Auth.Client.Menus;

public static class AuthenticationDataMenu
{
    public static async Task RunAsync(AuthenticationContext context)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Authentication Data Operations");
            Console.WriteLine("==============================");
            Console.WriteLine($"Active Tenant ID: {context.ActiveTenantId}");
            Console.WriteLine("1. Create Local User");
            Console.WriteLine("2. Create Tenant Membership");
            Console.WriteLine("3. Create Federated Identity");
            Console.WriteLine("4. List Users");
            Console.WriteLine("5. List Tenant Memberships");
            Console.WriteLine("6. List Federated Identities");
            Console.WriteLine("7. Save Changes");
            Console.WriteLine("0. Back");
            Console.Write("Select an option: ");

            try
            {
                switch (Console.ReadLine())
                {
                    case "1": await CreateUserAsync(context); break;
                    case "2": await CreateMembershipAsync(context); break;
                    case "3": await CreateFederatedIdentityAsync(context); break;
                    case "4": await ListUsersAsync(context); break;
                    case "5": await ListMembershipsAsync(context); break;
                    case "6": await ListFederatedIdentitiesAsync(context); break;
                    case "7": await SaveChangesAsync(context); break;
                    case "0": return;
                    default: Console.WriteLine("Invalid option."); Pause(); break;
                }
            }
            catch (DbUpdateException ex) { Console.WriteLine($"Database update error: {ex.GetBaseException().Message}"); Pause(); }
            catch (Exception ex) { Console.WriteLine($"Operation error: {ex.GetBaseException().Message}"); Pause(); }
        }
    }

    private static async Task CreateUserAsync(AuthenticationContext context)
    {
        var user = new AuthUserEntity
        {
            UserId = ReadGuidOrGenerate("User ID"),
            Email = ReadRequired("Email"),
            PasswordHash = new PasswordHasher().Hash(ReadSecret("Password"))
        };
        await context.DbContext.Users.AddAsync(user);
        await context.DbContext.SaveChangesAsync();
        Console.WriteLine($"User created: {user.UserId}");
        Console.WriteLine($"Canonical email: {user.Email}");
        Pause();
    }

    private static async Task CreateMembershipAsync(AuthenticationContext context)
    {
        await context.DbContext.TenantMemberships.AddAsync(new AuthTenantMembershipEntity
        {
            UserId = ReadGuid("User ID"),
            TenantId = ReadGuid("Tenant ID")
        });
        await context.DbContext.SaveChangesAsync();
        Console.WriteLine("Tenant membership created.");
        Pause();
    }

    private static async Task CreateFederatedIdentityAsync(AuthenticationContext context)
    {
        await context.DbContext.FederatedIdentities.AddAsync(new AuthFederatedIdentityEntity
        {
            Provider = ReadRequired("Provider"),
            Subject = ReadRequired("External Subject"),
            UserId = ReadGuid("User ID")
        });
        await context.DbContext.SaveChangesAsync();
        Console.WriteLine("Federated identity created.");
        Pause();
    }

    private static async Task ListUsersAsync(AuthenticationContext context)
    {
        var users = await context.DbContext.Users.AsNoTracking().OrderBy(x => x.Email).ToListAsync();
        Console.WriteLine(users.Count == 0 ? "No users found." : string.Join(Environment.NewLine, users.Select(x => $"{x.UserId} | {x.Email}")));
        Pause();
    }

    private static async Task ListMembershipsAsync(AuthenticationContext context)
    {
        var items = await context.DbContext.TenantMemberships.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.TenantId).ToListAsync();
        Console.WriteLine(items.Count == 0 ? "No tenant memberships found." : string.Join(Environment.NewLine, items.Select(x => $"{x.UserId} | {x.TenantId}")));
        Pause();
    }

    private static async Task ListFederatedIdentitiesAsync(AuthenticationContext context)
    {
        var items = await context.DbContext.FederatedIdentities.AsNoTracking().OrderBy(x => x.Provider).ThenBy(x => x.Subject).ToListAsync();
        Console.WriteLine(items.Count == 0 ? "No federated identities found." : string.Join(Environment.NewLine, items.Select(x => $"{x.Provider} | {x.Subject} | {x.UserId}")));
        Pause();
    }

    private static async Task SaveChangesAsync(AuthenticationContext context)
    {
        Console.WriteLine($"Rows affected: {await context.DbContext.SaveChangesAsync()}");
        Pause();
    }

    private static Guid ReadGuidOrGenerate(string label)
    {
        while (true)
        {
            Console.Write($"{label} (Enter = generate): ");
            var value = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(value)) return Guid.NewGuid();
            if (Guid.TryParse(value, out var result)) return result;
            Console.WriteLine("A valid GUID is required.");
        }
    }

    private static Guid ReadGuid(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            if (Guid.TryParse(Console.ReadLine(), out var result)) return result;
            Console.WriteLine("A valid GUID is required.");
        }
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