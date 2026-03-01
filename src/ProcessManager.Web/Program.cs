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
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

// ── Auth ──────────────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TokenService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        // Render terminates TLS at the load balancer; the container sees plain HTTP.
        // Always mark cookies Secure so browsers send them over the HTTPS connection.
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization();

// ── API client ────────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5100");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());

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
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var tokenService = context.RequestServices.GetRequiredService<TokenService>();
        tokenService.AccessToken = context.User.FindFirst("access_token")?.Value;
        tokenService.UserName = context.User.Identity.Name;
        tokenService.DisplayName = context.User.FindFirst("display_name")?.Value;
        tokenService.Role = context.User.FindFirst(ClaimTypes.Role)?.Value;
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
