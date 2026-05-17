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
    public class ChateauTitles : ChatBotCommand
    {
        public ChateauTitles()
        {
            Name = "titles";
            Aliases = new string[] { };
            Category = "Personalization";
            ShortDescription = "View all earned and granted titles";
            LongDescription = "View all titles someone has earned or been granted, including system titles (earned through achievements) and custom titles (granted by other characters). Use !settitle to choose which titles appear in your dossier.";
            Usage = "!titles\nor\n!titles [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "settitle", "systemtitles", "entitle", "dossier" };
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
            string targetUser;
            bool viewingOwnTitles = false;

            if (terms.Length < 1)
            {
                targetUser = characterName;
                viewingOwnTitles = true;
            }
            else
            {
                targetUser = commandController.GetUserNameFromCommandTerms(rawTerms);
            }

            Profile profile = MonDB.getProfile(targetUser);
            if (profile == null)
            {
                string errorMessage = viewingOwnTitles
                    ? ChateauInteractionHandler.notRegisteredText()
                    : ChateauInteractionHandler.notFoundText(characterName);
                bot.SendPrivateMessage(errorMessage, characterName);
                return;
            }

            // Build the titles list
            string titlesText = BuildTitlesText(profile, viewingOwnTitles);
            bot.SendPrivateMessage(titlesText, characterName);
        }

        private string BuildTitlesText(Profile profile, bool viewingOwnTitles)
        {
            string header = viewingOwnTitles
                ? "[b][u]Your Titles[/u][/b]\n"
                : $"[b][u]{profile.displayName}'s Titles[/u][/b]\n";

            if (profile.titles == null || profile.titles.Count == 0)
            {
                return header + (viewingOwnTitles
                    ? "You don't have any titles yet. Someone might see fit to !entitle you, or you can earn the recognition of the Chateau through your actions!"
                    : $"{profile.displayName} doesn't have any titles yet. Maybe you could !entitle them yourself?");
            }

            StringBuilder sb = new StringBuilder(header);
            DateTime Dat = DateTime.UtcNow;
            sb.AppendLine($"Total titles: {profile.titles.Count}\n");
            //version that includes dates
            //sb.AppendLine($"Total titles: {profile.titles.Count}\n Dates are provided in Unified Time of Chateau. It is currently the " + Dat.Day + Utils.GetDaySuffix(Dat.Day) + " day, " + Dat.Month.ToString() + Utils.GetDaySuffix(Dat.Month) + " month, in year 1" + Dat.Year.ToString() +  " of our Queen");

            for (int i = 0; i < profile.titles.Count; i++)
            {
                Title title = profile.titles[i];
                string formattedTitle = title.GetFormattedTitle();
                string grantedBy = title.IsSystemTitle ? "[i]Chateau[/i]" : MonDB.getDisplayName(title.givenBy);
                string grantedDate = title.grantedTime.ToString("dd-MM-1yyyy");

                sb.Append($"{formattedTitle}    ");
                //version that includes dates
                //sb.AppendLine($"{formattedTitle}    [sub]Granted by: {grantedBy} on {grantedDate}[/sub]");
            }

            // Show which titles are currently displayed
            if (profile.displayedTitleSlots != null)
            {
                sb.AppendLine("");
                sb.AppendLine("[u]Currently Displayed Titles[/u]");
                int slotCount = 0;
                foreach (int slot in profile.displayedTitleSlots)
                {
                    slotCount++;
                    sb.Append($"    {slotCount}: ");
                    if (slot >= 0)
                    {
                        sb.Append(profile.titles[slot].GetFormattedTitle());
                    }
                }
            }

            if (viewingOwnTitles)
            {
                sb.AppendLine("\n[sub]You can use !settitle {slot number} {\"title text in quotes\"} to set which titles display in your dossier (up to 9).[/sub]");
            }

            return sb.ToString();
        }
    }
}
