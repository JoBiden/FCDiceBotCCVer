using FChatDicebot;
using FChatDicebot.BotCommands;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Guards the section table behind <c>!collection</c>.
    ///
    /// The failure this exists for has already happened once at a different layer: panties
    /// shipped, were stored and transferable, and no readout in the bot would show them, because
    /// listing a type and adding a type were two separate steps and nothing failed when the
    /// second was skipped. These pin the step so it can't be skipped again — a
    /// <see cref="Collectible"/> subclass with no section, or two sections claiming one word,
    /// fails here rather than being noticed by a resident.
    /// </summary>
    public class CollectionSectionTests
    {
        private static List<Type> CollectibleTypes()
        {
            return typeof(Collectible).Assembly
                .GetTypes()
                .Where(t => t != null && !t.IsAbstract && typeof(Collectible).IsAssignableFrom(t))
                .ToList();
        }

        [Fact]
        public void EveryCollectibleType_HasASection()
        {
            var missing = CollectibleTypes()
                .Where(t => CollectionSections.InPrintOrder.All(s => s.ItemType != t))
                .Select(t => t.Name)
                .ToList();

            Assert.True(missing.Count == 0,
                "Collectible types with no CollectionSection (they'd be held but invisible in !collection): "
                + string.Join(", ", missing));
        }

        [Fact]
        public void EverySection_RendersARealCollectibleType()
        {
            var stale = CollectionSections.InPrintOrder
                .Where(s => s.ItemType == null
                    || s.ItemType.IsAbstract
                    || !typeof(Collectible).IsAssignableFrom(s.ItemType))
                .Select(s => s.GetType().Name)
                .ToList();

            Assert.True(stale.Count == 0,
                "Sections whose ItemType isn't a concrete Collectible: " + string.Join(", ", stale));
        }

        [Fact]
        public void EveryCollectibleType_DeclaresASlotSeteiconAccepts()
        {
            // A type's EiconVerbKey is the slot its source sets to decide what the item looks
            // like. Declaring one !seteicon doesn't answer to would leave every listing reading
            // a slot nobody can write — which is exactly how !seteicon panties shipped broken.
            var unsettable = CollectibleTypes()
                .Select(t => new { Type = t, Key = ((Collectible)FormatterServices.GetUninitializedObject(t)).EiconVerbKey })
                .Where(x => string.IsNullOrEmpty(x.Key)
                    || !FChatDicebot.InteractionProcessors.InteractionEiconSupport.TryResolveTokenToVerbKeys(x.Key, out _))
                .Select(x => x.Type.Name + " -> '" + x.Key + "'")
                .ToList();

            Assert.True(unsettable.Count == 0,
                "Collectible types whose EiconVerbKey no !seteicon token resolves to: "
                + string.Join(", ", unsettable));
        }

        [Fact]
        public void Keywords_AreUniqueAcrossTypesAndNonEmpty()
        {
            // Two sections claiming one word would make "!collection panties" — and worse,
            // "!pay ... panties #43" — mean whichever happened to be listed first.
            var keywords = CollectionSections.AllKeywords();

            Assert.DoesNotContain(keywords, string.IsNullOrWhiteSpace);
            Assert.Equal(keywords.Length, keywords.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Fact]
        public void CanonicalToken_IsAlwaysOneOfTheTypesOwnKeywords()
        {
            // FilterToken is what a payment stores and what the completion messages read back,
            // so a section whose canonical word doesn't resolve would break its own history.
            foreach (CollectionSection section in CollectionSections.InPrintOrder)
            {
                Assert.Same(section, CollectionSections.ByKeyword(section.FilterToken));
            }
        }

        [Fact]
        public void Keywords_AreWhatTheCommandsDeclareAsArgumentKeywords()
        {
            // Both !collection and !pay hand these to bare-name resolution. A keyword neither
            // declares gets reported as an unrecognised resident instead of doing its job.
            var declared = CollectionSections.AllKeywords().OrderBy(t => t).ToList();

            Assert.Equal(declared, new ChateauCollection().ArgumentKeywords.OrderBy(t => t).ToList());
            Assert.Equal(declared, new ChateauPay().ArgumentKeywords.OrderBy(t => t).ToList());
        }

        [Fact]
        public void EverySection_DeclaresItsTransferSurface()
        {
            // A type with no transfer wording still transfers — it just describes itself as
            // nothing in the consent prompt and the completion message, which is the silent
            // half-shipped state panties were in.
            var incomplete = CollectionSections.InPrintOrder
                .Where(s => string.IsNullOrWhiteSpace(s.TransferNoun)
                    || string.IsNullOrWhiteSpace(s.TransferGiveFlavor)
                    || string.IsNullOrWhiteSpace(s.TransferTakeFlavor)
                    || string.IsNullOrWhiteSpace(s.TransferGoneReason))
                .Select(s => s.GetType().Name)
                .ToList();

            Assert.True(incomplete.Count == 0,
                "Sections missing transfer wording: " + string.Join(", ", incomplete));
        }

        [Fact]
        public void EverySection_DescribesAParcelAsSomethingRecognisable()
        {
            // DescribeParcel's output goes into the consent slot that also carries currency
            // amounts, and DescribesCollectibles has to be able to tell them apart.
            foreach (CollectionSection section in CollectionSections.InPrintOrder)
            {
                string described = section.DescribeParcel(null, new List<Collectible> { SampleItem(section) });

                Assert.False(string.IsNullOrWhiteSpace(described), section.GetType().Name);
                Assert.True(CollectiblePayment.DescribesCollectibles(described),
                    section.GetType().Name + " parcel wouldn't be recognised as goods: " + described);
            }
        }

        [Fact]
        public void DescribeParcel_IgnoresItemsOfAnotherType()
        {
            // Selection refuses mixed parcels, but a section handed one anyway must not render
            // a bottle as a pair of panties.
            var mixed = new List<Collectible>
            {
                new MilkBottle { serial = 1, substance = "cum", subjectName = "Carol", quantity = 1 },
                new Panties { serial = 2, subjectName = "Carol" },
            };

            Assert.Contains("1 bottle", new BottleCollectionSection().DescribeParcel(null, mixed));
            Assert.Contains("1 pair of panties", new PantiesCollectionSection().DescribeParcel(null, mixed));
        }

        [Fact]
        public void EverySection_HasHeaderAndEmptyText()
        {
            var incomplete = CollectionSections.InPrintOrder
                .Where(s => string.IsNullOrWhiteSpace(s.Header) || string.IsNullOrWhiteSpace(s.EmptyText))
                .Select(s => s.GetType().Name)
                .ToList();

            Assert.True(incomplete.Count == 0,
                "Sections missing a Header or EmptyText: " + string.Join(", ", incomplete));
        }

        [Fact]
        public void ByKeyword_IsCaseInsensitiveAndAcceptsAlternateForms()
        {
            Assert.NotNull(CollectionSections.ByKeyword("PANTIES"));
            Assert.IsType<BottleCollectionSection>(CollectionSections.ByKeyword("bottle"));
            Assert.Null(CollectionSections.ByKeyword("statuettes"));
            Assert.Null(CollectionSections.ByKeyword(null));
        }

        [Fact]
        public void ByKeywordIn_FindsTheTypeWordAnywhereInTheTerms()
        {
            Assert.IsType<PantiesCollectionSection>(
                CollectionSections.ByKeywordIn(new[] { "[user]Bob[/user]", "panties", "#43" }));
            Assert.Null(CollectionSections.ByKeywordIn(new[] { "[user]Bob[/user]", "100", "gold" }));
            Assert.Null(CollectionSections.ByKeywordIn(null));
        }

        [Fact]
        public void For_FindsTheSectionRenderingAnItem()
        {
            Assert.IsType<BottleCollectionSection>(CollectionSections.For(new MilkBottle()));
            Assert.IsType<PantiesCollectionSection>(CollectionSections.For(new Panties()));
            Assert.Null(CollectionSections.For(null));
        }

        [Fact]
        public void Sections_TolerateANullProfile()
        {
            // !collection guards on the profile before it gets here, but a section that throws
            // on one would take down whichever future caller doesn't.
            foreach (CollectionSection section in CollectionSections.InPrintOrder)
            {
                CollectionSectionResult result = section.Build(null, null, null);

                Assert.False(result.Any);
                Assert.Empty(result.Holdings);
            }
        }

        /// <summary>
        /// A minimally valid item of a section's type. Base fields cover any future type; the
        /// bottle branch exists because a bottle with no substance is not a state the bot can
        /// produce, and rendering one is not a contract worth defending.
        /// </summary>
        private static Collectible SampleItem(CollectionSection section)
        {
            var item = (Collectible)Activator.CreateInstance(section.ItemType);
            item.serial = 1;
            item.subjectName = "Carol";
            if (item is MilkBottle bottle)
            {
                bottle.substance = "cum";
                bottle.quantity = 1;
            }
            return item;
        }

        [Fact]
        public void IsNarrowed_IsFalseOnlyForAnUnfilteredView()
        {
            Assert.False(new CollectionFilter().IsNarrowed);
            Assert.True(new CollectionFilter { TypeToken = "bottles" }.IsNarrowed);
            Assert.True(new CollectionFilter { Substance = "cum" }.IsNarrowed);
            Assert.True(new CollectionFilter { Subject = "Bob" }.IsNarrowed);
        }
    }
}
