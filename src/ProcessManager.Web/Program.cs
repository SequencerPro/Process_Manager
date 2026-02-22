using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessManager.Web.Components;
using ProcessManager.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// API client — points at the API server
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
