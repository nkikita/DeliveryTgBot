using DeliveryTgBot.Interfaces;
using DeliveryTgBot.Models;

namespace DeliveryTgBot.Handlers.Callbacks
{
    public class CityCallbackHandler : ICallbackHandler
    {
        private readonly ITelegramService _telegramService;
        private readonly IOrderCacheService _orderCacheService;
        private readonly ICityService _cityService;

        public CityCallbackHandler(
            ITelegramService telegramService,
            IOrderCacheService orderCacheService,
            ICityService cityService)
        {
            _telegramService = telegramService;
            _orderCacheService = orderCacheService;
            _cityService = cityService;
        }

        public bool CanHandle(string callbackData)
        {
            return callbackData.StartsWith("city_");
        }

        public async Task HandleAsync(long chatId, string callbackData)
        {
            var cityIdStr = callbackData.Replace("city_", "");
            if (int.TryParse(cityIdStr, out var cityId))
            {
                var cities = await _cityService.GetAllCitiesAsync();
                var selectedCity = cities.FirstOrDefault(c => c.Id == cityId);

                if (selectedCity != null)
                {
                    var currentOrder = await _orderCacheService.GetOrCreateOrderAsync(chatId);
                    currentOrder.CityId = selectedCity.Id;
                    await _orderCacheService.SaveOrderAsync(currentOrder);

                    await _telegramService.SendTextMessageAsync(chatId, 
                        $"📍 Отлично, выбран город — {selectedCity.CityName}.\nВведите объем груза(число от 1 до 5):");
                }
            }
        }
    }
}
