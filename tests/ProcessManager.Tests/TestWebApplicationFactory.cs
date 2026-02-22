using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcessManager.Api.Data;

namespace ProcessManager.Tests;

/// <summary>
/// Custom WebApplicationFactory that replaces SQLite with an in-memory database
/// so each test class gets a clean, isolated database.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ProcessManagerDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Add in-memory database
            services.AddDbContext<ProcessManagerDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }
}
