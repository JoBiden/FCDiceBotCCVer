using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Linq;

namespace FChatDicebot.InteractionProcessors.Involved
{
    /// <summary>
    /// Backs both <c>!drinkfrom</c> (the drinker asks) and <c>!forcedrink</c> (the source
    /// offers). One act, two directions: the same instance is registered under both type keys
    /// in <see cref="InteractionProcessorRegistry"/> and every read of "who actually drank"
    /// derives from the typed verb via <see cref="ResolveDrinker"/>.
    ///
    /// This is <c>!milk</c> plus <c>!drink</c> collapsed into one step, minus the bottle. The
    /// drinker takes the corruption and the addiction risk a bottle of the same substance would
    /// have carried, at double strength, and gets no numbered empty to show for it — the whole
    /// trade-off. No <see cref="MilkBottle"/> is created and no serial is claimed.
    ///
    /// <para>
    /// Count semantics follow the give/take convention from the acting party's side, matching
    /// <c>milkgive</c>/<c>milktake</c>: the <b>drinker</b> (who performs the act) takes
    /// <c>drinkfromgive</c>, the <b>source</b> (who it is done to) takes <c>drinkfromtake</c> —
    /// not the direction the fluid travels.
    /// </para>
    ///
    /// <para>
    /// <b>Why the typed verb travels the way it does.</b> The interaction identifier stays the
    /// bare substance, because <see cref="StatusEffectContributors.DoseStatusContributor"/>
    /// matches it against a vice name to decide whether a craving was satisfied — a composite
    /// <c>"substance|role"</c> payload would silently break that. So the verb reaches
    /// <see cref="ValidateInteraction(string, string, string, string)"/> as an explicit
    /// argument from the command layer, reaches <see cref="ProcessInteraction"/> on
    /// <see cref="Interaction.type"/>, and reaches the completion message through
    /// <see cref="_lastOutcome"/> — the same per-call stash pattern
    /// <see cref="ClimaxforProcessor"/> uses for its auto-dose fragment.
    /// </para>
    /// </summary>
    public class SourceDrinkProcessor : InteractionProcessorBase
    {
        public const string DrinkFromType = "drinkfrom";
        public const string ForceDrinkType = "forcedrink";

        public override string InteractionType => DrinkFromType;
        public override string InvestmentLevel => "involved";

        /// <summary>
        /// No bodypart eicon. The part in play belongs to whichever side is drinking, which
        /// flips between the two verbs, and <see cref="InteractionProcessorBase.BodypartEiconRule"/>
        /// is a fixed per-processor constant that can't see the typed verb. A resident's custom
        /// <c>!seteicon drinkfrom</c> still renders on the right person — that path goes through
        /// <see cref="GetEiconSubject"/>, which does get the verb.
        /// </summary>
        public override BodypartEiconRule BodypartEiconRule => null;

        /// <summary>End of the break refusal, in the drinking flavor.</summary>
        public const string DrinkBreakBlockTail = "too broken to drink from.";

        // Single mutable RNG for the addiction roll, swappable by tests for a seeded one.
        // Not thread-safe; the bot processes one interaction at a time.
        internal static Random Rng = new Random();

        /// <summary>
        /// Everything <see cref="ProcessInteraction"/> worked out, held for the
        /// <see cref="GetCompletionMessage"/> / <see cref="GetStatusEffectSubject"/> calls that
        /// follow it in the same consent turn. Reset at the top of every
        /// <see cref="ProcessInteraction"/>.
        /// </summary>
        private Outcome _lastOutcome;

        /// <summary>
        /// Contributors see the verb that was actually typed, not the single type this processor
        /// reports everywhere else. <see cref="StatusEffectContributors.DoseStatusContributor"/>
        /// decides whose craving a drink quiets, and the drinker is the initiator on
        /// <c>!drinkfrom</c> but the recipient on <c>!forcedrink</c> — with only
        /// <c>isInitiator</c> to go on, it cannot tell them apart from one shared type.
        ///
        /// Falls back to <see cref="DrinkFromType"/> outside a processed interaction (consent-time
        /// blocker checks, where no outcome has been stashed yet and no contributor cares).
        /// </summary>
        protected override string ContributorInteractionType => _lastOutcome?.TypedVerb ?? DrinkFromType;

        private class Outcome
        {
            public string TypedVerb;
            public string DrinkerUser;
            public string SourceUser;
            public string DrinkerDisplay;
            public string SourceDisplay;
            public string Substance;
            public string CorruptionTag;
            public ChateauDrink.CorruptionDrinkOutcome Corruption;
            public ChateauDrink.AddictionDrinkOutcome Addiction;
            /// <summary>Drained by <see cref="GetCompletionMessage"/>; the roles outlive it.</summary>
            public string Message;
        }

        public SourceDrinkProcessor(IChateauDatabase database) : base(database) { }
        public SourceDrinkProcessor() : base() { }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:    return "drank from";
                case VerbTense.Present: return "drinks from";
                case VerbTense.Future:  return "will drink from";
                default:                return "drink from";
            }
        }

        // -------------------------------------------------------------------
        // Roles
        // -------------------------------------------------------------------

        /// <summary>
        /// Who swallows. <c>!drinkfrom</c> reads "let me drink from you", so the initiator
        /// drinks; <c>!forcedrink</c> reads "drink from me", so the recipient does.
        /// </summary>
        public static string ResolveDrinker(string interactionType, string initiator, string recipient)
        {
            return IsForceDrink(interactionType) ? recipient : initiator;
        }

        /// <summary>Mirror of <see cref="ResolveDrinker"/> — whoever is drunk from.</summary>
        public static string ResolveSource(string interactionType, string initiator, string recipient)
        {
            return IsForceDrink(interactionType) ? initiator : recipient;
        }

        private static bool IsForceDrink(string interactionType)
        {
            return string.Equals(interactionType, ForceDrinkType, StringComparison.OrdinalIgnoreCase);
        }

        // -------------------------------------------------------------------
        // Validation
        // -------------------------------------------------------------------

        /// <summary>
        /// The role-agnostic half, and the only half <see cref="BotCommands.ChateauConsent"/>
        /// can run — it re-validates with the three-argument signature, which carries no verb.
        /// The role-dependent gates (the draw lock and the broken-part check both depend on
        /// which side is the source) live in the typed overload for the command-time check and
        /// are re-checked inside <see cref="ProcessInteraction"/>, which does know the verb.
        /// </summary>
        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            // Drinking from yourself produces nothing and consumes nothing — !drink already
            // covers "consume something you hold". Rejected at both layers rather than
            // shortcut, unlike !milk, because there is no payout to shortcut to.
            if (string.Equals(initiator, recipient, StringComparison.Ordinal))
            {
                return ValidationResult.Failure(SelfTargetText);
            }

            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("substance"));
            }

            // Same drinkable-category rule as !milk, deliberately identical: the two verbs draw
            // the same fluids out of the same bodies and must not disagree on what counts.
            Identifier substanceIdentifier = Database.GetIdentifier(identifier);
            if (substanceIdentifier == null
                || substanceIdentifier.categories == null
                || !(substanceIdentifier.categories.Contains("substance", StringComparer.OrdinalIgnoreCase)
                     || substanceIdentifier.categories.Contains("vice", StringComparer.OrdinalIgnoreCase)))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("substance"));
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Full command-time validation: the role-agnostic checks plus the two that need to know
        /// which side is the source. Called by <see cref="BotCommands.ChateauDrinkfrom"/> and
        /// <see cref="BotCommands.ChateauForcedrink"/>, each passing its own verb.
        /// </summary>
        public ValidationResult ValidateInteraction(
            string initiator, string recipient, string identifier, string typeKey)
        {
            var baseValidation = ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid) return baseValidation;

            string drinker = ResolveDrinker(typeKey, initiator, recipient);
            string source = ResolveSource(typeKey, initiator, recipient);

            var lockFailure = CheckDrawLock(drinker, source);
            if (lockFailure != null) return ValidationResult.Failure(lockFailure);

            string breakBlock = SubstanceBodyparts.CheckBreakBlock(
                Database, source, identifier, DrinkBreakBlockTail);
            if (breakBlock != null) return ValidationResult.Failure(breakBlock);

            return ValidationResult.Success();
        }

        /// <summary>
        /// The once-per-day draw lock, refused in the flavor of whichever verb spent it — an
        /// earlier <c>!milk</c> against the same source blocks this and says so.
        /// </summary>
        private string CheckDrawLock(string drinker, string source)
        {
            Profile drinkerProfile = Database.GetProfile(drinker);
            DrawVerb? heldBy = SourceDrawLock.ActiveLock(drinkerProfile, source);
            if (heldBy == null) return null;

            Profile sourceProfile = Database.GetProfile(source);
            string sourceDisplay = sourceProfile?.displayName ?? source;
            return SourceDrawLock.LockMessage(heldBy.Value, sourceDisplay);
        }

        // -------------------------------------------------------------------
        // Processing
        // -------------------------------------------------------------------

        public override string ProcessInteraction(PendingCommand command)
        {
            _lastOutcome = null;

            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string typeKey = command.pendingInteraction.type;
            string substance = command.pendingInteraction.identifier;

            string drinker = ResolveDrinker(typeKey, initiator, recipient);
            string source = ResolveSource(typeKey, initiator, recipient);

            // TOCTOU recheck. ChateauConsent re-validates before calling us, but only through
            // the role-agnostic three-argument overload — the lock and the break gate can both
            // have landed between the command and the consent and would not be caught there.
            // On either, do nothing at all and suppress the channel message, telling the
            // initiator privately why their interaction evaporated.
            string blocked = CheckDrawLock(drinker, source)
                ?? SubstanceBodyparts.CheckBreakBlock(Database, source, substance, DrinkBreakBlockTail);
            if (blocked != null)
            {
                _lastInitiatorPrivateMessage = blocked;
                Database.DeletePendingCommand(command.Id);
                return typeKey;
            }

            Profile drinkerProfile = Database.GetProfile(drinker);
            Profile sourceProfile = Database.GetProfile(source);

            // The tag comes off the source's corruption right now, at the same thresholds a
            // fresh milking uses — fresher is a bigger push, not a wider net, so a
            // neutral-corruption source still pours something that shifts nothing.
            string corruptionTag = ChateauCurrency.GetCorruptionTagForValue(
                Commitment.CorruptionProcessor.ReadCorruption(sourceProfile));

            var outcome = new Outcome
            {
                TypedVerb = typeKey,
                DrinkerUser = drinker,
                SourceUser = source,
                DrinkerDisplay = DisplayOf(drinkerProfile, drinker),
                SourceDisplay = DisplayOf(sourceProfile, source),
                Substance = substance,
                CorruptionTag = corruptionTag,
            };

            outcome.Corruption = ChateauDrink.ApplyDrinkCorruption(
                drinkerProfile, corruptionTag, ChateauCurrency.SourceDrinkCorruptionShift);
            outcome.Addiction = ChateauDrink.ApplyDrinkAddictionRoll(
                drinkerProfile, substance, ChateauCurrency.SourceDrinkAddictionChance, Rng);

            // The draw lock is shared with !milk but stamped under this verb's own key, so a
            // later !milk refusal can name drinking rather than claiming a milking.
            SourceDrawLock.Stamp(drinkerProfile, DrawVerb.DrinkFrom, source);

            Database.AddInteraction(command.pendingInteraction);

            // Persist BEFORE the count helper: it does its own read-modify-write on the DB
            // record and would otherwise stomp the corruption, vice and timer changes above.
            // (Same ordering trap as MilkProcessor and ClimaxforProcessor.)
            Database.SetProfile(drinker, drinkerProfile);

            // Counts follow the roles, not who typed the command.
            string initiatorLabel = string.Equals(initiator, drinker, StringComparison.Ordinal)
                ? "drinkfromgive"
                : "drinkfromtake";
            string recipientLabel = string.Equals(recipient, drinker, StringComparison.Ordinal)
                ? "drinkfromgive"
                : "drinkfromtake";
            // No rate limit: the per-direction daily lock is a far tighter gate than the
            // 30-minute involved limit would be, so applying both would only confuse.
            IncrementDifferentCounts(initiator, recipient, initiatorLabel, recipientLabel);

            outcome.Message = BuildChannelMessage(outcome);
            _lastOutcome = outcome;

            Database.DeletePendingCommand(command.Id);

            return typeKey;
        }

        private static string DisplayOf(Profile profile, string fallbackUser)
        {
            if (profile == null) return fallbackUser;
            return string.IsNullOrEmpty(profile.displayName) ? profile.userName : profile.displayName;
        }

        // -------------------------------------------------------------------
        // Messages
        // -------------------------------------------------------------------

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            var outcome = _lastOutcome;
            if (outcome == null || string.IsNullOrEmpty(outcome.Message))
            {
                // Suppressed path (lock or break landed between command and consent). Returning
                // empty also stops the base appending status fragments to nothing.
                return string.Empty;
            }

            // Drain the message but keep the roles: GetStatusEffectSubject runs after this in
            // the same wrapper call and still needs to know who drank.
            string message = outcome.Message;
            outcome.Message = string.Empty;
            return message;
        }

        /// <summary>
        /// Closing flavor for the opening sentence. <c>{source}</c> and <c>{drinker}</c> are
        /// substituted with the two display names, so a flourish can address either party by
        /// name rather than leaning on a pronoun that would not say which one it meant.
        /// </summary>
        private static readonly string[] ClosingFlourishes = new[]
        {
            "Fresher than anything bottled!",
            "Is {source} tasty?",
            "How does it taste, {drinker}?",
            "Careful you don't drain them dry!",
        };

        private string BuildChannelMessage(Outcome outcome)
        {
            string substanceText = Utils.SubstanceToText(
                outcome.Substance, Database?.GetIdentifier(outcome.Substance));

            string flourish = ClosingFlourishes[Rng.Next(ClosingFlourishes.Length)]
                .Replace("{source}", outcome.SourceDisplay)
                .Replace("{drinker}", outcome.DrinkerDisplay);

            string message = outcome.DrinkerDisplay + " drinks their fill of " + substanceText
                + " straight from " + outcome.SourceDisplay + ". " + flourish;

            string corruptionFragment = BuildCorruptionFragment(outcome);
            if (!string.IsNullOrEmpty(corruptionFragment)) message += " " + corruptionFragment;

            if (outcome.Addiction.Intensified)
            {
                string levelNote = outcome.Addiction.AddictionLevel > 0
                    ? " [sub](addiction " + outcome.Addiction.AddictionLevel + "/"
                        + Consequence.DoseProcessor.MaxAddictionLevel + ")[/sub]"
                    : string.Empty;
                message += " ...and " + outcome.DrinkerDisplay
                    + " finds themself wanting more already." + levelNote;
            }

            return message;
        }

        /// <summary>
        /// Two halves, the same shape <c>!drink</c> uses: the taste always lands, and the
        /// mechanical half is what the shared daily budget can take away.
        /// </summary>
        private static string BuildCorruptionFragment(Outcome outcome)
        {
            bool corrupt = string.Equals(
                outcome.CorruptionTag, ChateauCurrency.CorruptTag, StringComparison.OrdinalIgnoreCase);
            bool pure = string.Equals(
                outcome.CorruptionTag, ChateauCurrency.PurifiedTag, StringComparison.OrdinalIgnoreCase);
            if (!corrupt && !pure) return string.Empty;

            string taste = corrupt
                ? "Dark and rich, and still warm~"
                : "Light and bright, and still warm!";

            if (outcome.Corruption.BudgetSpent)
            {
                // !drink's wording, with the drinker named: that command has one party, this one
                // has two, so "they" would not say which.
                return taste + " No effect though... maybe tomorrow " + outcome.DrinkerDisplay
                    + " will be able to metabolize a bit more.";
            }

            return taste + " " + outcome.DrinkerDisplay + " gains "
                + Math.Abs(outcome.Corruption.Applied) + (corrupt ? " corruption." : " purity.");
        }

        /// <summary>
        /// Status fragments belong to the drinker — the person the substance is acting on —
        /// which is the initiator on <c>!drinkfrom</c> and the recipient on <c>!forcedrink</c>.
        /// Falls back to the recipient when there is no stashed outcome to read the roles from.
        /// </summary>
        protected override Profile GetStatusEffectSubject(
            Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            var outcome = _lastOutcome;
            if (outcome == null || initiatorProfile == null || recipientProfile == null)
            {
                return recipientProfile;
            }
            return string.Equals(outcome.DrinkerUser, initiatorProfile.userName, StringComparison.Ordinal)
                ? initiatorProfile
                : recipientProfile;
        }

        /// <summary>
        /// The custom <c>!seteicon drinkfrom</c> icon belongs to the drinker, the same
        /// redirection <see cref="GetStatusEffectSubject"/> makes — and here the typed verb is
        /// handed to us directly, so no stash is needed.
        /// </summary>
        protected override Profile GetEiconSubject(
            string interactionVerb, Profile initiatorProfile, Profile recipientProfile)
        {
            if (initiatorProfile == null || recipientProfile == null) return initiatorProfile;
            return IsForceDrink(interactionVerb) ? recipientProfile : initiatorProfile;
        }

        // -------------------------------------------------------------------
        // Consent
        // -------------------------------------------------------------------

        /// <summary>
        /// Untyped entry point. The command layer knows which verb was typed and should call
        /// <see cref="GetConsentWarning(Profile, Profile, string, string)"/> instead; this
        /// defaults to the <c>!drinkfrom</c> wording for any caller that doesn't.
        /// </summary>
        protected override string BuildConsentWarning(
            Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return BuildConsentWarning(initiatorProfile, recipientProfile, identifier, DrinkFromType);
        }

        /// <summary>
        /// Typed overload of <see cref="InteractionProcessorBase.GetConsentWarning"/> — the two
        /// verbs ask opposite questions of opposite people. Applies the same <c>(or !no)</c>
        /// reminder the untyped entry point does.
        /// </summary>
        public string GetConsentWarning(
            Profile initiatorProfile, Profile recipientProfile, string identifier, string typeKey)
        {
            return ConsentWarningText.WithDeclineHint(
                BuildConsentWarning(initiatorProfile, recipientProfile, identifier, typeKey));
        }

        private string BuildConsentWarning(
            Profile initiatorProfile, Profile recipientProfile, string identifier, string typeKey)
        {
            string initName = initiatorProfile?.displayName ?? "Someone";
            string recName = recipientProfile?.displayName ?? "you";
            string substanceText = Utils.SubstanceToText(identifier, Database?.GetIdentifier(identifier));

            if (IsForceDrink(typeKey))
            {
                // Asked of the drinker. "Force" is flavor, exactly as in !breed and !corrupt —
                // the recipient is being asked, and the wording must not suggest otherwise.
                // Named rather than pronouns: both parties appear in the sentence, so "them"
                // would not say which one is being drunk from.
                return initName + " is offering a taste of " + substanceText
                    + ", straight from the source." + SourceProjection(initiatorProfile, substanceText)
                    + " Do you !consent to drinking from " + initName + "?";
            }

            // Asked of the source.
            return initName + " wants to drink " + substanceText + " straight from "
                + recName + "." + SourceProjection(recipientProfile, substanceText)
                + " Do you !consent to being slurped?";
        }

        /// <summary>
        /// Project what the source's current corruption will put in the drink, the way
        /// <see cref="MilkProcessor.BuildConsentWarning"/> projects the tag onto its bottles —
        /// the source's corruption is exactly what the drinker is signing up for.
        /// </summary>
        private static string SourceProjection(Profile sourceProfile, string substanceText)
        {
            if (sourceProfile == null) return string.Empty;
            string tag = ChateauCurrency.GetCorruptionTagForValue(
                Commitment.CorruptionProcessor.ReadCorruption(sourceProfile));
            string sourceName = sourceProfile.displayName ?? sourceProfile.userName;

            if (tag == ChateauCurrency.CorruptTag)
            {
                return " " + sourceName + "'s " + substanceText + " is going to be quite [b]corrupt[/b].";
            }
            if (tag == ChateauCurrency.PurifiedTag)
            {
                return " " + sourceName + "'s " + substanceText + " is going to be quite [b]pure[/b].";
            }
            return string.Empty;
        }

        /// <summary>Refusal for drinking from yourself, pointing at the command that does work.</summary>
        public const string SelfTargetText =
            "You can't drink from yourself. If you want to drink something you're already holding, use !drink.";
    }
}
