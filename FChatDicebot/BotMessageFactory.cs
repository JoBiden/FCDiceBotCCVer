using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot
{
    public class BotMessageFactory
    {
        public const string IDN = "IDN", MSG = "MSG", PIN = "PIN", JCH = "JCH", PRI = "PRI";
        public const string STA = "STA", LCH = "LCH", COL = "COL";

        public static BotMessage NewMessage(string messageType, iSocketCommand messageFormat)
        {
            return new BotMessage() { messageType = messageType, MessageDataFormat = messageFormat };
        }
    }
}
