using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class JoinChateau : ChatBotCommand
    {
        public JoinChateau()
        {
            Name = "joinchateau";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string confirmMessage = MonDB.registerUserChateau(characterName);
            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(confirmMessage, characterName);
            }
            else
            {
                if (confirmMessage.Substring(0,1) == "Y")
                {
                    confirmMessage += " Showing someone the ropes?";
                }
                bot.SendMessageInChannel(confirmMessage, channel);
            }
        }
    }
}
