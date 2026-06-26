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
    public class ChateauC : ChatBotCommand
    {
        public ChateauC()
        {
            Name = "c";
            Aliases = new string[] { };
            Category = "Interaction Support";
            ShortDescription = "Alias for !consent";
            LongDescription = "Shorthand for the !consent command. See !help consent for full details.";
            Usage = "!c";
            RelatedCommands = new string[] { "consent" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            ChateauConsent accept = new ChateauConsent();
            accept.Run(bot, commandController, rawTerms, terms, address, command);
        }
    }
}
