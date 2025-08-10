using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Interfaces
{
    public interface ITelegramService
    {
        ITelegramBotClient BotClient { get; }
        Task SendTextMessageAsync(long chatId, string text, InlineKeyboardMarkup  replyMarkup = null);
        Task EditMessageReplyMarkupAsync(long chatId, int messageId, InlineKeyboardMarkup  replyMarkup);
    // Можно добавить обработку инлайн кнопок, колбеков и пр.
    }
}