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
    public class LeaveGame : ChatBotCommand
    {
        public LeaveGame()
        {
            Name = "leavegame";
            Aliases = new string[] { };
            Category = "Dicebot";
            ShortDescription = "Leave a dice game session you've joined";
            LongDescription = "Removes you from the named game's session in this channel. If you had a currency stake committed, it's refunded.";
            Usage = "!leavegame {game name}";
            RelatedCommands = new string[] { "joingame", "cancelgame" };
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
            RemovePlayerFromGame.Run(bot, commandController, rawTerms, terms, address, command, address.character, Name);
        }
    }
}
