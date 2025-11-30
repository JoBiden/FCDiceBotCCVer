using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauSettitle : ChatBotCommand
    {
        public ChateauSettitle()
        {
            Name = "settitle";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Set a title to display on your dossier";
            LongDescription = "Decide which title should display in which title slot on your dossier. ";
            Usage = "!settitle {slot} \"Full Title Text In Quotes\"";
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
            Profile profile = MonDB.getProfile(characterName);
            int slotNumber = Utils.GetNumberFromInputs(terms);
            if (profile == null)
            {
                bot.SendPrivateMessage(ChateauInteractionHandler.notRegisteredText(), characterName);
                return;
            }

            if (profile.titles == null || profile.titles.Count == 0)
            {
                bot.SendPrivateMessage("You don't have any titles yet! Earn them through your interactions in the Chateau. Maybe someone will see fit to !entitle you.", characterName);
                return;
            }

            // Parse the command
            // Expected format: !settitle {slot} {"title text"} OR !settitle {slot} to clear
            if (terms.Length < 1)
            {
                bot.SendPrivateMessage("You must specify a slot number (1-9). Usage: !settitle {slot} {\"title text in quotes\"} or !settitle {slot} to clear that slot.", characterName);
                return;
            }

            // Validate slot number is 1-9
            if (slotNumber < 1 || slotNumber > 9)
            {
                bot.SendPrivateMessage("Slot number must be between 1 and 9.", characterName);
                return;
            }

            // Extract title text if provided
            
            string titleText = commandController.GetQuotedTextFromCommandTerms(rawTerms);

            // Initialize displayedTitleSlots if null or on old system
            if (profile.displayedTitleSlots == null || profile.displayedTitleSlots.Length != 9)
            {
                profile.displayedTitleSlots = new int[9] { -1, -1, -1, -1, -1, -1, -1, -1, -1 };
            }

            // If no title text provided, clear that slot
            if (string.IsNullOrEmpty(titleText))
            {
                profile.displayedTitleSlots[slotNumber - 1] = -1; // Set to empty
                MonDB.setProfile(characterName, profile);
                bot.SendPrivateMessage($"Cleared slot {slotNumber}. This title will no longer appear in your dossier.", characterName);
                return;
            }

            // Find the title in the user's titles list
            int titleIndex = -1;
            for (int i = 0; i < profile.titles.Count; i++)
            {
                if (profile.titles[i].titleText.Equals(titleText, StringComparison.OrdinalIgnoreCase) ||
                    profile.titles[i].GetFormattedTitle().Equals(titleText, StringComparison.OrdinalIgnoreCase))
                {
                    titleIndex = i;
                    break;
                }
            }

            if (titleIndex == -1)
            {
                bot.SendPrivateMessage($"You don't have a title matching \"{titleText}\". Use !titles to see your available titles.", characterName);
                return;
            }



            // Update the slot
            profile.displayedTitleSlots[slotNumber - 1] = titleIndex;


            MonDB.setProfile(characterName, profile);
            string formattedTitle = profile.titles[titleIndex].GetFormattedTitle();
            bot.SendPrivateMessage($"Set slot {slotNumber} to display \"{formattedTitle}\" in your dossier! Use !dossier to see how it looks.", characterName);
        }
    }
}
