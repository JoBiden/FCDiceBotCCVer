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
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Set your personal mark eicon";
            LongDescription = "Set or change your personal mark that will appear on those you have !mark'd. Marks must be a valid F-List eicon name. Once set, anyone you mark will display this icon in their dossier.";
            Usage = "!setmark [noparse][eicon]YourMarkName[/eicon][/noparse]";
            RelatedCommands = new string[] { "mark", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
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
