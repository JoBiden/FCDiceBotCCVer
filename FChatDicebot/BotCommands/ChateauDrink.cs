using FChatDicebot.BotCommands.Base;
using FChatDicebot.Database;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.InteractionProcessors.StatusEffectContributors;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot.BotCommands
{
    /// <summary>
    /// Uncork and drink one bottle from your own collection. Self-targeted, so there is no
    /// consent flow, but it is deliberately channel-only: the effects are things the channel is
    /// meant to witness (<c>!dose</c>'s own consent text promises everyone will know when a
    /// craving is satisfied), and a private-message run would either swallow that or leak
    /// public-by-design text into a PM.
    ///
    /// Drinking does not destroy the bottle. It stamps <see cref="MilkBottle.emptiedAt"/> and
    /// the numbered empty stays in the collection as proof of what was drunk. That is what makes
    /// this different from <c>!sell</c>, where the Chateau keeps the bottle and the serial is
    /// gone. Because the empty is the keepsake, drinking credits no bottle currency.
    ///
    /// Three effects fire, in this order: the corruption the bottle's tag carries (bounded by a
    /// daily budget), whatever the status-effect contributors have to say (a satisfied craving,
    /// most of the time), and a roll to deepen an addiction the drinker already has.
    ///
    /// Structured like <see cref="ChateauDetox"/>: a thin command over a pure
    /// <see cref="Execute(IChateauDatabase, string, string, int, Random)"/> so the effect logic
    /// is testable without a bot connection.
    /// </summary>
    public class ChateauDrink : ChatBotCommand
    {
        public ChateauDrink()
        {
            Name = "drink";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Drink down one of the bottles you're holding.";
            LongDescription = "Uncork and drink a bottle from your collection, keeping the empty. Whatever was inside affects you: a corrupt or pure bottle shifts you the same way, up to 3 bottles per day, and a substance you're addicted to will quiet the craving. There's always a small chance you find yourself wanting more. Name a bottle by its number, or leave it off to drink your newest.";
            Usage = "!drink\nor\n!drink {substance}\nor\n!drink #{number}";
            RelatedCommands = new string[] { "bottles", "milk", "sell", "dose", "detox" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = "substance";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;

            string substanceFilter = commandController.GetIdentifierFromCommandTerms(rawTerms, "substance");
            int serial = ParseSerial(rawTerms);

            DrinkResult result = Execute(MonDB.GetDatabase(), characterName, substanceFilter, serial, new Random());

            if (!string.IsNullOrEmpty(result.ChannelMessage))
            {
                bot.SendMessageInChannel(result.ChannelMessage, channel);
            }
            if (!string.IsNullOrEmpty(result.PrivateMessage))
            {
                bot.SendPrivateMessage(result.PrivateMessage, characterName);
            }
        }

        /// <summary>
        /// Pulls a <c>#142</c>-style bottle number out of the raw terms. The <c>#</c> is required
        /// so a bare number can never be mistaken for a count, which matters because the sibling
        /// <c>!pay</c> syntax does take a bare amount. Returns 0 when no serial was named.
        /// </summary>
        public static int ParseSerial(string[] rawTerms)
        {
            if (rawTerms == null) return 0;
            foreach (string term in rawTerms)
            {
                if (string.IsNullOrEmpty(term) || term[0] != '#') continue;
                if (int.TryParse(term.Substring(1), out int parsed) && parsed > 0) return parsed;
            }
            return 0;
        }

        public class DrinkResult
        {
            public string ChannelMessage { get; set; } = string.Empty;
            public string PrivateMessage { get; set; } = string.Empty;
            /// <summary>The bottle that was drunk, or null when nothing was consumed.</summary>
            public MilkBottle Bottle { get; set; }
            /// <summary>Corruption actually applied: -1, +1, or 0 when untagged or over budget.</summary>
            public int CorruptionApplied { get; set; }
            /// <summary>True when the bottle was tagged but the daily budget was already spent.</summary>
            public bool BudgetSpent { get; set; }
            /// <summary>True when the drink deepened an addiction the drinker already carried.</summary>
            public bool AddictionIntensified { get; set; }
        }

        /// <summary>
        /// Drink one bottle and apply its effects. <paramref name="serial"/> of 0 means "no
        /// specific bottle", falling back to the newest full bottle matching
        /// <paramref name="substanceFilter"/>.
        /// </summary>
        public static DrinkResult Execute(
            IChateauDatabase database, string characterName, string substanceFilter, int serial, Random rng)
        {
            var result = new DrinkResult();
            rng = rng ?? new Random();

            Profile profile = database.GetProfile(characterName);
            if (profile == null)
            {
                result.PrivateMessage = ChateauInteractionHandler.notRegisteredText();
                return result;
            }

            MilkBottle bottle = SelectBottle(profile, substanceFilter, serial, result);
            if (bottle == null) return result;

            // The bottle stays put. Only its contents are gone.
            bottle.emptiedAt = DateTime.UtcNow;
            result.Bottle = bottle;

            string substanceText = Utils.SubstanceToText(bottle.substance, database.GetIdentifier(bottle.substance));
            string drinkerName = string.IsNullOrEmpty(profile.displayName) ? profile.userName : profile.displayName;

            ApplyCorruption(profile, bottle, result);

            // Status effects run before the addiction roll so the satisfaction fragment (if the
            // drinker is hooked on this substance) reads ahead of the "wanting another already"
            // line that follows from it.
            var fragments = SelfCommandStatusEffects.GetCompletionFragments(
                profile, DoseStatusContributor.DrinkType, bottle.substance);

            int addictionLevel = ApplyAddictionRoll(profile, bottle, rng, result);

            // One write for the whole command. Everything touched (the emptied bottle, the
            // corruption characteristic, the daily budget, the vice list) lives on this one
            // profile, matching how !detox persists its own multi-field change.
            database.SetProfile(characterName, profile);

            result.ChannelMessage = BuildChannelMessage(
                database, drinkerName, bottle, substanceText, result, addictionLevel, fragments.CompletionAppendix, rng);
            return result;
        }

        /// <summary>
        /// Resolve which bottle is being drunk, or record the refusal message and return null.
        /// Empties are never selected implicitly, and an explicitly named empty gets its own
        /// message — "you already drank that" is a different situation from "you don't have it".
        /// </summary>
        private static MilkBottle SelectBottle(
            Profile profile, string substanceFilter, int serial, DrinkResult result)
        {
            if (serial > 0)
            {
                MilkBottle named = BottleInventory.FindBySerial(profile, serial);
                if (named == null)
                {
                    result.PrivateMessage = "Bottle [b]#" + serial + "[/b] isn't in your collection."
                        + " Perhaps you sold it off... use !bottles to check your numbers.";
                    return null;
                }
                if (named.IsEmpty)
                {
                    result.PrivateMessage = "Bottle [b]#" + serial + "[/b] is already empty!"
                        + " Use !bottles to see which of yours still have something in them.";
                    return null;
                }
                return named;
            }

            var candidates = BottleInventory.SelectFull(profile, substanceFilter, null);
            if (candidates.Count > 0) return candidates[0];

            // Nothing to drink. Distinguish "you own nothing" from "your filter matched nothing"
            // so the remedy we point at is the useful one.
            if (BottleInventory.IsCollectionEmpty(profile))
            {
                result.PrivateMessage = ChateauBottles.EmptyCollectionText;
            }
            else if (!string.IsNullOrEmpty(substanceFilter))
            {
                result.PrivateMessage = "We don't see any of that in your collection."
                    + " Use !bottles to check what you're actually holding.";
            }
            else
            {
                result.PrivateMessage = "Every bottle you're holding is already empty."
                    + " Go !milk a willing resident if you'd like something to drink.";
            }
            return null;
        }

        /// <summary>
        /// Move the drinker's corruption by the bottle's tag, within the day's budget. The budget
        /// lives under its own key so it never draws from the per-pair <c>!corrupt</c> quota, and
        /// it is what bounds the drift now that bottles can be pooled through <c>!pay</c>.
        /// </summary>
        private static void ApplyCorruption(Profile profile, MilkBottle bottle, DrinkResult result)
        {
            int shift = ShiftForTag(bottle.corruptionTag);
            if (shift == 0) return;

            DateTime utcDay = DateTime.UtcNow.Date;
            int used = GetUsedDrinkShift(profile, utcDay);
            if (used >= ChateauCurrency.DrinkCorruptionDailyLimit)
            {
                result.BudgetSpent = true;
                return;
            }

            int current = CorruptionProcessor.ReadCorruption(profile);
            if (profile.characteristics == null)
            {
                profile.characteristics = new Dictionary<string, string>();
            }
            profile.characteristics[CorruptionProcessor.CorruptionCharacteristicKey] = (current + shift).ToString();
            RecordUsedDrinkShift(profile, utcDay, used + Math.Abs(shift));
            result.CorruptionApplied = shift;
        }

        /// <summary>
        /// Corrupt bottles pull toward corruption (negative), purified ones toward purity
        /// (positive), matching the sign convention in <see cref="ChateauCurrency"/>.
        /// </summary>
        public static int ShiftForTag(string corruptionTag)
        {
            if (string.Equals(corruptionTag, ChateauCurrency.CorruptTag, StringComparison.OrdinalIgnoreCase))
            {
                return -ChateauCurrency.DrinkCorruptionShiftPerBottle;
            }
            if (string.Equals(corruptionTag, ChateauCurrency.PurifiedTag, StringComparison.OrdinalIgnoreCase))
            {
                return ChateauCurrency.DrinkCorruptionShiftPerBottle;
            }
            return 0;
        }

        public static string DrinkShiftQuotaKey(DateTime utcDay) => "drinkshift_" + utcDay.ToString("yyyy-MM-dd");

        public static int GetUsedDrinkShift(Profile profile, DateTime utcDay)
        {
            if (profile?.dailyMagnitudes == null) return 0;
            return profile.dailyMagnitudes.TryGetValue(DrinkShiftQuotaKey(utcDay), out int used) ? used : 0;
        }

        /// <summary>
        /// Record today's spend and drop stale-dated drink keys, mirroring
        /// <see cref="CorruptionProcessor.RecordUsedQuota"/> so old days don't accumulate on the
        /// profile forever.
        /// </summary>
        public static void RecordUsedDrinkShift(Profile profile, DateTime utcDay, int newTotal)
        {
            if (profile == null) return;
            if (profile.dailyMagnitudes == null)
            {
                profile.dailyMagnitudes = new Dictionary<string, int>();
            }
            string todayKey = DrinkShiftQuotaKey(utcDay);
            var stale = profile.dailyMagnitudes.Keys
                .Where(k => k.StartsWith("drinkshift_", StringComparison.Ordinal)
                            && !string.Equals(k, todayKey, StringComparison.Ordinal))
                .ToList();
            foreach (var key in stale)
            {
                profile.dailyMagnitudes.Remove(key);
            }
            profile.dailyMagnitudes[todayKey] = newTotal;
        }

        /// <summary>
        /// Roll to deepen an addiction the drinker already carries for this substance. Returns
        /// the resulting addiction level, or 0 when nothing intensified.
        ///
        /// Note this deliberately fires even when the same drink satisfied the craving: the
        /// bottle quiets the want now and tightens the hook at the same time, which is the whole
        /// dynamic the vice system is modelling. Restricting it to unsatisfied drinks would make
        /// the roll unreachable, since only an existing vice can intensify and an existing vice
        /// is exactly what satisfaction requires.
        /// </summary>
        private static int ApplyAddictionRoll(Profile profile, MilkBottle bottle, Random rng, DrinkResult result)
        {
            if (string.IsNullOrEmpty(bottle.substance)) return 0;
            if (rng.NextDouble() >= ChateauCurrency.DrinkAddictionChance) return 0;

            var intensified = DoseProcessor.IntensifyExistingVices(profile, new[] { bottle.substance });
            if (intensified.Count == 0) return 0;

            result.AddictionIntensified = true;
            var vice = ViceInstance.LoadAll(profile)
                .FirstOrDefault(v => string.Equals(v.Vice, bottle.substance, StringComparison.OrdinalIgnoreCase));
            return vice?.AddictionLevel ?? 0;
        }

        // -------------------------------------------------------------------
        // Channel message
        // -------------------------------------------------------------------

        private static readonly string[] ClosingFlourishes = new[]
        {
            "Unbottled, unsealed, and thoroughly enjoyed.",
            "Not a drop wasted!",
            "The empty goes in the collection.",
            "Rumors say green clad adventurers will pay a lot for an empty bottle...",
            "Got {substance}?",
        };

        private static string BuildChannelMessage(
            IChateauDatabase database,
            string drinkerName,
            MilkBottle bottle,
            string substanceText,
            DrinkResult result,
            int addictionLevel,
            List<string> statusFragments,
            Random rng)
        {
            var parts = new List<string>();

            string serialText = bottle.serial > 0 ? " [b]#" + bottle.serial + "[/b]" : string.Empty;
            parts.Add(drinkerName + " uncorks bottle" + serialText + " and drinks down the tasty "
                + substanceText + " from " + ChateauBottles.DonorText(database, bottle.sourceName) + ".");

            parts.Add(ClosingFlourishes[rng.Next(ClosingFlourishes.Length)].Replace("{substance}", substanceText));

            string corruptionFragment = BuildCorruptionFragment(drinkerName, bottle, result);
            if (!string.IsNullOrEmpty(corruptionFragment)) parts.Add(corruptionFragment);

            if (statusFragments != null)
            {
                parts.AddRange(statusFragments.Where(f => !string.IsNullOrEmpty(f)));
            }

            if (result.AddictionIntensified)
            {
                string levelNote = addictionLevel > 0
                    ? " [sub](addiction " + addictionLevel + "/" + DoseProcessor.MaxAddictionLevel + ")[/sub]"
                    : string.Empty;
                parts.Add("...and " + drinkerName + " finds themself wanting another already." + levelNote);
            }

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Two halves: the taste always lands, and the mechanical half is what the daily budget
        /// can take away. An over-budget drink still reads as the thing it is, it just stops
        /// moving the needle.
        /// </summary>
        private static string BuildCorruptionFragment(string drinkerName, MilkBottle bottle, DrinkResult result)
        {
            bool corrupt = string.Equals(bottle.corruptionTag, ChateauCurrency.CorruptTag, StringComparison.OrdinalIgnoreCase);
            bool pure = string.Equals(bottle.corruptionTag, ChateauCurrency.PurifiedTag, StringComparison.OrdinalIgnoreCase);
            if (!corrupt && !pure) return string.Empty;

            string taste = corrupt ? "How delightfully dark and rich~" : "So light and invigorating!";

            string effect;
            if (result.BudgetSpent)
            {
                effect = "No effect though... maybe tomorrow they'll be able to metabolize a bit more.";
            }
            else
            {
                effect = drinkerName + " gains " + Math.Abs(result.CorruptionApplied)
                    + (corrupt ? " corruption." : " purity.");
            }

            return taste + " " + effect;
        }
    }
}
