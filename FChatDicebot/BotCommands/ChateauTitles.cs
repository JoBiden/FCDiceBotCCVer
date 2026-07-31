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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
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
            string header = ReadoutText.Title("Titles of " + profile.displayName) + "\n";

            if (profile.titles == null || profile.titles.Count == 0)
            {
                return header + (viewingOwnTitles
                    ? "You don't have any titles yet. Someone might see fit to !entitle you, or you can earn the recognition of the Chateau through your actions!"
                    : $"{profile.displayName} doesn't have any titles yet. Maybe you could !entitle them yourself?");
            }

            StringBuilder sb = new StringBuilder(header);
            sb.Append(ReadoutText.Row("Total titles", ReadoutText.Num(profile.titles.Count))).Append('\n');

            // Every earned title, on one wrapped row. Was a manual four-space join that also
            // left a trailing separator on the last entry.
            sb.Append(ReadoutText.InlineSection("All titles", ReadoutDomain.Relationship,
                profile.titles.Select(t => t.GetFormattedTitle()).ToList()));

            // Which of them the resident has pinned to their dossier. Only filled slots are
            // listed: rendering all nine unconditionally meant a resident using two of them got
            // seven numbered rows with nothing after the colon.
            if (profile.displayedTitleSlots != null)
            {
                List<string> displayed = new List<string>();
                for (int i = 0; i < profile.displayedTitleSlots.Length; i++)
                {
                    int slot = profile.displayedTitleSlots[i];
                    if (slot < 0 || slot >= profile.titles.Count) continue;
                    displayed.Add(ReadoutText.Row((i + 1).ToString(), profile.titles[slot].GetFormattedTitle()));
                }
                sb.Append(ReadoutText.InlineSection("Currently displayed", ReadoutDomain.Relationship, displayed));
            }

            if (viewingOwnTitles)
            {
                sb.Append(ReadoutText.Footer("You can use !settitle {slot number} {\"title text in quotes\"} to set which titles display in your dossier (up to 9)."));
            }

            return sb.ToString();
        }
    }
}
