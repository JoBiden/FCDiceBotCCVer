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
    public class TipDie : ChatBotCommand
    {
        public TipDie()
        {
            Name = "tipdie";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.CharacterInventories;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            if (BotMain._debug)
                bot.SendMessageInChannel("Command recieved: " + Utils.PrintList(terms), address);

            string resultMessage = "";
            DiceRoll diceRoll = bot.DiceBot.GetLastDiceRoll(address);
            if(diceRoll == null)
            {
                resultMessage = "Failed: No previous die roll in this channel was found.";
            }
            else
            {
                int changeTo = diceRoll.DiceSides;
                int changeFrom = -1;
                if(terms.Count() == 0)
                {
                    changeFrom = diceRoll.Rolls.Min(a => a.Result);
                }
                else{
                    string tempString = terms[0];
                    if(tempString.Contains(">") || tempString.Contains("to"))
                    {
                        string[] split = tempString.Split('>');
                        if(split == null || split.Length == 0)
                        {
                            split = tempString.Split(new char[] {'t','o'});
                        }
                        if(split != null && split.Length == 2)
                        {
                            split[0] = split[0].Trim();
                            split[1] = split[1].Trim();
                            int.TryParse(split[0], out changeFrom);
                            int.TryParse(split[1], out changeTo);
                        }
                        else
                        {

                        }
                    }
                }

                if(changeFrom < 0 || changeTo < 0)
                {
                    resultMessage = "Error: Incorrect input for TipDie. use !TipDie #>#";
                }
                else if(changeFrom > diceRoll.DiceSides || changeTo > diceRoll.DiceSides)
                {
                    resultMessage = "Error: The last roll recorded was done using a die with " + diceRoll.DiceSides + ". Numbers higher are not valid.";
                }
                else
                {
                    DiceFace foundFace = diceRoll.Rolls.FirstOrDefault(a => a.Result == changeFrom);
                    //int targetIndex = diceRoll.Rolls.IndexOf(changeFrom);
                    foundFace.Result = changeTo;
                    //diceRoll.Rolls[targetIndex].Result = changeTo;

                    diceRoll.Total = diceRoll.Rolls.Sum(a => a.Result);

                    resultMessage = TextFormat.GetCharacterUserTags(address.character) + " [color=yellow]tipped the die[/color] and " + diceRoll.ResultString();
                }
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
