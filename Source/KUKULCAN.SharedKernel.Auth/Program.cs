using KUKULCAN.SharedKernel.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<LocalAuthenticationService>();

var app = builder.Build();

app.MapPost("/api/auth/local", async (
    LocalAuthenticationRequest request,
    LocalAuthenticationService authenticationService,
    CancellationToken cancellationToken) =>
{
    var result = await authenticationService.AuthenticateAsync(request, cancellationToken);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.Unauthorized();
});

app.Run();

public partial class Program;
