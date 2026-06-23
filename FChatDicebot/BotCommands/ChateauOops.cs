using System;
using System.Collections.Generic;
using System.Linq;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// <c>!oops</c> (initiator side, B5) — cancel a pending you started, without waiting out the
    /// 10-minute timer. Bare / all / # / <c>&lt;name&gt;</c> grammar like <c>!consent</c>, where
    /// <c>&lt;name&gt;</c> is a recipient. For a group interaction, cancelling nukes every seat
    /// of that group (you're calling off the whole pile). Aliases <c>!o</c> / <c>!withdraw</c> /
    /// <c>!cancel</c> route here.
    /// </summary>
    public class ChateauOops : ChatBotCommand
    {
        // Owner-provided announcement (B5.7). Public — the recipient(s) didn't invoke the bot.
        public const string WithdrawAnnouncement =
            "Nevermind! {initiator} says that was a clerical error.";

        public ChateauOops()
        {
            Name = "oops";
            Aliases = new string[] { "o", "withdraw", "cancel" };
            Category = "General";
            ShortDescription = "Cancel a pending interaction you started";
            LongDescription = "Cancel a pending interaction you initiated, instead of waiting out the 10-minute timer. Works like !consent: bare to cancel your only outgoing request, '!oops all', '!oops {number}', or '!oops {name}' to cancel a specific person's request. Cancelling a group interaction removes the whole pile.";
            Usage = "!oops\nor\n!oops all\nor\n!oops {number}\nor\n!oops {name}";
            RelatedCommands = new string[] { "consent", "no" };
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
            int pendingMinutesKeep = GroupInteractionResolver.PendingMinutesKeep;
            string channelMessage = string.Empty;
            string privateMessage = string.Empty;

            // Opportunistic expiry sweep over this initiator's outgoing seats.
            foreach (PendingCommand seat in MonDB.getPendingByInitiator(characterName))
            {
                if (seat.startTime.CompareTo(DateTime.UtcNow.AddMinutes(-pendingMinutesKeep)) < 0)
                {
                    MonDB.removePendingInteraction(seat.Id);
                }
            }

            List<PendingCommand> outgoing = MonDB.getPendingByInitiator(characterName);

            // Collapse group seats into one logical entry each (the initiator cancels the whole
            // group at once); 1:1 seats stand alone. Order preserved by first appearance.
            var logicalEntries = BuildLogicalEntries(outgoing);

            string firstTerm = terms.FirstOrDefault();
            int numTarget = Utils.GetNumberFromInputs(terms);
            bool all = firstTerm == "all";

            List<List<PendingCommand>> toWithdraw = new List<List<PendingCommand>>();
            if (logicalEntries.Count == 0)
            {
                privateMessage = "You don't have any pending requests out right now.";
            }
            else if (all)
            {
                toWithdraw.AddRange(logicalEntries);
            }
            else if (numTarget >= 1 && numTarget <= logicalEntries.Count)
            {
                toWithdraw.Add(logicalEntries.ElementAt(numTarget - 1));
            }
            else
            {
                List<PendingCommand> named = FindEntryByRecipient(commandController, rawTerms, terms, logicalEntries);
                if (named != null)
                {
                    toWithdraw.Add(named);
                }
                else if (logicalEntries.Count == 1)
                {
                    toWithdraw.Add(logicalEntries[0]);
                }
                else
                {
                    privateMessage = "Use '!oops #' to cancel the correct request. You currently have these requests out:";
                    int n = 1;
                    foreach (List<PendingCommand> entry in logicalEntries)
                    {
                        privateMessage += "\n" + n + ": [b]" + entry[0].pendingInteraction.type + "[/b] with " + DescribeRecipients(entry);
                        if (entry[0].IsGroupSeat) privateMessage += " (group)";
                        n++;
                    }
                    channelMessage = "You've got more than one request out, " + characterName + ". Either '!oops all', or refer to the list we just messaged you.";
                }
            }

            int cleared = 0;
            foreach (List<PendingCommand> entry in toWithdraw)
            {
                foreach (PendingCommand seat in entry)
                {
                    MonDB.removePendingInteraction(seat.Id);
                    cleared++;
                }
            }

            if (cleared > 0)
            {
                Profile caller = MonDB.getProfile(characterName);
                string callerName = caller != null ? caller.displayName : characterName;
                channelMessage = WithdrawAnnouncement.Replace("{initiator}", callerName);
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

        private static List<List<PendingCommand>> BuildLogicalEntries(List<PendingCommand> seats)
        {
            var entries = new List<List<PendingCommand>>();
            var seenGroups = new HashSet<string>();
            foreach (PendingCommand seat in seats)
            {
                if (seat.IsGroupSeat)
                {
                    if (seenGroups.Add(seat.groupId))
                    {
                        entries.Add(seats.Where(s => s.groupId == seat.groupId).ToList());
                    }
                }
                else
                {
                    entries.Add(new List<PendingCommand> { seat });
                }
            }
            return entries;
        }

        private static string DescribeRecipients(List<PendingCommand> entry)
        {
            var names = entry.Select(s => "[user]" + s.awaitingConsentFrom + "[/user]").ToList();
            return InteractionProcessorBase.JoinNamesSerial(names);
        }

        private static List<PendingCommand> FindEntryByRecipient(
            BotCommandController commandController, string[] rawTerms, string[] terms,
            List<List<PendingCommand>> logicalEntries)
        {
            // Flatten, match a single seat by its recipient, then return that seat's entry.
            var flat = logicalEntries.SelectMany(e => e).ToList();
            PendingCommand match = Support.LifecycleTargeting.FindByCounterparty(
                commandController, rawTerms, terms, flat, s => s.awaitingConsentFrom);
            if (match == null) return null;
            return logicalEntries.FirstOrDefault(e => e.Contains(match));
        }
    }
}
