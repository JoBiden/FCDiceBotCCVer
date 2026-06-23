using FChatDicebot.InteractionProcessors;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.BotCommands.Support
{
    /// <summary>
    /// Shared multi-target wiring for the casual interaction commands (B4). When a casual
    /// command is invoked with more than one <c>[user]</c> tag it routes here, which:
    ///   - drops the initiator's own name (with a whisper) and de-duplicates,
    ///   - caps the recipient list at <see cref="MaxRecipients"/>,
    ///   - whispers about any unregistered names and skips them,
    ///   - mints one shared groupId and creates one seat per recipient, and
    ///   - posts a single group consent announcement.
    /// Each recipient then consents individually with plain <c>!consent</c>; the shared
    /// moment resolves via <see cref="GroupInteractionResolver"/> once no seat is left Pending.
    ///
    /// If the list collapses to a single valid recipient the call degrades to an ordinary
    /// 1:1 pending so the nicer 1:1 announcement is used.
    ///
    /// Lives outside the <c>FChatDicebot.BotCommands</c> namespace so the reflection-based
    /// command loader doesn't treat it as a chat command.
    /// </summary>
    public static class CasualGroupCommandSupport
    {
        public const int MaxRecipients = 10;

        // Owner-provided self-target whisper (B4.6).
        public const string SelfTargetWhisper =
            "You're already interacting with the other residents, you don't need to also interact with yourself!";

        /// <summary>
        /// Build a group (or degenerate 1:1) casual pending from a list of target names.
        /// </summary>
        /// <param name="interactionType">The processor type key (e.g. "cuddle", or "lap"/"sit").</param>
        /// <param name="identifier">Identifier carried on each seat (the verb for lapsit; null otherwise).</param>
        public static void Run(BotMain bot, string characterName, string channel,
            string interactionType, IReadOnlyList<string> targetNames, string identifier = null)
        {
            Profile initiatorProfile = MonDB.getProfile(characterName);
            if (initiatorProfile == null) return; // dispatcher already guards registration

            var processor = InteractionProcessorRegistry.GetProcessor(interactionType);

            // De-dupe (case-insensitive), drop self-targets, remember if we dropped self.
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ordered = new List<string>();
            bool droppedSelf = false;
            foreach (var name in targetNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (string.Equals(name, characterName, StringComparison.OrdinalIgnoreCase))
                {
                    droppedSelf = true;
                    continue;
                }
                if (seen.Add(name)) ordered.Add(name);
            }

            if (droppedSelf)
            {
                bot.SendPrivateMessage(SelfTargetWhisper, characterName);
            }

            // Resolve profiles; collect unregistered names to report.
            var recipientProfiles = new List<Profile>();
            var notFound = new List<string>();
            foreach (var name in ordered)
            {
                Profile profile = MonDB.getProfile(name);
                if (profile == null) notFound.Add(name);
                else recipientProfiles.Add(profile);
            }

            if (notFound.Count > 0)
            {
                bot.SendPrivateMessage(
                    "These residents aren't registered, so they were left out: " + string.Join(", ", notFound),
                    characterName);
            }

            // Cap the recipient list, whispering if we truncated.
            if (recipientProfiles.Count > MaxRecipients)
            {
                int dropped = recipientProfiles.Count - MaxRecipients;
                recipientProfiles = recipientProfiles.Take(MaxRecipients).ToList();
                bot.SendPrivateMessage(
                    $"That's a lot of people! Only the first {MaxRecipients} were included this time ({dropped} left out).",
                    characterName);
            }

            if (recipientProfiles.Count == 0)
            {
                bot.SendPrivateMessage("There was no one left to interact with after sorting out that list.", characterName);
                return;
            }

            // Single valid recipient → ordinary 1:1 pending (nicer 1:1 announcement).
            if (recipientProfiles.Count == 1)
            {
                Profile only = recipientProfiles[0];
                MonDB.addPendingCommand(new PendingCommand
                {
                    pendingInteraction = BuildInteraction(characterName, only.userName, interactionType, identifier),
                    awaitingConsentFrom = only.userName
                });

                string single = processor != null
                    ? processor.GetConsentWarning(initiatorProfile, only, identifier)
                    : initiatorProfile.displayName + " wants to interact with " + only.displayName + ". Do you !consent?";
                bot.SendMessageInChannel(single, channel);
                return;
            }

            // Group: one shared id, one seat per recipient, all awaiting their own consent.
            string groupId = Guid.NewGuid().ToString("N");
            foreach (var recipient in recipientProfiles)
            {
                MonDB.addPendingCommand(new PendingCommand
                {
                    pendingInteraction = BuildInteraction(characterName, recipient.userName, interactionType, identifier),
                    awaitingConsentFrom = recipient.userName,
                    groupId = groupId,
                    consentState = PendingCommand.PendingConsentState
                });
            }

            string announcement = processor != null
                ? processor.GetGroupConsentWarning(initiatorProfile, recipientProfiles, identifier)
                : initiatorProfile.displayName + " wants to interact with several residents. Each of you, do you !consent?";
            bot.SendMessageInChannel(announcement, channel);
        }

        private static Interaction BuildInteraction(string initiator, string recipient, string type, string identifier)
        {
            return new Interaction
            {
                initiator = initiator,
                recipient = recipient,
                type = type,
                identifier = identifier,
                investmentLevel = "casual"
            };
        }
    }
}
