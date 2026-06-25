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
    public class GeneratePotionInfo : ChatBotCommand
    {
        public GeneratePotionInfo()
        {
            Name = "generatepotioninfo";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.CharacterInventories;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            List<Enchantment> enchantments = bot.DiceBot.PotionGenerator.GetAllEnchantments(bot, true, address);

            ChannelSettings settings = bot.GetChannelSettings(address);
            if (!settings.AllowNsfw)
                enchantments = enchantments.Where(a => !a.Nsfw).ToList();

            string wholeList = string.Join(", ", enchantments.Select(a => (a.suffix) ) );

            bot.SendPrivateMessage("[i]List of every generateable potion by suffix name: [/i]" + wholeList, address);
            bot.SendMessageInChannel("Sent list of generateable potions to " + TextFormat.GetCharacterUserTags(address.character), address);
        }
    }
}
