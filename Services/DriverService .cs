using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DeliveryTgBot.Services
{
    public class DriverService : IDriverService
    {
        private readonly DeliveryDbContext _dbContext;

        public DriverService(DeliveryDbContext dbContext)
        {
            _dbContext = dbContext;
        }

       public async Task<IEnumerable<Driver>> GetAvailableDriversAsync(int cityId, double volume, int vehiclesCount)
        {
            return await _dbContext.Drivers
                .Where(d => d.IsAvailable &&
                            d.CityId == cityId &&
                            d.MaxVolume >= volume)
                .ToListAsync();
        }




        public async Task<Driver> GetDriverByIdAsync(Guid driverId)
        {
            return await _dbContext.Drivers.FindAsync(driverId);
        }

        public async Task UpdateDriverStatusAsync(Guid driverId, bool isAvailable)
        {
            var driver = await _dbContext.Drivers.FindAsync(driverId);
            if (driver != null)
            {
                driver.IsAvailable = isAvailable;
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
