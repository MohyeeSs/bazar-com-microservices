
using OrderService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<OrderDatabase>();

builder.Services.AddHttpClient("catalog", client =>
{
    var catalogUrl = builder.Configuration[
        "CatalogService:BaseUrl"]
        ?? "http://localhost:5002/";

    client.BaseAddress = new Uri(catalogUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

var database = app.Services
    .GetRequiredService<OrderDatabase>();

await database.InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
