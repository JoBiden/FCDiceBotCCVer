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
    public class MyTables : ChatBotCommand
    {
        public MyTables()
        {
            Name = "mytables";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            var thisCharacterTables = bot.SavedTables.Where(a => a.OriginCharacter == characterName);

            string sendMessage = "No tables found for " + Utils.GetCharacterUserTags(characterName);
            if (thisCharacterTables.Count() > 0)
            {
                string tablesList = "";
                foreach (SavedRollTable savedTable in thisCharacterTables)
                {
                    if (!string.IsNullOrEmpty(tablesList))
                        tablesList += ", ";

                    tablesList += savedTable.TableId;
                }
                sendMessage = "Tables found for " + Utils.GetCharacterUserTags(characterName) + ": " + tablesList;
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
