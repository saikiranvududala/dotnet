using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CatalogDb>(o => o.UseSqlite("Data Source=catalog.db"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/health");
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.MapGet("/api/products", async (CatalogDb db) =>
    await db.Products.AsNoTracking().ToListAsync());

app.MapGet("/api/products/{id:int}", async (int id, CatalogDb db) =>
    await db.Products.FindAsync(id) is { } p ? Results.Ok(p) : Results.NotFound());

app.MapPost("/api/products", async (Product p, CatalogDb db) =>
{
    db.Products.Add(p);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{p.Id}", p);
});

app.MapPut("/api/products/{id:int}", async (int id, Product input, CatalogDb db) =>
{
    var p = await db.Products.FindAsync(id);
    if (p is null) return Results.NotFound();
    db.Entry(p).CurrentValues.SetValues(input);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/products/{id:int}", async (int id, CatalogDb db) =>
{
    var p = await db.Products.FindAsync(id);
    if (p is null) return Results.NotFound();
    db.Remove(p);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDb>();
    db.Database.EnsureCreated();
    if (!db.Products.Any())
        db.Products.AddRange(new Product(1,"Keyboard",49.99m), new Product(2,"Mouse",19.99m));
    db.SaveChanges();
}

app.Run();
