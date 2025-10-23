using Microsoft.EntityFrameworkCore;

public record Product(int Id, string Name, decimal Price);

public class CatalogDb : DbContext
{
    public CatalogDb(DbContextOptions<CatalogDb> options) : base(options) {}

    public DbSet<Product> Products => Set<Product>();
}
