using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Interfaces
{
    public interface ICalendarService
    {
        DateTime GetDateFromCallback(string callbackData);
        Task<InlineKeyboardMarkup> GetKeyboardAsync(DateTime baseDate, int offset);
    }
}