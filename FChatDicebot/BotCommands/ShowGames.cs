using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using FChatDicebot.SavedData;
using Newtonsoft.Json;

namespace FChatDicebot.BotCommands
{
    public class ShowGames : ChatBotCommand
    {
        public ShowGames()
        {
            Name = "showgames";
            Aliases = new string[] { };
            Category = "Dicebot";
            ShortDescription = "List every game type available with !joingame";
            LongDescription = "Shows the full list of dice games currently supported, by name, for use with !joingame.";
            Usage = "!showgames";
            RelatedCommands = new string[] { "joingame" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {

            string output = string.Join(", ", bot.DiceBot.PossibleGames.Select(a => a.GetGameName()));

            bool fromChannel = commandController.MessageCameFromChannel(address);

            if (fromChannel)
                bot.SendMessageInChannel("List of current games available with !joingame: " + output, address);
            else
                bot.SendPrivateMessage("List of current games available with !joingame: " + output, address);

        }
    }
}
