using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.Model;
using System.Net;
using Newtonsoft.Json;
using System.Threading;
using FChatDicebot.SavedData;

namespace FChatDicebot
{
    public class BotFutureMessage
    {
        public string MessageContent;
        public string Channel;
        public string Character;

        public bool ChannelMessage;

        public bool Sent;

        public int MsWait;
    }
}
