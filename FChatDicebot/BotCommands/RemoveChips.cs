using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class RemoveChips : ChatBotCommand
    {
        public RemoveChips()
        {
            Name = "removechips";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {

            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            if (thisChannel.AllowChips)
            {
                string messageString = "";
                if (terms.Length < 1)
                {
                    messageString = "Error: This command requires a number.";
                }
                else
                {
                    bool pot = false;

                    int chipAmount = Utils.GetNumberFromInputs(terms);

                    if (terms != null && terms.Length >= 1 && terms.Contains("pot"))
                        pot = true;

                    messageString = "";
                    if (chipAmount <= 0)
                    {
                        messageString = "Error: You must specify a number of chips above 0 to remove.";
                    }
                    else
                    {
                        messageString = bot.DiceBot.AddChips(characterName, channel, -1 * chipAmount, pot);

                        commandController.SaveChipsToDisk("RemoveChips");
                    }
                }

                bot.SendMessageInChannel(messageString, channel);
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", channel);
            }
        }
    }
}
