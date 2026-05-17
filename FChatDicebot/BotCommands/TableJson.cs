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
    public class TableJson : ChatBotCommand
    {
        public TableJson()
        {
            Name = "tablejson";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = false;
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
                string tableName = commandController.GetTableNameFromCommandTerms(terms);

                SavedRollTable infoTable = Utils.GetTableFromId(bot.SavedTables, tableName);

                string sendMessage = "Table \'" + tableName + "\' not found.";
                if (infoTable != null)
                {
                    sendMessage = "Table id [b]" + infoTable.TableId + "[/b] created by [user]" + infoTable.OriginCharacter + "[/user]";

                    if (infoTable.Table != null)
                    {
                        string tabledesc = "\n" + infoTable.Table.Name + " JSON:\n";
                        tabledesc += JsonConvert.SerializeObject(infoTable.Table);

                        sendMessage += tabledesc;
                    }
                    else
                    {
                        sendMessage += "\n (Rolltable contents not found)";
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
