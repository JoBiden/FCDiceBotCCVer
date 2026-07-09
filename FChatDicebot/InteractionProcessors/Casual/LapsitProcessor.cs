using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

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

        // Lapsit is group-capable with the special per-position rule; the lap stack is
        // assembled and credited in the group overrides below.
        public override GroupSpec GroupSpec => GroupSpec.LapStack();

        // Owner-provided descriptors (shared by !lap and !sit, 1:1 and group).
        private static readonly List<string> LapsitDescriptors = new List<string>
        {
            "Does it count as a stack if it's only two?",
            "Lap! Lap! Lap!",
            "We have normal chairs too, in case you didn't know...",
            "That means {lapsitgiver} is on top, literally.",
            "Always nice to have some support."
        };

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

            string descriptor = GetRandomDescriptor(LapsitDescriptors)
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

        public override string GetGroupConsentWarning(Profile initiatorProfile, IReadOnlyList<Profile> recipients, string identifier)
        {
            string names = JoinNamesSerial(recipients.Select(p => p.displayName).ToList());
            if (InitiatorIsSitter(identifier))
            {
                // !sit: the first to consent becomes the bottom; the rest pile on above.
                return initiatorProfile.displayName + " wants to start a lap stack with " + names + ". The first to !consent gets to be under " + initiatorProfile.displayName + "!";
            }
            return initiatorProfile.displayName + " starts a lap stack, looking to pull " + names + " onto their lap one at a time. Do you each !consent to taking a seat?";
        }

        /// <summary>
        /// Per-position lapsit counts over the consented stack (B7.12). Position k (0 = bottom)
        /// gets +(M-1-k) lapsittake (people sitting on them) and +k lapsitgive (people they're
        /// sitting on). The verb on <paramref name="identifier"/> decides who claims the
        /// bottom; see <see cref="BuildStack{T}"/>.
        /// </summary>
        public override string ApplyGroupCounts(IChateauDatabase database, string initiator, IReadOnlyList<string> consentersInOrder, string identifier)
        {
            var stack = BuildStack(identifier, initiator, consentersInOrder); // bottom -> top
            int stackSize = stack.Count; // M
            var limited = new List<string>();

            for (int position = 0; position < stackSize; position++)
            {
                ApplyGroupIncrement(database, stack[position], "lapsittake", stackSize - 1 - position, limited);
                ApplyGroupIncrement(database, stack[position], "lapsitgive", position, limited);
            }

            return BuildGroupRateLimitNote(limited);
        }

        /// <summary>
        /// Per-position lapsit titles: each participant earns "below" titles for the number of
        /// people stacked under them and "above" titles for the number stacked over them, so a
        /// mid-stack rider can earn both. Mirrors the per-position count split in
        /// <see cref="ApplyGroupCounts"/>; the verb on <paramref name="identifier"/> decides who
        /// claims the bottom (see <see cref="BuildStack{T}"/>).
        /// </summary>
        public override List<GroupTitleGrant> GrantGroupTitles(IChateauDatabase database, string initiator, IReadOnlyList<string> consentersInOrder, string identifier)
        {
            var grants = new List<GroupTitleGrant>();
            var stack = BuildStack(identifier, initiator, consentersInOrder); // bottom -> top
            int stackSize = stack.Count; // M

            for (int position = 0; position < stackSize; position++)
            {
                int othersBelow = position;
                int othersAbove = stackSize - 1 - position;
                var titles = ChateauSystemTitles.GetLapsitPositionTitles(othersBelow, othersAbove);
                AddTitleGrant(database, stack[position], titles, grants);
            }

            return grants;
        }

        public override string GetGroupCompletionMessage(Profile initiatorProfile, IReadOnlyList<Profile> consentersInOrder, string identifier)
        {
            bool initiatorIsSitter = InitiatorIsSitter(identifier);
            var stack = BuildStack(identifier, initiatorProfile, consentersInOrder); // bottom -> top
            int stackSize = stack.Count; // M

            // Opener forms the bottom pair (positions 0 and 1).
            string opener;
            if (initiatorIsSitter)
            {
                // !sit: the initiator sits on whoever claimed the bottom (stack[0]).
                var sitOpeners = new List<string>
                {
                    $"{initiatorProfile.displayName} uses {stack[0].displayName} as a comfy lap to sit on.",
                    $"{initiatorProfile.displayName} pulls themselves onto {stack[0].displayName}'s lap."
                };
                opener = GetRandomDescriptor(sitOpeners);
            }
            else
            {
                // !lap: the initiator is the bottom and pulls the first sitter (stack[1]) on.
                opener = $"{initiatorProfile.displayName} pulls {stack[1].displayName} onto their lap.";
            }

            string message = opener;

            // Group extension (stack >= 3): each person above the opening pair, in stack order.
            if (stackSize >= 3)
            {
                var above = stack.Skip(2).Select(p => p.displayName).ToList();
                string tail = " Then " + above[0] + " takes a seat";
                for (int i = 1; i < above.Count; i++)
                {
                    tail += ", then " + above[i];
                }
                tail += $", forming a lap stack {stackSize} people tall!";
                message += tail;
            }

            // {lapsitgiver} = the topmost sitter (the most lapsitgive), i.e. the top of stack.
            string topmost = stack[stackSize - 1].displayName;
            string descriptor = GetRandomDescriptor(LapsitDescriptors).Replace("{lapsitgiver}", topmost);

            // Special handling for Queen Contract and The Corrupted Rin.
            if (topmost == "The Corrupted Rin" && stack.Any(p => p.userName == "Queen Contract"))
            {
                descriptor += " [eicon]rin_lap[/eicon]";
            }

            return $"{message} {descriptor}";
        }

        /// <summary>
        /// Per-position custom eicons for a resolved lap stack, rendered as a vertical "totem
        /// pole" — one figure per line, topmost sitter first — so the icons read like the
        /// physical stack (top of the pile sits on the top line). The bottom (position 0) is a
        /// pure lap, so it shows their <c>!lap</c> eicon; everyone stacked above (middle riders
        /// and the top) shows their <c>!sit</c> eicon — a mid-stack rider is doing both, and the
        /// owner prefers the sit icon there.
        ///
        /// Unlike every other group interaction, the lap stack fills empty slots: a participant
        /// who hasn't set the relevant eicon falls back to their character icon
        /// (<c>[icon]{userName}[/icon]</c>) so the pole always carries one figure per person and
        /// the stack stays visually intact.
        /// </summary>
        public override string GetGroupEiconSuffix(string interactionVerb, Profile initiatorProfile, IReadOnlyList<Profile> consentersInOrder)
        {
            string verb = string.IsNullOrEmpty(interactionVerb) ? InteractionType : interactionVerb;
            var stack = BuildStack(verb, initiatorProfile, consentersInOrder); // bottom -> top

            // Walk top -> bottom so the topmost sitter lands on the top line of the pole.
            var figures = new List<string>();
            for (int position = stack.Count - 1; position >= 0; position--)
            {
                string roleVerb = position == 0 ? LapType : SitType;
                figures.Add(GetStackFigure(stack[position], roleVerb));
            }

            if (figures.Count == 0) return string.Empty;
            // One figure per line, stacked beneath the completion sentence.
            return "\n" + string.Join("\n", figures);
        }

        /// <summary>
        /// The totem figure for one stack member: their custom eicon for
        /// <paramref name="roleVerb"/> if set, otherwise their F-List character icon so an unset
        /// slot still holds the pole together. Mirrors <c>Utils.GetCharacterIconTags</c>; kept
        /// inline to match this file's other literal tag flourishes (e.g. the rin_lap eicon).
        /// </summary>
        private static string GetStackFigure(Profile profile, string roleVerb)
        {
            string eicon = InteractionEiconSupport.GetInteractionEicon(profile, roleVerb);
            if (!string.IsNullOrEmpty(eicon)) return eicon;
            return "[icon]" + (profile?.userName ?? string.Empty) + "[/icon]";
        }

        /// <summary>
        /// Assemble the lap stack bottom -> top from the typed verb and the consenters in
        /// consent order. <c>!lap</c>: initiator is the bottom, consenters stack above in
        /// order. <c>!sit</c>: the first consenter claims the open bottom, the initiator sits
        /// at position 1, remaining consenters stack above in order.
        /// </summary>
        private static List<T> BuildStack<T>(string verb, T initiator, IReadOnlyList<T> consentersInOrder)
        {
            var stack = new List<T>();
            if (InitiatorIsSitter(verb))
            {
                if (consentersInOrder.Count > 0)
                {
                    stack.Add(consentersInOrder[0]);  // bottom
                    stack.Add(initiator);             // position 1
                    for (int i = 1; i < consentersInOrder.Count; i++)
                        stack.Add(consentersInOrder[i]);
                }
                else
                {
                    // No consenters (shouldn't resolve) — fall back to initiator alone.
                    stack.Add(initiator);
                }
            }
            else
            {
                stack.Add(initiator); // bottom
                foreach (var consenter in consentersInOrder)
                    stack.Add(consenter);
            }
            return stack;
        }
    }
}
