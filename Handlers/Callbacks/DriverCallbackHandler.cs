using DeliveryTgBot.Interfaces;
using DeliveryTgBot.Models;

namespace DeliveryTgBot.Handlers.Callbacks
{
    public class DriverCallbackHandler : ICallbackHandler
    {
        private readonly ITelegramService _telegramService;
        private readonly IOrderCacheService _orderCacheService;
        private readonly IOrderService _orderService;
        private readonly IDriverService _driverService;

        public DriverCallbackHandler(
            ITelegramService telegramService,
            IOrderCacheService orderCacheService,
            IOrderService orderService,
            IDriverService driverService)
        {
            _telegramService = telegramService;
            _orderCacheService = orderCacheService;
            _orderService = orderService;
            _driverService = driverService;
        }

        public bool CanHandle(string callbackData)
        {
            return callbackData.StartsWith("driver_");
        }

        public async Task HandleAsync(long chatId, string callbackData)
        {
            var driverIdStr = callbackData.Replace("driver_", "");
            if (Guid.TryParse(driverIdStr, out var driverId))
            {
                var currentOrder = await _orderCacheService.GetOrCreateOrderAsync(chatId);
                currentOrder.AssignedDriverId = driverId;
                await _orderCacheService.SaveOrderAsync(currentOrder);

                var today = DateTime.Today;
                var keyboard = InlineCalendarFactory.GetKeyboard(today, 0);
                await _telegramService.SendTextMessageAsync(chatId, "Выберите дату доставки:", keyboard);
            }
        }
    }
}
