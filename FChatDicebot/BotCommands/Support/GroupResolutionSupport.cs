using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using System.Collections.Generic;

namespace FChatDicebot.BotCommands.Support
{
    /// <summary>
    /// Shared tail of a group resolution, used by !consent, !no, and the timeout sweep in
    /// <see cref="BotMain.HandleGroupTimeoutsTick"/>: run
    /// <see cref="GroupInteractionResolver.CheckAndResolve"/> for a groupId, privately notify
    /// anyone dropped by a failed re-validation (H2), and — when the moment fired — fold the
    /// group-achievement grants (by resolved size / lap-stack position) in with each
    /// participant's lifetime-count title wins into one consolidated "Title Time!" banner.
    /// Returns the finished channel message (empty when the group didn't resolve); where to
    /// post it is the caller's business.
    ///
    /// Lives outside the <c>FChatDicebot.BotCommands</c> namespace so the reflection-based
    /// command loader doesn't pick it up.
    /// </summary>
    public static class GroupResolutionSupport
    {
        public static string ResolveAndFormat(BotMain bot, IChateauDatabase database, string groupId)
        {
            var resolution = GroupInteractionResolver.CheckAndResolve(database, groupId);

            foreach (var dropped in resolution.Dropped)
            {
                bot.SendPrivateMessage(dropped.Reason, dropped.Participant);
            }

            if (!resolution.Resolved || string.IsNullOrEmpty(resolution.ChannelMessage))
            {
                return string.Empty;
            }

            string groupMessage = resolution.ChannelMessage;

            var allTitleGrants = new List<GroupTitleGrant>(resolution.GroupTitleGrants);
            foreach (string participant in resolution.Participants)
            {
                var countGrant = ChateauSystemTitles.CheckAndGrantCountTitles(participant);
                if (countGrant != null) allTitleGrants.Add(countGrant);
            }
            groupMessage += ChateauSystemTitles.FormatGroupTitleNotification(allTitleGrants);

            return groupMessage;
        }
    }
}
