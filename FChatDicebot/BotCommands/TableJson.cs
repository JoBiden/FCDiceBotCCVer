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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = null;
            bool fromChannel = commandController.MessageCameFromChannel(address);

            if (fromChannel)
                thisChannel = bot.GetChannelSettings(address);

            if (!fromChannel || (thisChannel != null && thisChannel.AllowTableInfo))
            {
                string tableName = commandController.GetTableNameFromCommandTerms(terms);

                SavedRollTable infoTable = Utils.GetTableFromId(bot.SavedTables, tableName);

                string sendMessage = "Table \'" + tableName + "\' not found.";
                if (infoTable != null && Utils.GetNsfwError(thisChannel, infoTable.Table, out sendMessage))
                {
                    //sendMessage set in error method
                }
                if (infoTable != null)
                {
                    sendMessage = "Table id [b]" + infoTable.TableId + "[/b] created by " + TextFormat.GetCharacterUserTags(infoTable.OriginCharacter);

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
                    bot.SendMessageInChannel(sendMessage, address);
                }
                else
                    bot.SendPrivateMessage(sendMessage, address);
            }
            else
            {
                if(fromChannel)
                    bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }

        }
    }
}
