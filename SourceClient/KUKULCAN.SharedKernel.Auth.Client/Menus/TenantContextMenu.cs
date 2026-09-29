using KUKULCAN.SharedKernel.Auth.Client.Authentication;

namespace KUKULCAN.SharedKernel.Auth.Client.Menus;

public static class TenantContextMenu
{
    public static void Show(AuthenticationContext context)
    {
        Console.Clear();
        Console.WriteLine($"Active Tenant ID: {context.ActiveTenantId}");
        Pause();
    }

    public static void Change(AuthenticationContext context)
    {
        Console.Clear();
        Console.WriteLine($"Current Tenant ID: {context.ActiveTenantId}");
        context.SetActiveTenant(ReadGuid("New Active Tenant ID"));
        Console.WriteLine($"Active Tenant ID: {context.ActiveTenantId}");
        Pause();
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

    private static void Pause()
    {
        Console.WriteLine();
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }
}