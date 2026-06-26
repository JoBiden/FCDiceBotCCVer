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
    public class Bet : ChatBotCommand
    {
        public Bet()
        {
            Name = "bet";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            if (thisChannel.AllowChips)
            {
                bool all = false;

                int betAmount = Utils.GetNumberFromInputs(terms);

                if (terms != null && terms.Length >= 1 && terms.Contains("all"))
                    all = true;

                string messageString = "";
                if (betAmount <= 0 && !all)
                {
                    messageString = "Failed: You must input a number over 0 or 'all' to make a bet.";
                }
                else
                {
                    messageString = bot.DiceBot.BetChips(address, betAmount, all);

                    commandController.SaveChipsToDisk("Bet");
                }

                bot.SendMessageInChannel(messageString, address);
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
        }
    }
}
