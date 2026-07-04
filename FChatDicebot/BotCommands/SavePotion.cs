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
    public class SavePotion : ChatBotCommand
    {
        public SavePotion()
        {
            Name = "savepotion";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ///
            ChannelSettings thisChannel = null;
            bool fromChannel = commandController.MessageCameFromChannel(address);

            if (fromChannel)
                thisChannel = bot.GetChannelSettings(address);

            string channelName = commandController.GetChannelFromInputs(rawTerms, out string channelIdError);

            bool usedCustomChannel = !string.IsNullOrEmpty(channelName);

            if (!string.IsNullOrEmpty(channelIdError) && !fromChannel)
            {
                SendMessageToChannelOrUser(bot, commandController, address, channelIdError);
                return;
            }
            else if (thisChannel != null && thisChannel.AllowTableInfo)
            {
                if (fromChannel && string.IsNullOrEmpty(channelName))
                    channelName = address.GetChannelKey();
            }

            //secondary check for required ops
            if (command.ops == null)
            {
                //get the channel Id to check...
                bot.RequestChannelOpListAndQueueFurtherRequest(command, channelName);
                return;
            }
            else if (command.ops != null && !command.ops.Contains(command.characterName) && !Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, command.characterName))
            {
                SendMessageToChannelOrUser(bot, commandController, address, "Error: You are not a channel OP of the requested channel!");
                return;
            }

            ///

            string saveJson = Utils.GetFullStringOfInputs(rawTerms);
            if (usedCustomChannel)//new channel used
                saveJson = Utils.GetFullStringOfInputsAfterTermX(rawTerms, 1);
            string sendMessage = "save potion error";

            try
            {
                FChatDicebot.DiceFunctions.Enchantment newPotion = JsonConvert.DeserializeObject<FChatDicebot.DiceFunctions.Enchantment>(saveJson);

                MessageAddress tempAddress = address;
                if (!Utils.IsDiscordMessage(command) && usedCustomChannel)
                    tempAddress = new MessageAddress() { channel = channelName, character = address.character, guild = address.guild };

                ChannelSettings settings = bot.GetChannelSettings(tempAddress);
                if(newPotion == null)
                {
                    SendMessageToChannelOrUser(bot, commandController, address, "Failed: could not parse potion JSON.");
                    return;
                }
                else if (settings != null && Utils.GetNsfwError(settings, newPotion, out sendMessage))
                {
                    SendMessageToChannelOrUser(bot, commandController, address, sendMessage);
                    return;
                }

                List<Enchantment> allEnchantments = bot.DiceBot.PotionGenerator.GetAllEnchantments(bot, true, tempAddress);
                var thisCharacterEnchantments = allEnchantments.Where(a => a.CreatedBy == address.character);
                string lowerPre = newPotion.prefix.ToLower();
                string lowerSuf = newPotion.suffix.ToLower();
                var thisCharacterTotalEnchantments = bot.GetCharacterTotalEnchantments(address.character);

                FChatDicebot.DiceFunctions.Enchantment existingEnchantment = allEnchantments.FirstOrDefault(a => a.suffix.ToLower() == lowerSuf || a.prefix.ToLower() == lowerPre);

                if (thisCharacterTotalEnchantments.Count() >= BotMain.MaximumSavedPotionsPerCharacter && existingEnchantment == null)
                {
                    sendMessage = "Failed: A character can only save up to " + BotMain.MaximumSavedPotionsPerCharacter + " potions at one time. Delete or overwrite old potions.";
                }
                else if (existingEnchantment != null && existingEnchantment.CreatedBy != address.character)
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
                    newPotion.CreatedBy = address.character;
                    newPotion.Flag = EnchantmentFlag.UserGenerated;

                    newPotion.explanation = newPotion.explanation == null ? null : Utils.LimitStringToNCharacters(newPotion.explanation, BotMain.MaximumCharactersPotionDescription);
                    newPotion.prefix = newPotion.prefix == null ? null : Utils.LimitStringToNCharacters(newPotion.prefix, BotMain.MaximumCharactersPotionName);
                    newPotion.suffix = newPotion.suffix == null ? null : Utils.LimitStringToNCharacters(newPotion.suffix, BotMain.MaximumCharactersPotionName);
                    newPotion.OverrideEicon = newPotion.OverrideEicon == null ? null : Utils.LimitStringToNCharacters(newPotion.OverrideEicon, BotMain.MaximumCharactersPotionName);

                    if (existingEnchantment != null)
                    {
                        existingEnchantment.Copy(newPotion);
                    }
                    else
                    {
                        SavedPotion newSavedPotion = new SavedPotion()
                        {
                            Channel = tempAddress.GetChannelKey(),
                            DefaultPotion = false,
                            Enchantment = newPotion,
                            OriginCharacter = address.character
                        };

                        bot.SavedPotions.Add(newSavedPotion);
                    }

                    commandController.SavePotionDataToDisk();

                    sendMessage = "[b]Success[/b]. Potion saved by " + TextFormat.GetCharacterUserTags(address.character) + ". This can now be generated using !generatepotion " + newPotion.suffix.ToLower();
                }
            }
            catch (Exception)
            {
                sendMessage = "Failed to parse potion enchantment entry data. Make sure the Json is correctly formatted.";
            }

            SendMessageToChannelOrUser(bot, commandController, address, sendMessage);
        }
    }
}
