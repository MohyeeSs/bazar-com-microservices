
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddHttpClient("catalog", client =>
{
    var catalogUrl = builder.Configuration["CatalogService:BaseUrl"]
        ?? "http://localhost:5002/";

    client.BaseAddress = new Uri(catalogUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient("order", client =>
{
    var orderUrl = builder.Configuration["OrderService:BaseUrl"]
        ?? "http://localhost:5003/";

    client.BaseAddress = new Uri(orderUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
