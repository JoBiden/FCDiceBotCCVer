using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

//NOTE: Admin, used to manually remove dead channels, automatically remove redeemed coupons
namespace FChatDicebot.BotCommands
{
    public class RemoveOldData : ChatBotCommand
    {
        public RemoveOldData()
        {
            Name = "removeolddata";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            var chipCodes = bot.ChipsCoupons;
            var cleanedCodes = bot.ChipsCoupons.Where(a => !a.Redeemed).ToList();
            var removedCodes = bot.ChipsCoupons.Where(a => a.Redeemed);

            bot.ChipsCoupons = cleanedCodes;
            commandController.SaveCouponsToDisk();

            List<string> removeChannels = new List<string>() { "ADH-342ce0931bbbcb80da33", "ADH-6c3dcb1e6361fc4ae3f2"
            , "ADH-79c19ed4d6337d32abb7", "ADH-5cdcab4d6c7b7811599e", "ADH-ced6f513d78cc1a8aebc", "ADH-406fd764853c9dfa127b"
            , "ADH-c00102ed990f0158cfea", "ADH-323d6ad06a3457b21c4f" };
            var cleanChannels = bot.SavedChannelSettings.Where(a => !removeChannels.Contains(a.Name)).ToList();
            var removedChannels = bot.SavedChannelSettings.Where(a => removeChannels.Contains(a.Name)).ToList();
            bot.SavedChannelSettings = cleanChannels;
            commandController.SaveChannelSettingsToDisk();

            RemovedData removedStuff = new RemovedData() { coupons = removedCodes.ToList(), channels = removedChannels };

            var dumpFileName = "dataremoved_" + Utils.RandomString(bot.DiceBot.random, 8) + ".txt";
            Utils.WriteToFileAsData(removedStuff, Utils.GetTotalFileName(BotMain.FileFolder, dumpFileName));
            
            bot.SendMessageInChannel("Success: Removed redeemed coupons", channel);
        }

        public class RemovedData
        {
            public List<ChipsCoupon> coupons;
            public List<ChannelSettings> channels;
        }
    }
}
