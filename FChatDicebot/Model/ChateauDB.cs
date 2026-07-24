using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Linq;

namespace FChatDicebot.Model
{
    // Tolerates a stale document carrying a since-removed field (disposition #5) instead of
    // throwing FormatException on deserialize — Profile has grown many fields over the
    // project's history and there's no migration step run against old documents.
    [BsonIgnoreExtraElements]
    public class Profile
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string userName { get; set; }
        public string displayName { get; set; }
        public Dictionary<string, int> counts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> characteristics { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, List<string>> lists { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, CoolDown> timers { get; set; } = new Dictionary<string, CoolDown>();
        public Dictionary<string, int> currencies { get; set; } = new Dictionary<string, int>();
        // Currency committed to an in-flight wager game, debited out of `currencies` at commit
        // time so it can't be double-spent across tables and can be exactly refunded on
        // abandon/crash. Key is a Mongo-safe "{gameKey}|{currency}" label (see WagerEscrow.Key);
        // value is the amount of that currency staked into that game. Entries are cleared when
        // the pot is awarded or refunded. Mutated atomically via IChateauDatabase.ChangeEscrow.
        [BsonIgnoreIfNull]
        public Dictionary<string, int> escrow { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> jobExperience { get; set; } = new Dictionary<string, int>();
        public List<Title> titles { get; set; } = new List<Title>();
        public int[] displayedTitleSlots { get; set; } = new int[9] { -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        public List<Pregnancy> pregnancies { get; set; } = new List<Pregnancy>();

        // Sparse daily quota tracker keyed like "corruption_{recipient}_{yyyy-MM-dd}".
        // Each entry records magnitude already spent that UTC day by this profile against
        // a single recipient. Entries with stale date prefixes are dropped on next write
        // (see CorruptionProcessor.PruneStaleQuotaEntries) so the map stays small.
        [BsonIgnoreIfNull]
        public Dictionary<string, int> dailyMagnitudes { get; set; } = new Dictionary<string, int>();

        // Trophy/sellable bottles. One entry per milking session — not aggregated, so a
        // (substance, sourceName) pair can carry multiple entries with different milkedAt
        // and corruptionTag. See MilkBottle for the per-entry shape. The Chateau provides
        // the empty bottles needed to milk; the player's "bottle" tally from selling lives
        // in the standard `currencies` dict under ChateauCurrency.BottleCurrency.
        public List<MilkBottle> milkInventory { get; set; } = new List<MilkBottle>();

        // Per-training skill levels (0-100). Key is the training identifier (e.g. "magic"),
        // value is the current level. Populated by TrainProcessor; consumed by !work for
        // task selection / compensation modifiers.
        public Dictionary<string, int> trainings { get; set; } = new Dictionary<string, int>();

        // Per-day climax tally for the climaxer (the person actually orgasming). Key is
        // "yyyy-MM-dd" in UTC; value is how many times this profile climaxed that day.
        // Populated by ClimaxforProcessor for both !climaxfor and !climax. Drives the
        // daily-streak titles (Endurance / Tireless / Inhuman) and weights the flavor
        // descriptor selection. Old entries are pruned on write (keep last 30 days).
        [BsonIgnoreIfNull]
        public Dictionary<string, int> dailyClimaxCounts { get; set; } = new Dictionary<string, int>();

        // Lifetime MANOR kickbacks this profile has earned as an EMPLOYER, keyed by
        // employee userName -> currency -> cumulative amount. Written by !work when an
        // employee (employed by someone other than themselves) completes a duty; read
        // by !business. Persists across employment changes (it is history), so a former
        // employee's entry survives if they later change jobs or bosses. Display names
        // are resolved at render time from the userName key. Lifetime cumulative; never
        // decremented or reset in v1.
        [BsonIgnoreIfNull]
        public Dictionary<string, Dictionary<string, int>> employeeEarnings { get; set; }
            = new Dictionary<string, Dictionary<string, int>>();
    }

    public class Pregnancy
    {
        public string Id { get; set; }
        public string Initiator { get; set; }
        public string MonsterType { get; set; }
        public DateTime ConceivedAt { get; set; }
        public DateTime ReadyAt { get; set; }
        public int BroodSize { get; set; }
        // Snapshot of the monster's categories at breed time. Stored on the pregnancy
        // so birth can update per-category global counters without re-fetching the
        // Identifier (which may have been edited or removed between breed and birth).
        [BsonIgnoreIfNull]
        public List<string> Categories { get; set; }
        // True when the brood-size roll triggered the rare twin event (the monster's
        // resolved min and max were both 1, but the 1% twin chance produced 2 anyway).
        // Used to emit special "rare twins" flavor in the birth message.
        [BsonIgnoreIfDefault]
        public bool IsRareTwins { get; set; }

        // --- Mystery pregnancies (feedback 6a51d2fa) ---
        // "random" (whole brood one rolled species) or "mixed" (each child its own rolled
        // species); null for an ordinary pregnancy. The roll happens at breed time, but
        // every pre-birth display masks the species — the birth announcement reveals it.
        [BsonIgnoreIfNull]
        public string MysteryKind { get; set; }

        // Mixed broods only: one entry per child, rolled at breed time. Mirrors the
        // Categories snapshot above but per child, so birth-time counters don't need to
        // re-fetch Identifiers. Null for ordinary and "random" pregnancies (whose single
        // species lives in MonsterType/Categories as usual).
        [BsonIgnoreIfNull]
        public List<BroodChild> Children { get; set; }

        public const string MysteryRandom = "random";
        public const string MysteryMixed = "mixed";

        public bool IsMystery => !string.IsNullOrEmpty(MysteryKind);
        public bool IsMixedBrood => MysteryKind == MysteryMixed;
    }

    /// <summary>
    /// One child of a mixed-species brood: its rolled species and the category snapshot
    /// taken at breed time (same rationale as <see cref="Pregnancy.Categories"/>).
    /// </summary>
    public class BroodChild
    {
        public string Species { get; set; }
        [BsonIgnoreIfNull]
        public List<string> Categories { get; set; }
    }

    /// <summary>
    /// Aggregate count of all pregnancies conceived and all offspring born for either a
    /// specific monster type or a category. Keys are prefixed with "monster:" or
    /// "category:" so the two namespaces can't collide. Maintained incrementally at
    /// breed/birth time; could in principle be reconstructed by re-scanning every
    /// profile and historical interaction.
    /// </summary>
    public class MonsterStats
    {
        [BsonId]
        public string Id { get; set; } // e.g. "monster:lamia" or "category:snake"
        public int PregnancyCount { get; set; }
        public int OffspringCount { get; set; }
    }

    /// <summary>
    /// Persistent per-machine slots jackpot. Amounts is a per-currency running total (never
    /// converted between currencies) — each currency grows independently from spins made in
    /// that currency, but a jackpot win sweeps every currency's total in this machine into the
    /// payout (see DiceBot.SpinSlotsCurrency), then the whole dictionary resets so every
    /// currency floors back to the machine's StartingJackpotAmount on its next spin. Stored
    /// outside Profile so it never counts as resident wealth in !economics / !populations.
    /// Id is the machine name.
    /// </summary>
    public class SlotsJackpot
    {
        [BsonId]
        public string Id { get; set; }
        public Dictionary<string, int> Amounts { get; set; } = new Dictionary<string, int>();
    }

    public class Title
    {
        public string titleText { get; set; }
        public string givenBy { get; set; } // "system" for system-granted titles, otherwise character name
        public DateTime grantedTime { get; set; } = DateTime.UtcNow;

        // Helper property to check if this is a system title
        public bool IsSystemTitle => givenBy == "Chateau";

        // Get the formatted title text (with or without markers)
        public string GetFormattedTitle()
        {
            if (IsSystemTitle)
            {
                return $"·{titleText}·";
            }
            return titleText;
        }
    }

    public class CoolDown
    {
        public DateTime timerStart { get; set; } = DateTime.UtcNow;
        public DateTime timerEnd { get; set; }

    }

    [BsonIgnoreExtraElements]
    public class PendingCommand
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public Interaction pendingInteraction { get; set; }
        public string awaitingConsentFrom { get; set; }
        public DateTime startTime { get; set; } = DateTime.UtcNow;

        // --- Group interaction fields (B4) ---
        // For an ordinary 1:1 pending these stay null/default and the consent flow ignores
        // them entirely (it processes the moment the single recipient consents). A
        // multi-target casual command instead mints one shared groupId across every
        // recipient's seat; each seat is consented individually but the shared "moment"
        // only resolves once no Pending seat remains. [BsonIgnoreIfNull] keeps the field
        // off legacy 1:1 documents so no migration is needed.

        // Shared id linking every seat of one group invocation. Null/empty for 1:1.
        [BsonIgnoreIfNull]
        public string groupId { get; set; }

        // Channel the group announcement was posted in, so the timeout sweep can post the
        // resolved moment there without anyone typing another command. Only stamped on
        // group seats; null for 1:1 pendings and legacy documents.
        [BsonIgnoreIfNull]
        public string sourceChannel { get; set; }

        // PendingConsent / Consented. Only meaningful for group seats; a 1:1 pending never
        // transitions (it is processed-then-deleted on consent). Default keeps legacy docs
        // and 1:1 seats in the "still waiting" state.
        public string consentState { get; set; } = PendingConsentState;

        // Order in which this seat consented (1,2,3…); 0 until consented / for 1:1. Drives
        // lapsit stack ordering and the serial-comma name order in the combined message.
        [BsonIgnoreIfDefault]
        public int consentedOrder { get; set; }

        public const string PendingConsentState = "Pending";
        public const string ConsentedState = "Consented";

        public bool IsGroupSeat => !string.IsNullOrEmpty(groupId);
        public bool HasConsented => consentState == ConsentedState;
    }

    public class Command
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string commandName { get; set; }
        public string[] aliases { get; set; }
        public string[] neededParams { get; set; }
        public string[] extraParams { get; set; }
        public string description { get; set; }
    }
    public class Identifier
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string type { get; set; }
        public string description { get; set; }
        public string[] categories { get; set; }
        // Optional flavor-text override consumed by category-specific "ToText" helpers
        // (e.g. Utils.ObjectToText for "object" identifiers) instead of a hardcoded
        // per-type dictionary in code. Unset means "use the identifier's own type" —
        // so adding a new identifier to this collection never requires a code change
        // just to make it render sensibly. [BsonIgnoreIfNull] keeps existing documents
        // untouched — no migration required for identifiers that already read fine as
        // their raw type.
        [BsonIgnoreIfNull]
        public string displayText { get; set; }
        // Per-monster overrides for the breed/birth interaction. Zero = unset; in that
        // case BreedProcessor falls back to its category-defaults table (and finally to
        // 1 day / brood 1 if no category matches). [BsonIgnoreIfDefault] keeps zero
        // values out of the DB so existing identifier docs don't need migration.
        [BsonIgnoreIfDefault]
        public int gestationDays { get; set; } = 0;
        [BsonIgnoreIfDefault]
        public int broodSizeMin { get; set; } = 0;
        [BsonIgnoreIfDefault]
        public int broodSizeMax { get; set; } = 0;
    }
    [BsonIgnoreExtraElements]
    public class Interaction
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string initiator { get; set; }
        public string recipient { get; set; }
        public string type { get; set; }
        public string identifier { get; set; }
        public string investmentLevel { get; set; }
        public BsonArray extraParameters { get; set; }
        public DateTime interactionTime { get; set; }
    }

    public class PendingDuty
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public List<DutyResult> dutyResults { get; set; } = new List<DutyResult>();
        public string dutyLabel { get; set; }
        public string job { get; set; }
        public string awaitingInputFrom { get; set; }
        public DateTime startTime { get; set; } = DateTime.UtcNow.Date;

    }

    public class Duty
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string label { get; set; }
        public string[] categories { get; set; }
        public string job { get; set; }
        public string startText { get; set; }
        public Dictionary<string, DutyResult> dutyResults { get; set; } = new Dictionary<string, DutyResult>(); //must have one result with no condition
    }

    public class DutyResult
    {
        public string choiceName { get; set; }
        public string resultText { get; set; }
        public Conditional conditional { get; set; }
        public Dictionary<string, Reward> rewardList { get; set; } = new Dictionary<string, Reward>();

    }

    public class Reward
    {
        public string currency { get; set; }

        public int weight { get; set; }
        public int min { get; set; }
        public int max { get; set; }
    }

    public class Conditional
    {
        public string type { get; set; }
        public int value { get; set; }
    }

    /// <summary>
    /// A seeded, data-authored ambient channel event (the B12 "random events" system). Lives in
    /// the read-only <c>RandomEvents</c> collection and mirrors the <see cref="Duty"/> /
    /// <see cref="DutyResult"/> / <see cref="Reward"/> authoring shape so new events ship without
    /// a deploy. The bot fires one of these into an opted-in channel every several hours; residents
    /// join with <c>!random</c>; after the response window resolves, the bot announces the chosen
    /// outcome and grants its rewards to the winner(s). See
    /// <c>BotCommands.Support.RandomEventEngine</c> for selection/validation/resolution.
    /// </summary>
    public class RandomEvent
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string label { get; set; }
        public string[] categories { get; set; }
        public int weight { get; set; }                // selection weight among eligible events
        public string announceText { get; set; }       // posted to the channel when the event fires
        public string responseType { get; set; }       // "none" | "keyword" | "challenge"
        public int responseWindowSeconds { get; set; } // how long !random is accepted (0 => engine default)
        public string winnerRule { get; set; }         // "firstValid" | "allInWindow" | "nth" | "random"
        public int winnerN { get; set; }               // for "nth": which valid responder wins (1-based)
        public List<EventOutcome> outcomes { get; set; } = new List<EventOutcome>(); // weighted roll on resolve
    }

    public class EventOutcome
    {
        public int weight { get; set; }        // weighted pick among the event's outcomes
        // Announced when this outcome is granted. May include a {winners} placeholder (e.g.
        // "{winners} are now glowing purple.") substituted with every winner's [user] tag —
        // lets a multi-winner outcome (allInWindow/etc.) carry its own combined flavor instead
        // of a generic engine-authored line.
        public string resultText { get; set; }
        public List<EventReward> rewards { get; set; } = new List<EventReward>(); // applied to winner(s)
    }

    public class EventReward
    {
        public string type { get; set; } // "currency" | "title" | "training" | "corruption" | "purity" | "curse" | "none"
        public string key { get; set; }  // currency name / title text / training skill / curse id (unused for corruption/purity/none)
        public int min { get; set; }     // magnitude/amount low  (currency, training, corruption, purity)
        public int max { get; set; }     // magnitude/amount high
    }

    public class Pledge
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string pledger { get; set; }
        public string pledgee { get; set; }
        public string interactionType { get; set; }
        public string identifier { get; set; }
        public string investmentLevel { get; set; }
        public DateTime pledgeTime { get; set; } = DateTime.UtcNow;
        public string status { get; set; } = "active"; // active, fulfilled, abandoned
        public DateTime? fulfilledTime { get; set; }
        public bool pledgeHonored { get; set; } = false; // true if fulfilled 1+ days after creation

        // Helper to check if this pledge is active
        public bool IsActive => status == "active";
    }

    public class FeedbackEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string submitterUserName { get; set; }    // login name (survives renames)
        public string submitterDisplayName { get; set; } // snapshot at submit time
        public string category { get; set; }             // "general" | "bug" | "idea" | "other"
        public string text { get; set; }
        [BsonIgnoreIfNull]
        public string sourceChannel { get; set; }        // channel name, or null if PM
        public DateTime submittedAt { get; set; }
    }
}
