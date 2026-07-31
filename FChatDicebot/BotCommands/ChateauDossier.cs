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
            { "petgive", "Pets Given" },
            { "pettake", "Pets Received" },
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
            { "petgive", "Petting" },
            { "pettake", "Petted" },
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

        // ---------------------------------------------------------------------------
        // Section rendering
        //
        // Every block in the dossier is one of two shapes, decided by what a row holds:
        //
        //   Inline  — the row is a short label and a single number ("Maids: 5"). The whole
        //             block renders as one wrapped row. Used for the tally blocks.
        //   Lines   — the row carries names or prose ("Spouses: Alice, Bob, Carol"). Each
        //             row gets its own line.
        //
        // Before this, blocks with identical data shape disagreed: "Days of Experience"
        // (job -> number) was inline while "Currently employs" (job -> number) was one line
        // per job, which is what made a heavy employer's dossier enormous.
        //
        // Both shapes now come from ReadoutText, shared with every other readout, so the
        // dossier's header/label/number conventions can't drift from !bank's or !statistics'
        // again. Section colours come from the ReadoutDomain passed at each call site.
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Row count above which a Lines-shaped section collapses into a spoiler. Forwards to
        /// the shared grammar; kept as a member so callers and tests can name it on the class
        /// that owns the behaviour they're exercising.
        /// </summary>
        public const int SpoilerLineThreshold = ReadoutText.SpoilerLineThreshold;

        /// <summary>Gap between entries in an Inline-shaped section.</summary>
        private const string InlineSeparator = ReadoutText.InlineSeparator;

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
        /// Builds the complete dossier, grouped into themed clusters separated by blank
        /// lines: who they are, what's currently affecting them, who they're connected to,
        /// what they've racked up, and what happened lately. Sections were previously
        /// emitted in the order the features shipped, which scattered related facts.
        /// </summary>
        private string BuildFullDossier(Profile profile, string targetUser)
        {
            // Who they are.
            string header = BuildNameTitleSpecialties(profile, targetUser);
            string jobSection = BuildJobSection(profile);
            string identity = header + jobSection;

            // What's currently on them.
            string state =
                BuildActiveCursesSection(profile) +
                BuildActiveParasitesSection(profile) +
                BuildActiveBreaksSection(profile) +
                BuildActiveOdorizesSection(profile);

            // Who they know. "Currently employs" lives here rather than with the tallies
            // because it now names the residents, making it a roster like Bonds and Marks.
            string relationships =
                BuildBondsSection(profile) +
                BuildMarksSection(profile) +
                BuildCurrentlyEmploysSection(targetUser);

            // What they've done.
            string tallies =
                BuildCasualInteractionsSection(profile) +
                BuildInteractionCountsSection(profile) +
                BuildOffspringSection(profile, targetUser) +
                BuildPersonallyPlantedSection(targetUser) +
                BuildAtAGlanceSection(profile) +
                BuildJobExperienceSection(profile);

            // What happened lately.
            string lately =
                BuildLastReportedSection(targetUser) +
                BuildLastSeenSection(targetUser);

            // A profile with nothing but a name gets the new-arrival note. Checking the
            // content sections directly (rather than the old string-equality check against
            // header + "\n\n") keeps this correct as sections get added or reordered. A job
            // counts as content — it's something in their file, even with no interactions yet.
            if (jobSection.Length == 0 && state.Length == 0 && relationships.Length == 0
                && tallies.Length == 0 && lately.Length == 0)
            {
                return identity + "\n" + ReadoutText.Small("A recent arrival to the Chateau. There doesn't seem to be much in their file... there will be more to read once they interact with others. Maybe you should give them a !kiss");
            }

            return ReadoutText.JoinClusters(identity, state, relationships, tallies, lately);
        }

        /// <summary>
        /// Builds the header section with name, monster type, and specialist titles
        /// </summary>
        private string BuildNameTitleSpecialties(Profile profile, string targetUser)
        {
            StringBuilder sb = new StringBuilder();
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

            // Title line is "{name} the {monster}"; the specialist run moves to its own line
            // below. The deferred-separator handling that stopped a monsterless profile
            // trailing "the " is preserved — it just has one fewer part to glue now.
            if (!string.IsNullOrEmpty(monsterPart))
            {
                sb.Append(" the ").Append(monsterPart);
            }

            // "{specialty1} and {specialty2} Specialist", built the same way as before.
            StringBuilder specialistRun = new StringBuilder();
            for (int i = 0; i < specialistTitles.Count; i++)
            {
                specialistRun.Append(specialistTitles[i]);

                int remaining = specialistTitles.Count - i;
                if (remaining == 1)
                {
                    specialistRun.Append(" Specialist");
                }
                else if (remaining == 2)
                {
                    specialistRun.Append(" and ");
                }
                else
                {
                    specialistRun.Append(", ");
                }
            }

            // The name-and-monster line is the readout's Title and takes bold-underline; the
            // specialist run and the displayed titles get their own lines below it. Previously
            // all three were inside one [b][u]...[/u][/b], so a decorated resident opened their
            // dossier with a four-line underlined block that swamped everything under it.
            StringBuilder header = new StringBuilder();
            header.Append(ReadoutText.Title(sb.ToString()));

            if (specialistTitles.Count > 0)
            {
                header.Append('\n').Append(ReadoutText.Small(specialistRun.ToString()));
            }

            string displayedTitles = Utils.GetDisplayedTitlesText(profile);
            if (!string.IsNullOrEmpty(displayedTitles))
            {
                header.Append('\n').Append(displayedTitles);
            }

            header.Append('\n');
            return header.ToString();
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

            // Both branches need the "Currently working" lead-in: without it a self-employed
            // resident's line rendered as a bare fragment ("as a Boss") with nothing in front
            // of it, because the lead-in only existed inside the employer branch.
            sb.Append("Currently working ");

            if (profile.characteristics.ContainsKey("employer"))
            {
                Profile employerProfile = _database.GetProfile(profile.characteristics["employer"]);
                if (employerProfile != null)
                {
                    sb.Append("under ");
                    sb.Append(employerProfile.displayName);
                    sb.Append(" ");
                }
            }

            // Bold only: bold-underline is reserved for the readout's Title line, so a job
            // no longer competes with the resident's own name for the eye.
            sb.Append("as ");
            sb.Append(Utils.AnOrA(job));
            sb.Append(' ');
            sb.Append("[b]" + job + "[/b]");
            sb.Append(".\n");

            return sb.ToString();
        }

        /// <summary>
        /// One line per active curse, with the curser's display name. Empty when the
        /// curses list is empty.
        /// </summary>
        private string BuildActiveCursesSection(Profile profile)
        {
            var curses = CurseInstance.LoadAll(profile);
            List<string> rows = new List<string>();
            foreach (var curse in curses)
            {
                string applierName = string.IsNullOrEmpty(curse.AppliedBy) ? "someone" : ResolveDisplayName(curse.AppliedBy);
                rows.Add("[u]" + Utils.Capitalize(curse.Curse ?? string.Empty) + ":[/u] from " + applierName);
            }
            // Bare count: the header already names what's being counted, so "7 curses" here
            // would just repeat itself.
            return ReadoutText.LineSection("Active curses", ReadoutDomain.Affliction, rows.Count.ToString(), rows);
        }

        /// <summary>
        /// One line per active parasite. Grace-window indicator is shown for spread cases
        /// still inside the free-!purge window so the carrier can see at a glance that an
        /// early purge would cost nothing.
        /// </summary>
        private string BuildActiveParasitesSection(Profile profile)
        {
            var parasites = ParasiteInstance.LoadAll(profile);
            List<string> rows = new List<string>();
            foreach (var p in parasites)
            {
                string infesterName = string.IsNullOrEmpty(p.InfestedBy) ? "an unknown source" : ResolveDisplayName(p.InfestedBy);
                string row = "[u]" + ScentText.Capitalize(ParasiteText.ParasiteName(p.Parasite)) + ":[/u] from " + infesterName;
                if (p.SpreadFromContact && DateTime.UtcNow < p.GraceUntil)
                {
                    row += " (still within !purge grace window)";
                }
                rows.Add(row);
            }
            // Bare count, as with Active curses — the header already says what these are.
            return ReadoutText.LineSection("Active parasites", ReadoutDomain.Affliction, rows.Count.ToString(), rows);
        }

        /// <summary>
        /// One line per broken bodypart, with days remaining (using BreakInstance.Severity
        /// which lazy-decrements via LoadAllWithTick — but the lazy tick mutates state, so
        /// the dossier intentionally uses the non-mutating LoadAll for read-only inspection).
        /// </summary>
        private string BuildActiveBreaksSection(Profile profile)
        {
            var breaks = BreakInstance.LoadAll(profile);
            List<string> cells = breaks
                .Select(b => ReadoutText.Row(
                    Utils.Capitalize(Utils.BodypartToText(b.Part ?? string.Empty)),
                    ReadoutText.Num(b.Severity) + (b.Severity == 1 ? " day left" : " days left")))
                .ToList();
            return ReadoutText.InlineSection("Active breaks", ReadoutDomain.Affliction, cells);
        }

        /// <summary>
        /// One line per active scent layer (from !odorize), showing layer count.
        /// </summary>
        private string BuildActiveOdorizesSection(Profile profile)
        {
            var scents = ScentLayer.LoadAll(profile);
            List<string> cells = new List<string>();
            foreach (var s in scents)
            {
                // Route through the SSOT scent-phrase helper (same one !odorize itself uses)
                // instead of rendering the raw scent identifier token (L11) — a "personal"
                // or "scentof"-category scent renders as "Alice's musk" rather than "Musk".
                Identifier scentIdentifier = _database.GetIdentifier(s.Scent);
                string appliedByDisplay = _database.GetDisplayName(s.AppliedBy) ?? s.AppliedBy;
                string scentPhrase = ScentText.ScentPhrase(scentIdentifier, s.Scent, appliedByDisplay);

                cells.Add(ReadoutText.Row(
                    Utils.Capitalize(scentPhrase),
                    ReadoutText.Num(s.Layers) + (s.Layers == 1 ? " layer" : " layers")));
            }
            return ReadoutText.InlineSection("Active scents", ReadoutDomain.Affliction, cells);
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
        /// Sired and Birthed are both per-monster offspring tallies and both usually short,
        /// so they share one row instead of owning a header each.
        /// </summary>
        private string BuildOffspringSection(Profile profile, string targetUser)
        {
            List<string> cells = new List<string>();
            string sired = BuildSiredSection(targetUser);
            string birthed = BuildBirthedSection(profile);
            if (!string.IsNullOrEmpty(sired)) cells.Add(sired);
            if (!string.IsNullOrEmpty(birthed)) cells.Add(birthed);
            return ReadoutText.InlineSection("Offspring", ReadoutDomain.Record, cells);
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
            List<string> cells = byPlant
                .OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key)
                .Select(kv => ReadoutText.Row(
                    kv.Value == 1 ? Utils.Capitalize(kv.Key) : Utils.Capitalize(kv.Key) + "s",
                    ReadoutText.Num(kv.Value)))
                .ToList();
            return ReadoutText.InlineSection("Has personally planted", ReadoutDomain.Record, cells);
        }

        /// <summary>
        /// Per-job roster of residents this user employs, naming each employee the way the
        /// Bonds section names bonded residents. Scans all profiles for
        /// <c>characteristics["employer"]</c> matching the target's userName.
        ///
        /// This used to print a bare headcount per job, one job per line, which is what made
        /// a large employer's dossier balloon — the same job-to-number shape that "Days of
        /// Experience" had always rendered as a single inline row. Now that the rows carry
        /// names it's a Lines section, and the spoiler threshold keeps the length in check.
        /// </summary>
        private string BuildCurrentlyEmploysSection(string targetUser)
        {
            var allProfiles = _database.GetAllProfiles();
            Dictionary<string, List<string>> byJob = new Dictionary<string, List<string>>();
            foreach (var p in allProfiles)
            {
                if (p?.characteristics == null) continue;
                if (!p.characteristics.ContainsKey("employer")) continue;
                if (!string.Equals(p.characteristics["employer"], targetUser, StringComparison.OrdinalIgnoreCase)) continue;
                if (!p.characteristics.ContainsKey("job")) continue;
                if (string.Equals(p.userName, targetUser, StringComparison.OrdinalIgnoreCase)) continue; // skip self
                string job = (p.characteristics["job"] ?? string.Empty).ToLowerInvariant();
                if (string.IsNullOrEmpty(job)) continue;
                if (!byJob.ContainsKey(job)) byJob[job] = new List<string>();
                byJob[job].Add(string.IsNullOrEmpty(p.displayName) ? p.userName : p.displayName);
            }
            if (byJob.Count == 0) return string.Empty;

            // Biggest teams first (ties alphabetical by job) so the roles that define this
            // employer lead; names within a role are alphabetical for a stable read.
            List<string> rows = byJob
                .OrderByDescending(kv => kv.Value.Count).ThenBy(kv => kv.Key)
                .Select(kv => ReadoutText.Row(
                    kv.Value.Count == 1 ? Utils.JobToText(kv.Key) : Utils.JobToPlural(kv.Key),
                    string.Join(", ", kv.Value.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))))
                .ToList();

            int totalEmployees = byJob.Sum(kv => kv.Value.Count);
            string summary = totalEmployees + " residents across " + rows.Count + " roles";
            return ReadoutText.LineSection("Currently employs", ReadoutDomain.Relationship, summary, rows);
        }

        /// <summary>
        /// Single-line total title count. Includes both user-bestowed (!entitle) and
        /// system-conferred (<c>givenBy == "Chateau"</c>) titles.
        /// </summary>
        private string BuildTitlesEarnedSection(Profile profile)
        {
            int count = profile.titles?.Count ?? 0;
            if (count <= 0) return string.Empty;
            return ReadoutText.Row("Titles Earned", ReadoutText.Num(count));
        }

        /// <summary>
        /// Three standalone one-line facts — titles earned, wealthiest currency and bottle
        /// count — sharing a row so they read as an at-a-glance stat line rather than owning a
        /// line each. Any one renders alone when the others are absent.
        ///
        /// These used to be bare "[b]Label:[/b] value" fragments with no header of their own,
        /// which made three unrelated facts look like three separate sections. They're cells
        /// now, under one header like every other inline block.
        /// </summary>
        private string BuildAtAGlanceSection(Profile profile)
        {
            List<string> cells = new List<string>();
            string titles = BuildTitlesEarnedSection(profile);
            string currency = BuildMostAbundantCurrencySection(profile);
            string bottled = BuildBottledSection(profile);
            if (!string.IsNullOrEmpty(titles)) cells.Add(titles);
            if (!string.IsNullOrEmpty(currency)) cells.Add(currency);
            if (!string.IsNullOrEmpty(bottled)) cells.Add(bottled);
            return ReadoutText.InlineSection("At a glance", ReadoutDomain.Economy, cells);
        }

        /// <summary>
        /// Single-line bottle collection tally. Substance counts only, deliberately: the dossier
        /// is public and a bottle's sourceName amounts to "who has this resident milked", which
        /// is the donor's business rather than the reader's. Serial numbers are likewise omitted
        /// so a public page can't be used to shop someone else's collection.
        /// </summary>
        private string BuildBottledSection(Profile profile)
        {
            if (profile.milkInventory == null || profile.milkInventory.Count == 0) return string.Empty;

            var full = profile.milkInventory.Where(b => b != null && !b.IsEmpty).ToList();
            int emptyCount = profile.milkInventory.Count(b => b != null && b.IsEmpty);
            if (full.Count == 0 && emptyCount == 0) return string.Empty;

            List<string> parts = new List<string>();
            if (full.Count > 0)
            {
                var breakdown = BottleInventory.CountsBySubstance(full)
                    .Select(kv => kv.Value + " " + ReadoutText.CapitalizePastTags(Utils.SubstanceToText(kv.Key)))
                    .ToList();
                parts.Add(ReadoutText.Num(full.Count) + " bottle" + (full.Count == 1 ? "" : "s")
                    + (breakdown.Count > 0 ? " (" + string.Join(", ", breakdown) + ")" : ""));
            }
            if (emptyCount > 0)
            {
                parts.Add(ReadoutText.Num(emptyCount) + " empt" + (emptyCount == 1 ? "y" : "ies"));
            }

            return ReadoutText.Row("Bottled", string.Join(" and ", parts));
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
            return ReadoutText.Row("Richest Currency", ReadoutText.Num(max) + " " + currencyText);
        }

        /// <summary>
        /// Shared fragment for the Sired / Birthed per-monster tallies. Returns a single
        /// labelled cell ("[u]Sired:[/u] Goblins: 12, Ogres: 4") for the caller to place in
        /// an Inline section, or empty when nothing is countable.
        /// </summary>
        private string BuildPerMonsterBlock(string label, Dictionary<string, int> counts)
        {
            if (counts == null || counts.Count == 0) return string.Empty;
            List<string> parts = new List<string>();
            foreach (var entry in counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key))
            {
                if (entry.Value <= 0) continue;
                string monster = entry.Value == 1 ? Utils.Capitalize(entry.Key) : Utils.Capitalize(entry.Key) + "s";
                parts.Add(monster + ": " + ReadoutText.Num(entry.Value));
            }
            if (parts.Count == 0) return string.Empty;
            return ReadoutText.Row(label, string.Join(", ", parts));
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

            List<string> cells = casualCounts
                .Where(c => CountDisplayNames.ContainsKey(c.Key))
                .Select(c => ReadoutText.Row(CountDisplayNames[c.Key], ReadoutText.Num(c.Value)))
                .ToList();
            return ReadoutText.InlineSection("Casual interactions", ReadoutDomain.Record, cells);
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

            List<string> cells = new List<string>();
            foreach (var entry in summed)
            {
                cells.Add(ReadoutText.Row(entry.Key, ReadoutText.Num(entry.Value)));
            }
            foreach (var entry in individual.OrderBy(kv => CountDisplayNames[kv.Key]))
            {
                cells.Add(ReadoutText.Row(CountDisplayNames[entry.Key], ReadoutText.Num(entry.Value)));
            }
            return ReadoutText.InlineSection("Notable counts", ReadoutDomain.Record, cells);
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

            List<string> rows = new List<string>();
            int totalMarks = 0;

            foreach (var list in profile.lists)
            {
                if (list.Key.EndsWith("marks") && list.Value.Count > 0)
                {
                    string bodyPart = list.Key.Substring(0, list.Key.Length - 5);

                    // A listed marker only shows up if their profile still carries a "mark"
                    // characteristic, so gather the symbols first: a bodypart whose markers
                    // all failed to resolve would otherwise emit a label with nothing after
                    // it and still inflate the "across N places" summary.
                    List<string> symbols = new List<string>();
                    foreach (string marker in list.Value)
                    {
                        Profile markerProfile = _database.GetProfile(marker);
                        if (markerProfile != null && markerProfile.characteristics.ContainsKey("mark"))
                        {
                            symbols.Add(markerProfile.characteristics["mark"]);
                        }
                    }
                    if (symbols.Count == 0) continue;

                    totalMarks += symbols.Count;
                    // BodypartToText returns a bare lowercase key ("neck"); every other
                    // labelled row in the dossier capitalises, so capitalise here too.
                    rows.Add(ReadoutText.Row(
                        Utils.Capitalize(Utils.BodypartToText(bodyPart)),
                        string.Join(" ", symbols)));
                }
            }

            return ReadoutText.LineSection("Marks", ReadoutDomain.Relationship,
                totalMarks + " across " + rows.Count + " places", rows);
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

            List<string> rows = new List<string>();
            int totalBonds = 0;

            foreach (var list in profile.lists)
            {
                if (list.Key.StartsWith("bond") && list.Value.Count > 0)
                {
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
                        List<string> displayNames = new List<string>();
                        foreach (string bonder in list.Value)
                        {
                            Profile bonderProfile = _database.GetProfile(bonder);
                            if (bonderProfile != null)
                            {
                                displayNames.Add(bonderProfile.displayName);
                            }
                        }

                        totalBonds += displayNames.Count;
                        rows.Add(ReadoutText.Row(
                            Utils.Capitalize(Utils.BondToPlural(bondType, isInitiated)),
                            string.Join(", ", displayNames)));
                    }
                }
            }

            return ReadoutText.LineSection("Bonds", ReadoutDomain.Relationship,
                totalBonds + " across " + rows.Count + " kinds", rows);
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

            List<string> cells = profile.jobExperience
                .Select(jobExp => ReadoutText.Row(Utils.JobToText(jobExp.Key), ReadoutText.Num(jobExp.Value)))
                .ToList();
            return ReadoutText.InlineSection("Days of experience", ReadoutDomain.Economy, cells);
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

            // One line rather than two: the description is a single short sentence, so the
            // header owned an entire line to introduce a fragment shorter than itself.
            return ReadoutText.Section("Last reported", ReadoutDomain.None) + " "
                + Utils.GetInteractionDescription(mostRecent) + "\n";
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

            return ReadoutText.Section("Last seen", ReadoutDomain.None) + " "
                + Utils.GetInteractionDescription(mostRecent) + "\n";
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