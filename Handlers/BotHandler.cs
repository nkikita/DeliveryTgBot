using System.Collections.Concurrent;
public class BotHandler
{
    private readonly ITelegramService _telegramService;
    private readonly IDriverService _driverService;
     private readonly IOrderCacheService _orderCacheService; 
    private readonly ICityService _cityService;
    private readonly IOrderService _orderService;
    private readonly ConcurrentDictionary<long, Order> _activeOrders = new ConcurrentDictionary<long, Order>();
    public BotHandler(
        ITelegramService telegramService,
        IDriverService driverService,
        IOrderService orderService,
        IOrderCacheService orderCacheService,
        ICityService cityService)
    {
        _telegramService = telegramService;
        _driverService = driverService;
        _orderCacheService = orderCacheService;
        _cityService = cityService;
        _orderService = orderService;
    }
    private bool IsOrderComplete(Order order)
    {
        return order.CityId != null
            && order.Volume > 0
            && order.VehiclesCount > 0
            && order.AssignedDriverId != null
            && order.DeliveryDateTime != default;
    }
    public async Task HandleUpdateAsync(Update update)
    {

        Console.WriteLine($"Получено обновление: {update.Type} от {update.Message?.Chat.Id ?? update.CallbackQuery?.Message.Chat.Id}");
        var cities = await _cityService.GetAllCitiesAsync();

        var CityButtons = cities
            .Select(c => new[] { InlineKeyboardButton.WithCallbackData(c.CityName, $"city_{c.Id}") })
            .ToList();

        var CityButtons3 = new InlineKeyboardMarkup(CityButtons);
        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text;
            var currentOrder = await _orderCacheService.GetOrCreateOrderAsync(chatId);

            if (text == "/reset")
            {
                await _orderCacheService.ResetOrderAsync(chatId);
                await _telegramService.SendTextMessageAsync(chatId, "Анкета сброшена. Пожалуйста, выберите город:", CityButtons3);
                return;
            }
            else if (text == "/start")
            {
                await _orderCacheService.ResetOrderAsync(chatId);
                await _telegramService.SendTextMessageAsync(chatId, "Привет! Добро пожаловать. Чтобы начать, выберите город:", CityButtons3);
                return;
            }

           // После любого обновления заказа, например, после установки всех параметров:

            if (IsOrderComplete(currentOrder))
            {
                // Заказ заполнен — сохраняем в базу
                await _orderService.SaveOrderAsync(currentOrder);

                // Можно отправить сообщение о том, что заказ сохранен и обработка завершена
                await _telegramService.SendTextMessageAsync(chatId, "Ваш заказ сохранён и обработка завершена.");
            }
            else
            {
                // Иначе просто обновляем кэш
                await _orderCacheService.SaveOrderAsync(currentOrder);
            }

            Console.WriteLine($"Обработка текста от {chatId}: {text}");
            if (currentOrder.DeliveryDateTime.Date != default)
            {
                Console.WriteLine($"DEBUG: currentOrder = {System.Text.Json.JsonSerializer.Serialize(currentOrder)}");
                if (TimeSpan.TryParse(text, out var time))
                {
                    var date = currentOrder.DeliveryDateTime.Date;
                    var combined = date.Add(time);
                    currentOrder.DeliveryDateTime = DateTime.SpecifyKind(combined, DateTimeKind.Unspecified);
                    await _orderService.SaveOrderAsync(currentOrder);

                    await _orderCacheService.SaveOrderAsync(currentOrder);
                    

                    await _telegramService.SendTextMessageAsync(chatId,
                        $"Дата и время доставки установлены: {currentOrder.DeliveryDateTime:yyyy-MM-dd HH:mm}");
                }
                else
                {
                    await _telegramService.SendTextMessageAsync(chatId, "Введите корректное время в формате ЧЧ:ММ");
                }
                return;
            }
            // Проверяем, выбран ли город по CityId, НЕ по currentOrder.City (который может быть null)

            if (currentOrder.Volume == 0)
            {
                if (double.TryParse(text, out var volume))
                {
                    currentOrder.Volume = volume;
                    await _orderCacheService.SaveOrderAsync(currentOrder);
                    await _telegramService.SendTextMessageAsync(chatId, "Введите количество авто:");
                }
                else
                {
                    await _telegramService.SendTextMessageAsync(chatId, "Введите число для объема.");
                }
                return;
            }
            if (currentOrder.VehiclesCount == 0)
            {
                if (int.TryParse(text, out var count))
                {
                    currentOrder.VehiclesCount = count;

                    if (IsOrderComplete(currentOrder))
                    {
                        await _orderService.SaveOrderAsync(currentOrder);
                        await _telegramService.SendTextMessageAsync(chatId, "Ваш заказ сохранён и обработка завершена.");
                    }
                    else
                    {
                        await _orderCacheService.SaveOrderAsync(currentOrder);
                        
                        if (currentOrder.CityId == null)
                        {
                            await _telegramService.SendTextMessageAsync(chatId, "Пожалуйста, выберите город.", CityButtons3);
                            return;
                        }

                        var drivers = await _driverService.GetAvailableDriversAsync(currentOrder.CityId.Value, currentOrder.Volume, currentOrder.VehiclesCount);
                        if (!drivers.Any())
                        {
                            await _telegramService.SendTextMessageAsync(chatId, "К сожалению, сейчас нет доступных водителей с такими параметрами.");
                            return;
                        }

                        var buttons = drivers.Select(d =>
                            new[] { InlineKeyboardButton.WithCallbackData($"{d.Name} - {d.PricePerVolume}₽", $"driver_{d.Id}") }
                        ).ToList();
                        var markup = new InlineKeyboardMarkup(buttons);

                        await _telegramService.SendTextMessageAsync(chatId, "Выберите водителя:", markup);
                    }
                }
                else
                {
                    await _telegramService.SendTextMessageAsync(chatId, "Введите число для количества авто.");
                }
                return;
            }

        }
        else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
        {
            var callback = update.CallbackQuery;
            var chatId = callback.Message.Chat.Id;
            var data = callback.Data;
            var currentOrder = await _orderCacheService.GetOrCreateOrderAsync(chatId);
            
            await _telegramService.EditMessageReplyMarkupAsync(
                chatId,
                callback.Message.MessageId,
                replyMarkup: null);
            if (data.StartsWith("city_"))
            {
                var cityIdStr = data.Replace("city_", "");
                if (int.TryParse(cityIdStr, out var cityId))
                {
                    var selectedCity = cities.FirstOrDefault(c => c.Id == cityId);

                    if (selectedCity != null)
                    {
                        currentOrder.CityId = selectedCity.Id;
                        // НЕ трогаем currentOrder.City напрямую — EF сам подтянет по CityId при необходимости

                        await _orderCacheService.SaveOrderAsync(currentOrder);

                        await _telegramService.SendTextMessageAsync(chatId, $"Вы выбрали город: {selectedCity.CityName}");
                        await _telegramService.SendTextMessageAsync(chatId, "Введите объем груза:");
                    }
                }
                return;
            }

            if (data.StartsWith(new RequestDateInfo().KeyWord))
            {
                var dateStr = data.Substring(new RequestDateInfo().KeyWord.Length, 8);
                if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
                {
                    // Явно указываем, что это UTC-время
                    currentOrder.DeliveryDateTime = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);

                    await _orderCacheService.SaveOrderAsync(currentOrder);

                    await _telegramService.SendTextMessageAsync(chatId, $"Вы выбрали дату доставки: {date:yyyy-MM-dd}");
                    await _telegramService.SendTextMessageAsync(chatId, "Теперь введите время доставки (ЧЧ:ММ)");
                }
                return;
            }

            if (data.StartsWith("driver_"))
            {
                var driverIdStr = data.Replace("driver_", "");
                if (Guid.TryParse(driverIdStr, out var driverId))
                {
                    currentOrder.AssignedDriverId = driverId;
                    await _orderCacheService.SaveOrderAsync(currentOrder);

                    var today = DateTime.Today;
                    var keyboard = InlineCalendarFactory.GetKeyboard(today, 0);
                    await _telegramService.SendTextMessageAsync(chatId, "Выберите дату доставки:", keyboard);
                }
                return;
            }
        }
    }
}
