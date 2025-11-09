using DeliveryTgbot.Models;
using Microsoft.EntityFrameworkCore;



public class OrderService : IOrderService
{
    private readonly DeliveryDbContext _context;

    public OrderService(DeliveryDbContext context)
    {
        _context = context;
    }
    public async Task ResetOrderAsync(long chatId)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.ClientTelegramId == chatId);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Order> GetOrCreateOrderAsync(long chatId)
    {
        var unfinishedOrder = await _context.Orders
            .FirstOrDefaultAsync(o => o.ClientTelegramId == chatId && o.DeliveryDateTime == default);

        if (unfinishedOrder != null)
        {
            return unfinishedOrder;
        }

        var order = new Order
        {
            ClientTelegramId = chatId,
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order;
    }

    public async Task SaveOrderAsync(Order order)
{
    var existingOrder = await _context.Orders.FindAsync(order.Id);
    if (existingOrder == null)
    {
        // Если заказа нет в БД — добавляем
        _context.Orders.Add(order);
    }
    else
    {
        // Обновляем поля
        existingOrder.CityId = order.CityId;
        existingOrder.Volume = order.Volume;
        existingOrder.VehiclesCount = order.VehiclesCount;
        existingOrder.DeliveryDateTime = order.DeliveryDateTime;
        existingOrder.Status = order.Status;
        // Removed driver-related fields
        // остальные поля, если нужно
    }
    await _context.SaveChangesAsync();
}

    private void PrintException(Exception ex)
    {
        while (ex != null)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            ex = ex.InnerException;
        }
    }
}
