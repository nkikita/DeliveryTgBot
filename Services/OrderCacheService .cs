using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Services
{
    public class OrderCacheService : IOrderCacheService
{
    private readonly Dictionary<long, Order> _ordersCache = new();

    public Task<Order> GetOrCreateOrderAsync(long chatId)
    {
        if (!_ordersCache.TryGetValue(chatId, out var order))
        {
            order = new Order
            {
                ClientTelegramId = chatId,
                DeliveryDateTime = default,
                Volume = 0,
                VehiclesCount = 0,
            };
            _ordersCache[chatId] = order;
        }
        return Task.FromResult(order);
    }

    public Task SaveOrderAsync(Order order)
    {
        _ordersCache[order.ClientTelegramId] = order;
        return Task.CompletedTask;
    }

    public Task ResetOrderAsync(long chatId)
    {
        _ordersCache.Remove(chatId);
        return Task.CompletedTask;
    }
}

}