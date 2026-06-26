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
    public class UnlockDiceAll : ChatBotCommand
    {
        public UnlockDiceAll()
        {
            Name = "unlockdiceall";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = false;
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
            //MessageAddress targetUserAddress = new MessageAddress() { character = targetUserName, channel = address.channel, guild = address.guild };

            //CharacterData data = bot.DiceBot.GetCharacterData(targetUserAddress, false);

            string output = "";
            //if(data == null)
            //{
            //    output = "Failed: could not find character name";
            //}
            //else
            //{
            if (bot.AccountSettings.FullCosmeticsUnlockCharacters == null)
                bot.AccountSettings.FullCosmeticsUnlockCharacters = new List<string>();

            //bot.AccountSettings.FullCosmeticsUnlockCharacters.Add(targetUserName);
            //unlock = bot.AccountSettings.FullCosmeticsUnlockCharacters.Contains(targetUserName);
            if (unlock)
            {
                output = "Gold dice " + TextFormat.Emoji("dbgoldd6-1") + " " + TextFormat.Emoji("dbgoldd10-9") + " [color=yellow][b]unlocked[/b][/color] for " + TextFormat.GetCharacterUserTags(targetUserName) + " in all channels!";
                bot.AccountSettings.FullCosmeticsUnlockCharacters.Add(targetUserName);
            }
            else
            {
                output = "Updated dice status in all channels for " + TextFormat.GetCharacterUserTags(targetUserName);
                bot.AccountSettings.FullCosmeticsUnlockCharacters.Remove(targetUserName);
            }
            bot.BotCommandController.SaveAccountSettingsToDisk();
            //}

            bot.SendMessageInChannel(output, address);
        }
    }
}
