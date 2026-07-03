using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.BotCommands.Support;
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

        // Static readonly dictionaries for count display names and specialist text.
        // CountDisplayNames covers individual count keys rendered as a single row each.
        // Extends to give/take splits for non-casual interactions per the give/take split
        // brief — keys here are sourced from each processor's IncrementCount labels.
        private static readonly Dictionary<string, string> CountDisplayNames = new Dictionary<string, string>
        {
            { "kiss", "Kisses Shared" },
            { "handhold", "Hands Held" },
            { "cuddle", "Cuties Cuddled" },
            { "cum", "Cum Count" },
            { "spanktake", "Spanks Taken" },
            { "spankgive", "Spanks Delivered" },
            { "bullygive", "Big Bullies" },
            { "bullytake", "Boolied" },
            { "boobhatgive", "Boobhats Given" },
            { "boobhattake", "Boobhats Worn" },
            { "lickgive", "Licks Given" },
            { "licktake", "Licks Received" },
            { "lapsitgive", "Laps Sat On" },
            { "lapsittake", "Sitters Supported" },
            { "climaxtake", "Orgasms" },
            { "breaktake", "Bodyparts Exhausted" },
            { "cursetake", "Curses Endured" },
            { "dressuptake", "Costume Changes" },
            { "goldentake", "Golden Showers" },
            { "paymentGivegive", "Personal Payments" }
        };

        // SummedCountDisplay aggregates multiple count keys (e.g. give+take) under one
        // header. The display label is the dictionary key; the inner list is the count
        // labels to sum. Used for symmetric concepts the user wants surfaced as a single
        // "Shared" line rather than two split rows.
        private static readonly Dictionary<string, string[]> SummedCountDisplay = new Dictionary<string, string[]>
        {
            { "Marks Shared", new string[] { "markgive", "marktake" } },
            { "Meals Shared", new string[] { "feedgive", "feedtake" } }
        };

        private static readonly Dictionary<string, string> CasualCountSpecialistText = new Dictionary<string, string>
        {
            { "kiss", "Kissing" },
            { "cuddle", "Cuddling" },
            { "handhold", "Handholding" },
            { "spanktake", "Spankbaiting" },
            { "spankgive", "Spanking" },
            { "bullygive", "Bullying" },
            { "bullytake", "Bullybaiting" },
            { "boobhatgive", "Boobhat" },
            { "boobhattake", "Boob Wearing" },
            { "lickgive", "Licking" },
            { "licktake", "Living Lollipop" },
            { "lapsitgive", "Lap Sitting" },
            { "lapsittake", "Lap Providing" }
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
            // climaxgive/climaxtake are shared between !climax and !climaxfor — both route
            // through the single ClimaxforProcessor instance (see InteractionProcessorRegistry),
            // which swaps which party gets credited give vs. take depending on which verb was
            // typed (climaxtake = the climaxer, climaxgive = their partner). L4: if either verb's
            // crediting logic changes, keep it in sync with the other or these specialist counts
            // (and this dossier readout) will mis-attribute one side's history to the other.
            { "climaxgive", "Climax Claiming" },
            { "climaxtake", "Climaxing" }
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
            { "corrupttake", "Corrupted" },
            { "bond", "Bondbuilding" }
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
            Aliases = new string[] { "profile", "bio" };
            Category = "General";
            ShortDescription = "View a character's dossier, a public facing document summarizing their interactions in the Chateau";
            LongDescription = "View a detailed dossier for yourself or another character. If no character name is provided, shows your own dossier. The dossier shows:\n- Display name, titles, specializations (most performed interaction of each category)\n- Current job and employer\n- Casual interaction counts (kisses, cuddles, etc.)\n- Marks on their body\n- Bonds\n- Full job experience\n- Recent interactions\n";
            Usage = "!dossier [noparse][user]CharacterName[/user][/noparse]\nor simply\n!dossier";
            RelatedCommands = new string[] { "bank", "pledges", "statues" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
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
            string activeCurses = BuildActiveCursesSection(profile);
            string activeParasites = BuildActiveParasitesSection(profile);
            string activeBreaks = BuildActiveBreaksSection(profile);
            string activeOdorizes = BuildActiveOdorizesSection(profile);
            string casualSection = BuildCasualInteractionsSection(profile);
            string interactionCountsSection = BuildInteractionCountsSection(profile);
            string marksSection = BuildMarksSection(profile);
            string bondsSection = BuildBondsSection(profile);
            string siredSection = BuildSiredSection(targetUser);
            string birthedSection = BuildBirthedSection(profile);
            string plantedSection = BuildPersonallyPlantedSection(targetUser);
            string employsSection = BuildCurrentlyEmploysSection(targetUser);
            string titlesEarnedSection = BuildTitlesEarnedSection(profile);
            string abundantCurrencySection = BuildMostAbundantCurrencySection(profile);
            string experienceSection = BuildJobExperienceSection(profile);
            string lastReportedSection = BuildLastReportedSection(targetUser);
            string lastSeenSection = BuildLastSeenSection(targetUser);

            // Assemble the full dossier
            sb.Append(header);
            sb.Append(jobSection);
            sb.Append(activeCurses);
            sb.Append(activeParasites);
            sb.Append(activeBreaks);
            sb.Append(activeOdorizes);
            sb.Append("\n");
            sb.Append(casualSection);
            sb.Append(interactionCountsSection);
            sb.Append(marksSection);
            sb.Append(bondsSection);
            sb.Append(siredSection);
            sb.Append(birthedSection);
            sb.Append(plantedSection);
            sb.Append(employsSection);
            sb.Append(titlesEarnedSection);
            sb.Append(abundantCurrencySection);
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

            // Compose the header parts in order: optional monster, optional specialist titles.
            // Each part is appended through deferred separators so we never trail "; " or
            // "the " when a downstream part is empty (fixes the rendering bug where a profile
            // with a monster but no specialties showed "{name} the {Monster}; ").
            string monsterPart = profile.characteristics.ContainsKey("monster")
                ? Utils.Capitalize(profile.characteristics["monster"])
                : null;

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

            // Render: "{name} the {monster}; {specialty1} and {specialty2} Specialist"
            // Each portion is added only when populated; separators glue exactly the parts
            // present so no trailing "; " or stray "the " can appear.
            bool anyAfterName = !string.IsNullOrEmpty(monsterPart) || specialistTitles.Count > 0;
            if (anyAfterName)
            {
                sb.Append(" the ");
            }
            if (!string.IsNullOrEmpty(monsterPart))
            {
                sb.Append(monsterPart);
                if (specialistTitles.Count > 0)
                {
                    sb.Append("; ");
                }
            }
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
                if (anyAfterName)
                {
                    sb.AppendLine("");
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
        /// One line per active curse, with the curser's display name. Empty when the
        /// curses list is empty.
        /// </summary>
        private string BuildActiveCursesSection(Profile profile)
        {
            var curses = CurseInstance.LoadAll(profile);
            if (curses.Count == 0) return string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append("[b]Active Curses:[/b]");
            foreach (var curse in curses)
            {
                string applierName = string.IsNullOrEmpty(curse.AppliedBy) ? "someone" : ResolveDisplayName(curse.AppliedBy);
                sb.Append("\n[u]").Append(Utils.Capitalize(curse.Curse ?? string.Empty)).Append(":[/u] from ").Append(applierName);
            }
            sb.Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// One line per active parasite. Grace-window indicator is shown for spread cases
        /// still inside the free-!purge window so the carrier can see at a glance that an
        /// early purge would cost nothing.
        /// </summary>
        private string BuildActiveParasitesSection(Profile profile)
        {
            var parasites = ParasiteInstance.LoadAll(profile);
            if (parasites.Count == 0) return string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append("[b]Active Parasites:[/b]");
            foreach (var p in parasites)
            {
                string infesterName = string.IsNullOrEmpty(p.InfestedBy) ? "an unknown source" : ResolveDisplayName(p.InfestedBy);
                sb.Append("\n[u]").Append(ScentText.Capitalize(ParasiteText.ParasiteName(p.Parasite))).Append(":[/u] from ").Append(infesterName);
                if (p.SpreadFromContact && DateTime.UtcNow < p.GraceUntil)
                {
                    sb.Append(" (still within !purge grace window)");
                }
            }
            sb.Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// One line per broken bodypart, with days remaining (using BreakInstance.Severity
        /// which lazy-decrements via LoadAllWithTick — but the lazy tick mutates state, so
        /// the dossier intentionally uses the non-mutating LoadAll for read-only inspection).
        /// </summary>
        private string BuildActiveBreaksSection(Profile profile)
        {
            var breaks = BreakInstance.LoadAll(profile);
            if (breaks.Count == 0) return string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append("[b]Active Breaks:[/b]");
            foreach (var b in breaks)
            {
                sb.Append("\n[u]").Append(Utils.BodypartToText(b.Part ?? string.Empty)).Append(":[/u] ")
                  .Append(b.Severity).Append(b.Severity == 1 ? " day remaining" : " days remaining");
            }
            sb.Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// One line per active scent layer (from !odorize), showing layer count.
        /// </summary>
        private string BuildActiveOdorizesSection(Profile profile)
        {
            var scents = ScentLayer.LoadAll(profile);
            if (scents.Count == 0) return string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append("[b]Active Scents:[/b]");
            foreach (var s in scents)
            {
                // Route through the SSOT scent-phrase helper (same one !odorize itself uses)
                // instead of rendering the raw scent identifier token (L11) — a "personal"
                // or "scentof"-category scent renders as "Alice's musk" rather than "Musk".
                Identifier scentIdentifier = _database.GetIdentifier(s.Scent);
                string appliedByDisplay = _database.GetDisplayName(s.AppliedBy) ?? s.AppliedBy;
                string scentPhrase = ScentText.ScentPhrase(scentIdentifier, s.Scent, appliedByDisplay);

                sb.Append("\n[u]").Append(Utils.Capitalize(scentPhrase)).Append(":[/u] ")
                  .Append(s.Layers).Append(s.Layers == 1 ? " layer" : " layers");
            }
            sb.Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Per-monster lifetime sired count, parsed from every other carrier's
        /// <c>lists["offspring"]</c> entries (filtered by the "(parent: ...)" stamp).
        /// </summary>
        private string BuildSiredSection(string targetUser)
        {
            var sired = Support.ChateauStatisticsSupport.SiredByMonsterType(_database.GetAllProfiles(), targetUser);
            return BuildPerMonsterBlock("Sired", sired);
        }

        /// <summary>
        /// Per-monster lifetime birthed count, parsed from the user's own
        /// <c>lists["offspring"]</c> entries.
        /// </summary>
        private string BuildBirthedSection(Profile profile)
        {
            var birthed = Support.ChateauStatisticsSupport.BirthedByMonsterType(profile);
            return BuildPerMonsterBlock("Birthed", birthed);
        }

        /// <summary>
        /// Per-plant lifetime "planted by this user" count. Counts !plant interactions where
        /// the user was the initiator.
        /// </summary>
        private string BuildPersonallyPlantedSection(string targetUser)
        {
            List<Interaction> myPlants = _database.GetInteractionsByInitiator(targetUser);
            if (myPlants == null) return string.Empty;
            Dictionary<string, int> byPlant = new Dictionary<string, int>();
            foreach (var i in myPlants)
            {
                if (!string.Equals(i.type, "plant", StringComparison.OrdinalIgnoreCase)) continue;
                string plant = (i.identifier ?? string.Empty).ToLowerInvariant();
                if (string.IsNullOrEmpty(plant)) continue;
                if (!byPlant.ContainsKey(plant)) byPlant[plant] = 0;
                byPlant[plant]++;
            }
            if (byPlant.Count == 0) return string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append("[b]Has personally planted:[/b]");
            foreach (var kv in byPlant.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key))
            {
                string label = kv.Value == 1 ? Utils.Capitalize(kv.Key) : Utils.Capitalize(kv.Key) + "s";
                sb.Append("\n[u]").Append(label).Append(":[/u] ").Append(kv.Value);
            }
            sb.Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Per-job current headcount for residents this user employs. Scans all profiles for
        /// <c>characteristics["employer"]</c> matching the target's userName.
        /// </summary>
        private string BuildCurrentlyEmploysSection(string targetUser)
        {
            var allProfiles = _database.GetAllProfiles();
            Dictionary<string, int> byJob = new Dictionary<string, int>();
            foreach (var p in allProfiles)
            {
                if (p?.characteristics == null) continue;
                if (!p.characteristics.ContainsKey("employer")) continue;
                if (!string.Equals(p.characteristics["employer"], targetUser, StringComparison.OrdinalIgnoreCase)) continue;
                if (!p.characteristics.ContainsKey("job")) continue;
                if (string.Equals(p.userName, targetUser, StringComparison.OrdinalIgnoreCase)) continue; // skip self
                string job = (p.characteristics["job"] ?? string.Empty).ToLowerInvariant();
                if (string.IsNullOrEmpty(job)) continue;
                if (!byJob.ContainsKey(job)) byJob[job] = 0;
                byJob[job]++;
            }
            if (byJob.Count == 0) return string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append("[b]Currently employs:[/b]");
            foreach (var entry in byJob.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key))
            {
                string label = entry.Value == 1 ? Utils.JobToText(entry.Key) : Utils.JobToPlural(entry.Key);
                sb.Append("\n[u]").Append(label).Append(":[/u] ").Append(entry.Value);
            }
            sb.Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Single-line total title count. Includes both user-bestowed (!entitle) and
        /// system-conferred (<c>givenBy == "Chateau"</c>) titles.
        /// </summary>
        private string BuildTitlesEarnedSection(Profile profile)
        {
            int count = profile.titles?.Count ?? 0;
            if (count <= 0) return string.Empty;
            return "[b]Titles earned:[/b] " + count + "\n";
        }

        /// <summary>
        /// Single-line wealthiest currency tally. Picks the currency the resident has the
        /// most of by raw count — currencies don't weigh against each other in this system,
        /// so 5 lustessence beats 4 gold.
        /// </summary>
        private string BuildMostAbundantCurrencySection(Profile profile)
        {
            if (profile.currencies == null || profile.currencies.Count == 0) return string.Empty;
            var positive = profile.currencies.Where(kv => kv.Value > 0).ToList();
            if (positive.Count == 0) return string.Empty;
            int max = positive.Max(kv => kv.Value);
            var top = positive.Where(kv => kv.Value == max).Select(kv => kv.Key).OrderBy(k => k).ToList();
            string currencyText = string.Join("/", top);
            return "[b]Most abundant currency:[/b] " + max + " " + currencyText + "\n";
        }

        /// <summary>
        /// Shared block layout for the Sired / Birthed / similar per-monster sections.
        /// Mirrors the Bonds section style — header on its own line, then "Type: count"
        /// rows.
        /// </summary>
        private string BuildPerMonsterBlock(string label, Dictionary<string, int> counts)
        {
            if (counts == null || counts.Count == 0) return string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append("[b]").Append(label).Append(":[/b]");
            foreach (var entry in counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key))
            {
                if (entry.Value <= 0) continue;
                string monster = entry.Value == 1 ? Utils.Capitalize(entry.Key) : Utils.Capitalize(entry.Key) + "s";
                sb.Append("\n[u]").Append(monster).Append(":[/u] ").Append(entry.Value);
            }
            sb.Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Look up a userName's displayName, falling back to the userName itself when the
        /// referenced profile no longer exists (or never existed).
        /// </summary>
        private string ResolveDisplayName(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return userName;
            Profile profile = _database.GetProfile(userName);
            return profile != null && !string.IsNullOrEmpty(profile.displayName) ? profile.displayName : userName;
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
        /// Builds the non-casual interaction counts row — give/take splits for non-casual
        /// counters (climax/break/curse/dressup/golden/payment) plus summed entries for
        /// concepts the user wants surfaced as a single "Shared" line (marks, meals).
        /// </summary>
        private string BuildInteractionCountsSection(Profile profile)
        {
            if (profile.counts == null || profile.counts.Count == 0) return string.Empty;

            // Individual non-casual rows: CountDisplayNames entries that aren't the casual
            // ones (those land in BuildCasualInteractionsSection above).
            Dictionary<string, int> individual = new Dictionary<string, int>();
            foreach (var count in profile.counts)
            {
                if (!CountDisplayNames.ContainsKey(count.Key)) continue;
                if (CasualCountSpecialistText.ContainsKey(count.Key)) continue;
                if (count.Value > 0) individual[count.Key] = count.Value;
            }

            // Summed rows: aggregate give+take pairs under one display label.
            Dictionary<string, int> summed = new Dictionary<string, int>();
            foreach (var pair in SummedCountDisplay)
            {
                int total = 0;
                foreach (var key in pair.Value)
                {
                    if (profile.counts.ContainsKey(key)) total += profile.counts[key];
                }
                if (total > 0) summed[pair.Key] = total;
            }

            if (individual.Count == 0 && summed.Count == 0) return string.Empty;

            StringBuilder sb = new StringBuilder();
            sb.Append("[b]Notable counts:[/b] ");
            foreach (var entry in summed)
            {
                sb.Append("[u]").Append(entry.Key).Append(":[/u] ").Append(entry.Value).Append("   ");
            }
            foreach (var entry in individual.OrderBy(kv => CountDisplayNames[kv.Key]))
            {
                sb.Append("[u]").Append(CountDisplayNames[entry.Key]).Append(":[/u] ").Append(entry.Value).Append("   ");
            }
            sb.Append('\n');
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
            // Casual interactions (kiss, cuddle, spank, ...) are high-frequency and
            // low-stakes by design — they shouldn't bury a meaningful "Last seen" entry
            // (a mark, a breed, a payment) under whichever casual happened most recently.
            // Explicitly excluding casual-tier interactions here (rather than the old
            // approach of persisting a DateTime.MinValue timestamp for some casuals so
            // they'd always lose the "most recent" comparison) means every interaction can
            // carry its real timestamp for other features to use.
            List<Interaction> receivedInteractions = _database.GetInteractionsByRecipient(targetUser)
                ?.Where(i => !string.Equals(i.investmentLevel, "casual", StringComparison.OrdinalIgnoreCase))
                .ToList();
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