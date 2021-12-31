﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;

namespace FChatDicebot.BotCommands
{
    public class DeleteTable : ChatBotCommand
    {
        public DeleteTable()
        {
            Name = "deletetable";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string tableName = commandController.GetTableNameFromCommandTerms(terms);

            SavedRollTable deleteTable = Utils.GetTableFromId(bot.SavedTables, tableName);

            string sendMessage = "No tables found for [user]" + characterName + "[/user]";
            if (deleteTable != null)
            {
                if (characterName == deleteTable.OriginCharacter)
                {
                    bot.SavedTables.Remove(deleteTable);

                    sendMessage = "[b]" + deleteTable.TableId + "[/b] deleted by [user]" + characterName + "[/user]";

                    Utils.WriteToFileAsData(bot.SavedTables, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedTablesFileName));
                }
                else
                {
                    sendMessage = "Only " + deleteTable.OriginCharacter + " can delete their own saved table.";
                }
            }

            bot.SendMessageInChannel(sendMessage, channel);
        }
    }
}
