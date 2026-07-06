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
    public class FitD : ChatBotCommand
    {
        public FitD()
        {
            Name = "fitd";
            Aliases = new string[] { };
            Category = "Dicebot";
            ShortDescription = "Roll a Forged in the Dark style dice pool";
            LongDescription = "Rolls the given number of six-sided dice and reports the highest result, Forged-in-the-Dark style (6 = success, 4-5 = partial success, 1-3 = failure, multiple 6s = critical).";
            Usage = "!fitd {number of dice}";
            RelatedCommands = new string[] { "roll" };
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
            int numberRolled = Utils.GetNumberFromInputs(terms);

            string resultString = "";

            if (numberRolled > DiceBot.MaximumDice)
            {
                resultString = "Error: Cannot roll more than " + DiceBot.MaximumDice + " dice.";
            }
            else
            {
                resultString = bot.DiceBot.RollFitD(numberRolled, address);
            }

            if (!commandController.MessageCameFromChannel(address))
            {
                bot.SendPrivateMessage(resultString, address);
            }
            else
            {
                bot.SendMessageInChannel(resultString, address);
            }
        }
    }
}
