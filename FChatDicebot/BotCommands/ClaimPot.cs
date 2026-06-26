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
    public class ClaimPot : ChatBotCommand
    {
        public ClaimPot()
        {
            Name = "claimpot";
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
                double portion = 1;
                if (terms != null && terms.Length >= 1 && terms.Contains("half"))
                    portion = .5;

                if (terms != null && terms.Length >= 1 && terms.Contains("third"))
                    portion = .333333;

                int getNumberFrom = Utils.GetNumberFromInputs(terms);

                string messageString = bot.DiceBot.ClaimPot(address, portion, getNumberFrom);

                commandController.SaveChipsToDisk("ClaimPot");

                bot.SendMessageInChannel(messageString, address);
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
        }
    }
}
