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
    public class ChateauProfile : ChatBotCommand
    {
        public ChateauProfile()
        {
            Name = "profile";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !dossier";
            LongDescription = "Shorthand for the !dossier command. See !help dossier for full details.";
            Usage = "!profile\nor\n!profile [user]CharacterName[/user]";
            RelatedCommands = new string[] { "dossier", "bank", "list" };
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
            ChateauDossier dossier = new ChateauDossier();
            dossier.Run(bot, commandController, rawTerms, terms, characterName, channel, command);
        }
    }
}
