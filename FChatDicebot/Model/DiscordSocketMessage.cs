using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace FChatDicebot.Model
{
    //server hello recieved
    public class DiscordSocketMessage : iSocketCommand
    {
        public string message;
        public string character;
        public string channel;
        public string guild;

        public DiscordSocketMessage()
        {

        }
        public DiscordSocketMessage(MSGserver msgServer)
        {
            message = msgServer.message;
            character = msgServer.character;
            channel = msgServer.channel;
            guild = null;
        }

        public DiscordSocketMessage(SocketMessage message, string guild)
        {
            this.message = message.Content;
            character = message.Author.GlobalName;
            channel = message.Channel == null ? null : message.Channel.Id.ToString();
            this.guild = guild;
        }
    }
}
