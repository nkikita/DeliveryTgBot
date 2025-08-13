using DeliveryTgBot.Interfaces;
using DeliveryTgBot.Models;

namespace DeliveryTgBot.Handlers.Commands
{
    public class StartCommandHandler : ICommandHandler
    {
        private readonly ITelegramService _telegramService;
        private readonly IOrderCacheService _orderCacheService;
        private readonly ICityService _cityService;

        public string Command => "/start";

        public StartCommandHandler(
            ITelegramService telegramService,
            IOrderCacheService orderCacheService,
            ICityService cityService)
        {
            _telegramService = telegramService;
            _orderCacheService = orderCacheService;
            _cityService = cityService;
        }

        public async Task HandleAsync(long chatId, string[] arguments)
        {
            await _orderCacheService.ResetOrderAsync(chatId);
            var cities = await _cityService.GetAllCitiesAsync();
            var cityButtons = cities
                .Select(c => new[] { InlineKeyboardButton.WithCallbackData(c.CityName, $"city_{c.Id}") })
                .ToList();
            var markup = new InlineKeyboardMarkup(cityButtons);
            
            await _telegramService.SendTextMessageAsync(chatId, 
                "👋 Привет! Рады видеть вас. Для начала выберите город, в котором хотите сделать заказ. 🌍:", 
                markup);
        }
    }
}
