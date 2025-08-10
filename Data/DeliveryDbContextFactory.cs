using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DeliveryTgBot.Data;

public class DeliveryDbContextFactory : IDesignTimeDbContextFactory<DeliveryDbContext>
{
    public DeliveryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DeliveryDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=deliverydb;Username=postgres;Password=111");

        return new DeliveryDbContext(optionsBuilder.Options);
    }
}
