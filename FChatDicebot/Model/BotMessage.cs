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

        public string PrintedCommand() //only used for flist messages
        {
            string messageFull = string.Format("{0}{1}", messageType, (MessageDataFormat == null ? "" : " " + JsonConvert.SerializeObject(MessageDataFormat)));

            int maxLength = messageType == "PRI" ? BotMain.MaximumCharsInMessagePrivate : BotMain.MaximumCharsInMessageChannel;
            if (messageType == "STA")
                maxLength = 250;
            if (messageType == "CDS")
                maxLength = BotMain.MaximumCharsInMessagePrivate;

            //note: some characters count as more than one character, so length / 3 is the minimum length that could cause a problem
            if (messageFull.Length > maxLength / 3)
            {
                if (MessageDataFormat.GetType() == typeof(PRIclient))
                {
                    PRIclient ms = (PRIclient)MessageDataFormat;
                    int messageLengthBefore = ms.message.Length;
                    ms.message = Utils.LimitStringToNCharacters(ms.message, maxLength);
                    if (ms.message.Length < messageLengthBefore)
                        ms.message += "(maximum message length reached)";
                }
                else if (MessageDataFormat.GetType() == typeof(CDSclient))
                {
                    MSGclient ms = (MSGclient)MessageDataFormat;
                    int messageLengthBefore = ms.message.Length;
                    ms.message = Utils.LimitStringToNCharacters(ms.message, maxLength);
                    if (ms.message.Length < messageLengthBefore)
                        ms.message += "(maximum message length reached)";
                }
                else if (MessageDataFormat.GetType() == typeof(MSGclient))
                {
                    MSGclient ms = (MSGclient)MessageDataFormat;
                    int messageLengthBefore = ms.message.Length;
                    ms.message = Utils.LimitStringToNCharacters(ms.message, maxLength);
                    if (ms.message.Length < messageLengthBefore)
                        ms.message += "(maximum message length reached)";
                }
                else if (MessageDataFormat.GetType() == typeof(STAclient))
                {
                    STAclient ms = (STAclient)MessageDataFormat;
                    int messageLengthBefore = ms.statusmsg.Length;
                    ms.statusmsg = Utils.LimitStringToNCharacters(ms.statusmsg, maxLength);
                    if (ms.statusmsg.Length < messageLengthBefore)
                        ms.statusmsg += "(x)";
                }

                messageFull = string.Format("{0}{1}", messageType, (MessageDataFormat == null ? "" : " " + JsonConvert.SerializeObject(MessageDataFormat)));
            }

            return messageFull;
        }

        public bool IsDiscordmessage()
        {
            return messageType == BotMessageFactory.DiscordChannelMessage || messageType == BotMessageFactory.DiscordPrivateMessage;
        }
    }
}
