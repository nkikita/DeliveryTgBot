using System.Collections.Concurrent;
using DeliveryTgBot.Interfaces;
using DeliveryTgBot.Handlers.Commands;
using DeliveryTgBot.Handlers.Callbacks;
using DeliveryTgBot.Services;

namespace DeliveryTgBot.Handlers
{
    public class BotHandler
    {
        private readonly ITelegramService _telegramService;
        private readonly IOrderCacheService _orderCacheService;
        private readonly ICityService _cityService;
        private readonly ICalendarService _calendarService;
        private readonly IOrderNotificationService _orderNotificationService;
        private readonly MessageProcessor _messageProcessor;
        private readonly IOrderStateManager _orderStateManager;
        
        // Command handlers
        private readonly IEnumerable<ICommandHandler> _commandHandlers;
        
        // Callback handlers
        private readonly IEnumerable<ICallbackHandler> _callbackHandlers;

        public BotHandler(
            ITelegramService telegramService,
            IOrderService orderService,
            IOrderCacheService orderCacheService,
            ICityService cityService,
            ICalendarService calendarService,
            IOrderNotificationService orderNotificationService,
            IKeyboardBuilder keyboardBuilder,
            IAddressService addressService)
        {
            _telegramService = telegramService;
            _orderCacheService = orderCacheService;
            _cityService = cityService;
            _calendarService = calendarService;
            _orderNotificationService = orderNotificationService;
            
            // Initialize order state manager
            _orderStateManager = new OrderStateManager(telegramService, orderService, orderNotificationService);
            
            // Initialize message processor
            _messageProcessor = new MessageProcessor(
                orderCacheService, 
                _orderStateManager, 
                telegramService, 
                addressService, 
                keyboardBuilder, 
                orderService);
            
            // Initialize command handlers
            _commandHandlers = new List<ICommandHandler>
            {
                new StartCommandHandler(telegramService, orderCacheService, cityService),
                new ResetCommandHandler(telegramService, orderCacheService, cityService)
            };
            
            // Initialize callback handlers
            _callbackHandlers = new List<ICallbackHandler>
            {
                new CityCallbackHandler(telegramService, orderCacheService, cityService)
            };
        }

        public async Task HandleUpdateAsync(Update update)
        {
            Console.WriteLine($"Получено обновление: {update.Type} от {update.Message?.Chat.Id ?? update.CallbackQuery?.Message.Chat.Id}");
            
            try
            {
                if (update.Type == UpdateType.Message && update.Message?.Text != null)
                {
                    await HandleTextMessageAsync(update.Message);
                }
                else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                {
                    await HandleCallbackQueryAsync(update.CallbackQuery);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке обновления: {ex.Message}");
                // Log error and potentially notify admin
            }
        }

        private async Task HandleTextMessageAsync(Message message)
        {
            var chatId = message.Chat.Id;
            var text = message.Text;
            var username = message.From?.Username;

            // Check if it's a command
            if (text.StartsWith("/"))
            {
                var commandParts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = commandParts[0];
                var arguments = commandParts.Skip(1).ToArray();

                var handler = _commandHandlers.FirstOrDefault(h => h.Command == command);
                if (handler != null)
                {
                    await handler.HandleAsync(chatId, arguments);
                    return;
                }
            }

            // Process as regular text message
            await _messageProcessor.ProcessTextMessageAsync(chatId, text, username);
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;

            // Remove reply markup for certain callbacks
            if (data.StartsWith("city_") || data.StartsWith(new RequestDateInfo().KeyWord))
            {
                await _telegramService.EditMessageReplyMarkupAsync(
                    chatId,
                    callbackQuery.Message.MessageId,
                    replyMarkup: null);
            }

            // Handle address selection
            if (data.StartsWith("addr_"))
            {
                var addressMap = _messageProcessor.GetAddressMap(chatId);
                if (addressMap.TryGetValue(data, out var selectedAddress))
                {
                    await _messageProcessor.HandleAddressSelectionAsync(chatId, selectedAddress);
                    return;
                }
            }

            // Handle city selection
            if (data.StartsWith("city_"))
            {
                var handler = _callbackHandlers.OfType<CityCallbackHandler>().FirstOrDefault();
                if (handler != null)
                {
                    await handler.HandleAsync(chatId, data);
                    return;
                }
            }

            // Driver selection removed

            // Handle date selection
            if (data.StartsWith(new RequestDateInfo().KeyWord))
            {
                await HandleDateSelectionAsync(chatId, data);
                return;
            }

            // Handle calendar navigation
            if (data.StartsWith(new RequestPreviousMonthInfo().KeyWord))
            {
                await HandleCalendarNavigationAsync(callbackQuery, -1);
                return;
            }

            if (data.StartsWith(new RequestNextMonthInfo().KeyWord))
            {
                await HandleCalendarNavigationAsync(callbackQuery, 1);
                return;
            }
        }

        private async Task HandleDateSelectionAsync(long chatId, string data)
        {
            var dateStr = data.Substring(new RequestDateInfo().KeyWord.Length, 8);
            if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
            {
                if (date < DateTime.Today)
                {
                    await _telegramService.SendTextMessageAsync(chatId,
                        "⚠️ Нельзя выбрать дату в прошлом! Пожалуйста, выберите сегодняшнюю дату или позже.",
                        replyMarkup: InlineCalendarFactory.GetKeyboard(DateTime.Today, 0));
                    return;
                }

                var currentOrder = await _orderCacheService.GetOrCreateOrderAsync(chatId);
                currentOrder.DeliveryDateTime = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
                await _orderCacheService.SaveOrderAsync(currentOrder);

                await _telegramService.SendTextMessageAsync(chatId, 
                    $"Вы выбрали дату доставки: {date:yyyy-MM-dd}. \nТеперь введите время доставки (ЧЧ:ММ)");
            }
        }

        private async Task HandleCalendarNavigationAsync(CallbackQuery callbackQuery, int monthOffset)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var currentMonth = _calendarService.GetDateFromCallback(callbackQuery.Data);
            var newMonth = currentMonth.AddMonths(monthOffset);

            await _telegramService.EditMessageReplyMarkupAsync(
                chatId: chatId,
                messageId: callbackQuery.Message.MessageId,
                replyMarkup: InlineCalendarFactory.GetKeyboard(newMonth, callbackQuery.Message.MessageId)
            );
        }
    }
}