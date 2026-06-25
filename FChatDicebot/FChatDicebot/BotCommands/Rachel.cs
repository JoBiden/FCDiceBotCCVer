using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class Rachel : ChatBotCommand
    {
        public Rachel()
        {
            Name = "rachel";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string result = "[color=pink][b]Pink[/b] is the best color ˚*＊*˚*＊~ " + FChatDicebot.TextFormat.Emoji("smoochpink");

            SendMessageToChannelOrUser(bot, commandController, address, result);
        }
    }
}
