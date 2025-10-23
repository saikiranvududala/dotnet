using Yarp.ReverseProxy;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ---- LOCAL DEV TARGETS ----
// If you're running services locally via `dotnet run`, use localhost ports:
var catalogAddress = "http://localhost:5250"; // change to 5000 if you pinned Catalog there
var ordersAddress  = "http://localhost:5029"; // change if you pinned OrderService

// ---- DOCKER COMPOSE TARGETS ----
// If you run everything in docker compose, comment the two lines above and uncomment below:
// var catalogAddress = "http://catalog:5000";
// var ordersAddress  = "http://orders:5001";

var routes = new[]
{
    new RouteConfig
    {
        RouteId = "catalog",
        ClusterId = "catalog",
        Match = new RouteMatch { Path = "/catalog/{**catch-all}" },
        Transforms = new[]
        {
            new Dictionary<string, string> { ["PathRemovePrefix"] = "/catalog" }
        }
    },
    new RouteConfig
    {
        RouteId = "orders",
        ClusterId = "orders",
        Match = new RouteMatch { Path = "/orders/{**catch-all}" },
        Transforms = new[]
        {
            new Dictionary<string, string> { ["PathRemovePrefix"] = "/orders" }
        }
    }
};

var clusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "catalog",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["d1"] = new DestinationConfig { Address = catalogAddress }
        }
    },
    new ClusterConfig
    {
        ClusterId = "orders",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["d1"] = new DestinationConfig { Address = ordersAddress }
        }
    }
};

builder.Services.AddReverseProxy()
    .LoadFromMemory(routes, clusters);

var app = builder.Build();

app.MapGet("/", () => "API Gateway up. Try /catalog/api/products or /orders/api/orders");
app.MapReverseProxy();

app.Run();
