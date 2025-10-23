using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// -------- LOCAL DEV DEFAULTS --------
// If you run with Docker Compose instead, change these two to:
//   var catalogBase = "http://catalog:5000";
//   var rabbitHost  = "rabbitmq";
var catalogBase = builder.Configuration["CatalogService:BaseUrl"] ?? "http://localhost:5250";
var rabbitHost  = builder.Configuration["RabbitMQ:Host"]          ?? "localhost";

// Log what we’re using so you can verify in the console
Console.WriteLine($"[OrderService] CatalogService:BaseUrl = {catalogBase}");
Console.WriteLine($"[OrderService] RabbitMQ:Host          = {rabbitHost}");

builder.Services
    .AddHttpClient("catalog", c =>
    {
        c.BaseAddress = new Uri(catalogBase);
        c.Timeout = TimeSpan.FromSeconds(3);
    })
    .AddPolicyHandler(
        HttpPolicyExtensions
            .HandleTransientHttpError()               // 5xx, 408, network failures
            .OrResult(r => (int)r.StatusCode == 429)   // 429 Too Many Requests
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt))
    );

// IMPORTANT: use the resolved host (localhost for local dev)
builder.Services.AddSingleton<IEventBus>(_ => new RabbitMqBus(rabbitHost));

var app = builder.Build();

app.MapHealthChecks("/health");
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

var orders = new List<Order>();
var idSeq = 1;

app.MapPost("/api/orders", async (CreateOrderRequest req, IHttpClientFactory http, IEventBus bus) =>
{
    var client = http.CreateClient("catalog");
    var product = await client.GetFromJsonAsync<Product>($"/api/products/{req.ProductId}");
    if (product is null) return Results.BadRequest($"Product {req.ProductId} not found");

    var total = product.Price * req.Quantity;
    var order = new Order(idSeq++, req.ProductId, req.Quantity, total);
    orders.Add(order);

    bus.Publish("order.created", new OrderCreatedEvent(order.Id, order.ProductId, order.Quantity, order.Total));
    return Results.Created($"/api/orders/{order.Id}", order);
});

app.MapGet("/api/orders", () => orders);

app.Run();

record Product(int Id, string Name, decimal Price);
