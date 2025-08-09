using Microsoft.EntityFrameworkCore;
namespace DeliveryTgBot.Services
{
   public class CityService : ICityService
    {
        private readonly DeliveryDbContext _dbContext;

        public CityService(DeliveryDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Cityes>> GetAllCitiesAsync()
        {
            return await _dbContext.Cityes.ToListAsync();
        }
    }

}