using System;
using FChatDicebot.BotCommands.Base;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !suggestion: working alias for !feedback. Command dispatch matches on Name only, so an
    /// alias has to be its own command class that delegates to the canonical command
    /// (the ChateauW / ChateauBalance pattern) — the Aliases array is cosmetic (help display).
    /// </summary>
    public class ChateauSuggestion : ChatBotCommand
    {
        public ChateauSuggestion()
        {
            Name = "suggestion";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Alias for !feedback";
            LongDescription = "Alternative wording for the !feedback command. See !help feedback for full details.";
            Usage = "!suggestion <your idea or bug>\nor\n!suggestion [bug|idea|other] <text>";
            RelatedCommands = new string[] { "feedback", "modmessage", "help" };
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
            ChateauFeedback feedback = new ChateauFeedback();
            feedback.Run(bot, commandController, rawTerms, terms, characterName, channel, command);
        }
    }
}
