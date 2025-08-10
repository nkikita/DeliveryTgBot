using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Interfaces
{
    public interface IKeyboardBuilder
    {
        InlineKeyboardMarkup BuildAddressKeyboard(List<string> addresses);
    }
}