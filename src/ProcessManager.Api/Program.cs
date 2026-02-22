using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ProcessManagerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=processmanager.db"));

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Auto-migrate on startup (dev convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProcessManagerDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Marker class for WebApplicationFactory<T> in integration tests
public partial class Program { }
