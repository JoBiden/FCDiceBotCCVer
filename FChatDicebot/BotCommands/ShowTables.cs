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
    public class ShowTables : ChatBotCommand
    {
        public ShowTables()
        {
            Name = "showtables";
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
                List<SavedRollTable> relevantTables = bot.SavedTables;
                if(!terms.Contains("all"))
                {
                    relevantTables = relevantTables.Where(a => a.OriginCharacter == characterName).ToList();
                }

                if(relevantTables.Count == 0)
                {

                    sendMessage = "No tables found.";
                    if(!terms.Contains("all"))
                    {
                        sendMessage = "No tables found created by " + Utils.GetCharacterUserTags(characterName) + ".";
                    }
                }
                else
                {
                    sendMessage = "Tables found:";

                    relevantTables = relevantTables.OrderBy(a => a.OriginCharacter).ToList();

                    string tablesMessage = "";
                    string currentCharacter = "";
                    foreach(SavedRollTable table in relevantTables)
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
                        if(!string.IsNullOrEmpty(tablesMessage))
                        {
                            tablesMessage += ", ";
                        }

                        tablesMessage += table.TableId;
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
