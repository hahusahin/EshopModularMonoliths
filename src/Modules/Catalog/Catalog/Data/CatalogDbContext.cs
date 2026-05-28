namespace Catalog.Data;
public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }
    // this will be my Products table that I'll execute queries on
    public DbSet<Product> Products => Set<Product>();

    override protected void OnModelCreating(ModelBuilder builder)
    {
        // creates a schema inside the db (to make seperation)
        builder.HasDefaultSchema("catalog");
        // This one line auto-discovers all configuration classes (which implements IEntityTypeConfiguration) in the current project
        //  and applies them instead of you having to register each one manually below (to make this method cleaner). 
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(builder);
    }
}
