﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    public class UserMessageLine
    {
        public DateTime DinnerDate { get; }
        public string Name { get; }
        public int Minutes { get; }
        public UserMessageLine(DateTime dinnerDate, string info, int minutes)
        {
            DinnerDate = dinnerDate;
            Name = info;
            Minutes = minutes;
        }
        public override string ToString()
        {
            return $"{DinnerDate:HH:mm} {Name} {Minutes}";
        }
    }
}
