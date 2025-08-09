using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Interfaces
{
    public interface ICityService
    {
        Task<List<Cityes>> GetAllCitiesAsync();
    }
}