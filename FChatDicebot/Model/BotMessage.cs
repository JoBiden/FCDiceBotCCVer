using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    public class BotMessage
    {
        public string messageType; //3 letter message type
        public iSocketCommand MessageDataFormat; //JCHclient, for example

        public string PrintedCommand()
        {
            string messageFull = string.Format("{0}{1}", messageType, (MessageDataFormat == null ? "" : " " + JsonConvert.SerializeObject(MessageDataFormat)));

            if (messageFull.Length > BotMain.MaximumCharsInMessage && MessageDataFormat.GetType() == typeof(MSGclient))
            {
                MSGclient ms = (MSGclient)MessageDataFormat;
                ms.message = Utils.LimitStringToNCharacters(ms.message, BotMain.MaximumCharsInMessage) + "(maximum message length reached)";

                messageFull = string.Format("{0}{1}", messageType, (MessageDataFormat == null ? "" : " " + JsonConvert.SerializeObject(MessageDataFormat)));
            }
            
            return messageFull;
        }

    }
}
