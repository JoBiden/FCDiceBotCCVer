using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors;
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
            ShortDescription = "Set your personal mark eicon [sub](Legacy command, identical to !seteicon mark)[/sub]";
            LongDescription = "[sub](Legacy command, identical to !seteicon mark, which now works for every other interaction too.)[/sub]\n\nSet or change your personal mark that will appear on those you have !mark'd. Marks must be a valid F-List eicon name. Once set, anyone you mark will display this icon in their dossier.";
            Usage = "!setmark [noparse][eicon]YourMarkName[/eicon][/noparse]\n(or the newer !seteicon mark [noparse][eicon]YourMarkName[/eicon][/noparse])";
            RelatedCommands = new string[] { "seteicon", "mark", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string mark = commandController.GetEIconFromCommandTerms(rawTerms);
            Profile markProfile = MonDB.getProfile(characterName);
            if (markProfile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notFoundText(characterName), characterName);
                return;
            }
            if (mark != null && mark.Length > InteractionEiconSupport.MaxEiconLength)
            {
                bot.SendPrivateMessage("That icon name is way too long! Are you sure it's a real eicon?", characterName);
                return;
            }

            // Route through the shared !seteicon storage (writes characteristics["mark"], the
            // slot the dossier + MarkProcessor already read) so both commands stay in sync.
            InteractionEiconSupport.SetInteractionEicon(markProfile, InteractionEiconSupport.MarkVerbKey, mark);
            MonDB.setProfile(characterName, markProfile);

            string message = "Mark successfully changed! From now on, anyone who has been marked by " + markProfile.displayName + " will display " + mark + " as their mark in our records."
                + "\n[sub]Heads up: !setmark still works, but !seteicon lets you set eicons for every interaction now, including mark![/sub]";
            bot.SendPrivateMessage(message, characterName);
        }
    }
}
