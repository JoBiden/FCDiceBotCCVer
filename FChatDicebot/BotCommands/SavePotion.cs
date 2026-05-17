using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

//NOTE: currently unfinished and does not function
namespace FChatDicebot.BotCommands
{
    public class SavePotion : ChatBotCommand
    {
        public SavePotion()
        {
            Name = "savepotion";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string saveJson = Utils.GetFullStringOfInputs(rawTerms);
            string sendMessage = "save potion error";

            try
            {
                FChatDicebot.DiceFunctions.Enchantment newPotion = JsonConvert.DeserializeObject<FChatDicebot.DiceFunctions.Enchantment>(saveJson);

                List<Enchantment> allEnchantments = bot.DiceBot.PotionGenerator.GetAllEnchantments(bot, true, channel);
                var thisCharacterEnchantments = allEnchantments.Where(a => a.CreatedBy == characterName);
                string lowerPre = newPotion.prefix.ToLower();
                string lowerSuf = newPotion.suffix.ToLower();
                var thisCharacterTotalEnchantments = bot.GetCharacterTotalEnchantments(characterName);

                FChatDicebot.DiceFunctions.Enchantment existingEnchantment = allEnchantments.FirstOrDefault(a => a.suffix.ToLower() == lowerSuf || a.prefix.ToLower() == lowerPre);

                if (thisCharacterTotalEnchantments.Count() >= BotMain.MaximumSavedPotionsPerCharacter && existingEnchantment == null)
                {
                    sendMessage = "Failed: A character can only save up to " + BotMain.MaximumSavedPotionsPerCharacter + " potions at one time. Delete or overwrite old potions.";
                }
                else if (existingEnchantment != null && existingEnchantment.CreatedBy != characterName)
                {
                    sendMessage = "Failed: Potion name is already used by another character.";
                }
                else if (newPotion.prefix == null || newPotion.suffix == null || newPotion.prefix.Length < 2 || newPotion.suffix.Length < 2)
                {
                    sendMessage = "Failed: Potion must have a prefix and a suffix form of name, and they must be at least 2 characters long.";
                }
                else if (newPotion.explanation == null || newPotion.explanation.Length < 3)
                {
                    sendMessage = "Failed: A potion must have an explanation longer than 2 characters.";
                }
                else if (newPotion.Rarity > 2 || newPotion.Rarity <= 0)
                {
                    sendMessage = "Failed: A potion's rarity should vary between 2 (common) and .01 (super rare). Default to 1 if you don't care.";
                }
                else
                {
                    newPotion.CreatedBy = characterName;
                    newPotion.Flag = EnchantmentFlag.UserGenerated;

                    newPotion.explanation = newPotion.explanation == null ? null : Utils.LimitStringToNCharacters(newPotion.explanation, BotMain.MaximumCharactersPotionDescription);
                    newPotion.prefix = newPotion.prefix == null ? null : Utils.LimitStringToNCharacters(newPotion.prefix, BotMain.MaximumCharactersPotionName);
                    newPotion.suffix = newPotion.suffix == null ? null : Utils.LimitStringToNCharacters(newPotion.suffix, BotMain.MaximumCharactersPotionName);
                    newPotion.OverrideEicon = newPotion.OverrideEicon == null ? null : Utils.LimitStringToNCharacters(newPotion.OverrideEicon, BotMain.MaximumCharactersPotionName);

                    newPotion.HidePotionDetails = newPotion.HidePotionDetails;
                    if (existingEnchantment != null)
                    {
                        existingEnchantment.Copy(newPotion);
                    }
                    else
                    {
                        SavedPotion newSavedPotion = new SavedPotion()
                        {
                            Channel = channel,
                            DefaultPotion = false,
                            Enchantment = newPotion,
                            OriginCharacter = characterName
                        };

                        bot.SavedPotions.Add(newSavedPotion);
                    }

                    commandController.SavePotionDataToDisk();
                    
                    sendMessage = "[b]Success[/b]. Potion saved by " + Utils.GetCharacterUserTags(characterName) + ". This can now be generated using !generatepotion " + newPotion.suffix.ToLower();
                }
            }
            catch (Exception)
            {
                sendMessage = "Failed to parse potion enchantment entry data. Make sure the Json is correctly formatted.";
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
