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
    public class ChateauStats : ChatBotCommand
    {
        public ChateauStats()
        {
            Name = "stats";
            Aliases = new string[] { };
            Category = "Information";
            ShortDescription = "Alias for !statistics";
            LongDescription = "Alternative wording for the !statistics command. See !help statistics for full details.";
            Usage = "!stats";
            RelatedCommands = new string[] { "statistics", "populations", "economics" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChateauStatistics statistics = new ChateauStatistics();
            statistics.Run(bot, commandController, rawTerms, terms, characterName, channel, command);
        }
    }
}
