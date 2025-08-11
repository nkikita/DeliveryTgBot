using System.Collections.Concurrent;
namespace DeliveryTgBot.Handlers
{
    public class BotHandler
    {
        private readonly ITelegramService _telegramService;
        private readonly IDriverService _driverService;
        private readonly IOrderCacheService _orderCacheService;
        private readonly ICityService _cityService;
        private readonly ICalendarService _calendarService;
        private readonly IAddressService _addressService;
        private readonly IKeyboardBuilder _keyboardTGBuilder;
        private readonly IOrderService _orderService;
        private readonly IOrderNotificationService _orderNotificationService;
        private readonly ConcurrentDictionary<long, bool> _waitingForComment = new ConcurrentDictionary<long, bool>();
        private readonly ConcurrentDictionary<long, Dictionary<string, string>> _userAddressMaps = new();
         private readonly ConcurrentDictionary<long, int> _addressKeyboardMessageIds = new();

        public BotHandler(
            ITelegramService telegramService,
            IDriverService driverService,
            IOrderService orderService,
            IOrderCacheService orderCacheService,
            ICityService cityService,
            ICalendarService calendarService,
            IOrderNotificationService orderNotificationService,
            IKeyboardBuilder keyboardBuilder,
            IAddressService addressService)
        {
            _telegramService = telegramService;
            _driverService = driverService;
            _orderCacheService = orderCacheService;
            _cityService = cityService;
            _orderService = orderService;
            _calendarService = calendarService;
            _orderNotificationService = orderNotificationService;
            _addressService = addressService;
            _keyboardTGBuilder = keyboardBuilder;
        }
        private static bool IsOrderComplete(Order order)
        {
                Console.WriteLine("=== Проверка заказа ===");
                Console.WriteLine($"CityId: {order.CityId}");
                Console.WriteLine($"Volume: {order.Volume}");
                Console.WriteLine($"VehiclesCount: {order.VehiclesCount}");
                Console.WriteLine($"AssignedDriverId: {order.AssignedDriverId}");
                Console.WriteLine($"DeliveryDateTime: {order.DeliveryDateTime}");
                Console.WriteLine($"CommentFromUsers: {(order.CommentFromUsers == null ? "null" : $"'{order.CommentFromUsers}'")}");
                Console.WriteLine($"DeliveryAdress: {(order.DeliveryAdress == null ? "null" : $"'{order.DeliveryAdress}'")}");
                Console.WriteLine("======================");
            return order.CityId != null
                && order.Volume > 0
                && order.VehiclesCount > 0
                && order.AssignedDriverId != null
                && order.DeliveryDateTime != default
                && order.CommentFromUsers != null
                && order.DeliveryAdress != null;
        }
        public async Task HandleAddressQueryAsync(long chatId, string query)
        {
            var currentOrder = await _orderCacheService.GetOrCreateOrderAsync(chatId);

            if (string.IsNullOrEmpty(currentOrder.City.CityName))
            {
                await _telegramService.SendTextMessageAsync(chatId, "Сначала выберите город.");
                return;
            }

            var suggestions = await _addressService.GetAddressSuggestionsAsync(query, currentOrder.City.CityName);

            if (suggestions.Count == 0)
            {
                await _telegramService.SendTextMessageAsync(chatId, "Адрес не найден, попробуйте ввести подробнее.");
                return;
            }
              var (keyboard, map) = _keyboardTGBuilder.BuildAddressKeyboard(suggestions);
            _userAddressMaps[chatId] = map;
            await _telegramService.SendTextMessageAsync(chatId, "Выберите адрес из списка:", replyMarkup: keyboard);
            
        }
        public async Task HandleAddressSelectionAsync(long chatId, string selectedAddress)
        {
            // Получаем текущий заказ
            var currentOrder = await _orderCacheService.GetOrCreateOrderAsync(chatId);

            // Записываем выбранный адрес
            currentOrder.DeliveryAdress = selectedAddress;

            // Сохраняем изменения в кэше и в базе
            await _orderCacheService.SaveOrderAsync(currentOrder);
            await _orderService.SaveOrderAsync(currentOrder);
           


            // Отправляем сообщение пользователю
            await _telegramService.SendTextMessageAsync(chatId, $"Вы выбрали адрес: {selectedAddress}");
            if (IsOrderComplete(currentOrder))
            {
                await _telegramService.SendTextMessageAsync(chatId, "✅ Заявка заполнена. Отправляю водителю...");
                await _orderNotificationService.NotifyDriverAsync(currentOrder);
            }
            // Можно добавить переход к следующему шагу, например, выбору даты
            // await _telegramService.SendTextMessageAsync(chatId, "Пожалуйста, выберите дату доставки...");
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
                
                    if (_waitingForComment.TryGetValue(chatId, out var isWaiting) && isWaiting)
                    {
                        var comment = text.Trim();
                        if (comment == "-") comment = string.Empty;

                        currentOrder.CommentFromUsers = comment;

                        await _orderCacheService.SaveOrderAsync(currentOrder);
                        await _orderService.SaveOrderAsync(currentOrder);

                        _waitingForComment.TryRemove(chatId, out _);

                        await _telegramService.SendTextMessageAsync(chatId, "Введите адрес доставки (пример: Ленина 12):");

                        // Не вызываем HandleAddressQueryAsync здесь — ждем, пока пользователь введёт адрес и отправит сообщение
                        return;
                    }

                if (currentOrder.CommentFromUsers != null && currentOrder.DeliveryAdress == null)
                {
                    // Пользователь вводит адрес (текст)
                    // Запрашиваем подсказки и показываем клавиатуру
                    await HandleAddressQueryAsync(chatId, text);
                    return;
                }

                if (text == "/reset")
                {
                    await _orderCacheService.ResetOrderAsync(chatId);
                    await _telegramService.SendTextMessageAsync(chatId, "🔄 Анкета была сброшена. Чтобы продолжить, выберите, пожалуйста, город, в котором хотите оформить заказ. 🏙️:", CityButtons3);
                    return;
                }
                else if (text == "/start")
                {
                    await _orderCacheService.ResetOrderAsync(chatId);
                    await _telegramService.SendTextMessageAsync(chatId, "👋 Привет! Рады видеть вас. Для начала выберите город, в котором хотите сделать заказ. 🌍:", CityButtons3);
                    return;
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

                        await _orderCacheService.SaveOrderAsync(currentOrder);

                        await _telegramService.SendTextMessageAsync(chatId,
                            $"📅 Отлично! Дата и время доставки успешно установлены на {currentOrder.DeliveryDateTime:yyyy-MM-dd HH:mm}. двигаемся дальше! ➡️");
                        _waitingForComment[chatId] = true;
                        currentOrder.ClientTelegramUsername = update.Message.From.Username;

                        await _telegramService.SendTextMessageAsync(chatId, "💬 Есть пожелания для водителя? Напишите их здесь. Если комментариев нет — просто отправьте '-' (минус). ✍️:");
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
                                await _telegramService.SendTextMessageAsync(chatId, "😕 Извините, но сейчас нет свободных водителей под ваши параметры. Попробуйте изменить запрос или позже.");
                                return;
                            }

                            var buttons = drivers.Select(d =>
                                new[] { InlineKeyboardButton.WithCallbackData($"{d.Name} - {d.PricePerVolume}₽", $"driver_{d.Id}") }
                            ).ToList();
                            var markup = new InlineKeyboardMarkup(buttons);

                            await _telegramService.SendTextMessageAsync(chatId, "👤 Пожалуйста, выберите водителя из списка:", markup);
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
                var today = DateTime.Today;
                var callback = update.CallbackQuery;
                var chatId = callback.Message.Chat.Id;
                var data = callback.Data;
                var currentOrder = await _orderCacheService.GetOrCreateOrderAsync(chatId);

            
                if (data.StartsWith("addr_"))
                {
                    if (_userAddressMaps.TryGetValue(chatId, out var map))
                    {
                        if (map.TryGetValue(data, out var selectedAddress))
                        {
                            await HandleAddressSelectionAsync(chatId, selectedAddress);
                            return;
                        }
                    }
                }

              /*  if (CallbackHelper.IsAddressCallback(data))
                {
                    Console.WriteLine("ПРОВЕРКА ЛОГА ВЫВОДА АДРЕСА");
                    await HandleAddressSelectionAsync(chatId, data);
                    return;
                }
              */  
                if (data.StartsWith("city_") || data.StartsWith(new RequestDateInfo().KeyWord) || data.StartsWith("driver_"))
                {
                    await _telegramService.EditMessageReplyMarkupAsync(
                        chatId,
                        callback.Message.MessageId,
                        replyMarkup: null);
                }
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

                           await _telegramService.SendTextMessageAsync(chatId, $"📍 Отлично, выбран город — {selectedCity.CityName}.\nВведите объем груза(число от 1 до 5):");
                        }
                    }
                    return;
                }

                if (data.StartsWith(new RequestDateInfo().KeyWord))
                {
                    var dateStr = data.Substring(new RequestDateInfo().KeyWord.Length, 8);
                    if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
                    {
                        if (date < DateTime.Today)
                        {
                            await _telegramService.SendTextMessageAsync(chatId,
                                                "⚠️ Нельзя выбрать дату в прошлом! Пожалуйста, выберите сегодняшнюю дату или позже.",
                                                replyMarkup: InlineCalendarFactory.GetKeyboard(today, 0));

                            return; // прерываем обработку, дата не засчитывается
                        }

                        currentOrder.DeliveryDateTime = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
                        await _orderCacheService.SaveOrderAsync(currentOrder);

                        await _telegramService.SendTextMessageAsync(chatId, $"Вы выбрали дату доставки: {date:yyyy-MM-dd}. \nТеперь введите время доставки (ЧЧ:ММ)");
                    }
                    return;
                }

                if (data.StartsWith(new RequestPreviousMonthInfo().KeyWord))
                {
                    DateTime currentMonth =_calendarService.GetDateFromCallback(callback.Data);
                    DateTime previousMonth = currentMonth.AddMonths(-1);

                    await _telegramService.EditMessageReplyMarkupAsync(
                        chatId: callback.Message.Chat.Id,
                        messageId: callback.Message.MessageId,
                        replyMarkup: InlineCalendarFactory.GetKeyboard(previousMonth, callback.Message.MessageId)
                    );
                }
                if (data.StartsWith(new RequestNextMonthInfo().KeyWord))
                {
                    DateTime currentMonth = _calendarService.GetDateFromCallback(callback.Data);
                    DateTime nextMonth = currentMonth.AddMonths(1);

                    await _telegramService.EditMessageReplyMarkupAsync(
                        chatId: callback.Message.Chat.Id,
                        messageId: callback.Message.MessageId,
                        replyMarkup: InlineCalendarFactory.GetKeyboard(nextMonth, callback.Message.MessageId)
                    );
                }

                if (data.StartsWith("driver_"))
                {
                    var driverIdStr = data.Replace("driver_", "");
                    if (Guid.TryParse(driverIdStr, out var driverId))
                    {
                        currentOrder.AssignedDriverId = driverId;
                        await _orderCacheService.SaveOrderAsync(currentOrder);

                        var keyboard = InlineCalendarFactory.GetKeyboard(today, 0);
                        await _telegramService.SendTextMessageAsync(chatId, "Выберите дату доставки:", keyboard);
                    }
                    return;
                }
            }
        }
    }
}