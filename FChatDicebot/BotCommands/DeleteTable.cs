using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string tableName = commandController.GetTableNameFromCommandTerms(terms);

            bool characterIsAdmin = Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, address.character);
            SavedRollTable deleteTable = Utils.GetTableFromId(bot.SavedTables, tableName);

            string sendMessage = "No tables found for " + TextFormat.GetCharacterUserTags(address.character) + " with id " + tableName;
            if (deleteTable != null)
            {
                if (address.character == deleteTable.OriginCharacter || characterIsAdmin)
                {
                    bot.SavedTables.Remove(deleteTable);

                    sendMessage = "[b]" + deleteTable.TableId + "[/b] deleted by " + TextFormat.GetCharacterUserTags(address.character);

                    Utils.WriteToFileAsData(bot.SavedTables, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedTablesFileName));
                }
                else
                {
                    sendMessage = "Only " + deleteTable.OriginCharacter + " can delete their own saved table.";
                }
            }

            if (!commandController.MessageCameFromChannel(address))
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
