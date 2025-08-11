using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Services
{
    public class TelegramOrderNotificationService : IOrderNotificationService
    {
        private readonly ITelegramService _telegramService;
        private readonly IDriverService _driverService;

        public TelegramOrderNotificationService(
            ITelegramService telegramService,
            IDriverService driverService)
        {
            _telegramService = telegramService;
            _driverService = driverService;
        }

        public async Task NotifyDriverAsync(Order order)
        {
            if (order.AssignedDriverId == null)
                throw new InvalidOperationException("У заказа нет назначенного водителя.");

            var driver = await _driverService.GetDriverByIdAsync(order.AssignedDriverId.Value);
            if (driver == null)
                throw new InvalidOperationException("Водитель не найден.");

            string message = $"🚚 Новый заказ от @{order.ClientTelegramUsername}!\n" +
                            $"📍 Город: {driver.City.CityName}\n" +
                            $"🚩 Адрес: {order.DeliveryAdress}\n"+
                            $"📦 Объем: {order.Volume}\n" +
                            $"🚗 Кол-во авто: {order.VehiclesCount}\n" +
                            $"📅 Дата: {order.DeliveryDateTime:yyyy-MM-dd HH:mm}\n" +
                            $"💬 Комментарий: {(string.IsNullOrWhiteSpace(order.CommentFromUsers) ? "нет" : order.CommentFromUsers)}";

            await _telegramService.SendTextMessageAsync(driver.TelegramId, message);
        }
    }

}