using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauConsent : ChatBotCommand
    {
        public ChateauConsent()
        {
            Name = "consent";
            Aliases = new string[] { "c", "accept" };
            Category = "General";
            ShortDescription = "Consent to a pending interaction";
            LongDescription = "Give your consent to a pending interaction request from another character. When someone requests an interaction with you, you must !consent for it to actually happen and be recorded.\n\nPending requests expire after 10 minutes. If more than one interaction is awaiting your consent, you will be messaged a numbered list of the interactions, and can choose which interaction to consent to, '!consent {name}' to consent to a specific person's request, or '!consent all' to consent to every interaction awaiting your consent.\n\nIf the request is a group interaction, it resolves once everyone has either consented or declined (or the 10-minute timer runs out).";
            Usage = "!consent\nor\n!consent all\nor\n!consent {number}\nor\n!consent {name}\nor\n!c\nor\n!c all\nor\n!c {number}";
            RelatedCommands = new string[] { "kiss", "cuddle", "bully", "no" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;

            // A targeted game-wager proposal awaiting this player takes priority, exactly as
            // !accept already handles it (disposition #2) — !consent and !accept should be
            // interchangeable for accepting a wager, not just the latter.
            string wagerResult = DiceFunctions.Wager.WagerGameSupport.TryAcceptWager(bot, address);
            if (wagerResult != null)
            {
                bot.SendMessageInChannel(wagerResult, address);
                return;
            }

            var database = MonDB.GetDatabase();
            int pendingMinutesKeep = InteractionProcessors.GroupInteractionResolver.PendingMinutesKeep;
            int maxNoSpoilerLength = 500;
            string privateMessage = string.Empty;
            string channelMessage = string.Empty;

            // Sweep expired pendings awaiting this character. A group seat that expires can
            // let its group resolve with whoever else consented, so remember those groupIds.
            var touchedGroups = new HashSet<string>();
            foreach (PendingCommand pendingCommand in MonDB.getPending(characterName))
            {
                if (pendingCommand.startTime.CompareTo(DateTime.UtcNow.AddMinutes(-pendingMinutesKeep)) < 0)
                {
                    if (pendingCommand.IsGroupSeat) touchedGroups.Add(pendingCommand.groupId);
                    MonDB.removePendingInteraction(pendingCommand.Id);
                }
            }

            List<PendingCommand> pendingList = MonDB.getPending(characterName);
            string firstTerm = terms.FirstOrDefault();
            int numToConsent = Utils.GetNumberFromInputs(terms);
            bool all = firstTerm == "all";

            // Resolve the seats this !consent acts on, per the bare / all / # / <name> grammar.
            List<PendingCommand> toActOn = new List<PendingCommand>();
            if (pendingList.Count == 0)
            {
                privateMessage = "No one is awaiting your consent. Maybe you can make the first move?~";
            }
            else if (all)
            {
                toActOn.AddRange(pendingList);
            }
            else if (numToConsent >= 1 && numToConsent <= pendingList.Count)
            {
                toActOn.Add(pendingList.ElementAt(numToConsent - 1));
            }
            else
            {
                PendingCommand named = FindSeatByInitiator(commandController, rawTerms, terms, pendingList);
                if (named != null)
                {
                    toActOn.Add(named);
                }
                else if (pendingList.Count == 1)
                {
                    toActOn.Add(pendingList[0]);
                }
                else
                {
                    privateMessage = "Use '!consent #' to consent to the correct interaction. You have the following interactions awaiting your consent:";
                    int n = 1;
                    foreach (PendingCommand seat in pendingList)
                    {
                        privateMessage += "\n" + n + ": [b]" + seat.pendingInteraction.type + "[/b] initiated by [user]" + seat.pendingInteraction.initiator + "[/user]";
                        if (seat.IsGroupSeat) privateMessage += " (group)";
                        n++;
                    }
                    channelMessage = "There's multiple people awaiting your consent, " + characterName + ". Either '!consent all', or refer to the list we just messaged you.";
                }
            }

            // Group seats defer (mark consented; resolve when the last seat clears); 1:1 seats
            // process immediately as before.
            foreach (PendingCommand seat in toActOn)
            {
                if (seat.IsGroupSeat)
                {
                    InteractionProcessors.GroupInteractionResolver.MarkSeatConsented(database, seat);
                    touchedGroups.Add(seat.groupId);
                }
                else
                {
                    channelMessage += ProcessOneToOneSeat(bot, seat);
                }
            }

            // Fire any group whose last Pending seat just cleared.
            foreach (string groupId in touchedGroups)
            {
                var resolution = InteractionProcessors.GroupInteractionResolver.CheckAndResolve(database, groupId);

                // Privately notify anyone dropped by a failed re-validation (H2), regardless
                // of whether the rest of the group still resolved.
                foreach (var dropped in resolution.Dropped)
                {
                    bot.SendPrivateMessage(dropped.Reason, dropped.Participant);
                }

                if (resolution.Resolved && !string.IsNullOrEmpty(resolution.ChannelMessage))
                {
                    string groupMessage = resolution.ChannelMessage;

                    // One consolidated "Title Time!" banner for the whole moment, grouped by
                    // title. Fold each participant's lifetime-count title wins (granted now that
                    // the group counts have been applied) in with the group-achievement titles
                    // (by resolved size / lap-stack position) already granted during resolution.
                    var allTitleGrants = new List<InteractionProcessors.GroupTitleGrant>(resolution.GroupTitleGrants);
                    foreach (string participant in resolution.Participants)
                    {
                        var countGrant = ChateauSystemTitles.CheckAndGrantCountTitles(participant);
                        if (countGrant != null) allTitleGrants.Add(countGrant);
                    }
                    groupMessage += ChateauSystemTitles.FormatGroupTitleNotification(allTitleGrants);

                    if (!string.IsNullOrEmpty(channelMessage)) channelMessage += "\n\n";
                    channelMessage += groupMessage;
                }
            }

            if (channelMessage.Length > maxNoSpoilerLength)
            {
                channelMessage = "Consent is such a wonderful thing~ This was a long one though, so the details are in the spoiler below. \n[spoiler]" + channelMessage + "[/spoiler]";
            }

            if (channelMessage != string.Empty)
            {
                bot.SendMessageInChannel(channelMessage, channel);
            }
            if (privateMessage != string.Empty)
            {
                bot.SendPrivateMessage(privateMessage, characterName);
            }
        }

        /// <summary>
        /// Process one ordinary (non-group) 1:1 seat exactly as legacy !consent did: run the
        /// processor, build the completion + rate-limit text, route any initiator-private note,
        /// and append title notifications for both parties. Returns the channel fragment.
        /// </summary>
        private string ProcessOneToOneSeat(BotMain bot, PendingCommand toConsent)
        {
            string channelMessage = string.Empty;
            var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor(toConsent.pendingInteraction.type);
            if (processor != null)
            {
                // Enforcement spine: re-validate at consent time, not just at request time.
                // ValidateInteraction is side-effect free and already runs the status-effect
                // blocker pipeline (break/curse) plus any processor-specific self-gating
                // (train/golden), none of which was ever being checked here before.
                var validation = processor.ValidateInteraction(
                    toConsent.pendingInteraction.initiator,
                    toConsent.pendingInteraction.recipient,
                    toConsent.pendingInteraction.identifier);
                if (!validation.IsValid)
                {
                    MonDB.removePendingInteraction(toConsent.Id);
                    bot.SendPrivateMessage(validation.ErrorMessage, toConsent.awaitingConsentFrom);
                    return channelMessage;
                }

                // Process the interaction (saves to DB, updates profiles)
                string result = processor.ProcessInteraction(toConsent);

                // Drain any out-of-band private note the processor wants sent to the
                // initiator (e.g. corrupt/purify's TOCTOU exhaustion notice, or payment's
                // insufficient-funds-at-consent-time notice).
                string initiatorPrivate = processor.GetAndClearInitiatorPrivateMessage();
                if (!string.IsNullOrEmpty(initiatorPrivate))
                {
                    bot.SendPrivateMessage(initiatorPrivate, toConsent.pendingInteraction.initiator);
                }

                if (result == "NoInteraction")
                {
                    // The processor aborted (e.g. a queued payment whose payer can no longer
                    // afford it). Nothing happened, so there's nothing to announce in-channel.
                    return channelMessage;
                }

                // If this pending was a !fulfill of a pledge, mark it fulfilled now that it
                // actually landed on the real consent path (H3 — this was previously only
                // reachable via ChateauInteractionHandler.addInteraction's processor==null
                // fallback, which every registered interaction type bypasses).
                ChateauInteractionHandler.TryMarkPledgeFulfilled(toConsent);

                Profile initProfile = MonDB.getProfile(toConsent.pendingInteraction.initiator);
                Profile recipProfile = MonDB.getProfile(toConsent.pendingInteraction.recipient);
                channelMessage += processor.GetCompletionMessageWithStatusEffects(initProfile, recipProfile, toConsent.pendingInteraction.identifier);
                channelMessage += processor.GetAndClearRateLimitMessage();
            }
            else
            {
                // Every registered interaction type has a processor; reaching here means the
                // pending's type string doesn't match anything in InteractionProcessorRegistry
                // (e.g. a corrupted/stale document). Drop it rather than silently doing nothing.
                Console.WriteLine("ProcessOneToOneSeat: no processor registered for interaction type '"
                    + toConsent.pendingInteraction.type + "' — dropping pending " + toConsent.Id);
                MonDB.removePendingInteraction(toConsent.Id);
            }

            channelMessage = CheckAchievementsAndAppendToMessage(channelMessage, toConsent.pendingInteraction.initiator);
            channelMessage = CheckAchievementsAndAppendToMessage(channelMessage, toConsent.pendingInteraction.recipient);
            return channelMessage;
        }

        /// <summary>
        /// By-name targeting (B5.8): match a pending whose initiator equals the typed name —
        /// either an explicit [user] tag or the first plain word. Returns null when no name was
        /// supplied or none matches, so the caller can fall back to bare/list behavior.
        /// </summary>
        private PendingCommand FindSeatByInitiator(BotCommandController commandController, string[] rawTerms, string[] terms, List<PendingCommand> pendingList)
        {
            string tagged = commandController.GetUserNameFromCommandTerms(rawTerms);
            string candidate = !string.IsNullOrEmpty(tagged) ? tagged : terms.FirstOrDefault();
            if (string.IsNullOrEmpty(candidate)) return null;
            if (string.Equals(candidate, "all", StringComparison.OrdinalIgnoreCase)) return null;
            int ignored;
            if (int.TryParse(candidate, out ignored)) return null;

            return pendingList.FirstOrDefault(p =>
                string.Equals(p.pendingInteraction.initiator, candidate, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(MonDB.getDisplayName(p.pendingInteraction.initiator) ?? string.Empty, candidate, StringComparison.OrdinalIgnoreCase));
        }

        private string CheckAchievementsAndAppendToMessage(string message, string profile)
        {
            // Check for system title achievements
            string titlesText = ChateauSystemTitles.CheckAndGrantTitles(profile);

            // Append title notifications to the message
            if (!string.IsNullOrEmpty(titlesText))
            {
                message += titlesText;
            }

            return message;
        }
    }
}
