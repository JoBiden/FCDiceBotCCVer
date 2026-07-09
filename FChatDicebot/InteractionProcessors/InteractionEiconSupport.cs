using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Single source of truth for the per-interaction custom eicon feature (<c>!seteicon</c>).
    /// Residents pin one of their own eicons to an interaction — the way Chateau Contract has
    /// its own little easter-egg icons — and it surfaces on that interaction's completion
    /// message.
    ///
    /// Eicons are stored on <see cref="Profile.characteristics"/>, keyed by the interaction
    /// <b>verb</b> that gets stamped on <see cref="Interaction.type"/> at completion time. That
    /// keeps the storage key and the completion-time lookup in agreement, and lets most
    /// shared-processor pairs (corrupt/purify, lap/sit) carry distinct icons even though one
    /// processor backs both verbs. The one exception is <c>climax</c>/<c>climaxfor</c>: those
    /// two verbs are the same act typed from opposite directions, so they deliberately share a
    /// single stored slot (see <see cref="StorageKey"/>) — a resident sets their climax eicon
    /// once and it renders whichever direction they use.
    ///
    /// The <c>mark</c> eicon predates this system: it lives in <c>characteristics["mark"]</c>,
    /// is surfaced in the dossier and by <c>MarkProcessor</c>'s own reveal, and is therefore
    /// excluded from the generic completion suffix (see <see cref="IsSelfRendered"/>) so it is
    /// never double-appended. <c>!seteicon mark</c> and the legacy <c>!setmark</c> both write
    /// that same slot.
    /// </summary>
    public static class InteractionEiconSupport
    {
        /// <summary>Longest eicon token we accept, mirroring the legacy <c>!setmark</c> guard.</summary>
        public const int MaxEiconLength = 47;

        /// <summary>The verb whose eicon is stored/rendered specially (see class summary).</summary>
        public const string MarkVerbKey = "mark";

        /// <summary>
        /// The two directional climax verbs. They fold onto <see cref="ClimaxVerbKey"/> for
        /// storage so <c>!seteicon climax</c> and <c>!seteicon climaxfor</c> share one icon.
        /// </summary>
        public const string ClimaxVerbKey = "climax";
        public const string ClimaxforVerbKey = "climaxfor";

        // User-typed command token -> the interaction verb key(s) it reads/writes. Aliases fold
        // onto their canonical verb (hug->cuddle, dress->dressup, hire->employ, climaxfor->climax);
        // !pay covers both payment directions. Every value here is a verb that appears on
        // Interaction.type at completion time, so this map and the completion suffix stay in
        // lockstep (climaxfor's completion read folds back via StorageKey normalization).
        private static readonly Dictionary<string, string[]> TokenToVerbKeys =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // Casual
            { "boobhat", new[] { "boobhat" } },
            { "bully", new[] { "bully" } },
            { "cuddle", new[] { "cuddle" } },
            { "hug", new[] { "cuddle" } },
            { "handhold", new[] { "handhold" } },
            { "kiss", new[] { "kiss" } },
            { "lap", new[] { "lap" } },
            { "sit", new[] { "sit" } },
            { "lick", new[] { "lick" } },
            { "spank", new[] { "spank" } },
            // Involved
            { "climax", new[] { "climax" } },
            { "climaxfor", new[] { "climax" } },
            { "dressup", new[] { "dressup" } },
            { "dress", new[] { "dressup" } },
            { "feed", new[] { "feed" } },
            { "golden", new[] { "golden" } },
            { "milk", new[] { "milk" } },
            { "pay", new[] { "paymentGive", "paymentReceive" } },
            // Commitment
            { "birth", new[] { "birth" } },
            { "bond", new[] { "bond" } },
            { "breed", new[] { "breed" } },
            { "consume", new[] { "consume" } },
            { "corrupt", new[] { "corrupt" } },
            { "purify", new[] { "purify" } },
            { "employ", new[] { "employ" } },
            { "hire", new[] { "employ" } },
            { "entitle", new[] { "entitle" } },
            { "mark", new[] { "mark" } },
            { "objectify", new[] { "objectify" } },
            { "petrify", new[] { "petrify" } },
            { "plant", new[] { "plant" } },
            { "train", new[] { "train" } },
            // Consequence
            { "break", new[] { "break" } },
            { "curse", new[] { "curse" } },
            { "dose", new[] { "dose" } },
            { "infest", new[] { "infest" } },
            { "monsterize", new[] { "monsterize" } },
            { "odorize", new[] { "odorize" } },
            { "rename", new[] { "rename" } },
        };

        /// <summary>
        /// Canonical, alias-free tokens in display order — used when listing what a resident has
        /// set so aliases (hug/dress/hire) don't produce duplicate rows.
        /// </summary>
        public static readonly string[] CanonicalTokensInOrder = new[]
        {
            "boobhat", "bully", "cuddle", "handhold", "kiss", "lap", "sit", "lick", "spank",
            "climax", "dressup", "feed", "golden", "milk", "pay",
            "birth", "bond", "breed", "consume", "corrupt", "purify", "employ", "entitle",
            "mark", "objectify", "petrify", "plant", "train",
            "break", "curse", "dose", "infest", "monsterize", "odorize", "rename",
        };

        /// <summary>
        /// Resolve a user-typed command token (interaction name or alias) to the verb key(s) its
        /// eicon is stored under. Returns false for anything not in the supported interaction set
        /// (system / recovery / dice commands).
        /// </summary>
        public static bool TryResolveTokenToVerbKeys(string token, out string[] verbKeys)
        {
            if (!string.IsNullOrEmpty(token) && TokenToVerbKeys.TryGetValue(token.Trim(), out var keys))
            {
                verbKeys = keys;
                return true;
            }
            verbKeys = null;
            return false;
        }

        /// <summary>
        /// True when this verb renders its own eicon and so must be skipped by the generic
        /// completion suffix. Only <c>mark</c> qualifies (its reveal is woven into
        /// <c>MarkProcessor</c>'s completion sentence and shown in the dossier).
        /// </summary>
        public static bool IsSelfRendered(string verbKey)
        {
            return string.Equals(verbKey, MarkVerbKey, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Get a resident's stored eicon for the given verb, or empty string if unset.</summary>
        public static string GetInteractionEicon(Profile profile, string verbKey)
        {
            if (profile?.characteristics == null || string.IsNullOrEmpty(verbKey)) return string.Empty;
            return profile.characteristics.TryGetValue(StorageKey(verbKey), out var eicon) ? eicon : string.Empty;
        }

        /// <summary>Store a resident's eicon for the given verb (does not persist — caller saves).</summary>
        public static void SetInteractionEicon(Profile profile, string verbKey, string eicon)
        {
            if (profile == null || string.IsNullOrEmpty(verbKey)) return;
            if (profile.characteristics == null) profile.characteristics = new Dictionary<string, string>();
            profile.characteristics[StorageKey(verbKey)] = eicon;
        }

        /// <summary>Remove a resident's eicon for the given verb (does not persist — caller saves).</summary>
        public static void ClearInteractionEicon(Profile profile, string verbKey)
        {
            if (profile?.characteristics == null || string.IsNullOrEmpty(verbKey)) return;
            profile.characteristics.Remove(StorageKey(verbKey));
        }

        // Mark keeps its historical characteristics["mark"] slot so the dossier + existing marks
        // keep working unchanged; every other verb namespaces under "eicon_". climaxfor folds to
        // climax first so both directions resolve to the same slot on every set/read/clear —
        // this normalization is the single seam that keeps the two verbs sharing one icon,
        // whether the verb arrives typed by the user or read raw off Interaction.type.
        private static string StorageKey(string verbKey)
        {
            string normalized = NormalizeVerbKey(verbKey);
            return string.Equals(normalized, MarkVerbKey, StringComparison.OrdinalIgnoreCase)
                ? MarkVerbKey
                : "eicon_" + normalized;
        }

        private static string NormalizeVerbKey(string verbKey)
        {
            return string.Equals(verbKey, ClimaxforVerbKey, StringComparison.OrdinalIgnoreCase)
                ? ClimaxVerbKey
                : verbKey;
        }
    }
}
