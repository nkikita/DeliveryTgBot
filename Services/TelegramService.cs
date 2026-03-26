using Microsoft.Extensions.Logging;

namespace DeliveryTgBot.Services
{
 // Класс
    public class TelegramService : ITelegramService
    {
        public ITelegramBotClient BotClient { get; }
        private readonly ILogger<TelegramService> _logger;

        public TelegramService(ITelegramBotClient botClient, ILogger<TelegramService> logger)
        {
            BotClient = botClient;
            _logger = logger;
        }

        public async Task SendTextMessageAsync(long chatId, string text, InlineKeyboardMarkup? replyMarkup = null)
        {
            await BotClient.SendMessage(chatId, text, replyMarkup: replyMarkup, parseMode: ParseMode.Markdown);
        }

        public async Task EditMessageReplyMarkupAsync(long chatId, int messageId, InlineKeyboardMarkup? replyMarkup)
        {
            try
            {
                await BotClient.EditMessageReplyMarkup(chatId, messageId, replyMarkup);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                _logger.LogError(ex, "Ошибка Telegram API");
            }
        }
    }

}