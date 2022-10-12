using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot
{
    public class UserMessageLine
    {
        public DateTime DinnerDate { get; set; }
        public string Info { get; set; }
        public UserMessageLine(DateTime dinnerDate, string info)
        {
            DinnerDate = dinnerDate;
            Info = info;
        }
        public override string ToString()
        {
            return $"{DinnerDate:HH:mm} {Info}";
        }
    }
}
