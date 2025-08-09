using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Interfaces
{
    public interface IOrderService
      {
        Task<Order> GetOrCreateOrderAsync(long chatId);
        Task SaveOrderAsync(Order order);
        Task ResetOrderAsync(long chatId);

      }
}