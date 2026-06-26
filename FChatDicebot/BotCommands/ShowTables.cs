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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = null;
            if (commandController.MessageCameFromChannel(address))
                thisChannel = bot.GetChannelSettings(address);

            bool fromChannel = commandController.MessageCameFromChannel(address);

            if (!fromChannel || ( thisChannel != null && thisChannel.AllowTableInfo ))
            {
                string sendMessage = "_";
                List<SavedRollTable> relevantTables = bot.SavedTables;
                if(!terms.Contains("all"))
                {
                    relevantTables = relevantTables.Where(a => a.OriginCharacter == address.character).ToList();
                }

                if (thisChannel != null && !thisChannel.AllowNsfw)
                    relevantTables = relevantTables.Where(a => a.Table != null && !a.Table.Nsfw).ToList();

                if (relevantTables.Count == 0)
                {

                    sendMessage = "No tables found.";
                    if(!terms.Contains("all"))
                    {
                        sendMessage = "No tables found created by " + TextFormat.GetCharacterUserTags(address.character) + ".";
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
                            sendMessage += "\n" + TextFormat.GetCharacterUserTags(table.OriginCharacter) + ": ";
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
