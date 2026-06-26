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
    public class GenerateMonster : ChatBotCommand
    {
        public GenerateMonster()
        {
            Name = "generatemonster";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.RPGData;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = null;
            if (commandController.MessageCameFromChannel(address))
                thisChannel = bot.GetChannelSettings(address);

            bool fromChannel = commandController.MessageCameFromChannel(address);

            if (!fromChannel || ( thisChannel != null && thisChannel.AllowRPG )) //verify channel has permissions
            {
                string monsterIdentifier = Utils.GetFullStringOfInputs(rawTerms);

                bool shortStats = true;

                if (terms.Contains("fullstats") || terms.Contains("allstats"))
                {
                    shortStats = false;
                    var remainingTerms = Utils.GetRemainingTermsAfterRemovingOneTerm(terms, "fullstats");
                    remainingTerms = Utils.GetRemainingTermsAfterRemovingOneTerm(remainingTerms, "allstats");
                    monsterIdentifier = Utils.GetFullStringOfInputs(remainingTerms);
                }

                double targetCr = 0;
                double.TryParse(monsterIdentifier, out targetCr);

                string result = bot.WebRequests.MGGenerateMonster(shortStats, targetCr, bot.AccountSettings.MonsterGeneratorPresharedKey);
                //TODO: add monster attacks roll option?

                if (!fromChannel)
                {
                    bot.SendPrivateMessage(result, address);
                }
                else
                {
                    bot.SendMessageInChannel(result, address);
                }
            }
            else
            {
                if(fromChannel)
                    bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }

        }
    }
}
