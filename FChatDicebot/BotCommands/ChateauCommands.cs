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
    public class ChateauCommands : ChatBotCommand
    {
        public ChateauCommands()
        {
            Name = "commands";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !help";
            LongDescription = "Alternative wording for the !help command. See !help for the full command list.";
            Usage = "!commands\nor\n!commands {commandname}";
            RelatedCommands = new string[] { "help", "botinfo", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            ChateauHelp help = new ChateauHelp();
            help.Run(bot, commandController, rawTerms, terms, address, command);
        }
    }
}
