using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class FitD : ChatBotCommand
    {
        public FitD()
        {
            Name = "fitd";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            int numberRolled = Utils.GetNumberFromInputs(terms);

            string resultString = "";

            if (numberRolled > DiceBot.MaximumDice)
            {
                resultString = "Error: Cannot roll more than " + DiceBot.MaximumDice + " dice.";
            }
            else
            {
                resultString = bot.DiceBot.RollFitD(numberRolled, channel, characterName);
            }

            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(resultString, characterName);
            }
            else
            {
                bot.SendMessageInChannel(resultString, channel);
            }
        }
    }
}
