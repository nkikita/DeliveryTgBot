using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Handlers
{
    public class TelegramKeyboardBuilder : IKeyboardBuilder
    {
        private Dictionary<string, string> _callbackDataToAddress = new Dictionary<string, string>();
        public InlineKeyboardMarkup BuildAddressKeyboard(List<string> addresses)
        {
            _callbackDataToAddress.Clear();

            var buttons = new List<InlineKeyboardButton[]>();

            for (int i = 0; i < addresses.Count; i++)
            {
                string id = $"addr_{i}"; // короткий id
                string address = addresses[i];

                _callbackDataToAddress[id] = address;

                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(address, id)
                });
            }

            return new InlineKeyboardMarkup(buttons);
        }

    }
}