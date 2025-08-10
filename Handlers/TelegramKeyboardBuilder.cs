using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Handlers
{
    public class TelegramKeyboardBuilder : IKeyboardBuilder
    {
        private Dictionary<string, string> _callbackDataToAddress = new Dictionary<string, string>();
        public (InlineKeyboardMarkup keyboard, Dictionary<string, string> map) BuildAddressKeyboard(List<string> addresses)
        {
            var map = new Dictionary<string, string>();
            var buttons = new List<InlineKeyboardButton[]>();

            for (int i = 0; i < addresses.Count; i++)
            {
                var id = $"addr_{i}";
                var address = addresses[i];
                map[id] = address;

                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(address, id) });
            }

            var keyboard = new InlineKeyboardMarkup(buttons);
            return (keyboard, map);
        }


    }
}