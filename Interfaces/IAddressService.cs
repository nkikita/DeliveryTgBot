using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Interfaces
{
    public interface IAddressService
    {
        Task<List<string>> GetAddressSuggestionsAsync(string query, string cityName);
        string ExtractStreetAndHouse(string fullAddress, string cityName);
    }
}