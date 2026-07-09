using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.InteractionProcessors
{
    /// <summary>
    /// Coordinates the hybrid group flow (B4): each recipient consents to their own seat with
    /// plain <c>!consent</c>, but the shared "moment" only fires once no <c>Pending</c> seat
    /// remains. Resolution gathers whoever consented, applies the group count math through the
    /// processor, emits one combined completion message, and clears every seat of the group.
    ///
    /// Lives outside <c>FChatDicebot.BotCommands</c> so the reflection-based command loader
    /// doesn't pick it up. Stateless and database-injected so it can be unit-tested against the
    /// same <see cref="IChateauDatabase"/> the processors use; title-granting is intentionally
    /// left to the command layer (which runs through MonDB) — the resolver only returns the
    /// participant set so the caller can check titles.
    /// </summary>
    public static class GroupInteractionResolver
    {
        // Mirrors the 10-minute window ChateauConsent sweeps with.
        public const int PendingMinutesKeep = 10;

        /// <summary>
        /// Stamp a group seat as consented: assign the next consent-order number for its group
        /// and flip its state. Caller is expected to follow up with
        /// <see cref="CheckAndResolve"/> for the same groupId.
        /// </summary>
        public static void MarkSeatConsented(IChateauDatabase database, PendingCommand seat)
        {
            var siblings = database.GetPendingCommandsByGroupId(seat.groupId);
            int nextOrder = siblings
                .Where(s => s.HasConsented)
                .Select(s => s.consentedOrder)
                .DefaultIfEmpty(0)
                .Max() + 1;

            seat.consentState = PendingCommand.ConsentedState;
            seat.consentedOrder = nextOrder;
            database.UpdatePendingCommand(seat);
        }

        /// <summary>
        /// Resolve the group if it's ready. Expired un-consented seats are swept first (lazy
        /// expiry — there is no background timer), then:
        ///   - if any non-expired seat is still Pending, nothing happens (still waiting);
        ///   - if every remaining seat has consented, the moment fires with that set;
        ///   - if no seats remain at all (everyone refused/expired), the group dies silently.
        /// </summary>
        public static GroupResolutionResult CheckAndResolve(IChateauDatabase database, string groupId)
        {
            var result = new GroupResolutionResult();
            if (string.IsNullOrEmpty(groupId)) return result;

            var seats = database.GetPendingCommandsByGroupId(groupId);
            if (seats.Count == 0) return result;

            // Sweep expired, never-consented seats so a non-responding member can't wedge the
            // group past the 10-minute mark.
            DateTime cutoff = DateTime.UtcNow.AddMinutes(-PendingMinutesKeep);
            var liveSeats = new List<PendingCommand>();
            foreach (var seat in seats)
            {
                if (!seat.HasConsented && seat.startTime.CompareTo(cutoff) < 0)
                {
                    database.DeletePendingCommand(seat.Id);
                }
                else
                {
                    liveSeats.Add(seat);
                }
            }

            if (liveSeats.Count == 0) return result;                  // silent expiry
            if (liveSeats.Any(s => !s.HasConsented)) return result;   // still waiting on a seat

            // Every remaining seat consented → fire with whoever consented.
            var consented = liveSeats.OrderBy(s => s.consentedOrder).ToList();
            var anySeat = consented[0];
            string initiator = anySeat.pendingInteraction.initiator;
            string type = anySeat.pendingInteraction.type;
            string identifier = anySeat.pendingInteraction.identifier;
            string investmentLevel = anySeat.pendingInteraction.investmentLevel;

            var processor = InteractionProcessorRegistry.GetProcessor(type);
            if (processor == null)
            {
                // No processor (shouldn't happen for a casual) — clear the seats and bail.
                DeleteAllSeats(database, groupId);
                return result;
            }

            // Enforcement spine (H2), group side: a seat that was fine at request time can
            // have picked up a break/curse blocker (or any other ValidateInteraction failure)
            // by the time the group actually resolves. Drop those seats from the moment
            // instead of force-processing them; the command layer privately notifies each
            // dropped participant with the reason (this helper stays BotMain-free).
            var validatedSeats = new List<PendingCommand>();
            foreach (var seat in consented)
            {
                var seatValidation = processor.ValidateInteraction(initiator, seat.awaitingConsentFrom, identifier);
                if (!seatValidation.IsValid)
                {
                    result.Dropped.Add(new GroupValidationDrop { Participant = seat.awaitingConsentFrom, Reason = seatValidation.ErrorMessage });
                    continue;
                }
                validatedSeats.Add(seat);
            }

            if (validatedSeats.Count == 0)
            {
                // Everyone who consented got blocked between request and resolution — the
                // moment never happens, but every seat still needs clearing.
                DeleteAllSeats(database, groupId);
                return result;
            }

            consented = validatedSeats;
            var consenterNames = consented.Select(s => s.awaitingConsentFrom).ToList();

            // History: record one interaction per consenting recipient, mirroring N
            // independent 1:1 records so statistics / type counts stay consistent.
            DateTime now = DateTime.UtcNow;
            foreach (var recipient in consenterNames)
            {
                database.AddInteraction(new Interaction
                {
                    initiator = initiator,
                    recipient = recipient,
                    type = type,
                    identifier = identifier,
                    investmentLevel = investmentLevel,
                    interactionTime = now
                });
            }

            // Counts (processor owns the symmetric / directional / lapsit math + the +N helper).
            string rateLimitNote = processor.ApplyGroupCounts(database, initiator, consenterNames, identifier);

            // Group-achievement titles, by resolved size (symmetric: everyone; directional:
            // the initiator) or lap-stack position. Granting runs through the injected database
            // so it stays on the same store and unit-testable; the command layer formats the
            // per-participant "Title Time!" notifications from the returned grants.
            result.GroupTitleGrants = processor.GrantGroupTitles(database, initiator, consenterNames, identifier);

            // Combined completion message.
            Profile initiatorProfile = database.GetProfile(initiator);
            var consenterProfiles = consenterNames.Select(database.GetProfile).ToList();
            string message = processor.GetGroupCompletionMessage(initiatorProfile, consenterProfiles, identifier);
            // Custom !seteicon flourish (keyed to the typed verb), before the rate-limit sub-note.
            message += processor.GetGroupEiconSuffix(type, initiatorProfile, consenterProfiles);
            if (!string.IsNullOrEmpty(rateLimitNote))
            {
                message += rateLimitNote;
            }

            DeleteAllSeats(database, groupId);

            result.Resolved = true;
            result.ChannelMessage = message;
            result.Participants.Add(initiator);
            result.Participants.AddRange(consenterNames);
            return result;
        }

        private static void DeleteAllSeats(IChateauDatabase database, string groupId)
        {
            foreach (var seat in database.GetPendingCommandsByGroupId(groupId))
            {
                database.DeletePendingCommand(seat.Id);
            }
        }
    }

    /// <summary>
    /// Outcome of a <see cref="GroupInteractionResolver.CheckAndResolve"/> call.
    /// </summary>
    public class GroupResolutionResult
    {
        /// <summary>True when the group fired this call (counts applied, seats cleared).</summary>
        public bool Resolved { get; set; }

        /// <summary>Combined completion message to post in the channel (empty if not resolved).</summary>
        public string ChannelMessage { get; set; } = string.Empty;

        /// <summary>Initiator + consenting recipients — for the caller's title checks.</summary>
        public List<string> Participants { get; set; } = new List<string>();

        /// <summary>
        /// Group-achievement titles granted this resolution (by size / lap-stack position),
        /// one entry per participant who earned at least one new title. Already persisted to
        /// the database during resolution; the command layer only formats the notifications.
        /// </summary>
        public List<GroupTitleGrant> GroupTitleGrants { get; set; } = new List<GroupTitleGrant>();

        /// <summary>
        /// Consented seats that failed <c>ValidateInteraction</c> at resolution time (e.g. a
        /// break/curse blocker landed between request and resolution) and were excluded from
        /// the moment. The command layer privately notifies each of these — this list is
        /// populated even when the group as a whole did not resolve (everyone dropped).
        /// </summary>
        public List<GroupValidationDrop> Dropped { get; set; } = new List<GroupValidationDrop>();
    }

    /// <summary>One participant dropped from a group resolution by a failed ValidateInteraction.</summary>
    public class GroupValidationDrop
    {
        public string Participant { get; set; }
        public string Reason { get; set; }
    }
}
