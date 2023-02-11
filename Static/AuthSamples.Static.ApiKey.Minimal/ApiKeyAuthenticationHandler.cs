using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AuthSamples.Static.ApiKey.Minimal;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IConfiguration configuration)
        : base(options, logger, encoder, clock)
    {
        this.configuration = configuration;
    }

    /// <summary>
    /// Handle authentication logic
    /// </summary>
    /// <returns></returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // The two fail results (and in this case the success result) could be cached
        // using a static field for example.

        // You could use a custom header here aswell like x-api-key
        string? rawHeader = Request.Headers["Authorization"];
        if (rawHeader is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Authorization header is empty"));
        }

        // Parse: "Bearer SomeRandomApiKeyHere"
        // You could remove this logic and compare the raw header content directly
        // if you don't want to prefix your ApiKey
        if (!AuthenticationHeaderValue.TryParse(rawHeader, out var authHeader) || authHeader.Parameter is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("ApiKey couldn't get parsed"));
        }

        // Check if provided ApiKey matches configured ApiKey
        if (!authHeader.Parameter.Equals(configuration.GetValue<string>("Auth:ApiKey")))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid ApiKey"));
        }

        // Authenticate the incoming request
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(claimsPrincipal, Scheme.Name)
        ));
    }
}

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

/// <summary>
/// Default values used by the <see cref="ApiKeyAuthenticationHandler"/> authentication middleware.
/// </summary>
public static class ApiKeyAuthenticationDefaults
{
    /// <summary>
    /// Default value for <see cref="AuthenticationScheme.Name"/>.
    /// </summary>
    public const string AuthenticationScheme = "ApiKey";
}
