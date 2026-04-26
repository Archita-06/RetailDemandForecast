using Microsoft.EntityFrameworkCore;
using RetailDemandForecastingAPI.Domain.Entities;

namespace RetailDemandForecastingAPI.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<Forecast> Forecasts { get; set; }
        public DbSet<DataUpload> DataUploads { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

           
            modelBuilder.Entity<Store>()
                .HasIndex(s => s.StoreCode)
                .IsUnique();

            
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.ProductCode)
                .IsUnique();

            
            modelBuilder.Entity<Sale>()
                .HasIndex(s => new { s.ProductId, s.StoreId, s.Date })
                .IsUnique();
        }
    }

}
