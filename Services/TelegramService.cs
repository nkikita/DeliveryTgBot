namespace DeliveryTgBot.Services
{
 // Класс
    public class TelegramService : ITelegramService
    {
        public ITelegramBotClient BotClient { get; }

        public TelegramService(ITelegramBotClient botClient)
        {
            BotClient = botClient;
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
                Console.WriteLine($"Telegram API Error: {ex.Message}");
            }
        }
    }

}