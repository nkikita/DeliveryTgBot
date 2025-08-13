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
        private readonly IDriverService _driverService;
        private readonly IOrderService _orderService;
        private readonly ConcurrentDictionary<long, bool> _waitingForComment = new();
        private readonly ConcurrentDictionary<long, Dictionary<string, string>> _userAddressMaps = new();

        public MessageProcessor(
            IOrderCacheService orderCacheService,
            IOrderStateManager orderStateManager,
            ITelegramService telegramService,
            IAddressService addressService,
            IKeyboardBuilder keyboardBuilder,
            IDriverService driverService,
            IOrderService orderService)
        {
            _orderCacheService = orderCacheService;
            _orderStateManager = orderStateManager;
            _telegramService = telegramService;
            _addressService = addressService;
            _keyboardBuilder = keyboardBuilder;
            _driverService = driverService;
            _orderService = orderService;
        }

        public async Task ProcessTextMessageAsync(long chatId, string text, string username)
        {
            var currentOrder = await _orderCacheService.GetOrCreateOrderAsync(chatId);

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

                // Handle driver selection if needed
                if (currentOrder.VehiclesCount > 0 && currentOrder.AssignedDriverId == null)
                {
                    await HandleDriverSelectionAsync(chatId, currentOrder);
                    return;
                }

                // Check if we need to wait for comment (after time input)
                if (currentOrder.DeliveryDateTime != default && currentOrder.CommentFromUsers == null)
                {
                    _waitingForComment[chatId] = true;
                    var commentPrompt = await _orderStateManager.GetNextPromptAsync(currentOrder);
                    await _telegramService.SendTextMessageAsync(chatId, commentPrompt);
                    return;
                }

                // Get next prompt for other cases
                var nextPrompt = await _orderStateManager.GetNextPromptAsync(currentOrder);
                await _telegramService.SendTextMessageAsync(chatId, nextPrompt);
            }
            else
            {
                // If no state was processed, check if we need to send a prompt
                // This handles cases where the input wasn't recognized but we need to guide the user
                if (currentOrder.CommentFromUsers == null)
                {
                    // User should enter comment
                    _waitingForComment[chatId] = true;
                    await _telegramService.SendTextMessageAsync(chatId, "💬 Есть пожелания для водителя? Напишите их здесь. Если комментариев нет — просто отправьте '-' (минус). ✍️:");
                }
                else if (currentOrder.DeliveryAdress == null)
                {
                    // User should enter address
                    await _telegramService.SendTextMessageAsync(chatId, "Введите адрес доставки (пример: Ленина 12):");
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
            var suggestions = await _addressService.GetAddressSuggestionsAsync(text, order.City.CityName);

            if (suggestions.Count == 0)
            {
                await _telegramService.SendTextMessageAsync(chatId, "Адрес не найден, попробуйте ввести подробнее.");
                return;
            }

            var (keyboard, map) = _keyboardBuilder.BuildAddressKeyboard(suggestions);
            _userAddressMaps[chatId] = map;
            await _telegramService.SendTextMessageAsync(chatId, "Выберите адрес из списка:", replyMarkup: keyboard);
        }

        private async Task HandleDriverSelectionAsync(long chatId, Order order)
        {
            var drivers = await _driverService.GetAvailableDriversAsync(order.CityId.Value, order.Volume, order.VehiclesCount);
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

        public async Task HandleAddressSelectionAsync(long chatId, string selectedAddress)
        {
            var currentOrder = await _orderCacheService.GetOrCreateOrderAsync(chatId);
            currentOrder.DeliveryAdress = selectedAddress;

            await _orderCacheService.SaveOrderAsync(currentOrder);
            await _orderService.SaveOrderAsync(currentOrder);

            await _telegramService.SendTextMessageAsync(chatId, $"Вы выбрали адрес: {selectedAddress}");
            
            if (await _orderStateManager.IsOrderCompleteAsync(currentOrder))
            {
                await _orderService.SaveOrderAsync(currentOrder);
            }
        }

        public bool IsWaitingForComment(long chatId) => _waitingForComment.ContainsKey(chatId);
        public Dictionary<string, string> GetAddressMap(long chatId) => _userAddressMaps.GetValueOrDefault(chatId, new());
    }
}
