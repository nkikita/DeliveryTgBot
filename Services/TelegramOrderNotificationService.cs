using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Services
{
    public class TelegramOrderNotificationService : IOrderNotificationService
    {
        private readonly ITelegramService _telegramService;
        private readonly IConfigurationService _configurationService;

        public TelegramOrderNotificationService(
            ITelegramService telegramService,
            IConfigurationService configurationService)
        {
            _telegramService = telegramService;
            _configurationService = configurationService;
        }

        public async Task NotifyManagerAsync(Order order)
        {
            var managerId = _configurationService.ManagerTelegramUserId;

            string usernamePart = string.IsNullOrWhiteSpace(order.ClientTelegramUsername)
                ? "(username не указан)"
                : $"@{order.ClientTelegramUsername}";

            string message =
                $"📦 Новый заказ от {usernamePart}\n" +
                $"📍 Город: {order.City.CityName}\n" +
                $"🚗 Кол-во авто: {order.VehiclesCount}\n" +
                $"🔢 Объем: {order.Volume}\n" +
                $"📅 Доставка: {order.DeliveryDateTime:yyyy-MM-dd HH:mm}\n" +
                $"🏠 Адрес: {order.DeliveryAdress}\n" +
                $"💬 Комментарий: {(string.IsNullOrWhiteSpace(order.CommentFromUsers) ? "нет" : order.CommentFromUsers)}";

            await _telegramService.SendTextMessageAsync(managerId, message);
        }
    }

}