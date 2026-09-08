using FChatDicebot.Database;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FChatDicebot
{
    /// <summary>
    /// Parsing, selection, and wording shared by the goods-transfer path of <c>!pay</c> and the
    /// payment processors that complete it.
    ///
    /// <para>
    /// Collectibles are not a currency bucket, so they can't ride the atomic <c>$inc</c> the
    /// currency path uses. What they get instead is exactness: a transfer is resolved to concrete
    /// serial numbers when the command is typed, and those numbers are what the consent-time
    /// recheck asks about. That closes the gap where a payer could promise three corrupt bottles,
    /// drink them, and deliver three plain ones under the same filter.
    /// </para>
    ///
    /// <para>
    /// <b>This was <c>BottlePayment</c>, and only ever moved bottles.</b> Panties shipped with
    /// <c>IsTransferable => true</c> and help text telling residents to hand a pair on with
    /// <c>!pay</c>, but every selection here resolved through <see cref="BottleInventory"/>, so a
    /// panties serial answered "Bottle #43 isn't in your collection". The type-shaped parts —
    /// which items an amount means, and how a parcel reads in the consent prompt — moved out to
    /// <see cref="CollectionSection"/>, and what's left is the same exactness machinery with no
    /// opinion about what it's moving.
    /// </para>
    ///
    /// <para>
    /// <b>One type per payment.</b> The parcel's type is stored in the interaction's
    /// <c>identifier</c> slot, which holds one string, and the completion message names the goods
    /// from it. Mixing bottles and panties in one parcel would leave both of those lying, so a
    /// request that names serials of the wrong type is refused with a message pointing at the
    /// right keyword rather than silently splitting.
    /// </para>
    /// </summary>
    public static class CollectiblePayment
    {
        /// <summary>
        /// The section a request names, or null when it is a currency payment. The typed keyword
        /// is what decides — see <see cref="CollectionSection.Keywords"/>.
        /// </summary>
        public static CollectionSection SectionFor(string[] rawTerms)
        {
            return CollectionSections.ByKeywordIn(rawTerms);
        }

        /// <summary>
        /// Every <c>#142</c>-style item number in the terms, in the order given. The <c>#</c>
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
            public List<Collectible> Items = new List<Collectible>();

            /// <summary>The one type in this parcel. Null on a failed selection.</summary>
            public CollectionSection Section;

            public bool IsValid => Items.Count > 0 && string.IsNullOrEmpty(_error);
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
                string noun = Section == null ? "goods" : Section.TransferNoun;
                return payerDisplayName + " doesn't have the " + noun + " for that. They can use !collection to see what they're holding.";
            }

            /// <summary>
            /// A refusal still carries the section, so the bill-facing rewording can name the
            /// type the payer came up short on.
            /// </summary>
            public static Selection Failed(CollectionSection section, string error)
            {
                return new Selection { Section = section, _error = error };
            }

            public static Selection Of(CollectionSection section, List<Collectible> items)
            {
                return new Selection { Section = section, Items = items };
            }
        }

        /// <summary>
        /// Resolve a request into the exact items that will move.
        ///
        /// Named serials transfer whatever they name, in whatever state — a numbered empty is a
        /// keepsake worth handing over, and naming it is the friction that buys the right. An
        /// amount with no serials asks the type what it would offer
        /// (<see cref="CollectionSection.TransferCandidates"/>), which for bottles means full ones
        /// only.
        /// </summary>
        public static Selection Select(
            Profile payerProfile, CollectionSection section, List<int> requestedSerials,
            string substanceFilter, int amount)
        {
            if (section == null) return Selection.Failed(null, NotEnoughText(null));
            if (payerProfile == null) return Selection.Failed(section, NotEnoughText(section));

            if (requestedSerials != null && requestedSerials.Count > 0)
            {
                var named = new List<Collectible>();
                foreach (int serial in requestedSerials)
                {
                    Collectible item = CollectionInventory.FindBySerial(payerProfile, serial);
                    if (item == null)
                    {
                        return Selection.Failed(section,
                            "Nothing in your collection is numbered [b]#" + serial + "[/b]."
                            + " Use !collection to check your numbers.");
                    }

                    // Holding #43 but calling it a bottle is a different mistake from not holding
                    // it, and the remedy is a different word rather than a different number.
                    //
                    // Compared on ItemType, not by reference: sections are cheap and callers
                    // construct their own, so reference equality would reject a section that is
                    // in every way the right one.
                    if (section.ItemType != item.GetType())
                    {
                        return Selection.Failed(section, WrongTypeText(serial, section, item));
                    }

                    if (!item.IsTransferable)
                    {
                        return Selection.Failed(section, NotTransferableText(serial));
                    }

                    named.Add(item);
                }
                return Selection.Of(section, named);
            }

            var candidates = section.TransferCandidates(payerProfile, substanceFilter);
            if (candidates.Count < amount)
            {
                return Selection.Failed(section, NotEnoughText(section));
            }
            return Selection.Of(section, candidates.Take(amount).ToList());
        }

        /// <summary>
        /// "You don't have that many bottles to hand over." Names the type asked for, because a
        /// resident who typed the wrong keyword should be able to see that from the refusal.
        /// </summary>
        public static string NotEnoughText(CollectionSection section)
        {
            string noun = section == null ? "of those" : section.TransferNoun;
            return "You don't have that many " + noun + " to hand over. Use !collection to see what you're holding.";
        }

        /// <summary>
        /// The resident named a real item of theirs under the wrong keyword. Says what it
        /// actually is and points at the word that would have worked, rather than the
        /// "isn't in your collection" they used to get for something they were holding.
        /// </summary>
        private static string WrongTypeText(int serial, CollectionSection asked, Collectible item)
        {
            CollectionSection actual = CollectionSections.For(item);
            if (actual == null) return NotTransferableText(serial);

            return "[b]#" + serial + "[/b] is a " + item.TypeLabel + ", not one of your "
                + asked.TransferNoun + ". Use !pay with " + actual.FilterToken + " instead.";
        }

        private static string NotTransferableText(int serial)
        {
            return "[b]#" + serial + "[/b] isn't something that can change hands.";
        }

        /// <summary>
        /// How a promised item is written into the pending command's extra parameters: the serial,
        /// negated if a bottle was empty when promised. Carrying the state alongside the number is
        /// what lets the consent-time recheck notice that a promised-full bottle has since been
        /// drunk, rather than handing over an empty nobody agreed to.
        ///
        /// Types with no such state always encode positive, which is why the recheck compares the
        /// flag rather than assuming it means anything on its own.
        /// </summary>
        public static int EncodePromise(Collectible item)
        {
            return WasEmpty(item) ? -item.serial : item.serial;
        }

        /// <summary>
        /// Whether an item was in its "spent" state at promise time. Only bottles have one; every
        /// other type answers false forever, and a future type that grows one overrides
        /// <see cref="MilkBottle.IsEmpty"/>'s role here by being added to this check.
        /// </summary>
        private static bool WasEmpty(Collectible item)
        {
            return item is MilkBottle bottle && bottle.IsEmpty;
        }

        /// <summary>
        /// Items promised by a pending payment, as (serial, wasEmpty) pairs. The first extra
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

        public static bool IsCollectiblePayment(Interaction interaction)
        {
            return interaction != null && IsCollectiblePayment(interaction.identifier);
        }

        /// <summary>
        /// True when a payment's stored identifier is a collectible type token rather than a
        /// currency name. This is what the completion messages branch on.
        ///
        /// The stored value is a <see cref="CollectionSection.FilterToken"/>, so every payment
        /// completed before panties existed carries "bottles" and still resolves — which is the
        /// reason a shipped token is not renameable.
        /// </summary>
        public static bool IsCollectiblePayment(string identifier)
        {
            return CollectionSections.ByKeyword(identifier) != null;
        }

        /// <summary>
        /// True when the string handed to a consent warning is a rendered parcel summary from
        /// <see cref="CollectionSection.DescribeParcel"/> rather than the currency path's
        /// "{amount} {currency}".
        ///
        /// The consent slot carries one string and the two paths put different things in it, so
        /// this has to read the shape. It's unambiguous in practice: every parcel summary opens
        /// with a bolded count followed by a colon, and a currency description is bare text with
        /// no BBCode at all (see ChateauPay.Run's <c>amountAndCurrency</c>).
        /// </summary>
        public static bool DescribesCollectibles(string described)
        {
            return !string.IsNullOrEmpty(described)
                && described.StartsWith("[b]", StringComparison.Ordinal)
                && described.IndexOf("[/b]:", StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// Move the promised items from payer to payee, or report why not.
        ///
        /// Every promised serial must still be in the payer's collection <i>in the state the
        /// recipient agreed to</i>. A bottle drunk during the consent gap is still there under the
        /// same number, but it's an empty now, and nobody consented to an empty — that's a
        /// mismatch, not a substitution, so the whole transfer aborts rather than silently
        /// downgrading.
        /// </summary>
        public static bool TryTransfer(
            IChateauDatabase database, CollectionSection section, string payerUserName, string payeeUserName,
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
                failureMessage = GoneMessage(database, section, payerUserName);
                return false;
            }

            var moving = new List<Collectible>();
            foreach (var promise in promises)
            {
                Collectible item = CollectionInventory.FindBySerial(payer, promise.Key);
                // IsTransferable is the type's own answer to "can this change hands", so a
                // keepsake type refuses here without this method knowing what it is.
                if (item == null || !item.IsTransferable || WasEmpty(item) != promise.Value)
                {
                    failureMessage = GoneMessage(database, section, payerUserName);
                    return false;
                }
                moving.Add(item);
            }

            if (payee.collectibles == null) payee.collectibles = new List<Collectible>();
            foreach (var item in moving)
            {
                payer.collectibles.Remove(item);
                // Serial, subject, timestamp, tag and empty-state all travel untouched. Provenance
                // surviving the handoff is the point: a bottle from a corrupt donor still corrupts
                // whoever drinks it three owners later, a pair of panties still remembers whose
                // they were, and both still answer to the same number.
                payee.collectibles.Add(item);
            }

            database.SetCollectibles(payerUserName, payer.collectibles);
            database.SetCollectibles(payeeUserName, payee.collectibles);
            return true;
        }

        /// <summary>
        /// The consent-gap refusal. Both the noun and the way an item plausibly left are per-type:
        /// a bottle is "sold or enjoyed", which is the wrong story for something the Chateau will
        /// not buy and nobody can drink.
        /// </summary>
        private static string GoneMessage(IChateauDatabase database, CollectionSection section, string payerUserName)
        {
            string payerName = database?.GetDisplayName(payerUserName);
            if (string.IsNullOrEmpty(payerName)) payerName = payerUserName;
            string noun = section == null ? "those" : "those " + section.TransferNoun;
            string reason = section == null ? "parted with" : section.TransferGoneReason;
            return payerName + " doesn't have " + noun + " anymore. It seems they were " + reason
                + " while we waited on an answer... nothing changed hands.";
        }

        /// <summary>
        /// Player-facing summary of what is changing hands, for the consent prompt. Delegated to
        /// the type, because what a recipient needs to know before agreeing is a per-type question
        /// — see <see cref="CollectionSection.DescribeParcel"/>.
        /// </summary>
        public static string Describe(IChateauDatabase database, CollectionSection section, List<Collectible> items)
        {
            if (section == null || items == null || items.Count == 0) return "nothing at all";
            return section.DescribeParcel(database, items);
        }
    }
}
