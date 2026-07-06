using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class Scissors : ChatBotCommand
    {
        public Scissors()
        {
            Name = "scissors";
            Aliases = new string[] { };
            Category = "Dicebot";
            ShortDescription = "Play scissors in an active Rock-Paper-Scissors game";
            LongDescription = "Submits scissors as your move in the Rock-Paper-Scissors session you've joined in this channel via !joingame. (Lizard and Spock only apply if the session was started as the five-way variant.)";
            Usage = "!scissors";
            RelatedCommands = new string[] { "rock", "paper", "lizard", "spock", "joingame" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            SendCommandToGame.Run(bot, commandController, rawTerms, terms, address, command, "scissors", "RockPaperScissors");
        }
    }
}
