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
    public class SaveTable : ChatBotCommand
    {
        public SaveTable()
        {
            Name = "savetable";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string saveJson = Utils.GetFullStringOfInputs(rawTerms);
            string sendMessage = "";

            try
            {
                FChatDicebot.DiceFunctions.RollTable newTable = JsonConvert.DeserializeObject<FChatDicebot.DiceFunctions.RollTable>(saveJson);
                newTable.Name = Utils.LimitStringToNCharacters(newTable.Name, BotMain.MaximumCharactersTableName);
                string newTableId = newTable.Name.Replace(" ", "").ToLower();

                newTable.Description = Utils.LimitStringToNCharacters(newTable.Description, BotMain.MaximumCharactersTableDescription);

                var thisCharacterTables = bot.SavedTables.Where(a => a.OriginCharacter == characterName);

                SavedRollTable existingTable = Utils.GetTableFromId(bot.SavedTables, newTableId);

                if (thisCharacterTables.Count() >= BotMain.MaximumSavedTablesPerCharacter && existingTable == null)
                {
                    sendMessage = "Failed: A character can only save up to " + BotMain.MaximumSavedTablesPerCharacter + " tables at one time. Delete or overwrite old tables.";
                }
                else if (existingTable != null && existingTable.OriginCharacter != characterName)
                {
                    sendMessage = "Failed: This table name is taken by a different character.";
                }
                else if (newTableId.Length < 2)
                {
                    sendMessage = "Failed: Table name too short.";
                }
                else if (newTable.TableEntries == null)
                {
                    sendMessage = "Failed: No table entries found for this table.";
                }
                else if (newTable.TableEntries.Count > BotMain.MaximumSavedEntriesPerTable)
                {
                    sendMessage = "Failed: Table contains more than " + BotMain.MaximumSavedEntriesPerTable + " entries.";
                }
                else
                {
                    foreach (FChatDicebot.DiceFunctions.TableEntry t in newTable.TableEntries)
                    {
                        t.Name = Utils.LimitStringToNCharacters(t.Name, BotMain.MaximumCharactersTableName);
                        t.Description = Utils.LimitStringToNCharacters(t.Description, BotMain.MaximumCharactersTableEntryDescription);

                        if (t.Triggers != null && t.Triggers.Count > BotMain.MaximumRollTriggersPerEntry)
                        {
                            t.Triggers = t.Triggers.Take(BotMain.MaximumRollTriggersPerEntry).ToList();
                        }
                    }

                    SavedRollTable newSavedRollTable = new SavedRollTable()
                    {
                        Table = newTable,
                        OriginCharacter = characterName,
                        DefaultTable = false,
                        TableId = newTableId
                    };

                    if (existingTable != null)
                    {
                        existingTable.Copy(newSavedRollTable);
                    }
                    else
                    {
                        bot.SavedTables.Add(newSavedRollTable);
                    }

                    Utils.WriteToFileAsData(bot.SavedTables, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedTablesFileName));

                    sendMessage = "[b]Success[/b]. Table saved by [user]" + characterName + "[/user]. Roll on this table using !rolltable " + newTableId;
                }
            }
            catch (Exception)
            {
                sendMessage = "Failed to parse table entry data. Make sure the Json is correctly formatted.";
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
