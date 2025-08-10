using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeliveryTgBot.Models
{
    public class UserSession
    {
        public bool WaitingForTime { get; set; }
        public bool WaitingForComment { get; set; }
    }

}