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
    public class SaveTableSimple : ChatBotCommand
    {
        public SaveTableSimple()
        {
            Name = "savetablesimple";
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
                SavedRollTable newSavedTable = new SavedRollTable();

                string allTerms = Utils.GetFullStringOfInputs(rawTerms);

                string[] titleSplit = allTerms.Split(':');

                //command format: !savetablesimple Table Name: item one, item two, item three
                if(titleSplit == null || titleSplit.Length != 2)
                {
                    sendMessage = "Failed: Input must contain exactly one colon to separate.";
                }
                else
                {
                    string[] entriesSplit = titleSplit[1].Split(',');

                    if (entriesSplit.Length <= 1)
                    {
                        sendMessage = "Failed: The minimum amount of entries is 2.";
                    }
                    else if (entriesSplit.Length > BotMain.MaximumSavedEntriesPerTable)
                    {
                        sendMessage = "Failed: The maximum amount of entries is " + BotMain.MaximumSavedEntriesPerTable + ".";
                    }
                    else
                    {
                        List<TableEntry> entries = new List<TableEntry>();

                        for(int i = 0; i < entriesSplit.Length; i++)
                        {
                            string desc = Utils.LimitStringToNCharacters(entriesSplit[i], BotMain.MaximumCharactersTableEntryDescription);
                            entries.Add(new TableEntry() { Roll = i + 1, Description = desc, Name = "", Range = 0 });
                        }

                        newSavedTable.Table = new DiceFunctions.RollTable();
                        newSavedTable.Table.Name = Utils.LimitStringToNCharacters(titleSplit[0], BotMain.MaximumCharactersTableName);
                        string newTableId = newSavedTable.Table.Name.Replace(" ", "").ToLower();
                        newSavedTable.Table.Description = "";
                        newSavedTable.Table.RollBonus = 0;
                        newSavedTable.Table.DieSides = entriesSplit.Length;
                        newSavedTable.DefaultTable = false;
                        newSavedTable.OriginCharacter = characterName;

                        newSavedTable.Table.TableEntries = entries;

                        SavedRollTable existingTable = Utils.GetTableFromId(bot.SavedTables, newTableId);

                        var thisCharacterTables = bot.SavedTables.Where(a => a.OriginCharacter == characterName);

                        if (thisCharacterTables.Count() >= BotMain.MaximumSavedTablesPerCharacter && existingTable == null)
                        {
                            sendMessage = "Failed: A character can only save up to 3 tables at one time. Delete or overwrite old tables.";
                        }
                        else if (existingTable != null && existingTable.OriginCharacter != characterName)
                        {
                            sendMessage = "Failed: This table name is taken by a different character.";
                        }
                        else if (newTableId.Length < 2)
                        {
                            sendMessage = "Failed: Table name too short.";
                        }
                        else if (newSavedTable.Table.TableEntries == null)
                        {
                            sendMessage = "Failed: No table entries found for this table.";
                        }
                        else if (newSavedTable.Table.TableEntries.Count > BotMain.MaximumSavedEntriesPerTable)
                        {
                            sendMessage = "Failed: Table contains more than " + BotMain.MaximumSavedEntriesPerTable + " entries.";
                        }
                        else
                        {
                            newSavedTable.TableId = newTableId;

                            if (existingTable != null)
                            {
                                existingTable.Copy(newSavedTable);
                            }
                            else
                            {
                                bot.SavedTables.Add(newSavedTable);
                            }

                            Utils.WriteToFileAsData(bot.SavedTables, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedTablesFileName));

                            sendMessage = "[b]Success[/b]. Table saved by [user]" + characterName + "[/user]. Roll on this table using !rolltable " + newTableId;
                        }
                    }
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
