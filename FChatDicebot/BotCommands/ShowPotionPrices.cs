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
    public class ShowPotionPrices : ChatBotCommand
    {
        public ShowPotionPrices()
        {
            Name = "showpotionprices";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = null;
            if (commandController.MessageCameFromChannel(address))
                thisChannel = bot.GetChannelSettings(address);

            bool fromChannel = commandController.MessageCameFromChannel(address);

            if (thisChannel == null)
            {
                bot.SendPrivateMessage("Error: failed to get channel settings", address);
            }
            else if (thisChannel != null && !thisChannel.AllowTableInfo)
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
            else
            {
                string potionName = thisChannel.PotionCommandsAlias;
                bool secret = false;

                if (terms.Contains("s") || terms.Contains("secret"))
                    secret = true;

                string sendMessage = "_";
                List<SavedPotion> relevantPotions = bot.SavedPotions.Where(a => a.Enchantment != null).ToList();

                if(thisChannel != null && thisChannel.UseDefaultPotions)
                    relevantPotions = relevantPotions.Where(a => a.DefaultPotion || a.Channel == address.channel).ToList();
                else
                    relevantPotions = relevantPotions.Where(a => !a.DefaultPotion && a.Channel == address.channel).ToList();

                if (!thisChannel.AllowNsfw)
                    relevantPotions = relevantPotions.Where(a => a.Enchantment != null && !a.Enchantment.Nsfw).ToList();

                if (relevantPotions.Count == 0)
                {
                    sendMessage = "No " + potionName + "s found.";
                }
                else
                {
                    sendMessage = TextFormat.CapitalizeFirst(potionName) + "s found:\n";

                    relevantPotions = relevantPotions.OrderBy(a => a.Enchantment.prefix).ToList();

                    string pricesMessage = "";
                    //string currentCharacter = "";
                    //string currentChannel = "";
                    foreach (SavedPotion potion in relevantPotions)
                    {
                        //PotionGenerator p = new PotionGenerator(bot.DiceBot.random);
                        //p.GeneratePotionWithSpecificEffect(relevantPotions.Select(a => a.Enchantment).ToList(),  potion.Enchantment.prefix)
                        Enchantment ench = potion.Enchantment;
                        //Potion p =  potion.Enchantment.
                        //potion.getname
                        string thisPotionString = "";//
                        //if (ench.HidePotionDetails)
                            thisPotionString = ench.prefix + " (" + ench.Value + " " + BotMain.CurrencyPlaceholder + "s): " + ench.explanation + "\n";

                        if (!string.IsNullOrEmpty(pricesMessage))
                        {
                            pricesMessage += ", ";
                        }

                        pricesMessage += thisPotionString;
                    }

                    sendMessage += pricesMessage;
                }


                if (!fromChannel || secret)
                {
                    bot.SendPrivateMessage(sendMessage, address);
                }
                else
                {
                    bot.SendMessageInChannel(sendMessage, address);
                }
            }

        }
    }
}
