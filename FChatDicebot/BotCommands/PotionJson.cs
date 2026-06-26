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
    public class PotionJson : ChatBotCommand
    {
        public PotionJson()
        {
            Name = "potionjson";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            bool fromChannel = commandController.MessageCameFromChannel(address);


            string channelName = commandController.GetChannelFromInputs(rawTerms, out string channelIdError);
            bool usedCustomChannel = false;
            MessageAddress usedAddress = address;
            if (!string.IsNullOrEmpty(channelName))
            {
                usedCustomChannel = true;
                usedAddress = new MessageAddress() { channel = channelName, character = address.character, guild = address.guild };
            }
            else
                channelName = address.GetChannelKey();

            ChannelSettings thisChannel = bot.GetChannelSettings(usedAddress);
            //todo: channel stuff error handling and such, remove channel id from terms (first term)

            string outputMessage = "";

            if (!string.IsNullOrEmpty(channelIdError) && !fromChannel)
            {
                outputMessage = channelIdError;
            }
            else if (!fromChannel || (thisChannel != null && thisChannel.AllowTableInfo))
            {
                outputMessage = "(no potion found)";

                string searchTerm = bot.DiceBot.GetPotionSearchString(terms, usedCustomChannel? channelName : null);

                List<Enchantment> channelPotions = bot.GetChannelPotions(usedAddress);

                Potion retrievedPotion = bot.DiceBot.GetSpecificPotion(channelPotions, thisChannel.UseDefaultPotions, searchTerm);

                if (retrievedPotion != null && Utils.GetNsfwError(thisChannel, retrievedPotion.enchantment, out outputMessage))
                {
                    //sendMessage set in error method
                }
                else if (retrievedPotion != null)
                {
                    FChatDicebot.DiceFunctions.Enchantment enchantmentRetrieved = retrievedPotion.enchantment;

                    if (enchantmentRetrieved != null)
                    {
                        outputMessage = "Potion id [b]" + retrievedPotion.enchantment.prefix + "[/b] created by [user]" + retrievedPotion.enchantment.CreatedBy + "[/user]";

                        string potiondesc = "\n" + enchantmentRetrieved.prefix + " JSON:\n"; // [sub](note: CreatedBy field is auto-populated on potion save and does nothing when set in !savepotion)[/sub]
                        potiondesc += JsonConvert.SerializeObject(enchantmentRetrieved, Formatting.None, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DefaultValueHandling = DefaultValueHandling.Ignore,
                            ContractResolver = new IgnoreCreatedByResolver()
                        });

                        //JsonConvert.SerializeObject(enchantmentRetrieved);

                        outputMessage += potiondesc;
                    }
                    else
                    {
                        outputMessage += "\n Error: No saved enchantment found on this potion!";
                    }
                }

                //if (fromChannel)
                //{
                //    bot.SendMessageInChannel(sendMessage, address);
                //}
                //else
                //    bot.SendPrivateMessage(sendMessage, address);
            }
            else
            {
                outputMessage = Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.";
            }

            if(!string.IsNullOrEmpty(outputMessage))
                SendMessageToChannelOrUser(bot, commandController, address, outputMessage);
        }
    }
}
