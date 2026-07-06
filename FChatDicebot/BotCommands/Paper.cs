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
    public class Paper : ChatBotCommand
    {
        public Paper()
        {
            Name = "paper";
            Aliases = new string[] { };
            Category = "Dicebot";
            ShortDescription = "Play paper in an active Rock-Paper-Scissors game";
            LongDescription = "Submits paper as your move in the Rock-Paper-Scissors session you've joined in this channel via !joingame. (Lizard and Spock only apply if the session was started as the five-way variant.)";
            Usage = "!paper";
            RelatedCommands = new string[] { "rock", "scissors", "lizard", "spock", "joingame" };
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
            SendCommandToGame.Run(bot, commandController, rawTerms, terms, address, command, "paper", "RockPaperScissors");
        }
    }
}
