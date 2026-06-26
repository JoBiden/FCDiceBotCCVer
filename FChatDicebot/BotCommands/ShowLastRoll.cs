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
    public class ShowLastRoll : ChatBotCommand
    {
        public ShowLastRoll()
        {
            Name = "showlastroll";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.CharacterInventories;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            if (BotMain._debug)
                bot.SendMessageInChannel("Command recieved: " + Utils.PrintList(terms), address);

            string resultMessage = "";
            ChannelDiceRoll channelDiceRoll = bot.DiceBot.GetLastChannelDiceRoll(address);
            if (channelDiceRoll == null)
            {
                resultMessage = "Failed: No previous die roll in this channel was found.";
            }
            else if (channelDiceRoll.DiceRoll == null)
            {
                resultMessage = "Failed: Bad data for dice roll.";
            }
            else
            {
                DiceRoll diceRoll = channelDiceRoll.DiceRoll;

                bool sort = terms != null && terms.Contains("sort");
                //bool secret = terms != null && (terms.Contains("secret") || terms.Contains("s"));

                resultMessage = "Showing last dice roll for this channel: " + TextFormat.GetCharacterUserTags(channelDiceRoll.Character) + " rolled.\n" + diceRoll.ResultString(DiceRollFormat.Inherit, sort);
            }

            if (!commandController.MessageCameFromChannel(address))
            {
                bot.SendPrivateMessage(resultMessage, address);
            }
            else
            {
                bot.SendMessageInChannel(resultMessage, address);
            }

            if (BotMain._debug)
                Console.WriteLine("Command finished: " + resultMessage);
        }
    }
}
