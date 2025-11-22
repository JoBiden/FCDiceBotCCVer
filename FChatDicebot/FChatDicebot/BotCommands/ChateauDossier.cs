using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Database;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using System.Windows.Markup;
using ZstdSharp;

namespace FChatDicebot.BotCommands
{
    public class ChateauDossier : ChatBotCommand
    {
        private readonly IChateauDatabase _database;

        // Static readonly dictionaries for count display names and specialist text
        private static readonly Dictionary<string, string> CountDisplayNames = new Dictionary<string, string>
        {
            { "kiss", "Kisses Shared" },
            { "handhold", "Hands Held" },
            { "cuddle", "Cuties Cuddled" },
            { "cum", "Cum Count" },
            { "spanktake", "Spanks Taken" },
            { "spankgive", "Spanks Delivered" },
            { "bullygive", "Big Bullies" },
            { "bullytake", "Boolied" }
        };

        private static readonly Dictionary<string, string> CasualCountSpecialistText = new Dictionary<string, string>
        {
            { "kiss", "Kissing" },
            { "cuddle", "Cuddling" },
            { "handhold", "Handholding" },
            { "spanktake", "Spankbaiting" },
            { "spankgive", "Spanking" },
            { "bullygive", "Bullying" },
            { "bullytake", "Bullybaiting" }
        };

        private static readonly Dictionary<string, string> InvolvedSpecialistText = new Dictionary<string, string>
        {
            { "milkgive", "Livestock" },
            { "milktake", "Milking" },
            { "paymentGivegive", "Currency Distributing" },
            { "paymentGivetake", "Currency Collecting" },
            { "paymentReceivegive", "Debt Collecting" },
            { "paymentReceivetake", "Debt Paying" },
            { "feedgive", "Feeding" },
            { "feedtake", "Eating" },
            { "goldengive", "Golden Flow" },
            { "goldentake", "Golden Receptacle" },
            { "pledge", "Pledging" },
            { "dressupgive", "Beautifying" },
            { "dressuptake", "Dressup" },
            { "climaxgive", "Climax Claiming" },
            { "climaxtake", "Climaxing" },
            { "bond", "Bondbuilding" }
        };

        private static readonly Dictionary<string, string> CommitmentSpecialistText = new Dictionary<string, string>
        {
            { "markgive", "Marking" },
            { "marktake", "Mark Collecting" },
            { "consumegive", "Devouring" },
            { "consumetake", "Snack" },
            { "petrifygive", "Petrifying" },
            { "petrifytake", "Statuesque" },
            { "plantgive", "Gardening" },
            { "planttake", "Greenery" },
            { "objectifygive", "Objectifying" },
            { "objectifytake", "Objectified" },
            { "entitlegive", "Title Bestowing" },
            { "entitletake", "Title Claiming" },
            { "breedgive", "Impregnation" },
            { "breedtake", "Breeding" },
            { "employgive", "Hiring" },
            { "employtake", "Job Hopping" },
            { "traingive", "Teaching" },
            { "traintake", "Learning" },
            { "corruptgive", "Corruptive" },
            { "corrupttake", "Corrupted" }
        };

        private static readonly Dictionary<string, string> ConsequenceSpecialistText = new Dictionary<string, string>
        {
            { "monsterizegive", "Monster Making" },
            { "monsterizetake", "Shapeshifting" },
            { "infestgive", "Infesting" },
            { "infesttake", "Infested" },
            { "renamegive", "Naming" },
            { "renametake", "Identity Hopping" },
            { "odorizegive", "Perfuming" },
            { "odorizetake", "Stench" },
            { "cursegive", "Cursing" },
            { "cursetake", "Cursebearing" },
            { "breakgive", "Breaking" },
            { "breaktake", "Broken" },
            { "dosegive", "Addictive Substance" },
            { "dosetake", "Addicted" }
        };

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public ChateauDossier(IChateauDatabase database)
        {
            _database = database;
            Name = "dossier";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        /// <summary>
        /// Legacy constructor for backward compatibility (uses MonDB)
        /// </summary>
        public ChateauDossier() : this(MonDB.GetDatabase())
        {
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string targetUser = terms.Length < 1
                ? characterName
                : commandController.GetUserNameFromCommandTerms(rawTerms);

            Profile profile = _database.GetProfile(targetUser);

            string dossierText;
            if (profile == null)
            {
                dossierText = "Dossier not found. Either they aren't registered, or you're looking for the wrong person (check your spelling!)";
            }
            else
            {
                dossierText = BuildFullDossier(profile, targetUser);
            }

            bot.SendPrivateMessage(dossierText, characterName);
        }

        /// <summary>
        /// Builds the complete dossier by calling all section builders
        /// </summary>
        private string BuildFullDossier(Profile profile, string targetUser)
        {
            StringBuilder sb = new StringBuilder();

            // Build all sections
            string header = BuildNameTitleSpecialties(profile, targetUser);
            string jobSection = BuildJobSection(profile);
            string casualSection = BuildCasualInteractionsSection(profile);
            string marksSection = BuildMarksSection(profile);
            string bondsSection = BuildBondsSection(profile);
            string experienceSection = BuildJobExperienceSection(profile);
            string lastReportedSection = BuildLastReportedSection(targetUser);
            string lastSeenSection = BuildLastSeenSection(targetUser);

            // Assemble the full dossier
            sb.Append(header);
            sb.Append(jobSection);
            sb.Append("\n");
            sb.Append(casualSection);
            sb.Append(marksSection);
            sb.Append(bondsSection);
            sb.Append(experienceSection);
            sb.Append("\n");
            sb.Append(lastReportedSection);
            sb.Append(lastSeenSection);

            // Check if this is a new arrival with no meaningful content
            string finalText = sb.ToString();
            if (finalText == header + "\n\n")
            {
                sb.Append("[sub]A recent arrival to the Chateau. There doesn't seem to be much in their file... there will be more to read once they interact with others. Maybe you should give them a !kiss[/sub]");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the header section with name, monster type, and specialist titles
        /// </summary>
        private string BuildNameTitleSpecialties(Profile profile, string targetUser)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[b][u]");
            sb.Append(profile.displayName);
            sb.Append(" the ");

            // Add monster type if present
            if (profile.characteristics.ContainsKey("monster"))
            {
                sb.Append(Utils.Capitalize(profile.characteristics["monster"]));
                sb.Append("; ");
            }

            // Gather specialist titles from all categories
            List<string> specialistTitles = new List<string>();

            // Casual interaction specialist (based on profile.counts)
            string casualSpecialist = GetCasualInteractionSpecialist(profile);
            if (!string.IsNullOrEmpty(casualSpecialist))
            {
                specialistTitles.Add(casualSpecialist);
            }

            // Involved, Commitment, and Consequence specialists (based on database queries)
            string involvedSpecialist = GetSpecialistFromDictionary(targetUser, InvolvedSpecialistText);
            if (!string.IsNullOrEmpty(involvedSpecialist))
            {
                specialistTitles.Add(involvedSpecialist);
            }

            string commitmentSpecialist = GetSpecialistFromDictionary(targetUser, CommitmentSpecialistText);
            if (!string.IsNullOrEmpty(commitmentSpecialist))
            {
                specialistTitles.Add(commitmentSpecialist);
            }

            string consequenceSpecialist = GetSpecialistFromDictionary(targetUser, ConsequenceSpecialistText);
            if (!string.IsNullOrEmpty(consequenceSpecialist))
            {
                specialistTitles.Add(consequenceSpecialist);
            }

            // Format specialist titles with proper grammar
            if (specialistTitles.Count > 0)
            {
                for (int i = 0; i < specialistTitles.Count; i++)
                {
                    sb.Append(specialistTitles[i]);

                    int remaining = specialistTitles.Count - i;
                    if (remaining == 1)
                    {
                        sb.Append(" Specialist");
                    }
                    else if (remaining == 2)
                    {
                        sb.Append(" and ");
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                }
            }

            // Add displayed titles
            string displayedTitles = Utils.GetDisplayedTitlesText(profile);
            if (!string.IsNullOrEmpty(displayedTitles))
            {
                if (specialistTitles.Count > 0)
                {
                    sb.Append("; ");
                }
                sb.Append(displayedTitles);
            }

            sb.Append("[/u][/b]\n");
            return sb.ToString();
        }

        /// <summary>
        /// Builds the job section showing current employment
        /// </summary>
        private string BuildJobSection(Profile profile)
        {
            if (!profile.characteristics.ContainsKey("job"))
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            string job = Utils.JobToText(profile.characteristics["job"]);

            if (profile.characteristics.ContainsKey("employer"))
            {
                Profile employerProfile = _database.GetProfile(profile.characteristics["employer"]);
                if (employerProfile != null)
                {
                    sb.Append("Currently working under ");
                    sb.Append(employerProfile.displayName);
                    sb.Append(" ");
                }
            }

            sb.Append("as ");
            sb.Append(Utils.AnOrA(job));
            sb.Append(" [b][u]");
            sb.Append(job);
            sb.Append("[/u][/b]\n");

            return sb.ToString();
        }

        /// <summary>
        /// Builds the casual interactions section (kisses, cuddles, etc.)
        /// </summary>
        private string BuildCasualInteractionsSection(Profile profile)
        {
            if (profile.counts == null || profile.counts.Count == 0)
            {
                return string.Empty;
            }

            // Filter to only casual counts
            Dictionary<string, int> casualCounts = new Dictionary<string, int>();
            foreach (var count in profile.counts)
            {
                if (CasualCountSpecialistText.ContainsKey(count.Key))
                {
                    casualCounts.Add(count.Key, count.Value);
                }
            }

            if (casualCounts.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("[b]Casual interactions:[/b] ");

            foreach (var count in casualCounts)
            {
                if (CountDisplayNames.ContainsKey(count.Key))
                {
                    sb.Append("[u]");
                    sb.Append(CountDisplayNames[count.Key]);
                    sb.Append(":[/u] ");
                    sb.Append(count.Value);
                    sb.Append("   ");
                }
            }

            sb.Append("\n");
            return sb.ToString();
        }

        /// <summary>
        /// Builds the marks section showing all marks on body parts
        /// </summary>
        private string BuildMarksSection(Profile profile)
        {
            if (profile.lists == null || profile.lists.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            bool hasMarks = false;

            foreach (var list in profile.lists)
            {
                if (list.Key.EndsWith("marks") && list.Value.Count > 0)
                {
                    if (!hasMarks)
                    {
                        sb.Append("Currently known [b]Marks[/b]");
                        hasMarks = true;
                    }

                    string bodyPart = list.Key.Substring(0, list.Key.Length - 5);
                    sb.Append("\n[u]");
                    sb.Append(Utils.BodypartToText(bodyPart));
                    sb.Append(":[/u]");

                    foreach (string marker in list.Value)
                    {
                        Profile markerProfile = _database.GetProfile(marker);
                        if (markerProfile != null && markerProfile.characteristics.ContainsKey("mark"))
                        {
                            sb.Append(" ");
                            sb.Append(markerProfile.characteristics["mark"]);
                        }
                    }
                }
            }

            if (hasMarks)
            {
                sb.Append("\n");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the bonds section showing all bond relationships
        /// </summary>
        private string BuildBondsSection(Profile profile)
        {
            if (profile.lists == null || profile.lists.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            bool hasBonds = false;

            foreach (var list in profile.lists)
            {
                if (list.Key.StartsWith("bond") && list.Value.Count > 0)
                {
                    if (!hasBonds)
                    {
                        sb.Append("\nCurrently known [b]Bonds[/b]:");
                        hasBonds = true;
                    }

                    string bondType = string.Empty;
                    bool isInitiated = false;

                    if (list.Key.EndsWith("received"))
                    {
                        bondType = list.Key.Substring(4, list.Key.Length - 12);
                        isInitiated = false;
                    }
                    else if (list.Key.EndsWith("initiated"))
                    {
                        bondType = list.Key.Substring(4, list.Key.Length - 13);
                        isInitiated = true;
                    }

                    if (!string.IsNullOrEmpty(bondType))
                    {
                        sb.Append("\n[u]");
                        sb.Append(Utils.Capitalize(Utils.BondToPlural(bondType, isInitiated)));
                        sb.Append(":[/u] ");

                        List<string> displayNames = new List<string>();
                        foreach (string bonder in list.Value)
                        {
                            Profile bonderProfile = _database.GetProfile(bonder);
                            if (bonderProfile != null)
                            {
                                displayNames.Add(bonderProfile.displayName);
                            }
                        }

                        sb.Append(string.Join(", ", displayNames));
                    }
                }
            }

            if (hasBonds)
            {
                sb.Append("\n");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds the job experience section showing days worked in each job
        /// </summary>
        private string BuildJobExperienceSection(Profile profile)
        {
            if (profile.jobExperience == null || profile.jobExperience.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("\nDays of [b]Experience[/b] working as... \n");

            foreach (var jobExp in profile.jobExperience)
            {
                sb.Append("[u]");
                sb.Append(Utils.JobToText(jobExp.Key));
                sb.Append(":[/u] ");
                sb.Append(jobExp.Value);
                sb.Append("   ");
            }

            sb.Append("\n");
            return sb.ToString();
        }

        /// <summary>
        /// Builds the "last reported" section showing most recent initiated interaction
        /// </summary>
        private string BuildLastReportedSection(string targetUser)
        {
            List<Interaction> initiatedInteractions = _database.GetInteractionsByInitiator(targetUser);
            if (initiatedInteractions == null || initiatedInteractions.Count == 0)
            {
                return string.Empty;
            }

            Interaction mostRecent = initiatedInteractions[0];
            foreach (Interaction interaction in initiatedInteractions)
            {
                if (interaction.interactionTime > mostRecent.interactionTime)
                {
                    mostRecent = interaction;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("[b]Last reported:[/b]\n");
            sb.Append(Utils.GetInteractionDescription(mostRecent));
            sb.Append("\n");

            return sb.ToString();
        }

        /// <summary>
        /// Builds the "last seen" section showing most recent received interaction
        /// </summary>
        private string BuildLastSeenSection(string targetUser)
        {
            List<Interaction> receivedInteractions = _database.GetInteractionsByRecipient(targetUser);
            if (receivedInteractions == null || receivedInteractions.Count == 0)
            {
                return string.Empty;
            }

            Interaction mostRecent = receivedInteractions[0];
            foreach (Interaction interaction in receivedInteractions)
            {
                if (interaction.interactionTime > mostRecent.interactionTime)
                {
                    mostRecent = interaction;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("[b]Last seen:[/b]\n");
            sb.Append(Utils.GetInteractionDescription(mostRecent));
            sb.Append("\n");

            return sb.ToString();
        }

        /// <summary>
        /// Gets the casual interaction specialist title based on highest count
        /// </summary>
        private string GetCasualInteractionSpecialist(Profile profile)
        {
            if (profile.counts == null || profile.counts.Count == 0)
            {
                return null;
            }

            Dictionary<string, int> casualCounts = new Dictionary<string, int>();
            foreach (var count in profile.counts)
            {
                if (CasualCountSpecialistText.ContainsKey(count.Key))
                {
                    casualCounts.Add(count.Key, count.Value);
                }
            }

            if (casualCounts.Count == 0)
            {
                return null;
            }

            string maxKey = casualCounts.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
            return CasualCountSpecialistText[maxKey];
        }

        /// <summary>
        /// Generic method to get specialist title from any specialist dictionary
        /// </summary>
        private string GetSpecialistFromDictionary(string targetUser, Dictionary<string, string> specialistDict)
        {
            long largestCount = 0;
            string largestKey = null;

            foreach (string key in specialistDict.Keys)
            {
                long currentCount;

                if (key.EndsWith("give"))
                {
                    string interactionType = key.Substring(0, key.Length - 4);
                    currentCount = _database.GetTypeCount(targetUser, interactionType, "initiator");
                }
                else if (key.EndsWith("take"))
                {
                    string interactionType = key.Substring(0, key.Length - 4);
                    currentCount = _database.GetTypeCount(targetUser, interactionType, "recipient");
                }
                else
                {
                    currentCount = _database.GetTypeCount(targetUser, key, "both");
                }

                if (currentCount > largestCount)
                {
                    largestCount = currentCount;
                    largestKey = key;
                }
            }

            // Only return specialist if count is greater than 1
            if (largestCount > 1 && largestKey != null)
            {
                return specialistDict[largestKey];
            }

            return null;
        }
    }
}