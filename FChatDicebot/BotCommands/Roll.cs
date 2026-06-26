using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class Roll : ChatBotCommand
    {
        public Roll()
        {
            Name = "roll";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            if (BotMain._debug)
                bot.SendMessageInChannel("Command recieved: " + Utils.PrintList(terms), address);

            bool debugOverride = true;
            string finalResult = bot.DiceBot.GetRollResult(terms, address, debugOverride);

            string userName = "[i]" + TextFormat.GetCharacterUserTags(address.character) + "[/i]: ";

            if (!commandController.MessageCameFromChannel(address))
            {
                bot.SendPrivateMessage(userName + finalResult, address);
            }
            else
            {
                bot.SendMessageInChannel(userName + finalResult, address);
            }

            if (BotMain._debug)
                Console.WriteLine("Command finished: " + finalResult);
        }
    }
}
