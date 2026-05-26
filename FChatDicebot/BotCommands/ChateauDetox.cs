using FChatDicebot.BotCommands.Base;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// !detox is the self-targeted reversal for !dose. Clears one vice at a cost. With no
    /// argument the highest-AddictionLevel vice is targeted; with a vice argument that
    /// specific vice is targeted. The cost is one <see cref="PurgeCostType"/> applied via
    /// <see cref="PurgeCostApplier.Apply"/> — default
    /// <see cref="DoseProcessor.DefaultDetoxCost"/> = RandomBreak (the body rebels at
    /// withdrawal).
    /// </summary>
    public class ChateauDetox : ChatBotCommand
    {
        public ChateauDetox()
        {
            Name = "detox";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Liberate yourself from an addiction, but at what cost?";
            LongDescription = "Suffer the cost of withdrawal in order to end your addiction to a vice. This cost might be a missed day of work, a broken body part, loss of training prowess, or a curse, decided at random.";
            Usage = "!detox\n!detox {vice}";
            RelatedCommands = new string[] { "dose", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = "vice";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string specifiedVice = commandController.GetIdentifierFromCommandTerms(rawTerms, "vice");
            DetoxResult result = ExecuteDetox(MonDB.GetDatabase(), characterName, specifiedVice);

            if (!string.IsNullOrEmpty(result.ChannelMessage))
            {
                bot.SendMessageInChannel(result.ChannelMessage, channel);
            }
            if (!string.IsNullOrEmpty(result.PrivateMessage))
            {
                bot.SendPrivateMessage(result.PrivateMessage, characterName);
            }
        }

        public class DetoxResult
        {
            public string ChannelMessage { get; set; } = string.Empty;
            public string PrivateMessage { get; set; } = string.Empty;
        }

        /// <summary>
        /// Pure detox logic, factored out for testability. Non-empty ChannelMessage on a
        /// successful detox (so the channel sees it); non-empty PrivateMessage when the
        /// caller has nothing to detox or named a vice they don't actually have.
        /// </summary>
        /// <param name="specifiedVice">Null/empty selects the highest-AddictionLevel vice;
        /// a specific name detoxes that one.</param>
        public static DetoxResult ExecuteDetox(IChateauDatabase database, string characterName, string specifiedVice)
        {
            return ExecuteDetox(database, characterName, specifiedVice, DoseProcessor.DefaultDetoxCost, new Random());
        }

        /// <summary>
        /// Test seam: caller chooses the cost type and the rng used by the cost applier.
        /// Production callers use the default-cost overload above.
        /// </summary>
        public static DetoxResult ExecuteDetox(
            IChateauDatabase database,
            string characterName,
            string specifiedVice,
            PurgeCostType costType,
            Random rng)
        {
            var result = new DetoxResult();
            Profile caller = database.GetProfile(characterName);
            if (caller == null)
            {
                result.PrivateMessage = ChateauInteractionHandler.notFoundText(characterName);
                return result;
            }

            List<ViceInstance> vices = ViceInstance.LoadAll(caller);
            if (vices.Count == 0)
            {
                result.PrivateMessage = "You don't currently have any addictive vices. If you'd like to change that, consider asking someone to !dose you...";
                return result;
            }

            ViceInstance target;
            if (!string.IsNullOrEmpty(specifiedVice))
            {
                target = null;
                foreach (var v in vices)
                {
                    if (string.Equals(v.Vice, specifiedVice, StringComparison.OrdinalIgnoreCase))
                    {
                        target = v;
                        break;
                    }
                }
                if (target == null)
                {
                    Identifier missIdent = database.GetIdentifier(specifiedVice);
                    string missPhrase = ViceText.ViceName(missIdent, specifiedVice, caller.displayName);
                    result.PrivateMessage = "You aren't currently addicted to " + missPhrase
                        + ", but you are addicted to " + DescribeCarriedVices(database, vices)
                        + ". Maybe you'd like to !detox from one of those instead?";
                    return result;
                }
            }
            else
            {
                target = vices[0];
                foreach (var v in vices)
                {
                    if (v.AddictionLevel > target.AddictionLevel
                        || (v.AddictionLevel == target.AddictionLevel && v.FirstDosedAt < target.FirstDosedAt))
                    {
                        target = v;
                    }
                }
            }

            string viceName = target.Vice;
            Identifier viceIdentifier = database.GetIdentifier(viceName);
            string vicePhrase = ViceText.ViceName(viceIdentifier, viceName, target.DosedBy);
            vices.Remove(target);
            ViceInstance.SaveAll(caller, vices);

            // Persist the vice removal BEFORE the cost applier writes — the applier does
            // its own SetProfile for the work timer / training decrement branches, and
            // would otherwise stomp the vice-list update we just made.
            database.SetProfile(characterName, caller);

            PurgeCostResult cost = PurgeCostApplier.Apply(database, caller, costType, rng);

            string callerName = string.IsNullOrEmpty(caller.displayName) ? caller.userName : caller.displayName;
            // Detox is private per user spec — no one else in channel needs to know who's
            // suffering withdrawals.
            result.PrivateMessage = callerName + " manages to avoid " + vicePhrase + " for a whole day. " + cost.Description;
            return result;
        }

        /// <summary>
        /// Render the user's current vice inventory as a comma-separated list — used when
        /// they ask to detox a vice they don't actually have, so the error message points
        /// at the real options. Each vice runs through <see cref="ViceText.ViceName"/> so
        /// scents and color-coded substances render the same way they do in !dose / !odorize.
        /// </summary>
        private static string DescribeCarriedVices(IChateauDatabase database, List<ViceInstance> vices)
        {
            var parts = new List<string>();
            foreach (var v in vices)
            {
                Identifier id = database?.GetIdentifier(v.Vice);
                string phrase = ViceText.ViceName(id, v.Vice, v.DosedBy);
                parts.Add(phrase + " (" + v.AddictionLevel + "/" + DoseProcessor.MaxAddictionLevel + ")");
            }
            if (parts.Count == 0) return string.Empty;
            if (parts.Count == 1) return parts[0];
            if (parts.Count == 2) return parts[0] + " and " + parts[1];
            return string.Join(", ", parts.GetRange(0, parts.Count - 1)) + ", and " + parts[parts.Count - 1];
        }
    }
}
