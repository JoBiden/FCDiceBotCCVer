using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;

namespace FChatDicebot.BotCommands
{
    public class Alive : ChatBotCommand
    {
        public Alive()
        {
            Name = "alive";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string messageText = "Yes, the Chateau is ALIVE! Always listening, always watching... in kind of a pervy way, to be honest.";

            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(messageText + "\n\nThank you for checking on the Chateau, it means a lot ♥", characterName);
            }
            else
            {
                bot.SendMessageInChannel(messageText, channel);
            }
        }
    }
}
