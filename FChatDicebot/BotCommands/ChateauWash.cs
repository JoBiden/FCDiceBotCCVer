using FChatDicebot.BotCommands.Base;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !wash is the self-targeted reversal for !odorize. It scrubs off a single layer of
    /// one scent per Chateau day — the cost is the slow pace, not a consequence. With no
    /// argument the most-saturated scent (highest Layers) is targeted; with a scent
    /// argument that specific scent is targeted.
    /// </summary>
    public class ChateauWash : ChatBotCommand
    {
        public ChateauWash()
        {
            Name = "wash";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Wash off one layer of a scent applied to you";
            LongDescription = "Wash off a single layer of one scent currently saturating you. Only your own scents can be washed off, and only once per Chateau day. If no scent is specified, the most-saturated scent (highest layer count) is targeted. Specify a scent to wash that one instead.";
            Usage = "!wash\n!wash {scent}";
            RelatedCommands = new string[] { "odorize", "dossier" };
            CooldownDuration = "1 day";
            CooldownAppliesTo = "initiator";
            IdentifierCategory = "scent";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string specifiedScent = commandController.GetIdentifierFromCommandTerms(rawTerms, "scent");
            WashResult result = ExecuteWash(MonDB.GetDatabase(), characterName, specifiedScent);

            if (!string.IsNullOrEmpty(result.ChannelMessage))
            {
                bot.SendMessageInChannel(result.ChannelMessage, channel);
            }
            if (!string.IsNullOrEmpty(result.PrivateMessage))
            {
                bot.SendPrivateMessage(result.PrivateMessage, characterName);
            }
        }

        public class WashResult
        {
            public string ChannelMessage { get; set; } = string.Empty;
            public string PrivateMessage { get; set; } = string.Empty;
        }

        /// <summary>
        /// Pure wash logic, factored out for testability. Non-empty ChannelMessage on a
        /// successful wash (so the channel can see the scrubbing happen); non-empty
        /// PrivateMessage when the caller has nothing to wash, was rate-limited, or named
        /// a scent that isn't actually on them.
        /// </summary>
        /// <param name="specifiedScent">Null/empty selects the most-saturated scent; a
        /// specific scent name washes that one.</param>
        public static WashResult ExecuteWash(IChateauDatabase database, string characterName, string specifiedScent)
        {
            var result = new WashResult();
            Profile caller = database.GetProfile(characterName);
            if (caller == null)
            {
                result.PrivateMessage = ChateauInteractionHandler.notFoundText(characterName);
                return result;
            }

            if (caller.timers != null
                && caller.timers.ContainsKey("wash_used")
                && caller.timers["wash_used"].timerEnd.CompareTo(DateTime.UtcNow) > 0)
            {
                string remaining = Utils.GetTimeSpanPrint(caller.timers["wash_used"].timerEnd - DateTime.UtcNow);
                result.PrivateMessage = "You've already washed today. You can wash off another layer in " + remaining + ".";
                return result;
            }

            List<ScentLayer> layers = ScentLayer.LoadAll(caller);
            if (layers.Count == 0)
            {
                result.PrivateMessage = "You already smell squeaky clean! If that's not what you want, consider asking someone to !odorize you...";
                return result;
            }

            ScentLayer target;
            if (!string.IsNullOrEmpty(specifiedScent))
            {
                target = null;
                foreach (var layer in layers)
                {
                    if (string.Equals(layer.Scent, specifiedScent, StringComparison.OrdinalIgnoreCase))
                    {
                        target = layer;
                        break;
                    }
                }
                if (target == null)
                {
                    result.PrivateMessage = "You aren't carrying the scent of " + specifiedScent + ". You're currently carrying: "
                        + DescribeCarriedScents(database, layers) + ". Try !wash with one of those instead.";
                    return result;
                }
            }
            else
            {
                target = layers[0];
                foreach (var layer in layers)
                {
                    if (layer.Layers > target.Layers) target = layer;
                }
            }

            string scentName = target.Scent;
            target.Layers -= 1;
            if (target.Layers <= 0)
            {
                layers.Remove(target);
            }
            else
            {
                target.RemainingMentions = target.Layers * OdorizeProcessor.MentionsPerLayer;
            }

            ScentLayer.SaveAll(caller, layers);

            DateTime now = DateTime.UtcNow;
            CoolDown washTimer = new CoolDown { timerStart = now, timerEnd = now.Date.AddDays(1) };
            if (caller.timers == null) caller.timers = new Dictionary<string, CoolDown>();
            caller.timers["wash_used"] = washTimer;

            database.SetProfile(characterName, caller);

            int remainingLayers = target.Layers > 0 ? target.Layers : 0;
            Identifier scentIdentifier = database.GetIdentifier(scentName);
            string scentPhrase = ScentText.ScentPhrase(scentIdentifier, scentName, target.AppliedBy);
            string remainingPhrase;
            if (remainingLayers == 0)
            {
                remainingPhrase = "The scent is gone entirely.";
            }
            else
            {
                remainingPhrase = remainingLayers == 1
                    ? "1 layer remains."
                    : remainingLayers + " layers remain.";
            }

            result.ChannelMessage = caller.displayName + " washes off a layer of " + scentPhrase + ". " + remainingPhrase;
            return result;
        }

        /// <summary>
        /// Render the user's current scent inventory as a comma-separated list — used when
        /// they ask to wash a scent they don't actually have, so the error message can
        /// point at the real options.
        /// </summary>
        private static string DescribeCarriedScents(IChateauDatabase database, List<ScentLayer> layers)
        {
            var parts = new List<string>();
            foreach (var layer in layers)
            {
                Identifier ident = database.GetIdentifier(layer.Scent);
                string phrase = ScentText.ScentPhrase(ident, layer.Scent, layer.AppliedBy);
                string layerWord = layer.Layers == 1 ? "1 layer" : layer.Layers + " layers";
                parts.Add(phrase + " (" + layerWord + ")");
            }
            return string.Join(", ", parts);
        }
    }
}
