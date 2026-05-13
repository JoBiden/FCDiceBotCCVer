using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.InteractionProcessors.Commitment
{
    /// <summary>
    /// Processor for the breed interaction - implants the recipient with a new pregnancy
    /// of the specified monster type. Pregnancies gestate for a monster-defined number of
    /// days, then can be released via !birth (a self-action). Multiple concurrent
    /// pregnancies are supported.
    /// </summary>
    public class BreedProcessor : InteractionProcessorBase
    {
        public const int MaxGestationDays = 7;
        public const int DefaultGestationDays = 1;
        public const int DefaultBroodSize = 1;

        public override string InteractionType => "breed";
        public override string InvestmentLevel => "commitment";

        public BreedProcessor(IChateauDatabase database) : base(database)
        {
        }

        public BreedProcessor() : base()
        {
        }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:
                    return "bred";
                case VerbTense.Present:
                    return "breeds";
                case VerbTense.Future:
                    return "will breed";
                default:
                    return "breed";
            }
        }

        public override ValidationResult ValidateInteraction(string initiator, string recipient, string identifier)
        {
            var baseValidation = base.ValidateInteraction(initiator, recipient, identifier);
            if (!baseValidation.IsValid)
            {
                return baseValidation;
            }

            if (string.IsNullOrEmpty(identifier))
            {
                return ValidationResult.Failure(ChateauInteractionHandler.typeNotFoundText("monster"));
            }

            Identifier monsterIdentifier = Database.GetIdentifier(identifier);
            if (monsterIdentifier == null)
            {
                return ValidationResult.Failure(ChateauInteractionHandler.notFoundText(identifier));
            }

            return ValidationResult.Success();
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string monsterType = command.pendingInteraction.identifier;

            Database.AddInteraction(command.pendingInteraction);

            Profile initiatorProfile = Database.GetProfile(initiator);
            Profile recipientProfile = Database.GetProfile(recipient);

            int gestationDays = DefaultGestationDays;
            int broodSize = DefaultBroodSize;
            Identifier monsterIdentifier = Database.GetIdentifier(monsterType);
            if (monsterIdentifier != null)
            {
                gestationDays = ClampGestation(monsterIdentifier.gestationDays);
                broodSize = RollBroodSize(monsterIdentifier);
            }

            DateTime now = DateTime.UtcNow;
            Pregnancy pregnancy = new Pregnancy
            {
                Id = Guid.NewGuid().ToString("N"),
                Initiator = initiator,
                MonsterType = monsterType,
                ConceivedAt = now,
                ReadyAt = now.AddDays(gestationDays),
                BroodSize = broodSize
            };

            if (recipientProfile.pregnancies == null)
            {
                recipientProfile.pregnancies = new List<Pregnancy>();
            }
            recipientProfile.pregnancies.Add(pregnancy);

            CoolDown pairTimer = new CoolDown { timerEnd = now.AddDays(1) };
            initiatorProfile.timers[PairTimerKey(recipient)] = pairTimer;
            recipientProfile.timers[PairTimerKey(initiator)] = pairTimer;

            Database.SetProfile(initiator, initiatorProfile);
            Database.SetProfile(recipient, recipientProfile);

            Database.DeletePendingCommand(command.Id);

            return "breed";
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            return initiatorProfile.displayName + " has bred " + recipientProfile.displayName
                + " with new " + identifier + " life! "
                + recipientProfile.displayName + " will carry the pregnancy until they're ready to !birth their brood.";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            int existingPregnancies = recipientProfile.pregnancies != null ? recipientProfile.pregnancies.Count : 0;
            string pregnancyCountText = existingPregnancies > 0
                ? " (" + recipientProfile.displayName + " is already carrying " + existingPregnancies + " other "
                    + (existingPregnancies == 1 ? "pregnancy" : "pregnancies") + ".)"
                : string.Empty;

            return initiatorProfile.displayName + " wants to breed " + recipientProfile.displayName
                + " with new " + identifier + " life! [b]This should not be taken lightly, and can not be done frequently.[/b]"
                + pregnancyCountText
                + " Do you !consent to being bred?";
        }

        public static string PairTimerKey(string otherUser)
        {
            return "breed_pair_" + otherUser;
        }

        private static int ClampGestation(int rawDays)
        {
            if (rawDays < 1) return DefaultGestationDays;
            if (rawDays > MaxGestationDays) return MaxGestationDays;
            return rawDays;
        }

        private static int RollBroodSize(Identifier monsterIdentifier)
        {
            int min = monsterIdentifier.broodSizeMin > 0 ? monsterIdentifier.broodSizeMin : DefaultBroodSize;
            int max = monsterIdentifier.broodSizeMax >= min ? monsterIdentifier.broodSizeMax : min;
            if (min == max) return min;
            return new Random().Next(min, max + 1);
        }
    }
}
