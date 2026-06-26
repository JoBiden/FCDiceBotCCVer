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
    public class ChateauBio : ChatBotCommand
    {
        public ChateauBio()
        {
            Name = "bio";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !dossier";
            LongDescription = "Alternative wording for the !dossier command. See !help dossier for full details.";
            Usage = "!bio\nor\n!bio [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "dossier", "bank", "pledges" };
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
            ChateauDossier dossier = new ChateauDossier();
            dossier.Run(bot, commandController, rawTerms, terms, address, command);
        }
    }
}
