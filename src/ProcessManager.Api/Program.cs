using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProcessManager.Api.Data;
using ProcessManager.Api.Services;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
// Render injects the connection string as a postgresql:// URL.
// Npgsql's connection string builder can segfault parsing that format on Linux,
// so we convert it to key-value format explicitly.
var rawConnStr = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not configured.");

static string ToNpgsqlConnectionString(string raw)
{
    if (!raw.StartsWith("postgresql://") && !raw.StartsWith("postgres://"))
        return raw; // already key-value format (local dev)

    var uri = new Uri(raw);
    var userInfo = uri.UserInfo.Split(':', 2);
    var host     = uri.Host;
    var port     = uri.IsDefaultPort ? 5432 : uri.Port;
    var database = uri.AbsolutePath.TrimStart('/');
    var user     = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
    return $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}

var connStr = ToNpgsqlConnectionString(rawConnStr);

builder.Services.AddScoped<ProcessManager.Api.Services.ITenantContext, ProcessManager.Api.Services.TenantContext>();
builder.Services.AddScoped<ProcessManager.Api.Data.TenantSaveChangesInterceptor>();
builder.Services.AddSingleton<ProcessManager.Api.Services.JwtTokenService>();
builder.Services.AddSingleton<ProcessManager.Api.Services.IStripeService, ProcessManager.Api.Services.StripeService>();
builder.Services.AddScoped<ProcessManager.Api.Services.IPlanEnforcementService, ProcessManager.Api.Services.PlanEnforcementService>();
builder.Services.AddScoped<ProcessManager.Api.Services.IUsageMeteringService, ProcessManager.Api.Services.UsageMeteringService>();
builder.Services.AddSingleton<ProcessManager.Api.Services.ISpcCalculationService, ProcessManager.Api.Services.SpcCalculationService>();
builder.Services.AddScoped<ProcessManager.Api.Services.IOeeCalculationService, ProcessManager.Api.Services.OeeCalculationService>();

builder.Services.AddDbContext<ProcessManagerDbContext>((sp, options) =>
{
    options.UseNpgsql(connStr);
    options.AddInterceptors(sp.GetRequiredService<ProcessManager.Api.Data.TenantSaveChangesInterceptor>());
});

// ── ASP.NET Core Identity ─────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<ProcessManagerDbContext>()
    .AddDefaultTokenProviders();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key not configured.");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { });

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
            JwtBearerDefaults.AuthenticationScheme, "ApiKey")
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy(ProcessManager.Api.Controllers.PlatformAdminPolicy.Name, policy =>
        policy.RequireClaim("platform_admin", "true"));
});
builder.Services.AddHttpContextAccessor();

// ── CORS (allow Blazor frontend to fetch files directly, e.g. 3D model viewer) ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5097", "https://localhost:5097" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ── File storage (Local or Azure Blob) ────────────────────────────────────────
var storageProvider = builder.Configuration["Storage:Provider"] ?? "Local";
if (storageProvider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IImageStorageService, AzureBlobStorageService>();
else
    builder.Services.AddScoped<IImageStorageService, LocalImageStorageService>();

// ── Webhook event system ──────────────────────────────────────────────────────
builder.Services.AddSingleton<WebhookEventQueue>();
builder.Services.AddSingleton<IWebhookEventPublisher>(sp => sp.GetRequiredService<WebhookEventQueue>());
builder.Services.AddHttpClient("Webhooks");

// ── Background services (skipped in Testing environment) ──────────────────────
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<WorkflowSchedulerService>();
    builder.Services.AddHostedService<WebhookDeliveryService>();
}

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token (without 'Bearer ' prefix)."
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── HTTP pipeline ─────────────────────────────────────────────────────────────
// Basic-auth gate on /swagger — set Swagger__Password in Render dashboard
app.UseMiddleware<ProcessManager.Api.Middleware.SwaggerBasicAuthMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

// Trust the X-Forwarded-Proto header set by Render's load balancer so that
// UseHttpsRedirection sees the original HTTPS scheme and doesn't redirect loop.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
// In development we run over plain HTTP; HTTPS redirection would cause HttpClient
// to follow the redirect and strip the Authorization header → 401.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<ProcessManager.Api.Middleware.TenantContextMiddleware>();
app.UseMiddleware<ProcessManager.Api.Middleware.TenantSuspensionMiddleware>();
app.UseAuthorization();
app.MapControllers();

// Health check — Render probes this to determine service readiness
app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));

// Start listening on the port FIRST so Render's port scan succeeds,
// then run migrations and seeding. This prevents timeout on cold deploys
// where migration + seeding can exceed Render's port-scan window.
await app.StartAsync();

// ── Schema + seed (after port is open) ───────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();

    // Ensure the Default tenant exists before any seeding runs — every seeded row
    // is stamped with its Id via the SaveChanges interceptor.
    if (!db.Tenants.IgnoreQueryFilters().Any(t => t.Id == ProcessManager.Domain.Entities.Tenant.DefaultTenantId))
    {
        db.Tenants.Add(new ProcessManager.Domain.Entities.Tenant
        {
            Id = ProcessManager.Domain.Entities.Tenant.DefaultTenantId,
            Subdomain = "default",
            Name = "Default Tenant",
            Status = ProcessManager.Domain.Entities.TenantStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    // Run seeding inside a tenant scope so every inserted row gets stamped with DefaultTenantId.
    var tenantContext = scope.ServiceProvider.GetRequiredService<ProcessManager.Api.Services.ITenantContext>();
    using (tenantContext.BeginScope(ProcessManager.Domain.Entities.Tenant.DefaultTenantId))
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Engineer", "Participant" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (!userManager.Users.Any())
        {
            var adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@processmanager.local",
                DisplayName = "Administrator",
                TenantId = ProcessManager.Domain.Entities.Tenant.DefaultTenantId
            };
            var adminPassword = app.Configuration["SeedAdminPassword"] ?? "Admin1234!";
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        await DataSeeder.SeedAsync(db);
        await DataSeeder.SeedQmsDocumentsAsync(db);
        await DataSeeder.SeedTrainingDocumentsAsync(db);
        await DataSeeder.SeedStandardsClausesAsync(db);
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Migration/seeding failed — the API is running but the database may not be ready");
}

await app.WaitForShutdownAsync();

// Marker class for WebApplicationFactory<T> in integration tests
public partial class Program { }
