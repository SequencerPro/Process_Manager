using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace ProcessManager.Api.Middleware;

/// <summary>
/// Protects the /swagger path with HTTP Basic Authentication.
/// Credentials are read from configuration:
///   Swagger__User     (default: swagger)
///   Swagger__Password (set as a secret in the Render dashboard)
/// </summary>
public class SwaggerBasicAuthMiddleware(RequestDelegate next, IConfiguration config)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Only guard Swagger routes
        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            await next(context);
            return;
        }

        var expectedUser     = config["Swagger__User"]     ?? "swagger";
        var expectedPassword = config["Swagger__Password"] ?? "";

        // If no password is configured, skip auth (local dev convenience)
        if (string.IsNullOrEmpty(expectedPassword))
        {
            await next(context);
            return;
        }

        string? authHeader = context.Request.Headers.Authorization;
        if (authHeader is not null && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var encoded     = authHeader["Basic ".Length..].Trim();
                var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var colonIndex  = credentials.IndexOf(':');
                if (colonIndex > 0)
                {
                    var user     = credentials[..colonIndex];
                    var password = credentials[(colonIndex + 1)..];
                    if (user == expectedUser && password == expectedPassword)
                    {
                        await next(context);
                        return;
                    }
                }
            }
            catch { /* fall through to 401 */ }
        }

        // Challenge the browser to show its native credentials dialog
        context.Response.Headers.WWWAuthenticate = "Basic realm=\"Process Manager Swagger\", charset=\"UTF-8\"";
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
    }
}
