using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class RollTable : ChatBotCommand
    {
        public RollTable()
        {
            Name = "rolltable";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string sendMessage = ParseCommandsAndRoll(bot, commandController, terms, characterName, channel);
            bot.SendMessageInChannel(sendMessage, channel);
        }

        public static string ParseCommandsAndRoll(BotMain bot, BotCommandController commandController, string[] terms, string characterName, string channel, int callDepth = DiceBot.MaximumSecondaryTableRolls)
        {
            string sendMessage = "";

            if(string.IsNullOrEmpty(characterName) || string.IsNullOrEmpty(channel))
            {
                return "Failed: Missing data on rolltable call";
            }

            ChannelSettings channelSettings = bot.GetChannelSettings(channel);

            if(!channelSettings.AllowTableRolls)
            {
                return "Failed: !RollTable is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.";
            }

            bool includeLabel = true;
            bool secondaryRolls = true;
            int dataX = 0;
            int dataY = 0;
            int dataZ = 0;

            if (terms != null && terms.Length >= 1)
            {
                if (terms != null && terms.Length >= 1 && terms.Contains("nolabel"))
                    includeLabel = false;
                if (terms != null && terms.Length >= 1 && terms.Contains("nosecondary"))
                    secondaryRolls = false;
                string foundTerm = terms.FirstOrDefault(a => a.StartsWith("x"));
                if (foundTerm != null)
                {
                    int.TryParse(foundTerm.Replace(" ", "").Replace("x=", ""), out dataX);
                }
                foundTerm = terms.FirstOrDefault(a => a.StartsWith("y"));
                if (foundTerm != null)
                {
                    int.TryParse(foundTerm.Replace(" ", "").Replace("y=", ""), out dataY);
                }
                foundTerm = terms.FirstOrDefault(a => a.StartsWith("z"));
                if (foundTerm != null)
                {
                    int.TryParse(foundTerm.Replace(" ", "").Replace("z=", ""), out dataZ);
                }
            }

            int rollModifier = commandController.GetRollModifierFromCommandTerms(terms);
            string tableName = commandController.GetTableNameFromCommandTerms(terms);

            SavedRollTable savedTable = Utils.GetTableFromId(bot.SavedTables, tableName);

            if (savedTable == null)
            {
                sendMessage = "Failed: The table \'" + tableName + "\' was not found.";
            }
            else if (savedTable.DefaultTable || channelSettings.AllowCustomTableRolls)
            {
                sendMessage = bot.DiceBot.GetRollTableResult(bot.SavedTables, tableName, characterName, channel, rollModifier, includeLabel, secondaryRolls, dataX, dataY, dataZ, callDepth);
            }
            else
            {
                sendMessage = "Failed: Only default tables are allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.";
            }
            return sendMessage;
        }
    }

    public class RollVariableData
    {
        public string Name;
        public int Value;
    }
}
