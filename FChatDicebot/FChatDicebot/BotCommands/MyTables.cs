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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            var thisCharacterTables = bot.SavedTables.Where(a => a.OriginCharacter == address.character);

            string sendMessage = "No tables found for " + TextFormat.GetCharacterUserTags(address.character);
            bool fromChannel = commandController.MessageCameFromChannel(address);
            bool includeNsfw = fromChannel ? bot.GetChannelSettings(address).AllowNsfw : true;

            if (thisCharacterTables.Count() > 0)
            {
                string tablesList = "";
                foreach (SavedRollTable savedTable in thisCharacterTables)
                {
                    if (!(savedTable.Table != null && savedTable.Table.Nsfw) || includeNsfw)
                    {
                        if (!string.IsNullOrEmpty(tablesList))
                            tablesList += ", ";

                        tablesList += savedTable.TableId;
                    }
                }
                sendMessage = "Tables found for " + TextFormat.GetCharacterUserTags(address.character) + ": " + tablesList;
            }

            SendMessageToChannelOrUser(bot, commandController, address, sendMessage);
        }
    }
}
