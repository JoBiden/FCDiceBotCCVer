using System;
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
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string tableName = commandController.GetTableNameFromCommandTerms(terms);

            bool characterIsAdmin = Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, characterName);
            SavedRollTable deleteTable = Utils.GetTableFromId(bot.SavedTables, tableName);

            string sendMessage = "No tables found for " + Utils.GetCharacterUserTags(characterName) + " with id " + tableName;
            if (deleteTable != null)
            {
                if (characterName == deleteTable.OriginCharacter || characterIsAdmin)
                {
                    bot.SavedTables.Remove(deleteTable);

                    sendMessage = "[b]" + deleteTable.TableId + "[/b] deleted by " + Utils.GetCharacterUserTags(characterName);

                    Utils.WriteToFileAsData(bot.SavedTables, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedTablesFileName));
                }
                else
                {
                    sendMessage = "Only " + deleteTable.OriginCharacter + " can delete their own saved table.";
                }
            }

            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(sendMessage, characterName);
            }
            else
            {
                bot.SendMessageInChannel(sendMessage, channel);
            }
        }
    }
}
