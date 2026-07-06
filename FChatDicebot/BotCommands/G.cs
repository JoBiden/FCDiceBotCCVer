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
    public class G : ChatBotCommand
    {
        public G()
        {
            Name = "g";
            Aliases = new string[] { };
            Category = "Dicebot";
            ShortDescription = "Alias for !gamecommand";
            LongDescription = "Shorthand for !gamecommand. See !help gamecommand for full details.";
            Usage = "!g {game name} {command}";
            RelatedCommands = new string[] { "gamecommand" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            GameCommand c = new GameCommand();
            c.Run(bot, commandController, rawTerms, terms, address, command);
        }
    }
}
