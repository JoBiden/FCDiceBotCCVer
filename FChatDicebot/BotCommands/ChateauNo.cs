using System;
using System.Collections.Generic;
using System.Linq;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!no</c> (recipient side, B5) — clear a pending that's awaiting your consent, without
    /// waiting out the 10-minute timer. Mirrors <c>!consent</c>'s bare / all / # /
    /// <c>&lt;name&gt;</c> grammar. For a group seat, declining clears only your seat; the
    /// shared moment still resolves with whoever else consented. Aliases <c>!refuse</c> /
    /// <c>!decline</c> route here.
    /// </summary>
    public class ChateauNo : ChatBotCommand
    {
        // Owner-provided announcement (B5.7). Public, because the initiator didn't invoke the
        // bot and so can't be PM'd.
        public const string RefuseAnnouncement =
            "Looks like {recipient} isn't in the mood. Remember, it's not true consent if you can't say 'no'!";

        public ChateauNo()
        {
            Name = "no";
            Aliases = new string[] { "refuse", "decline" };
            Category = "General";
            ShortDescription = "Decline a pending interaction awaiting your consent";
            LongDescription = "Decline a pending interaction that's awaiting your consent, instead of waiting out the 10-minute timer. Works just like !consent: bare to decline your only pending, '!no all', '!no {number}', or '!no {name}' to decline a specific person's request. For a group interaction, this only removes your own seat — the rest can still go ahead.";
            Usage = "!no\nor\n!no all\nor\n!no {number}\nor\n!no {name}";
            RelatedCommands = new string[] { "consent", "oops" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            var database = MonDB.GetDatabase();
            int pendingMinutesKeep = InteractionProcessors.GroupInteractionResolver.PendingMinutesKeep;
            string channelMessage = string.Empty;
            string privateMessage = string.Empty;

            // Opportunistic expiry sweep; a swept group seat may let its group resolve.
            var touchedGroups = new HashSet<string>();
            foreach (PendingCommand seat in MonDB.getPending(characterName))
            {
                if (seat.startTime.CompareTo(DateTime.UtcNow.AddMinutes(-pendingMinutesKeep)) < 0)
                {
                    if (seat.IsGroupSeat) touchedGroups.Add(seat.groupId);
                    MonDB.removePendingInteraction(seat.Id);
                }
            }

            List<PendingCommand> pendingList = MonDB.getPending(characterName);
            string firstTerm = terms.FirstOrDefault();
            int numTarget = Utils.GetNumberFromInputs(terms);
            bool all = firstTerm == "all";

            List<PendingCommand> toRefuse = new List<PendingCommand>();
            if (pendingList.Count == 0)
            {
                privateMessage = "No one is awaiting your consent, so there's nothing to decline.";
            }
            else if (all)
            {
                toRefuse.AddRange(pendingList);
            }
            else if (numTarget >= 1 && numTarget <= pendingList.Count)
            {
                toRefuse.Add(pendingList.ElementAt(numTarget - 1));
            }
            else
            {
                PendingCommand named = Support.LifecycleTargeting.FindByCounterparty(
                    commandController, rawTerms, terms, pendingList, p => p.pendingInteraction.initiator);
                if (named != null)
                {
                    toRefuse.Add(named);
                }
                else if (pendingList.Count == 1)
                {
                    toRefuse.Add(pendingList[0]);
                }
                else
                {
                    privateMessage = "Use '!no #' to decline the correct interaction. You have the following interactions awaiting your consent:";
                    int n = 1;
                    foreach (PendingCommand seat in pendingList)
                    {
                        privateMessage += "\n" + n + ": [b]" + seat.pendingInteraction.type + "[/b] initiated by [user]" + seat.pendingInteraction.initiator + "[/user]";
                        if (seat.IsGroupSeat) privateMessage += " (group)";
                        n++;
                    }
                    channelMessage = "There's more than one request awaiting you, " + characterName + ". Either '!no all', or refer to the list we just messaged you.";
                }
            }

            // Clear the caller's own seat(s). For a group seat this leaves the rest intact.
            foreach (PendingCommand seat in toRefuse)
            {
                if (seat.IsGroupSeat) touchedGroups.Add(seat.groupId);
                MonDB.removePendingInteraction(seat.Id);
            }

            if (toRefuse.Count > 0)
            {
                Profile caller = MonDB.getProfile(characterName);
                string callerName = caller != null ? caller.displayName : characterName;
                channelMessage = RefuseAnnouncement.Replace("{recipient}", callerName);
            }

            // A declined group seat may complete the group for whoever else already consented.
            foreach (string groupId in touchedGroups)
            {
                var resolution = InteractionProcessors.GroupInteractionResolver.CheckAndResolve(database, groupId);
                if (resolution.Resolved && !string.IsNullOrEmpty(resolution.ChannelMessage))
                {
                    string groupMessage = resolution.ChannelMessage;
                    foreach (string participant in resolution.Participants)
                    {
                        string titles = ChateauSystemTitles.CheckAndGrantTitles(participant);
                        if (!string.IsNullOrEmpty(titles)) groupMessage += titles;
                    }
                    if (!string.IsNullOrEmpty(channelMessage)) channelMessage += "\n\n";
                    channelMessage += groupMessage;
                }
            }

            if (!string.IsNullOrEmpty(channelMessage))
            {
                bot.SendMessageInChannel(channelMessage, channel);
            }
            if (!string.IsNullOrEmpty(privateMessage))
            {
                bot.SendPrivateMessage(privateMessage, characterName);
            }
        }
    }
}
