using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ProcessManager.Api.Data;
using ProcessManager.Api.Services;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Tests;

/// <summary>
/// Custom WebApplicationFactory that replaces SQLite with an in-memory database
/// so each test class gets a clean, isolated database. Creates an authenticated
/// HTTP client (Admin role) by generating a valid JWT with the test signing key.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "TestJwtKey_AtLeast32Characters_ForIntegrationTests!!";
    public const string TestIssuer = "ProcessManager.Api";
    public const string TestAudience = "ProcessManager";
    public const string DefaultTestUserId = "test-user-id";
    public static readonly Guid DefaultTenantId = Tenant.DefaultTenantId;

    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide JWT config so Program.cs startup doesn't throw
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience,
                ["Jwt:ExpiryMinutes"] = "60",
                ["Stripe:Prices:Starter"] = "price_test_starter",
                ["Stripe:Prices:Professional"] = "price_test_professional",
                ["Stripe:Prices:Enterprise"] = "price_test_enterprise"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ProcessManagerDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Add in-memory database with the tenant-stamping interceptor
            services.AddDbContext<ProcessManagerDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>());
            });

            // Replace Stripe service with a test stub
            var stripeDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IStripeService));
            if (stripeDescriptor is not null)
                services.Remove(stripeDescriptor);
            services.AddSingleton<IStripeService, TestStripeService>();

            // Override JWT validation parameters to use the known test key
            // (PostConfigure ensures this runs after Program.cs configures JWT)
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey))
                };
            });

            // Ensure database is created and seed the default tenant row so
            // SaveChanges-stamped inserts have a valid FK target.
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            db.Database.EnsureCreated();
            if (!db.Tenants.IgnoreQueryFilters().Any(t => t.Id == Tenant.DefaultTenantId))
            {
                db.Tenants.Add(new Tenant
                {
                    Id = Tenant.DefaultTenantId,
                    Subdomain = "default",
                    Name = "Default Test Tenant",
                    Status = TenantStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            }
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>Provision a fresh tenant directly in the DB (bypasses platform API). Used by isolation tests.</summary>
    public Guid CreateTenant(string subdomain, string? name = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Subdomain = subdomain,
            Name = name ?? subdomain,
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);
        db.SaveChanges();
        return tenant.Id;
    }

    /// <summary>
    /// Creates an HttpClient pre-configured with a valid Admin JWT bearer token for the default test user
    /// scoped to the Default tenant. The overwhelming majority of existing tests use this.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateAdminJwt());
        return client;
    }

    /// <summary>
    /// Creates an HttpClient authenticated as the specified user ID, with the given role,
    /// scoped to the Default tenant.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string userId, string role = "Admin")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateJwt(userId, role, DefaultTenantId));
        return client;
    }

    /// <summary>Creates an HttpClient scoped to an arbitrary tenant — used by isolation tests.</summary>
    public HttpClient CreateTenantClient(Guid tenantId, string userId = DefaultTestUserId, string role = "Admin")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateJwt(userId, role, tenantId));
        return client;
    }

    /// <summary>Creates an HttpClient bearing the platform_admin claim — bypasses tenant filtering.</summary>
    public HttpClient CreatePlatformAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GeneratePlatformAdminJwt());
        return client;
    }

    public static string GenerateAdminJwt() => GenerateJwt(DefaultTestUserId, "Admin", DefaultTenantId);

    public static string GeneratePlatformAdminJwt() =>
        GenerateJwt("platform-admin-id", "Admin", DefaultTenantId, isPlatformAdmin: true);

    public static string GenerateJwt(string userId, string role = "Admin")
        => GenerateJwt(userId, role, DefaultTenantId);

    public static string GenerateJwt(string userId, string role, Guid tenantId, bool isPlatformAdmin = false)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userId),
            new(ClaimTypes.Email, $"{userId}@test.local"),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", tenantId.ToString())
        };
        if (isPlatformAdmin)
            claims.Add(new Claim("platform_admin", "true"));

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
