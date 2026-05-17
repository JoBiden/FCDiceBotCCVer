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
    public class PotionJson : ChatBotCommand
    {
        public PotionJson()
        {
            Name = "potionjson";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = null;
            if(commandController.MessageCameFromChannel(channel))
                thisChannel = bot.GetChannelSettings(channel);

            bool fromChannel = commandController.MessageCameFromChannel(channel);

            if (!fromChannel || (thisChannel != null && thisChannel.AllowTableInfo))
            {
                string sendMessage = "(no potion found)";
                
                string searchTerm = bot.DiceBot.GetPotionSearchString(terms);
                
                List<Enchantment> channelPotions = bot.GetChannelPotions(channel);

                Potion retrievedPotion = bot.DiceBot.GetSpecificPotion(channelPotions, thisChannel.UseDefaultPotions, searchTerm);

                if (retrievedPotion != null)
                {
                    FChatDicebot.DiceFunctions.Enchantment enchantmentRetrieved = retrievedPotion.enchantment;

                    if (enchantmentRetrieved != null)
                    {
                        sendMessage = "Potion id [b]" + retrievedPotion.enchantment.prefix + "[/b] created by [user]" + retrievedPotion.enchantment.CreatedBy + "[/user]";

                        string potiondesc = "\n" + enchantmentRetrieved.prefix + " JSON [sub](note: CreatedBy field is auto-populated on potion save and does nothing when set in !savepotion)[/sub]:\n";
                        potiondesc += JsonConvert.SerializeObject(enchantmentRetrieved);

                        sendMessage += potiondesc;
                    }
                    else
                    {
                        sendMessage += "\n Error: No saved enchantment found on this potion!";
                    }
                }

                if (fromChannel)
                {
                    bot.SendMessageInChannel(sendMessage, channel);
                }
                else
                    bot.SendPrivateMessage(sendMessage, characterName);
            }
            else
            {
                if(fromChannel)
                    bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", channel);
            }
            
        }
    }
}
