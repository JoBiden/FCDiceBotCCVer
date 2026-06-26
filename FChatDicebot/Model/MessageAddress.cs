using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    //server hello recieved
    public class MessageAddress
    {
        public string character;
        public string channel;
        public string guild;

        public MessageAddress()
        {

        }
        public MessageAddress(MessageAddress address, string newCharacter)
        {
            character = newCharacter;
            guild = address.guild;
            channel = address.channel;
        }

        public MessageAddress(ChatMessage msg)
        {
            character = msg.character;
            channel = msg.channel;
            guild = msg.guild;
        }

        public string GetChannelKey()
        {
            if (Utils.IsDiscordMessage(this))
            {
                if (guild == BotMain.DiscordPmGuild)
                    return null;
                else
                    return guild;
            }
            else
            {
                return channel;// channel == null ? null : channel;
            }

            //return (address.guild == null ? "" : address.guild) + (address.channel == null? "_nullchannel" : address.channel);
        }
    }
}
