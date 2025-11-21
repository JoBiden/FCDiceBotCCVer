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
using System.ComponentModel;

namespace FChatDicebot.BotCommands
{
    public class ChateauSetmark : ChatBotCommand
    {
        public ChateauSetmark()
        {
            Name = "setmark";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string mark = commandController.GetEIconFromCommandTerms(rawTerms);
            Profile initiatorProfile = MonDB.getProfile(characterName);
            Boolean valid = true;
            if (mark.Length > 47)
            {
                string textTooLong = "That icon name is way too long! Are you sure it's a real EIcon?";
                bot.SendPrivateMessage(textTooLong, characterName);
                valid = false;
            }
            if (valid)
            {
                string message = "Mark successfully changed! From now on, anyone who has been marked by " + initiatorProfile.displayName + " will display " + mark + " as their mark in our records.";

                
                Profile markProfile = MonDB.getProfile(characterName);
                markProfile.characteristics["mark"] = mark;
                MonDB.setProfile(characterName, markProfile);
                

                bot.SendPrivateMessage(message, characterName);
            }
        }
    }
}
