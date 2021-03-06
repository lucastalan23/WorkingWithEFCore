using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Proxies;

namespace Packt.Shared
{
  // this manages the connection to the database
  public class Northwind : DbContext
  {
    // these properties map to tables in the database 
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnConfiguring(
      DbContextOptionsBuilder optionsBuilder)
    {
      string path = System.IO.Path.Combine(
        System.Environment.CurrentDirectory, "Northwind.db");
        
      optionsBuilder.UseLazyLoadingProxies()
        .UseSqlite($"Filename={path}");
    }

    protected override void OnModelCreating(
      ModelBuilder modelBuilder)
    {
<<<<<<< HEAD
=======
      // example of using Fluent API instead of attributes
>>>>>>> 7323235c1afce145587432f4c48f5f8e805149a2
      modelBuilder.Entity<Category>()
        .Property(category => category.CategoryName)
        .IsRequired() // NOT NULL
        .HasMaxLength(15);

      modelBuilder.Entity<Product>()
        .Property(product => product.Cost)
        .HasConversion<double>();

      modelBuilder.Entity<Product>()
        .HasQueryFilter(p => !p.Discontinued);
    }
  }
}
