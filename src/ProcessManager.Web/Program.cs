using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using ProcessManager.Web.Components;
using ProcessManager.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor ────────────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        // Default is 32 KB — far too small for CAD file uploads (STEP files can be 10+ MB).
        // InputFile streams through the SignalR hub, so this limit must cover the largest
        // file the user might select.  100 MB matches the OpenReadStream limit in ApiClient.
        options.MaximumReceiveMessageSize = 100 * 1024 * 1024; // 100 MB
    });

builder.Services.AddCascadingAuthenticationState();

// ── Auth ──────────────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<VocabularyService>();
builder.Services.AddScoped<FeatureFlagService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        // In production Render terminates TLS at the load balancer, so Always is correct.
        // In development we run over plain HTTP, so SameAsRequest avoids the browser
        // silently dropping the cookie.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization();

// ── API client ────────────────────────────────────────────────────────────────
// TokenHandler is scoped so it can depend on the circuit-scoped services
// (TokenService, AuthenticationStateProvider).
builder.Services.AddScoped<TokenHandler>();
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5100");
}).AddHttpMessageHandler<TokenHandler>();

builder.Services.AddSingleton(new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
});

var app = builder.Build();

// ── HTTP pipeline ─────────────────────────────────────────────────────────────
// Trust X-Forwarded-Proto/For set by Render's load balancer so that the app
// correctly sees HTTPS as the scheme — required for antiforgery and cookies.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Populate TokenService from auth cookie claims so ApiClient has the bearer token
// throughout the Blazor Server circuit lifetime.
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("AuthMiddleware");
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var tokenService = context.RequestServices.GetRequiredService<TokenService>();
        tokenService.AccessToken = context.User.FindFirst("access_token")?.Value;
        tokenService.UserName = context.User.Identity.Name;
        tokenService.DisplayName = context.User.FindFirst("display_name")?.Value;
        tokenService.Role = context.User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(tokenService.AccessToken))
        {
            logger.LogWarning(
                "[AuthMiddleware] User '{User}' authenticated but NO access_token claim found. Claims: {Claims}",
                tokenService.UserName,
                string.Join(", ", context.User.Claims.Select(c => c.Type)));
        }
        else
        {
            logger.LogDebug(
                "[AuthMiddleware] Populated TokenService for '{User}' (token length: {Len})",
                tokenService.UserName, tokenService.AccessToken.Length);
        }
    }
    else
    {
        logger.LogDebug("[AuthMiddleware] Request from unauthenticated user to {Path}", context.Request.Path);
    }
    await next();
});

app.UseAntiforgery();

// ── Logout endpoint (needs HttpContext to sign out) ───────────────────────────
app.MapGet("/account/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/account/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
