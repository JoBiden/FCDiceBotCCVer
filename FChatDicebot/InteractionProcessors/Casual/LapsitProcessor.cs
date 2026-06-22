using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace FChatDicebot.InteractionProcessors.Casual
{
    /// <summary>
    /// Backs both <c>!lap</c> (initiator is the lap — the one being sat on) and <c>!sit</c>
    /// (initiator is the sitter, the recipient is the lap). The same instance is registered
    /// under both type keys in <see cref="InteractionProcessorRegistry"/>, the way
    /// <c>!corrupt</c>/<c>!purify</c> share one processor.
    ///
    /// This is the single-target (1:1) form of lapsit. The full multi-person "lap stack"
    /// (3+ participants, per-position counts) needs the group-interaction infrastructure
    /// and is intentionally out of scope here.
    ///
    /// Count semantics follow the project-wide give/take convention: the <b>sitter</b>
    /// (topmost) gets <c>lapsitgive</c> ("they sat on someone"), the <b>lap</b> (bottom)
    /// gets <c>lapsittake</c> ("they were sat on"). Which side the initiator plays depends
    /// on the typed verb, carried on <see cref="Interaction.type"/> and mirrored onto the
    /// <see cref="Interaction.identifier"/> so <see cref="GetCompletionMessage"/> (which only
    /// receives the identifier) can render the right opener.
    /// </summary>
    public class LapsitProcessor : InteractionProcessorBase
    {
        public const string LapType = "lap";
        public const string SitType = "sit";

        public override string InteractionType => LapType;
        public override string InvestmentLevel => "casual";

        private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Constructor for dependency injection (for testing)
        /// </summary>
        public LapsitProcessor(IChateauDatabase database) : base(database)
        {
        }

        /// <summary>
        /// Legacy constructor for backward compatibility
        /// </summary>
        public LapsitProcessor() : base()
        {
        }

        public override string GetInteractionVerb(VerbTense tense)
        {
            switch (tense)
            {
                case VerbTense.Past:    return "sat on";
                case VerbTense.Present: return "sits on";
                case VerbTense.Future:  return "will sit on";
                default:                return "lapsit";
            }
        }

        /// <summary>
        /// True when the typed verb makes the initiator the sitter (top), i.e. <c>!sit</c>.
        /// Defaults to false (<c>!lap</c>: initiator is the lap) for any unrecognized verb.
        /// </summary>
        public static bool InitiatorIsSitter(string verb)
        {
            return string.Equals(verb, SitType, StringComparison.OrdinalIgnoreCase);
        }

        public override string ProcessInteraction(PendingCommand command)
        {
            string initiator = command.pendingInteraction.initiator;
            string recipient = command.pendingInteraction.recipient;
            string verb = command.pendingInteraction.type;

            // Save the interaction to history
            Database.AddInteraction(command.pendingInteraction);

            // Sitter (top) → lapsitgive; lap (bottom) → lapsittake. The typed verb decides
            // which side the initiator is on.
            string initiatorLabel = InitiatorIsSitter(verb) ? "lapsitgive" : "lapsittake";
            string recipientLabel = InitiatorIsSitter(verb) ? "lapsittake" : "lapsitgive";

            _lastRateLimitMessage = IncrementDifferentCountsWithRateLimit(
                initiator, recipient, initiatorLabel, recipientLabel, RateLimit);

            // Remove pending interaction
            Database.DeletePendingCommand(command.Id);

            return verb;
        }

        public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            bool initiatorIsSitter = InitiatorIsSitter(identifier);

            // {lapsitgiver} = the topmost sitter (whoever gets lapsitgive): the initiator on
            // !sit, the recipient on !lap.
            string lapsitGiver = initiatorIsSitter
                ? initiatorProfile.displayName
                : recipientProfile.displayName;

            // Owner-provided descriptors (shared by !lap and !sit).
            var lapsitDescriptors = new List<string>
            {
                "Does it count as a stack if it's only two?",
                "Lap! Lap! Lap!",
                "We have normal chairs too, in case you didn't know...",
                "That means {lapsitgiver} is on top, literally.",
                "Always nice to have some support."
            };

            string descriptor = GetRandomDescriptor(lapsitDescriptors)
                .Replace("{lapsitgiver}", lapsitGiver);

            string opener;
            if (initiatorIsSitter)
            {
                // !sit: the initiator sits on the recipient (the bottom/lap).
                var sitOpeners = new List<string>
                {
                    $"{initiatorProfile.displayName} uses {recipientProfile.displayName} as a comfy lap to sit on.",
                    $"{initiatorProfile.displayName} pulls themselves onto {recipientProfile.displayName}'s lap."
                };
                opener = GetRandomDescriptor(sitOpeners);
            }
            else
            {
                // !lap: the initiator is the lap and pulls the recipient onto it.
                opener = $"{initiatorProfile.displayName} pulls {recipientProfile.displayName} onto their lap.";
            }
            // Special handling for Queen Contract and The Corrupted Rin
            if (lapsitGiver == "The Corrupted Rin" && (recipientProfile.userName == "Queen Contract" || initiatorProfile.userName == "Queen Contract"))
            {
                descriptor += " [eicon]rin_lap[/eicon]";
            }

            return $"{opener} {descriptor}";
        }

        public override string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
        {
            if (InitiatorIsSitter(identifier))
            {
                return initiatorProfile.displayName + " wants to sit on " + recipientProfile.displayName + "'s lap. Do you !consent to being their seat?";
            }
            return initiatorProfile.displayName + " wants to pull " + recipientProfile.displayName + " onto their lap. Do you !consent to taking a seat?";
        }
    }
}
