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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = null;
            if (commandController.MessageCameFromChannel(channel))
                thisChannel = bot.GetChannelSettings(channel);

            bool fromChannel = commandController.MessageCameFromChannel(channel);

            if (!fromChannel || ( thisChannel != null && thisChannel.AllowTableInfo ))
            {
                string sendMessage = "_";
                List<SavedPotion> relevantPotions = bot.SavedPotions;
                if(!terms.Contains("all"))
                {
                    relevantPotions = relevantPotions.Where(a => a.OriginCharacter == characterName).ToList();
                }

                if (relevantPotions.Count == 0)
                {

                    sendMessage = "No potions found.";
                    if(!terms.Contains("all"))
                    {
                        sendMessage = "No potions found created by " + Utils.GetCharacterUserTags(characterName) + ".";
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
                            sendMessage += "\n" + Utils.GetCharacterUserTags(table.OriginCharacter) + ": ";
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
                    bot.SendPrivateMessage(sendMessage, characterName);
                }
                else
                {
                    bot.SendMessageInChannel(sendMessage, channel);
                }
            }
            else
            {
                if(fromChannel)
                    bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", channel);
            }
            
        }
    }
}
