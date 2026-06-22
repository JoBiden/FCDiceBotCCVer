using System;
using System.Linq;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !feedback (alias !suggestion): lets any registered resident submit a free-text idea or
    /// bug report into the Feedback collection. Not an interaction — no consent, investment
    /// level, status hook, or reversal. Staff read submissions back via the admin !feedbacklist.
    /// </summary>
    public class ChateauFeedback : ChatBotCommand
    {
        // 5-minute submission cooldown, gated via profile.timers (the !work pattern).
        public const string CooldownKey = "feedback";
        public const int CooldownMinutes = 5;

        // Known leading category keywords. A first term matching one of these (case-insensitive)
        // is stripped and stored as the category; otherwise everything is stored as "general".
        public static readonly string[] KnownCategories = new string[] { "bug", "idea", "other" };
        public const string DefaultCategory = "general";

        public ChateauFeedback()
        {
            Name = "feedback";
            Aliases = new string[] { "suggestion" };
            Category = "General";
            ShortDescription = "Send the staff an idea or a bug report";
            LongDescription = "Send the Chateau mods an idea or a bug report. Works in a channel or in a private message to the bot. Optionally start your message with 'bug' 'idea' or 'other' to tag it. A mod may reach out to you for clarification, discussion, or to inform you of a change resulting from your feedback. A 5 minute cooldown applies between submissions.";
            Usage = "!feedback <your idea or bug>\nor\n!feedback [bug|idea|other] <text>";
            RelatedCommands = new string[] { "modmessage", "help", "botinfo" };
            CooldownDuration = "5 minutes";
            CooldownAppliesTo = "initiator";
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            Profile userProfile = MonDB.getProfile(characterName);
            bool fromChannel = commandController.MessageCameFromChannel(channel);

            // Cooldown gate — set only on a successful submission, so a too-soon retry that was
            // never accepted (e.g. an empty message) does not start the timer.
            if (userProfile.timers.ContainsKey(CooldownKey) && userProfile.timers[CooldownKey].timerEnd > DateTime.UtcNow)
            {
                TimeSpan timeLeft = userProfile.timers[CooldownKey].timerEnd - DateTime.UtcNow;
                Reply(bot, fromChannel, channel, characterName, CooldownMessage(timeLeft));
                return;
            }

            (string category, string text) = ParseFeedback(rawTerms);

            if (string.IsNullOrWhiteSpace(text))
            {
                Reply(bot, fromChannel, channel, characterName, EmptyUsageMessage);
                return;
            }

            string displayName = string.IsNullOrEmpty(userProfile.displayName) ? characterName : userProfile.displayName;

            FeedbackEntry entry = new FeedbackEntry
            {
                submitterUserName = characterName,
                submitterDisplayName = displayName,
                category = category,
                text = text,
                sourceChannel = fromChannel ? channel : null,
                submittedAt = DateTime.UtcNow
            };
            MonDB.addFeedback(entry);

            CoolDown timer = new CoolDown();
            timer.timerEnd = DateTime.UtcNow.AddMinutes(CooldownMinutes);
            if (userProfile.timers.ContainsKey(CooldownKey))
            {
                userProfile.timers[CooldownKey] = timer;
            }
            else
            {
                userProfile.timers.Add(CooldownKey, timer);
            }
            MonDB.setProfile(characterName, userProfile);

            Reply(bot, fromChannel, channel, characterName, AcknowledgementMessage);
        }

        /// <summary>
        /// Splits the raw (original-case) argument terms into an optional leading category and the
        /// remaining free text. A first term matching <see cref="KnownCategories"/> is consumed as
        /// the category; otherwise the category is <see cref="DefaultCategory"/> and all terms are
        /// the text. Pure and case-preserving so it can be unit tested without a bot or DB.
        /// </summary>
        public static (string category, string text) ParseFeedback(string[] rawTerms)
        {
            if (rawTerms == null || rawTerms.Length == 0)
            {
                return (DefaultCategory, "");
            }

            string firstToken = rawTerms[0].Trim().ToLower();
            if (KnownCategories.Contains(firstToken))
            {
                string remainder = string.Join(" ", rawTerms.Skip(1)).Trim();
                return (firstToken, remainder);
            }

            return (DefaultCategory, string.Join(" ", rawTerms).Trim());
        }

        private static void Reply(BotMain bot, bool fromChannel, string channel, string characterName, string message)
        {
            if (fromChannel)
            {
                bot.SendMessageInChannel(message, channel);
            }
            else
            {
                bot.SendPrivateMessage(message, characterName);
            }
        }

        // --- User-facing strings (acknowledgement is owner-final; others are first drafts pending review) ---

        public const string EmptyUsageMessage =
            "Be sure you actually include your feedback! Usage: !feedback <your idea or bug report>";

        public const string AcknowledgementMessage =
            "Thank you for your feedback! A mod might reach out to you with further inquiry, and we'll do our best to let you know how we used your feedback, one way or the other.";

        public static string CooldownMessage(TimeSpan timeLeft)
        {
            return "You've shared feedback very recently. Please wait " +
                string.Format("{0:D2}m:{1:D2}s", timeLeft.Minutes, timeLeft.Seconds) +
                " before sending more.";
        }
    }
}
