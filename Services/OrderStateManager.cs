using DeliveryTgBot.Interfaces;
using DeliveryTgBot.Models;

namespace DeliveryTgBot.Services
{
    public class OrderStateManager : IOrderStateManager
    {
        private readonly ITelegramService _telegramService;
        private readonly IOrderService _orderService;
        private readonly IDriverService _driverService;
        private readonly IOrderNotificationService _orderNotificationService;

        public OrderStateManager(
            ITelegramService telegramService,
            IOrderService orderService,
            IDriverService driverService,
            IOrderNotificationService orderNotificationService)
        {
            _telegramService = telegramService;
            _orderService = orderService;
            _driverService = driverService;
            _orderNotificationService = orderNotificationService;
        }

        public async Task<bool> ProcessOrderStateAsync(Order order, string input)
        {
            // Handle volume input
            if (order.Volume == 0)
            {
                if (double.TryParse(input, out var volume))
                {
                    order.Volume = volume;
                    return true;
                }
                await _telegramService.SendTextMessageAsync(order.ClientTelegramId, "Введите число для объема.");
                return false;
            }

            // Handle vehicles count input
            if (order.VehiclesCount == 0)
            {
                if (int.TryParse(input, out var count))
                {
                    order.VehiclesCount = count;
                    return true;
                }
                await _telegramService.SendTextMessageAsync(order.ClientTelegramId, "Введите число для количества авто.");
                return false;
            }

            // Handle time input when date is set
            if (order.DeliveryDateTime.Date != default && order.DeliveryDateTime.TimeOfDay == default)
            {
                if (TimeSpan.TryParse(input, out var time))
                {
                    var date = order.DeliveryDateTime.Date;
                    var combined = date.Add(time);
                    order.DeliveryDateTime = DateTime.SpecifyKind(combined, DateTimeKind.Unspecified);
                    return true;
                }
                await _telegramService.SendTextMessageAsync(order.ClientTelegramId, "Введите корректное время в формате ЧЧ:ММ");
                return false;
            }

            return false;
        }

        public async Task<bool> IsOrderCompleteAsync(Order order)
        {
            var isComplete = order.CityId != null
                && order.Volume > 0
                && order.VehiclesCount > 0
                && order.AssignedDriverId != null
                && order.DeliveryDateTime != default
                && order.CommentFromUsers != null
                && order.DeliveryAdress != null;

            if (isComplete)
            {
                await _telegramService.SendTextMessageAsync(order.ClientTelegramId, "✅ Заявка заполнена. Отправляю водителю...");
                await _orderNotificationService.NotifyDriverAsync(order);
            }

            return isComplete;
        }

        public async Task<string> GetNextPromptAsync(Order order)
        {
            if (order.Volume == 0)
            {
                return "Введите объем груза(число от 1 до 5):";
            }

            if (order.VehiclesCount == 0)
            {
                return "Введите количество авто:";
            }

            if (order.AssignedDriverId == null)
            {
                var drivers = await _driverService.GetAvailableDriversAsync(order.CityId.Value, order.Volume, order.VehiclesCount);
                if (!drivers.Any())
                {
                    return "😕 Извините, но сейчас нет свободных водителей под ваши параметры. Попробуйте изменить запрос или позже.";
                }
                return "👤 Пожалуйста, выберите водителя из списка:";
            }

            if (order.DeliveryDateTime.Date == default)
            {
                return "Выберите дату доставки:";
            }

            if (order.DeliveryDateTime.TimeOfDay == default)
            {
                return "Теперь введите время доставки (ЧЧ:ММ)";
            }

            if (order.CommentFromUsers == null)
            {
                return "💬 Есть пожелания для водителя? Напишите их здесь. Если комментариев нет — просто отправьте '-' (минус). ✍️:";
            }

            if (order.DeliveryAdress == null)
            {
                return "Введите адрес доставки (пример: Ленина 12):";
            }

            return "Заказ завершен!";
        }
    }
}
