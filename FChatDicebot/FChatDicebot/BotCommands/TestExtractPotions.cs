using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.SavedData;
using Newtonsoft.Json;

namespace FChatDicebot.BotCommands
{
    public class TestExtractPotions : ChatBotCommand
    {
        public TestExtractPotions()
        {
            Name = "testextractpotions";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            System.Random r = new Random();
            PotionGenerator p = bot.DiceBot.PotionGenerator;
            var all = p.GetAllEnchantments(bot, false, channel);
            var common = p.GetCommonEnchantments();
            var words = p.GetTriggerWords();

            PotionGenerationInfo generationInfo = new PotionGenerationInfo();
            generationInfo.AllEnchantments = all;
            generationInfo.CommonEnchantments = common;
            generationInfo.AllTriggerWords = words;
            string json = JsonConvert.SerializeObject(generationInfo);
            Console.WriteLine("data extracted:::::: " + json);

            //Potion
        }
    }
}
