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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            if (thisChannel.AllowChips)
            {
                bool all = false;

                int betAmount = Utils.GetNumberFromInputs(terms);

                if (terms != null && terms.Length >= 1 && terms.Contains("all"))
                    all = true;

                string messageString = "";
                if (betAmount == 0 && !all)
                {
                    messageString = "Error: You must input a number or 'all' to make a bet.";
                }
                else
                {
                    messageString = bot.DiceBot.BetChips(characterName, channel, betAmount, all);

                    commandController.SaveChipsToDisk("Bet");
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
