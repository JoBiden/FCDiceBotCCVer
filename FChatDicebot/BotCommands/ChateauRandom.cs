using System;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.BotCommands.Support;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !random: join the ambient random event currently running in this channel (B12). The bot
    /// occasionally fires an event into an opted-in channel; residents respond with !random
    /// (plus an event-requested keyword or answer, when the event demands one to thwart snipers).
    /// Not a consent interaction — it has no recipient, investment level, or reversal. All the
    /// scheduling, validation, and reward logic lives in <see cref="RandomEventEngine"/>; this
    /// command is just the thin channel-side entry point.
    /// </summary>
    public class ChateauRandom : ChatBotCommand
    {
        public ChateauRandom()
        {
            Name = "random";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Join the random event happening in this channel";
            LongDescription = "Take part in the random event the bot is currently running in this channel. " +
                "The bot fires an event into the channel every so often; respond with !random to join in. " +
                "Some events ask you to include an extra word or answer (it'll say so) to keep things fair — " +
                "for those, type it after the command, e.g. !random rose. Winners may earn currency, a title, " +
                "training, a shift in corruption or purity, a curse, or just a bit of fun. There's no cooldown; " +
                "participation is limited by the event's own response window.";
            Usage = "!random\nor\n!random <keyword/answer>";
            RelatedCommands = new string[] { "dossier", "help" };
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

            if (bot.RandomEventEngine == null)
            {
                bot.SendPrivateMessage(RandomEventEngine.NoEventMessage, characterName);
                return;
            }

            // The original-case joined args are the event's requested keyword/answer (if any).
            string arg = rawTerms != null && rawTerms.Length > 0 ? string.Join(" ", rawTerms).Trim() : "";

            RandomResponseResult result = bot.RandomEventEngine.HandleRandom(channel, characterName, arg, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(result.ChannelAnnouncement))
                bot.SendMessageInChannel(result.ChannelAnnouncement, channel);
            if (!string.IsNullOrEmpty(result.ReplyToUser))
                bot.SendPrivateMessage(result.ReplyToUser, characterName);
        }
    }
}
