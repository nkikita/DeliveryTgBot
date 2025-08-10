using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Helpers
{
    public class CallbackHelper
    {
         public static bool IsAddressCallback(string data)
        {
            return data.StartsWith("address:");
        }
    }
}