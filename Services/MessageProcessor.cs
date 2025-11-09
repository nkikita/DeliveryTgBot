using DeliveryTgBot.Interfaces;
using DeliveryTgBot.Models;
using System.Collections.Concurrent;

namespace DeliveryTgBot.Services
{
    public class MessageProcessor
    {
        private readonly IOrderCacheService _orderCacheService;
        private readonly IOrderStateManager _orderStateManager;
        private readonly ITelegramService _telegramService;
        private readonly IAddressService _addressService;
        private readonly IKeyboardBuilder _keyboardBuilder;
        private readonly IOrderService _orderService;
        private readonly ConcurrentDictionary<long, bool> _waitingForComment = new();
        private readonly ConcurrentDictionary<long, Dictionary<string, string>> _userAddressMaps = new();
        private readonly ConcurrentDictionary<long, int> _lastAddressMsgIds = new();
        private readonly ConcurrentDictionary<long, int> _lastCalendarMsgIds = new();

        public MessageProcessor(
            IOrderCacheService orderCacheService,
            IOrderStateManager orderStateManager,
            ITelegramService telegramService,
            IAddressService addressService,
            IKeyboardBuilder keyboardBuilder,
            IOrderService orderService)
        {
            _orderCacheService = orderCacheService;
            _orderStateManager = orderStateManager;
            _telegramService = telegramService;
            _addressService = addressService;
            _keyboardBuilder = keyboardBuilder;
            _orderService = orderService;
        }

        public async Task ProcessTextMessageAsync(long chatId, string text, string username)
        {
            var currentOrder = await _orderCacheService.GetOrCreateOrderAsync(chatId);

            // If user typed a new message, disable previously sent calendar to avoid outdated clicks
            if (_lastCalendarMsgIds.TryGetValue(chatId, out var prevCalendarMsgId))
            {
                await _telegramService.EditMessageReplyMarkupAsync(chatId, prevCalendarMsgId, replyMarkup: null);
                _lastCalendarMsgIds.TryRemove(chatId, out _);
            }

            // Handle comment input
            if (_waitingForComment.TryGetValue(chatId, out var isWaiting) && isWaiting)
            {
                await HandleCommentInputAsync(chatId, text, currentOrder, username);
                return;
            }

            // Handle address input
            if (currentOrder.CommentFromUsers != null && currentOrder.DeliveryAdress == null)
            {
                await HandleAddressInputAsync(chatId, text, currentOrder);
                return;
            }

            // Handle order state progression
            if (await _orderStateManager.ProcessOrderStateAsync(currentOrder, text))
            {
                await _orderCacheService.SaveOrderAsync(currentOrder);
                
                if (await _orderStateManager.IsOrderCompleteAsync(currentOrder))
                {
                    await _orderService.SaveOrderAsync(currentOrder);
                    return;
                }

                // Driver selection removed

                // Check if we need to wait for comment (only after time is set)
                if (currentOrder.DeliveryDateTime != default 
                    && currentOrder.DeliveryDateTime.TimeOfDay != default
                    && currentOrder.CommentFromUsers == null)
                {
                    _waitingForComment[chatId] = true;
                    var commentPrompt = await _orderStateManager.GetNextPromptAsync(currentOrder);
                    await _telegramService.SendTextMessageAsync(chatId, commentPrompt);
                    return;
                }

                // Get next prompt for other cases (send only one prompt)
                var nextPrompt = await _orderStateManager.GetNextPromptAsync(currentOrder);
                if (nextPrompt.StartsWith("Выберите дату доставки:"))
                {
                    // Disable old calendar keyboard if exists
                    if (_lastCalendarMsgIds.TryGetValue(chatId, out var prevCalMsgId))
                    {
                        await _telegramService.EditMessageReplyMarkupAsync(chatId, prevCalMsgId, replyMarkup: null);
                    }
                    var today = DateTime.Today;
                    var keyboard = InlineCalendarFactory.GetKeyboard(today, 0);
                    var sent = await _telegramService.BotClient.SendMessage(chatId, nextPrompt, replyMarkup: keyboard);
                    _lastCalendarMsgIds[chatId] = sent.MessageId;
                }
                else
                {
                    await _telegramService.SendTextMessageAsync(chatId, nextPrompt);
                }
            }
            else
            {
                // If no state was processed, derive the next prompt based on current order state
                var nextPrompt = await _orderStateManager.GetNextPromptAsync(currentOrder);
                if (nextPrompt.Contains("пожелания для заказа"))
                {
                    _waitingForComment[chatId] = true;
                    await _telegramService.SendTextMessageAsync(chatId, nextPrompt);
                }
                else if (nextPrompt.StartsWith("Введите адрес доставки"))
                {
                    await _telegramService.SendTextMessageAsync(chatId, nextPrompt);
                }
                else if (nextPrompt.StartsWith("Выберите дату доставки"))
                {
                    var today = DateTime.Today;
                    var keyboard = InlineCalendarFactory.GetKeyboard(today, 0);
                    await _telegramService.SendTextMessageAsync(chatId, nextPrompt, keyboard);
                }
                else
                {
                    await _telegramService.SendTextMessageAsync(chatId, nextPrompt);
                }
            }
        }

        private async Task HandleCommentInputAsync(long chatId, string text, Order order, string username)
        {
            var comment = text.Trim();
            if (comment == "-") comment = string.Empty;

            order.CommentFromUsers = comment;
            order.ClientTelegramUsername = username;

            await _orderCacheService.SaveOrderAsync(order);
            await _orderService.SaveOrderAsync(order);

            _waitingForComment.TryRemove(chatId, out _);

            await _telegramService.SendTextMessageAsync(chatId, "Введите адрес доставки (пример: Ленина 12):");
        }

        private async Task HandleAddressInputAsync(long chatId, string text, Order order)
        {
            try
            {
                var query = text?.Trim();
                if (string.IsNullOrWhiteSpace(query))
                {
                    await _telegramService.SendTextMessageAsync(chatId, "Пожалуйста, введите адрес подробнее (например: Ленина 12).");
                    return;
                }

                // Treat negative/none confirmations as a request to enter a new address
                var negativeInputs = new[] { "нету", "не подходит", "не найдено", "none", "no", "other" };
                if (negativeInputs.Any(x => string.Equals(query, x, StringComparison.OrdinalIgnoreCase)))
                {
                    await _telegramService.SendTextMessageAsync(chatId, "Хорошо, введите другой адрес доставки (например: Ленина 12).");
                    return;
                }

                var cityName = order.City?.CityName;
                if (string.IsNullOrWhiteSpace(cityName))
                {
                    await _telegramService.SendTextMessageAsync(chatId, "Сначала выберите город, затем введите адрес доставки.");
                    return;
                }

                var suggestions = await _addressService.GetAddressSuggestionsAsync(query, cityName);
                var safeSuggestions = (suggestions ?? new List<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct()
                    .ToList();

                // Prefer entries that mention the selected city
                var cityMatched = safeSuggestions
                    .Where(s => s.Contains(cityName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                List<string> effectiveSuggestions;
                if (cityMatched.Count > 0)
                {
                    effectiveSuggestions = cityMatched;
                }
                else
                {
                    // Fall back to removing obvious out-of-city noise (countries/highways)
                    bool IsLikelyOutOfCity(string s)
                    {
                        return s.Contains("Казахстан", StringComparison.OrdinalIgnoreCase)
                            || s.Contains("Россия,", StringComparison.OrdinalIgnoreCase)
                            || s.Contains(" М-", StringComparison.OrdinalIgnoreCase)
                            || s.Contains(" Р-", StringComparison.OrdinalIgnoreCase)
                            || s.Contains(" A-", StringComparison.OrdinalIgnoreCase)
                            || s.Contains(" M-", StringComparison.OrdinalIgnoreCase)
                            || s.Contains(" P-", StringComparison.OrdinalIgnoreCase);
                    }

                    effectiveSuggestions = safeSuggestions
                        .Where(s => !IsLikelyOutOfCity(s))
                        .ToList();
                }

                effectiveSuggestions = effectiveSuggestions
                    .Distinct()
                    .Take(10)
                    .ToList();

                if (effectiveSuggestions.Count == 0)
                {
                    await _telegramService.SendTextMessageAsync(chatId, "🚫 Мы не смогли найти этот адрес в вашем городе. Пожалуйста, введите другой адрес или напишите в комментариях, если возникли проблемы.");
                    return;
                }

                var (keyboard, map) = _keyboardBuilder.BuildAddressKeyboard(effectiveSuggestions);
                _userAddressMaps[chatId] = map;

                // Disable old address keyboard if exists
                if (_lastAddressMsgIds.TryGetValue(chatId, out var prevAddrMsgId))
                {
                    await _telegramService.EditMessageReplyMarkupAsync(chatId, prevAddrMsgId, replyMarkup: null);
                }

                var sent = await _telegramService.BotClient.SendMessage(chatId, "Выберите адрес из списка:", replyMarkup: keyboard);
                _lastAddressMsgIds[chatId] = sent.MessageId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Address suggestions error: {ex.Message}");
                await _telegramService.SendTextMessageAsync(chatId, "⚠️ Не удалось распознать адрес из-за ошибки сервиса. Попробуйте снова, укажите адрес подробнее или добавьте его в комментарии.");
            }
        }

        // Driver selection removed

        public async Task HandleAddressSelectionAsync(long chatId, string selectedAddress)
        {
            var currentOrder = await _orderCacheService.GetOrCreateOrderAsync(chatId);
            currentOrder.DeliveryAdress = selectedAddress;

            await _orderCacheService.SaveOrderAsync(currentOrder);
            await _orderService.SaveOrderAsync(currentOrder);

            await _telegramService.SendTextMessageAsync(chatId, $"Вы выбрали адрес: {selectedAddress}");

            // Disable last address keyboard after selection
            if (_lastAddressMsgIds.TryGetValue(chatId, out var lastMsgId))
            {
                await _telegramService.EditMessageReplyMarkupAsync(chatId, lastMsgId, replyMarkup: null);
                _lastAddressMsgIds.TryRemove(chatId, out _);
            }
            
            if (await _orderStateManager.IsOrderCompleteAsync(currentOrder))
            {
                await _orderService.SaveOrderAsync(currentOrder);
            }
        }

        public bool IsWaitingForComment(long chatId) => _waitingForComment.ContainsKey(chatId);
        public Dictionary<string, string> GetAddressMap(long chatId) => _userAddressMaps.GetValueOrDefault(chatId, new());
    }
}
