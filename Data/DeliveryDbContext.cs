using Microsoft.EntityFrameworkCore;

namespace DeliveryTgBot.Data
{
    public class DeliveryDbContext : DbContext
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Cityes> Cityes { get; set; }

        public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>()
                .Property(o => o.DeliveryDateTime)
                .HasColumnType("timestamp without time zone");
        }

    }
}
