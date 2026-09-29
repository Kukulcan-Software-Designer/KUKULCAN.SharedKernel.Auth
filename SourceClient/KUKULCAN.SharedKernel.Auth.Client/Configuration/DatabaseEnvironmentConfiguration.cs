namespace KUKULCAN.SharedKernel.Auth.Client.Configuration;

public sealed record DatabaseEnvironmentConfiguration(
    string? Provider,
    string? ConnectionString)
{
    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Provider) &&
        !string.IsNullOrWhiteSpace(ConnectionString);

    public static DatabaseEnvironmentConfiguration Load() =>
        new(
            Environment.GetEnvironmentVariable("AUTH_DB_PROVIDER"),
            Environment.GetEnvironmentVariable("AUTH_DB_CONNECTION_STRING"));

    public void SetEnvironmentVariables()
    {
        if (!IsComplete)
            throw new InvalidOperationException("Database configuration is incomplete.");

        Environment.SetEnvironmentVariable("AUTH_DB_PROVIDER", Provider);
        Environment.SetEnvironmentVariable("AUTH_DB_CONNECTION_STRING", ConnectionString);
    }
}