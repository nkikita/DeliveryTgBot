using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Interfaces
{
    public interface IOrderCacheService
    {
        Task<Order> GetOrCreateOrderAsync(long chatId);
        Task SaveOrderAsync(Order order);
        Task ResetOrderAsync(long chatId);
    }

}