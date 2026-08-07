using FChatDicebot.BotCommands;
using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot
{
    /// <summary>
    /// Parsing, selection, and wording shared by the bottle-transfer path of <c>!pay</c> and the
    /// payment processors that complete it.
    ///
    /// Bottles are not a currency bucket, so they can't ride the atomic <c>$inc</c> the currency
    /// path uses. What they get instead is exactness: a transfer is resolved to concrete serial
    /// numbers when the command is typed, and those numbers are what the consent-time recheck
    /// asks about. That closes the gap where a payer could promise three corrupt bottles, drink
    /// them, and deliver three plain ones under the same filter.
    /// </summary>
    public static class BottlePayment
    {
        /// <summary>
        /// Identifier stored on the interaction so the processors know a payment is bottles
        /// rather than a currency name. Not a currency-category identifier, which is exactly why
        /// the keyword was free to take.
        /// </summary>
        public const string BottleIdentifier = "bottles";

        /// <summary>True when the terms name bottles rather than a currency.</summary>
        public static bool MentionsBottles(string[] rawTerms)
        {
            if (rawTerms == null) return false;
            return rawTerms.Any(t => !string.IsNullOrEmpty(t)
                && (string.Equals(t, "bottles", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, "bottle", StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Every <c>#142</c>-style bottle number in the terms, in the order given. The <c>#</c>
        /// keeps serials distinguishable from the bare amount this command also accepts.
        /// </summary>
        public static List<int> ParseSerials(string[] rawTerms)
        {
            var serials = new List<int>();
            if (rawTerms == null) return serials;
            foreach (string term in rawTerms)
            {
                if (string.IsNullOrEmpty(term) || term[0] != '#') continue;
                if (int.TryParse(term.Substring(1), out int parsed) && parsed > 0 && !serials.Contains(parsed))
                {
                    serials.Add(parsed);
                }
            }
            return serials;
        }

        public class Selection
        {
            public List<MilkBottle> Bottles = new List<MilkBottle>();
            public bool IsValid => Bottles.Count > 0 && string.IsNullOrEmpty(_error);
            private string _error;

            /// <summary>Message for the resident who typed the command, when they are the payer.</summary>
            public string PayerFacingError => _error;

            /// <summary>
            /// Same refusal, reworded for a bill: the person typing isn't the one whose collection
            /// came up short.
            /// </summary>
            public string ThirdPartyError(string payerDisplayName)
            {
                if (string.IsNullOrEmpty(_error)) return string.Empty;
                return payerDisplayName + " doesn't have the bottles for that. They can use !bottles to see what they're holding.";
            }

            public static Selection Failed(string error)
            {
                return new Selection { _error = error };
            }

            public static Selection Of(List<MilkBottle> bottles)
            {
                return new Selection { Bottles = bottles };
            }
        }

        /// <summary>
        /// Resolve a request into the exact bottles that will move.
        ///
        /// Named serials transfer whatever they name, full or empty — a numbered empty is a
        /// keepsake worth handing over. An amount with no serials means full bottles only:
        /// someone asking for "3 bottles" means three with something in them, and the friction of
        /// naming a number is the right price for moving a collectible.
        /// </summary>
        public static Selection Select(Profile payerProfile, List<int> requestedSerials, string substanceFilter, int amount)
        {
            if (payerProfile == null) return Selection.Failed(NotEnoughText);

            if (requestedSerials != null && requestedSerials.Count > 0)
            {
                var named = new List<MilkBottle>();
                foreach (int serial in requestedSerials)
                {
                    MilkBottle bottle = BottleInventory.FindBySerial(payerProfile, serial);
                    if (bottle == null)
                    {
                        return Selection.Failed("Bottle [b]#" + serial + "[/b] isn't in your collection."
                            + " Use !bottles to check your numbers.");
                    }
                    named.Add(bottle);
                }
                return Selection.Of(named);
            }

            var candidates = BottleInventory.SelectFull(payerProfile, substanceFilter, null);
            if (candidates.Count < amount)
            {
                return Selection.Failed(NotEnoughText);
            }
            return Selection.Of(candidates.Take(amount).ToList());
        }

        public const string NotEnoughText =
            "You don't have that many bottles to hand over. Use !bottles to see what you're holding.";

        /// <summary>
        /// How a promised bottle is written into the pending command's extra parameters: the
        /// serial, negated if the bottle was empty when promised. Carrying the state alongside
        /// the number is what lets the consent-time recheck notice that a promised-full bottle
        /// has since been drunk, rather than handing over an empty nobody agreed to.
        /// </summary>
        public static int EncodePromise(MilkBottle bottle)
        {
            return bottle.IsEmpty ? -bottle.serial : bottle.serial;
        }

        /// <summary>
        /// Bottles promised by a pending payment, as (serial, wasEmpty) pairs. The first extra
        /// parameter is the signed amount (shared with the currency path); everything after it is
        /// an encoded promise.
        /// </summary>
        public static List<KeyValuePair<int, bool>> ReadPromises(Interaction interaction)
        {
            var promises = new List<KeyValuePair<int, bool>>();
            if (interaction?.extraParameters == null) return promises;
            for (int i = 1; i < interaction.extraParameters.Count; i++)
            {
                int encoded = interaction.extraParameters[i].ToInt32();
                promises.Add(new KeyValuePair<int, bool>(Math.Abs(encoded), encoded < 0));
            }
            return promises;
        }

        public static bool IsBottlePayment(Interaction interaction)
        {
            return interaction != null && IsBottlePayment(interaction.identifier);
        }

        /// <summary>
        /// True when a payment's stored identifier is the bottle keyword rather than a currency
        /// name. This is what the completion messages branch on.
        /// </summary>
        public static bool IsBottlePayment(string identifier)
        {
            return string.Equals(identifier, BottleIdentifier, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when the string handed to a consent warning is a rendered bottle summary from
        /// <see cref="Describe"/> rather than the currency path's "{amount} {currency}".
        ///
        /// The consent slot carries one string and the two paths put different things in it, so
        /// this has to read the shape. It's unambiguous in practice: <see cref="Describe"/> always
        /// opens with a bolded bottle count, and a currency description is bare text with no
        /// BBCode at all (see ChateauPay.Run's <c>amountAndCurrency</c>).
        /// </summary>
        public static bool DescribesBottles(string described)
        {
            return !string.IsNullOrEmpty(described)
                && described.StartsWith("[b]", StringComparison.Ordinal)
                && described.IndexOf(" bottle", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Move the promised bottles from payer to payee, or report why not.
        ///
        /// Every promised serial must still be in the payer's collection <i>in the state the
        /// recipient agreed to</i>. A bottle drunk during the consent gap is still there under the
        /// same number, but it's an empty now, and nobody consented to an empty — that's a
        /// mismatch, not a substitution, so the whole transfer aborts rather than silently
        /// downgrading.
        /// </summary>
        public static bool TryTransfer(
            IChateauDatabase database, string payerUserName, string payeeUserName,
            List<KeyValuePair<int, bool>> promises, out string failureMessage)
        {
            failureMessage = null;
            Profile payer = database.GetProfile(payerUserName);
            Profile payee = database.GetProfile(payeeUserName);
            if (payer == null || payee == null)
            {
                failureMessage = "We couldn't find both parties in our records, so nothing changed hands.";
                return false;
            }
            if (promises == null || promises.Count == 0)
            {
                failureMessage = GoneMessage(database, payerUserName);
                return false;
            }

            var moving = new List<MilkBottle>();
            foreach (var promise in promises)
            {
                MilkBottle bottle = BottleInventory.FindBySerial(payer, promise.Key);
                // IsTransferable is the type's own answer to "can this change hands", so a
                // keepsake type refuses here without this method knowing what it is.
                if (bottle == null || !bottle.IsTransferable || bottle.IsEmpty != promise.Value)
                {
                    failureMessage = GoneMessage(database, payerUserName);
                    return false;
                }
                moving.Add(bottle);
            }

            if (payee.collectibles == null) payee.collectibles = new List<Collectible>();
            foreach (var bottle in moving)
            {
                payer.collectibles.Remove(bottle);
                // Serial, donor, timestamp, tag and empty-state all travel untouched. Provenance
                // surviving the handoff is the point: a bottle from a corrupt donor still corrupts
                // whoever drinks it three owners later, and still answers to the same number.
                payee.collectibles.Add(bottle);
            }

            database.SetCollectibles(payerUserName, payer.collectibles);
            database.SetCollectibles(payeeUserName, payee.collectibles);
            return true;
        }

        private static string GoneMessage(IChateauDatabase database, string payerUserName)
        {
            string payerName = database?.GetDisplayName(payerUserName);
            if (string.IsNullOrEmpty(payerName)) payerName = payerUserName;
            return payerName + " doesn't have those bottles anymore. It seems they were sold or enjoyed"
                + " while we waited on an answer... nothing changed hands.";
        }

        /// <summary>
        /// Player-facing summary of what is changing hands, for the consent prompt. Names the
        /// substance, the donor, and any corruption tag, because a recipient deserves to know
        /// they're being handed corrupt goods before they agree to take them.
        /// </summary>
        public static string Describe(IChateauDatabase database, List<MilkBottle> bottles)
        {
            if (bottles == null || bottles.Count == 0) return "nothing at all";

            var pieces = new List<string>();
            foreach (var group in BottleInventory.Group(bottles))
            {
                string piece = CountWord(group.Count) + " of the "
                    + Utils.SubstanceToText(group.Substance)
                    + " from " + ChateauBottles.DonorText(database, group.SourceName);

                if (group.CorruptionTag == ChateauCurrency.CorruptTag) piece += " ([b]corrupt[/b])";
                else if (group.CorruptionTag == ChateauCurrency.PurifiedTag) piece += " ([b]pure[/b])";

                pieces.Add(piece);
            }

            string bottleWord = bottles.Count == 1 ? "bottle" : "bottles";
            string described = "[b]" + bottles.Count + " " + bottleWord + "[/b]: " + JoinWithAnd(pieces);

            // Whether the contents are still in there is the single most important thing the
            // recipient needs to know before agreeing, so it goes on the whole parcel rather than
            // per line. Mixed parcels say so instead of implying either.
            int emptyCount = bottles.Count(b => b.IsEmpty);
            if (emptyCount == bottles.Count)
            {
                described += " [sub](already emptied)[/sub]";
            }
            else if (emptyCount > 0)
            {
                described += " [sub](" + emptyCount + " of them already emptied)[/sub]";
            }
            return described;
        }

        private static string CountWord(int count)
        {
            switch (count)
            {
                case 1: return "one";
                case 2: return "two";
                case 3: return "three";
                default: return count.ToString();
            }
        }

        private static string JoinWithAnd(List<string> pieces)
        {
            if (pieces.Count == 0) return string.Empty;
            if (pieces.Count == 1) return pieces[0];
            if (pieces.Count == 2) return pieces[0] + " and " + pieces[1];
            return string.Join(", ", pieces.Take(pieces.Count - 1)) + ", and " + pieces[pieces.Count - 1];
        }
    }
}
