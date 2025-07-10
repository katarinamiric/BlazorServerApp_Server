using BlazorServerApp_Server.Data.Model;
using BlazorServerApp_Server.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlazorServerApp_Server.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Weather> Weather { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductRelatedProduct> ProductRelatedProducts { get; set; }
        public DbSet<AdvancedNavigationLogEntry> AdvancedNavigationLogEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the many-to-many relationship for related products
            modelBuilder.Entity<ProductRelatedProduct>()
                .HasKey(prp => new { prp.ProductId, prp.RelatedProductId });

            modelBuilder.Entity<ProductRelatedProduct>()
                .HasOne(prp => prp.Product)
                .WithMany(p => p.RelatedProductPairs)
                .HasForeignKey(prp => prp.ProductId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent circular delete issues

            modelBuilder.Entity<ProductRelatedProduct>()
                .HasOne(prp => prp.RelatedProduct)
                .WithMany() // No direct collection on RelatedProduct for simplicity, but you could add one
                .HasForeignKey(prp => prp.RelatedProductId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent circular delete issues
        }
    }
}