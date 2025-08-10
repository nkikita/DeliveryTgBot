using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Interfaces
{
   public interface IKeyboardBuilder
    {
        (InlineKeyboardMarkup keyboard, Dictionary<string, string> map) BuildAddressKeyboard(List<string> addresses);
    }

}