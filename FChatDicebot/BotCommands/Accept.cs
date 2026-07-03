using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class Accept : ChatBotCommand
    {
        public Accept()
        {
            Name = "accept";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            // !accept is the ordinary alias for !consent, which itself checks for a pending
            // targeted wager proposal first (disposition #2) — no need to duplicate that
            // check here too.
            ChateauConsent accept = new ChateauConsent();
            accept.Run(bot, commandController, rawTerms, terms, address, command);
        }
    }
}
