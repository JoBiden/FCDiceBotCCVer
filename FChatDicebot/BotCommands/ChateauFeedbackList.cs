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
            sb.Append("[b]Recent feedback submissions:[/b]\n");

            int shown = 0;
            foreach (FeedbackEntry e in ordered)
            {
                string relative = Utils.TimeDifferenceText(e.submittedAt, now) + " ago";
                string cat = string.IsNullOrEmpty(e.category) ? ChateauFeedback.DefaultCategory : e.category;
                string name = string.IsNullOrEmpty(e.submitterDisplayName) ? e.submitterUserName : e.submitterDisplayName;
                string block = "\n" + relative + " — [b]" + name + "[/b] ([i]" + cat + "[/i]): " + e.text + "\n";

                if (shown > 0 && sb.Length + block.Length > maxChars)
                {
                    sb.Append("\n…(output truncated — showing the ").Append(shown)
                      .Append(" most recent of ").Append(ordered.Count).Append(')');
                    break;
                }

                sb.Append(block);
                shown++;
            }

            return sb.ToString().TrimEnd('\n');
        }

        // First-draft empty-state wording, pending owner review.
        public const string EmptyStateMessage = "No feedback has been submitted yet.";
    }
}
