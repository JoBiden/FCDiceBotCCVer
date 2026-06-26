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
    public class ChateauDress : ChatBotCommand
    {
        public ChateauDress()
        {
            Name = "dress";
            Aliases = new string[] { };
            Category = "Involved Interaction";
            ShortDescription = "Alias for !dressup";
            LongDescription = "Alternative wording for the !dressup command. See !help dressup for full details.";
            Usage = "!dress [noparse][user]NameInUserTag[/user][/noparse] {attire}\n(use your own username to dress yourself)";
            RelatedCommands = new string[] { "dressup", "volunteer", "work" };
            CooldownDuration = "30 Minutes";
            CooldownAppliesTo = "both initiator and recipient";
            IdentifierCategory = "attire";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            ChateauDressup dressup = new ChateauDressup();
            dressup.Run(bot, commandController, rawTerms, terms, address, command);
        }
    }
}
