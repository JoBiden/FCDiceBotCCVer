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
    public class JoinGame : ChatBotCommand
    {
        public JoinGame()
        {
            Name = "joingame";
            Aliases = new string[] { };
            Category = "Dicebot";
            ShortDescription = "Join or start a dice game";
            LongDescription = "Join the current game session for a game type, or start a new one if none exists. Optionally stake an amount of currency - the first player to name a stake opens it, later players either match it exactly or propose a different stake (which the opener must !accept). Joining with no stake at all makes it a friendly, no-money game.";
            Usage = "!joingame {game name}\nor\n!joingame {game name} {amount} {currency}";
            RelatedCommands = new string[] { "startgame", "leavegame", "gamestatus", "showgames", "cancelgame" };
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
            AddPlayerToGame.Run(bot, commandController, rawTerms, terms, address, command, address.character, Name);
        }
    }
}
