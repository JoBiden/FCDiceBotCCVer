using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Commitment
{
    /// <summary>
    /// Processor for the train interaction - the initiator and recipient practice a
    /// training together. Both parties' levels in that training advance, with the
    /// higher-level partner acting as a tutor and constraining the lower-level partner's
    /// growth above level 10.
    ///
    /// Level rules (Hi/Lo are the two parties' current levels in the training):
    ///   - Both below 10: both +1.
    ///   - Lo &lt; 10 ≤ Hi: only Lo +1.
    ///   - Both ≥ 10 and equal: both +1.
    ///   - Both ≥ 10, not equal, gap ≤ 10: only Lo +1.
    ///   - Both ≥ 10, gap &gt; 10: only Lo +1 (Hi can't learn from Lo at this gap).
    /// Cap is 100.
    /// </summary>
    public class TrainProcessor : InteractionProcessorBase
    {
        public const int LevelCap = 100;
        public const int TutorThreshold = 10;
        public const int MaxLearnableGap = 10;

        public override string InteractionType => "train";
        public override string InvestmentLevel => "commitment";

        // Per-call snapshot of the pre-session levels so GetCompletionMessage can narrate
        // "N → M" without re-deriving the pre-state from the post-state (which would be
        // ambiguous around the TutorThreshold). Populated in ProcessInteraction and read
        // by GetCompletionMessage in the same ChateauConsent flow.
        private int _lastInitiatorLevelBefore;
        private int _lastRecipientLevelBefore;
        private int _lastInitiatorLevelAfter;
        private int _lastRecipientLevelAfter;
        private bool _lastSnapshotPopulated;

        public TrainProcessor(IChateauDatabase database) : base(database)
        {
        }

        public TrainProcessor() : base()
        {
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            // Per spec: solo training isn't supported — training is always a practice
            // session between two parties.
            if (string.Equals(initiator, recipient, StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Failure("You can't train with yourself. Find someone to train with!");
            }

            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("training"));
            }

            Identifier trainingIdentifier = Database.GetIdentifier(identifier);
            if (trainingIdentifier == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(identifier));
            }
            if (trainingIdentifier.categories == null
                || !trainingIdentifier.categories.Contains("training", StringComparer.OrdinalIgnoreCase))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("training"));
            }

            // Break gating by training type → required bodyparts. Training is bilateral —
            // both parties practice together — so a broken required part on *either* side
            // blocks the session.
            HashSet<string> requiredParts = RequiredPartsForTraining(identifier);
            if (requiredParts != null)
            {
                var initiatorBlock = BlockForBrokenRequiredParts(initiator, requiredParts, identifier);
                if (initiatorBlock != null) return ValidationResult.Failure(initiatorBlock);

                var recipientBlock = BlockForBrokenRequiredParts(recipient, requiredParts, identifier);
                if (recipientBlock != null) return ValidationResult.Failure(recipientBlock);
            }

            return ValidationResult.Success();
        }

        private string BlockForBrokenRequiredParts(string userName, HashSet<string> requiredParts, string trainingType)
        {
            Profile profile = Database.GetProfile(userName);
            if (profile == null) return null;

            var breaks = BreakInstance.LoadAllWithTick(profile);
            var brokenAndRequired = breaks.Where(b => requiredParts.Contains(b.Part))
                                          .Select(b => b.Part)
                                          .ToList();
            if (brokenAndRequired.Count == 0) return null;

            string displayName = string.IsNullOrEmpty(profile.displayName) ? userName : profile.displayName;
            string parts = brokenAndRequired.Count == 1
                ? brokenAndRequired[0]
                : (brokenAndRequired.Count == 2
                    ? brokenAndRequired[0] + " and " + brokenAndRequired[1]
                    : string.Join(", ", brokenAndRequired.GetRange(0, brokenAndRequired.Count - 1)) + ", and " + brokenAndRequired[brokenAndRequired.Count - 1]);
            string verbToBe = brokenAndRequired.Count == 1 ? "is" : "are";
            return displayName + "'s " + parts + " " + verbToBe + " too broken for " + trainingType + " training.";
        }

        public static HashSet<string> RequiredPartsForTraining(string trainingType)
        {
            if (string.IsNullOrEmpty(trainingType)) return null;
            switch (trainingType.ToLowerInvariant())
            {
                case "corset":      return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "breast", "torso" };
                case "heel":        return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "foot" };
                case "anal":        return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ass" };
                case "deepthroat":  return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "mouth", "tongue" };
                case "ponygirl":    return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "body", "foot", "leg", "ass" };
                case "mathematics": return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "mind" };
                case "magic":       return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "mind" };
                case "flight":      return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "wing" };
                case "instrument":  return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "hand" };
                case "dance":       return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "foot", "leg" };
                default:            return null;  // obedience and any future training with no anatomy requirement
            }
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string trainingId = command.pendingInteraction.identifier;

            Database.AddInteraction(command.pendingInteraction);

            Profile initiatorProfile = Database.GetProfile(initiator);
            Profile recipientProfile = Database.GetProfile(recipient);

            int initiatorBefore = GetLevel(initiatorProfile, trainingId);
            int recipientBefore = GetLevel(recipientProfile, trainingId);

            ApplyProgression(initiatorBefore, recipientBefore, out int initiatorAfter, out int recipientAfter);

            SetLevel(initiatorProfile, trainingId, initiatorAfter);
            SetLevel(recipientProfile, trainingId, recipientAfter);

            // Symmetric daily pair-lock — binds both directions across all trainings.
            CoolDown pairTimer = new CoolDown { timerEnd = DateTime.UtcNow.Date.AddDays(1) };
            initiatorProfile.timers[PairTimerKey(recipient)] = pairTimer;
            recipientProfile.timers[PairTimerKey(initiator)] = pairTimer;

            // Award per-training tier titles for any thresholds crossed by this session.
            GrantTrainingTitles(initiatorProfile, trainingId, initiatorBefore, initiatorAfter);
            GrantTrainingTitles(recipientProfile, trainingId, recipientBefore, recipientAfter);

            Database.SetProfile(initiator, initiatorProfile);
            Database.SetProfile(recipient, recipientProfile);

            Database.IncrementCount(initiator, "traingive");
            Database.IncrementCount(recipient, "traintake");

            _lastInitiatorLevelBefore = initiatorBefore;
            _lastRecipientLevelBefore = recipientBefore;
            _lastInitiatorLevelAfter = initiatorAfter;
            _lastRecipientLevelAfter = recipientAfter;
            _lastSnapshotPopulated = true;

            Database.DeletePendingCommand(command.Id);

            return "train";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            bool initiatorAdvanced;
            bool recipientAdvanced;

            if (_lastSnapshotPopulated)
            {
                initiatorAdvanced = _lastInitiatorLevelAfter > _lastInitiatorLevelBefore;
                recipientAdvanced = _lastRecipientLevelAfter > _lastRecipientLevelBefore;
                _lastSnapshotPopulated = false; // drained — don't reuse on next interaction
            }
            else
            {
                // No snapshot captured (e.g. GetCompletionMessage called in isolation).
                // Default to the both-advance phrasing so the message reads as flavor
                // rather than a misleading "nothing happened".
                initiatorAdvanced = true;
                recipientAdvanced = true;
            }

            string initName = initiatorProfile.displayName;
            string recName = recipientProfile.displayName;

            if (initiatorAdvanced && recipientAdvanced)
            {
                return initName + " and " + recName + " do some " + identifier
                    + " training! They both feel a little more practiced now.";
            }

            if (!initiatorAdvanced && !recipientAdvanced)
            {
                return initName + " and " + recName + " do some " + identifier
                    + " training! They're both just showing off at this point, there's not much more for them to learn.";
            }

            // Asymmetric — the tutor is the one who didn't advance, the student is the
            // one who did. Phrase the message from the tutor's perspective.
            string tutor = initiatorAdvanced ? recName : initName;
            string student = initiatorAdvanced ? initName : recName;

            return initName + " and " + recName + " do some " + identifier
                + " training! " + tutor + " was a lot more knowledgeable, so only "
                + student + " benefitted from the time spent.";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " wants to do some " + identifier
                + " training with " + recipientProfile.displayName
                + "! Do you !consent to improving your skills together?";
        }

        /// <summary>
        /// Compute the new levels for both parties given the level-progression rule.
        /// Pure function — no profile / DB access — so it's easy to unit-test.
        /// </summary>
        public static void ApplyProgression(int initiatorLevel, int recipientLevel, out int initiatorAfter, out int recipientAfter)
        {
            initiatorAfter = initiatorLevel;
            recipientAfter = recipientLevel;

            bool initiatorIsLow = initiatorLevel <= recipientLevel;
            bool bothBelowThreshold = initiatorLevel < TutorThreshold && recipientLevel < TutorThreshold;

            if (bothBelowThreshold)
            {
                initiatorAfter = Bump(initiatorLevel);
                recipientAfter = Bump(recipientLevel);
                return;
            }

            // At least one party is at/above TutorThreshold.
            if (initiatorLevel < TutorThreshold && recipientLevel >= TutorThreshold)
            {
                // Initiator is the low partner, recipient is the tutor — only initiator advances.
                initiatorAfter = Bump(initiatorLevel);
                return;
            }
            if (recipientLevel < TutorThreshold && initiatorLevel >= TutorThreshold)
            {
                recipientAfter = Bump(recipientLevel);
                return;
            }

            // Both ≥ TutorThreshold.
            if (initiatorLevel == recipientLevel)
            {
                initiatorAfter = Bump(initiatorLevel);
                recipientAfter = Bump(recipientLevel);
                return;
            }

            // Both ≥ TutorThreshold and not equal — only the lower party advances. The
            // gap-too-wide case (|Hi - Lo| > MaxLearnableGap) collapses to the same
            // outcome under the current spec: lower still advances; higher stays.
            if (initiatorIsLow)
            {
                initiatorAfter = Bump(initiatorLevel);
            }
            else
            {
                recipientAfter = Bump(recipientLevel);
            }
        }

        public static string PairTimerKey(string otherUserName)
        {
            return "train_pair_" + otherUserName;
        }

        /// <summary>
        /// Tier thresholds for per-training system titles. Crossing a threshold during a
        /// training session awards the corresponding tier title for that training.
        /// </summary>
        internal static readonly (int Threshold, string TierLabel)[] TitleTiers = new[]
        {
            (10, "Apprentice"),
            (25, "Adept"),
            (50, "Master"),
            (100, "Grandmaster"),
        };

        internal static string FormatTitle(string tierLabel, string trainingId)
        {
            return TitleCase(trainingId) + " " + tierLabel;
        }

        private static void GrantTrainingTitles(Profile profile, string trainingId, int oldLevel, int newLevel)
        {
            if (profile.titles == null)
            {
                profile.titles = new List<Title>();
            }
            foreach (var tier in TitleTiers)
            {
                if (newLevel >= tier.Threshold && oldLevel < tier.Threshold)
                {
                    string titleText = FormatTitle(tier.TierLabel, trainingId);
                    bool alreadyHas = profile.titles.Any(t =>
                        t.IsSystemTitle && string.Equals(t.titleText, titleText, StringComparison.OrdinalIgnoreCase));
                    if (!alreadyHas)
                    {
                        profile.titles.Add(new Title
                        {
                            titleText = titleText,
                            givenBy = "Chateau",
                            grantedTime = DateTime.UtcNow,
                        });
                    }
                }
            }
        }

        private static int Bump(int level)
        {
            int next = level + 1;
            return next > LevelCap ? LevelCap : next;
        }

        private static int GetLevel(Profile profile, string trainingId)
        {
            if (profile == null || profile.trainings == null) return 0;
            return profile.trainings.TryGetValue(trainingId, out int level) ? level : 0;
        }

        private static void SetLevel(Profile profile, string trainingId, int level)
        {
            if (profile.trainings == null) profile.trainings = new Dictionary<string, int>();
            profile.trainings[trainingId] = level;
        }

        private static string TitleCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0]) + (s.Length > 1 ? s.Substring(1) : string.Empty);
        }
    }
}
