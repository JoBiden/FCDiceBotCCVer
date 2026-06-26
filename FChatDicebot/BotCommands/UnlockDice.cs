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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {

            string targetUserName = address.character;

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
            MessageAddress targetUserAddress = new MessageAddress() { character = targetUserName, channel = address.channel, guild = address.guild };

            CharacterData data = bot.DiceBot.GetCharacterData(targetUserAddress, false);

            string output = "";
            if(data == null)
            {
                output = "Failed: could not find character name";
            }
            else
            {
                data.DiceUnlocked = unlock;
                if (unlock)
                    output = "Gold dice " + TextFormat.Emoji("dbgoldd6-1") + " " + TextFormat.Emoji("dbgoldd10-9") + " [color=yellow][b]unlocked[/b][/color] for " + TextFormat.GetCharacterUserTags(targetUserName) + " in this channel!";
                else
                    output = "Updated dice status for " + TextFormat.GetCharacterUserTags(targetUserName);
                bot.BotCommandController.SaveCharacterDataToDisk();
            }

            bot.SendMessageInChannel(output, address);
        }
    }
}
