using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Services
{
    public class CalendarService : ICalendarService
    {
        public DateTime GetDateFromCallback(string callbackData)
        {
            string dateString = callbackData.Split(':')[1];
            int year = int.Parse(dateString.Substring(0, 4));
            int month = int.Parse(dateString.Substring(4, 2));
            int day = int.Parse(dateString.Substring(6, 2));

            return new DateTime(year, month, day);
        }

        public Task<InlineKeyboardMarkup> GetKeyboardAsync(DateTime baseDate, int offset)
        {
            var targetDate = baseDate.AddMonths(offset);
            var buttons = new List<InlineKeyboardButton[]>();
            var today = DateTime.Today;

            // Заголовок месяца
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(
                targetDate.ToString("MMMM yyyy"),
                $"month:{targetDate:yyyyMM}") 
            });

            // Кнопки дней
            for (int i = 1; i <= DateTime.DaysInMonth(targetDate.Year, targetDate.Month); i++)
            {
                var day = new DateTime(targetDate.Year, targetDate.Month, i);

                if (day < today)
                    continue;

                var button = InlineKeyboardButton.WithCallbackData(
                    day.ToString("dd"),
                    $"date:{day:yyyyMMdd}"
                );
                buttons.Add(new[] { button });
            }

            // Кнопки переключения месяцев
            var navButtons = new List<InlineKeyboardButton>();

            // Кнопка "предыдущий месяц" — только если не уходим в прошлое
            if (targetDate.AddMonths(-1) >= today.AddMonths(0))
            {
                navButtons.Add(InlineKeyboardButton.WithCallbackData("⬅️", $"prev:{targetDate:yyyyMM}"));
            }

            // Кнопка "следующий месяц"
            navButtons.Add(InlineKeyboardButton.WithCallbackData("➡️", $"next:{targetDate:yyyyMM}"));

            buttons.Add(navButtons.ToArray());

            return Task.FromResult(new InlineKeyboardMarkup(buttons));
        }
    }
        

}