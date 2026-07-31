using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !feedbacklist (admin): pull the most recent feedback submissions, newest first, and PM
    /// them to the requesting admin. A dedicated command rather than overloading !feedback, so
    /// there is no ambiguity between "submit the word 'list' as feedback" and "list feedback".
    /// </summary>
    public class ChateauFeedbackList : ChatBotCommand
    {
        public const int DefaultCount = 10;
        public const int MaxCount = 50;
        // Stay clear of the F-Chat 4096-char message cap when assembling the readout.
        public const int MaxMessageChars = 3900;

        public ChateauFeedbackList()
        {
            Name = "feedbacklist";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Staff: view recent feedback submissions";
            LongDescription = "Staff only. View the most recent feedback submissions (from !feedback / !suggestion), newest first. Optionally pass a count, e.g. !feedbacklist 25. The result is sent to you in a private message.";
            Usage = "!feedbacklist\nor\n!feedbacklist [count]";
            RelatedCommands = new string[] { "feedback", "modmessage" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            int count = DefaultCount;
            if (terms.Length > 0 && int.TryParse(terms[0], out int requested) && requested > 0)
            {
                count = Math.Min(requested, MaxCount);
            }

            List<FeedbackEntry> entries = MonDB.getRecentFeedback(count);
            string message = BuildFeedbackList(entries, DateTime.UtcNow);
            bot.SendPrivateMessage(message, characterName);
        }

        /// <summary>
        /// Renders the feedback readout, newest first, one block per entry. Pure and DB-free so it
        /// can be unit tested over a synthetic list. Stops short of <paramref name="maxChars"/> and
        /// appends a truncation note rather than overflowing the F-Chat message cap.
        /// </summary>
        public static string BuildFeedbackList(List<FeedbackEntry> entries, DateTime now, int maxChars = MaxMessageChars)
        {
            if (entries == null || entries.Count == 0)
            {
                return EmptyStateMessage;
            }

            List<FeedbackEntry> ordered = entries.OrderByDescending(e => e.submittedAt).ToList();

            StringBuilder sb = new StringBuilder();
            sb.Append(ReadoutText.Title("Recent Feedback Submissions")).Append('\n');

            int shown = 0;
            foreach (FeedbackEntry e in ordered)
            {
                // One prefix builder for both the normal and the truncation path: they used to
                // be built separately, so the budget arithmetic could drift from what was
                // actually emitted.
                string prefix = "\n" + EntryPrefix(e, now);
                string block = prefix + e.text + "\n";

                if (shown == 0 && block.Length > maxChars)
                {
                    // The newest entry alone exceeds the cap (L14): the old `shown > 0` guard
                    // let this through unconditionally, producing an over-cap message the
                    // caller's PM layer would silently drop entirely. Truncate this entry's
                    // own text instead of skipping straight to "nothing shown".
                    string truncNote = "\n" + ReadoutText.Small("(truncated)") + "\n";
                    int textBudget = Math.Max(0, maxChars - prefix.Length - truncNote.Length);
                    string truncatedText = e.text.Length > textBudget ? e.text.Substring(0, textBudget) : e.text;
                    sb.Append(prefix).Append(truncatedText).Append(truncNote);
                    shown++;
                    continue;
                }

                if (shown > 0 && sb.Length + block.Length > maxChars)
                {
                    sb.Append('\n').Append(ReadoutText.Small("(output truncated, showing the " + shown
                        + " most recent of " + ordered.Count + ")"));
                    break;
                }

                sb.Append(block);
                shown++;
            }

            return sb.ToString().TrimEnd('\n');
        }

        /// <summary>
        /// "  [u]Alice:[/u] [sub](bug, 2 hours ago)[/sub] " — the submitter is the row label,
        /// with category and age as small print beside it. Was
        /// "2 hours ago — [b]Alice[/b] ([i]bug[/i]): ", which led with the timestamp and used
        /// an em-dash the style guide bans.
        /// </summary>
        private static string EntryPrefix(FeedbackEntry e, DateTime now)
        {
            string relative = Utils.TimeDifferenceText(e.submittedAt, now) + " ago";
            string cat = string.IsNullOrEmpty(e.category) ? ChateauFeedback.DefaultCategory : e.category;
            string name = string.IsNullOrEmpty(e.submitterDisplayName) ? e.submitterUserName : e.submitterDisplayName;
            return ReadoutText.RowIndent + ReadoutText.Label(name) + " "
                + ReadoutText.Small("(" + cat + ", " + relative + ")") + " ";
        }

        // First-draft empty-state wording, pending owner review.
        public const string EmptyStateMessage = "No feedback has been submitted yet.";
    }
}
