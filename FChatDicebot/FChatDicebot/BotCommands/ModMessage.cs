using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;

namespace FChatDicebot.BotCommands
{
    public class ModMessage : ChatBotCommand
    {
        public ModMessage()
        {
            Name = "modmessage";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string messageText = MonDB.modMessage(terms[0]);

            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(messageText, characterName);
            }
            else
            {
                bot.SendMessageInChannel(messageText, channel);
            }
        }
    }
}
