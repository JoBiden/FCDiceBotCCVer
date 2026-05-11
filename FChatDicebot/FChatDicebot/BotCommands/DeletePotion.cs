using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;

namespace FChatDicebot.BotCommands
{
    public class DeletePotion : ChatBotCommand
    {
        public DeletePotion()
        {
            Name = "deletepotion";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string potionName = Utils.GetFullStringOfInputs(rawTerms).Trim();

            bool characterIsAdmin = Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, characterName);
            var totalPotions = bot.SavedPotions.Where(a => a.OriginCharacter == characterName);
            SavedPotion deletePotion = totalPotions.FirstOrDefault(a => a.Enchantment.prefix.ToLower() == potionName.ToLower() || a.Enchantment.suffix.ToLower() == potionName.ToLower());

            string sendMessage = "No potions found for " + Utils.GetCharacterUserTags(characterName) + " with prefix " + potionName;
            if (deletePotion != null)
            {
                if (characterName == deletePotion.OriginCharacter || characterIsAdmin)
                {
                    bot.SavedPotions.Remove(deletePotion);

                    sendMessage = "[b]" + deletePotion.Enchantment.prefix + "[/b] deleted by " + Utils.GetCharacterUserTags(characterName);

                    commandController.SavePotionDataToDisk();
                }
                else
                {
                    sendMessage = "Only " + deletePotion.OriginCharacter + " can delete their own saved potion.";
                }
            }

            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(sendMessage, characterName);
            }
            else
            {
                bot.SendMessageInChannel(sendMessage, channel);
            }
        }
    }
}
