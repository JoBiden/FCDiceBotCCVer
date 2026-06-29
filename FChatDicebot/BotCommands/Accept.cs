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
            string characterName = address.character;
            string channel = address.channel;

            // A game wager proposal awaiting this player takes priority; otherwise !accept is the
            // ordinary alias for !consent.
            string wagerResult = DiceFunctions.Wager.WagerGameSupport.TryAcceptWager(bot, address);
            if (wagerResult != null)
            {
                bot.SendMessageInChannel(wagerResult, address);
                return;
            }

            ChateauConsent accept = new ChateauConsent();
            accept.Run(bot, commandController, rawTerms, terms, address, command);
        }
    }
}
