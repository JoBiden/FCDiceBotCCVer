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
    public class ChateauV : ChatBotCommand
    {
        public ChateauV()
        {
            Name = "v";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !volunteer";
            LongDescription = "Shorthand for the !volunteer command. See !help volunteer for full details.";
            Usage = "!v\n!v [choice number]";
            RelatedCommands = new string[] { "volunteer", "work", "bank" };
            CooldownDuration = "1 day";
            CooldownAppliesTo = "initiator";
            IdentifierCategory = "job";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChateauVolunteer volunteer = new ChateauVolunteer();
            volunteer.Run(bot, commandController, rawTerms, terms, characterName, channel, command);
           
        }
    }
}
