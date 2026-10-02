using KUKULCAN.SharedKernel.Database.Configuration;

namespace KUKULCAN.SharedKernel.Auth.Persistence;

/// <summary>Resolves the provider-specific EF Core migration assembly used by Auth.</summary>
public static class AuthMigrations
{
    /// <summary>Gets the migration assembly name for the selected database provider.</summary>
    /// <param name="provider">The configured database provider.</param>
    /// <returns>The assembly name containing Auth migrations for the provider.</returns>
    /// <exception cref="NotSupportedException">Thrown when the provider is unsupported.</exception>
    public static string GetAssemblyName(DatabaseProvider provider)
        => provider switch
        {
            DatabaseProvider.PostgresSql => "KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL",
            DatabaseProvider.SqlServer => "KUKULCAN.SharedKernel.Auth.Migrations.SQLServer",
            DatabaseProvider.MySql => "KUKULCAN.SharedKernel.Auth.Migrations.MySQL",
            _ => throw new NotSupportedException(
                $"Database provider '{provider}' does not have an Auth migration assembly.")
        };
}
