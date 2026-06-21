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
    public class ChateauHire : ChatBotCommand
    {
        public ChateauHire()
        {
            Name = "hire";
            Aliases = new string[] { };
            Category = "Commitment Interaction";
            ShortDescription = "Alias for !employ";
            LongDescription = "Alternative wording for the !employ command. See !help employ for full details.";
            Usage = "!hire [noparse][user]NameInUserTag[/user][/noparse] {job}\n(use your own username to self-employ)";
            RelatedCommands = new string[] { "employ", "work", "bank" };
            CooldownDuration = "1 Day";
            CooldownAppliesTo = "recipient";
            IdentifierCategory = "job";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChateauEmploy employ = new ChateauEmploy();
            employ.Run(bot, commandController, rawTerms, terms, characterName, channel, command);
        }
    }
}
