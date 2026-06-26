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
    public class ShowPotions : ChatBotCommand
    {
        public ShowPotions()
        {
            Name = "showpotions";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = null;
            if (commandController.MessageCameFromChannel(address))
                thisChannel = bot.GetChannelSettings(address);

            bool fromChannel = commandController.MessageCameFromChannel(address);

            if (!fromChannel || ( thisChannel != null && thisChannel.AllowTableInfo ))
            {
                string sendMessage = "_";
                List<SavedPotion> relevantPotions = bot.SavedPotions;
                if(!terms.Contains("all"))
                {
                    relevantPotions = relevantPotions.Where(a => a.OriginCharacter == address.character).ToList();
                }

                if (thisChannel != null && !thisChannel.AllowNsfw)
                    relevantPotions = relevantPotions.Where(a => a.Enchantment != null && !a.Enchantment.Nsfw).ToList();

                if (relevantPotions.Count == 0)
                {

                    sendMessage = "No potions found.";
                    if(!terms.Contains("all"))
                    {
                        sendMessage = "No potions found created by " + TextFormat.GetCharacterUserTags(address.character) + ".";
                    }
                }
                else
                {
                    sendMessage = "Potions found:";

                    relevantPotions = relevantPotions.OrderBy(a => a.OriginCharacter).ToList();

                    string tablesMessage = "";
                    string currentCharacter = "";
                    string currentChannel = "";
                    foreach (SavedPotion table in relevantPotions)
                    {
                        if(currentCharacter != table.OriginCharacter)
                        {
                            currentCharacter = table.OriginCharacter;

                            if(!string.IsNullOrEmpty(tablesMessage))
                            {
                                sendMessage += tablesMessage;
                                tablesMessage = "";
                            }
                            sendMessage += "\n" + TextFormat.GetCharacterUserTags(table.OriginCharacter) + ": ";
                        }
                        if (currentChannel != table.Channel)
                        {
                            currentChannel = table.Channel;

                            if (!string.IsNullOrEmpty(tablesMessage))
                            {
                                sendMessage += tablesMessage;
                                tablesMessage = "";
                            }
                            sendMessage += "(channel: " + (table.Channel) + ") ";
                        }
                        if(!string.IsNullOrEmpty(tablesMessage))
                        {
                            tablesMessage += ", ";
                        }

                        tablesMessage += table.Enchantment == null? ("null") : table.Enchantment.prefix;
                    }

                    sendMessage += tablesMessage;
                }


                if (!fromChannel)
                {
                    bot.SendPrivateMessage(sendMessage, address);
                }
                else
                {
                    bot.SendMessageInChannel(sendMessage, address);
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
