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
    public class UnlockDice : ChatBotCommand
    {
        public UnlockDice()
        {
            Name = "unlockdice";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.CharacterInventories;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {

            string targetUserName = characterName;

            if(rawTerms != null && rawTerms.Length > 0)
            {
                targetUserName = Utils.GetUserNameFromFullInputs(rawTerms);
            }
            bool unlock = true;
            if(terms.Contains("false"))
            {
                unlock = false;
                targetUserName = targetUserName.Replace("false", "").Replace("   "," ").Replace("  "," ").Trim();
            }

            CharacterData data = bot.DiceBot.GetCharacterData(targetUserName, channel, false);

            string output = "";
            if(data == null)
            {
                output = "Failed: could not find character name";
            }
            else
            {
                data.DiceUnlocked = unlock;
                if (unlock)
                    output = "Gold dice [eicon]dbgoldd6-1[/eicon] [eicon]dbgoldd10-9[/eicon] [color=yellow][b]unlocked[/b][/color] for " + Utils.GetCharacterUserTags(targetUserName) + " in this channel!";
                else
                    output = "Updated dice status for " + Utils.GetCharacterUserTags(targetUserName);
                bot.BotCommandController.SaveCharacterDataToDisk();
            }

            bot.SendMessageInChannel(output, channel);
        }
    }
}
