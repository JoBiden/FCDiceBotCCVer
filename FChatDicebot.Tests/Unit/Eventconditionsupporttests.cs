using FChatDicebot.BotCommands.Support;
using FChatDicebot.InteractionProcessors.Commitment;
using FChatDicebot.InteractionProcessors.Consequence;
using FChatDicebot.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Pure unit tests for the random-event winner conditions: the per-stat integer projection
    /// and the inclusive-range test built on it. No Mongo, no engine — these are all static reads
    /// of a hand-built <see cref="Profile"/>.
    /// </summary>
    public class EventConditionSupportTests
    {
        private static EventCondition Cond(string stat, string key = null, int? min = null, int? max = null)
        {
            return new EventCondition { stat = stat, key = key, min = min, max = max };
        }

        private static Profile WithCorruption(int value)
        {
            var p = new Profile { userName = "Alice" };
            p.characteristics[CorruptionProcessor.CorruptionCharacteristicKey] = value.ToString();
            return p;
        }

        // =========================== The signed corruption axis ===========================

        // The reason these conditions exist rather than reusing the duty Conditional: corruption
        // runs negative-for-corrupt through positive-for-pure, so both halves must be expressible.
        [Theory]
        [InlineData(-120, true)]
        [InlineData(-10, true)]
        [InlineData(-9, false)]
        [InlineData(0, false)]
        [InlineData(40, false)]
        public void Corruption_UpperBoundSelectsTheCorrupted(int corruption, bool expected)
        {
            Assert.Equal(expected, EventConditionSupport.Matches(WithCorruption(corruption), Cond("corruption", max: -10)));
        }

        [Theory]
        [InlineData(120, true)]
        [InlineData(10, true)]
        [InlineData(9, false)]
        [InlineData(-40, false)]
        public void Corruption_LowerBoundSelectsThePure(int corruption, bool expected)
        {
            Assert.Equal(expected, EventConditionSupport.Matches(WithCorruption(corruption), Cond("corruption", min: 10)));
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(9, true)]
        [InlineData(-9, true)]
        [InlineData(10, false)]
        [InlineData(-10, false)]
        public void Corruption_TwoBoundsSelectTheNeutralBand(int corruption, bool expected)
        {
            Assert.Equal(expected, EventConditionSupport.Matches(WithCorruption(corruption), Cond("corruption", min: -9, max: 9)));
        }

        [Fact]
        public void Corruption_AbsentCharacteristicReadsAsZero()
        {
            var p = new Profile { userName = "Alice" };
            Assert.True(EventConditionSupport.Matches(p, Cond("corruption", min: 0, max: 0)));
        }

        // =========================== Bounds are inclusive and optional ===========================

        [Fact]
        public void NoBounds_MatchesAnyProjectableValue()
        {
            Assert.True(EventConditionSupport.Matches(WithCorruption(-77), Cond("corruption")));
        }

        [Fact]
        public void Bounds_AreInclusiveOnBothEnds()
        {
            Assert.True(EventConditionSupport.Matches(WithCorruption(5), Cond("corruption", min: 5, max: 5)));
            Assert.False(EventConditionSupport.Matches(WithCorruption(4), Cond("corruption", min: 5, max: 5)));
            Assert.False(EventConditionSupport.Matches(WithCorruption(6), Cond("corruption", min: 5, max: 5)));
        }

        // =========================== Dictionary-backed stats ===========================

        [Fact]
        public void Currency_ProjectsTheBalance_MissingKeyIsZero()
        {
            var p = new Profile { userName = "Alice" };
            p.currencies["rosequartz"] = 40;

            Assert.True(EventConditionSupport.Matches(p, Cond("currency", "rosequartz", min: 40)));
            Assert.False(EventConditionSupport.Matches(p, Cond("currency", "rosequartz", min: 41)));
            // An unheld currency projects to zero rather than failing, so "the broke" is authorable.
            Assert.True(EventConditionSupport.Matches(p, Cond("currency", "gold", max: 0)));
        }

        [Fact]
        public void Currency_KeyLookupIsCaseInsensitive()
        {
            var p = new Profile { userName = "Alice" };
            p.currencies["RoseQuartz"] = 12;
            Assert.True(EventConditionSupport.Matches(p, Cond("currency", "rosequartz", min: 12, max: 12)));
        }

        [Fact]
        public void Training_And_Job_And_Count_ProjectTheirDictionaries()
        {
            var p = new Profile { userName = "Alice" };
            p.trainings["magic"] = 80;
            p.jobExperience["adventurer"] = 6;
            p.counts["kissgive"] = 3;

            Assert.True(EventConditionSupport.Matches(p, Cond("training", "magic", min: 50)));
            Assert.True(EventConditionSupport.Matches(p, Cond("job", "adventurer", min: 5)));
            Assert.True(EventConditionSupport.Matches(p, Cond("count", "kissgive", min: 3, max: 3)));
            Assert.False(EventConditionSupport.Matches(p, Cond("count", "kissgive", min: 4)));
        }

        // A dictionary stat has no meaningful "total across all keys", so a blank key is treated
        // as authoring error and fails closed rather than matching everyone.
        [Theory]
        [InlineData("currency")]
        [InlineData("training")]
        [InlineData("job")]
        [InlineData("count")]
        public void DictionaryStats_RequireAKey(string stat)
        {
            var p = new Profile { userName = "Alice" };
            Assert.True(EventConditionSupport.RequiresKey(stat));
            Assert.False(EventConditionSupport.Matches(p, Cond(stat, null, min: 0)));
        }

        // =========================== List-backed stats ===========================

        [Fact]
        public void Title_BlankKeyCountsThemAll_NamedKeyNarrows()
        {
            var p = new Profile { userName = "Alice" };
            p.titles.Add(new Title { titleText = "Cutie", givenBy = "Chateau" });
            p.titles.Add(new Title { titleText = "Lucky", givenBy = "Chateau" });

            // Blank key is how a "titles held" count is authored — no separate stat needed.
            Assert.True(EventConditionSupport.Matches(p, Cond("title", min: 2, max: 2)));
            Assert.True(EventConditionSupport.Matches(p, Cond("title", "cutie", min: 1)));   // has it
            Assert.True(EventConditionSupport.Matches(p, Cond("title", "Sinner", max: 0)));  // lacks it
            Assert.False(EventConditionSupport.Matches(p, Cond("title", "Sinner", min: 1)));
        }

        [Fact]
        public void Curse_And_Parasite_ProjectHeldInstances()
        {
            var p = new Profile { userName = "Alice" };
            CurseInstance.SaveAll(p, new List<CurseInstance>
            {
                new CurseInstance { Curse = "poverty", AppliedBy = "Chateau", AppliedAt = DateTime.UtcNow },
            });
            ParasiteInstance.SaveAll(p, new List<ParasiteInstance>
            {
                new ParasiteInstance { Parasite = "worms", InfestedBy = "Chateau", InfestedAt = DateTime.UtcNow },
            });

            Assert.True(EventConditionSupport.Matches(p, Cond("curse", "poverty", min: 1)));
            Assert.True(EventConditionSupport.Matches(p, Cond("curse", min: 1, max: 1)));
            Assert.True(EventConditionSupport.Matches(p, Cond("parasite", min: 1)));
            Assert.True(EventConditionSupport.Matches(p, Cond("parasite", "worms", min: 1)));
            Assert.False(EventConditionSupport.Matches(p, Cond("parasite", "botfly", min: 1)));
        }

        [Fact]
        public void Curse_And_Parasite_AreZeroOnACleanProfile()
        {
            var p = new Profile { userName = "Alice" };
            Assert.True(EventConditionSupport.Matches(p, Cond("curse", max: 0)));
            Assert.True(EventConditionSupport.Matches(p, Cond("parasite", max: 0)));
        }

        // A NAMED vice projects its addiction level rather than a plain 0/1 — that is the number
        // an author wants to gate on. A blank key falls back to the plain list count.
        [Fact]
        public void Vice_NamedKeyProjectsAddictionLevel_BlankKeyCountsThem()
        {
            var p = new Profile { userName = "Alice" };
            ViceInstance.SaveAll(p, new List<ViceInstance>
            {
                new ViceInstance { Vice = "lustessence", AddictionLevel = 7, DosedBy = "Chateau" },
                new ViceInstance { Vice = "golden", AddictionLevel = 1, DosedBy = "Chateau" },
            });

            Assert.True(EventConditionSupport.Matches(p, Cond("vice", "lustessence", min: 7, max: 7)));
            Assert.True(EventConditionSupport.Matches(p, Cond("vice", "lustessence", min: 1)));  // "has it" still reads
            Assert.False(EventConditionSupport.Matches(p, Cond("vice", "golden", min: 5)));
            Assert.True(EventConditionSupport.Matches(p, Cond("vice", min: 2, max: 2)));         // holds two vices
            Assert.True(EventConditionSupport.Matches(p, Cond("vice", "cream", max: 0)));        // not hooked on it
        }

        [Fact]
        public void Pregnancy_CountsActiveOnes_NamedKeyNarrowsBySpecies()
        {
            var p = new Profile { userName = "Alice" };
            p.pregnancies.Add(new Pregnancy { Id = "1", MonsterType = "slime" });
            p.pregnancies.Add(new Pregnancy { Id = "2", MonsterType = "slime" });
            p.pregnancies.Add(new Pregnancy { Id = "3", MonsterType = "imp" });

            Assert.True(EventConditionSupport.Matches(p, Cond("pregnancy", min: 3, max: 3)));
            Assert.True(EventConditionSupport.Matches(p, Cond("pregnancy", "slime", min: 2, max: 2)));
            Assert.True(EventConditionSupport.Matches(p, Cond("pregnancy", min: 1)));    // "the expecting"
            Assert.False(EventConditionSupport.Matches(p, Cond("pregnancy", max: 0)));
        }

        [Fact]
        public void Collectible_CountsHeldItems_NamedKeyNarrowsByTypeLabel()
        {
            var p = new Profile { userName = "Alice" };
            p.collectibles.Add(new MilkBottle { serial = 1, subjectName = "Alice", acquiredAt = DateTime.UtcNow });
            p.collectibles.Add(new MilkBottle { serial = 2, subjectName = "Bob", acquiredAt = DateTime.UtcNow });

            string label = p.collectibles[0].TypeLabel;
            Assert.True(EventConditionSupport.Matches(p, Cond("collectible", min: 2, max: 2)));
            Assert.True(EventConditionSupport.Matches(p, Cond("collectible", label, min: 2)));
            Assert.True(EventConditionSupport.Matches(p, Cond("collectible", "not-a-type", max: 0)));
        }

        // =========================== Failure direction ===========================

        // A typo must kill the branch it was written on, never widen it — a malformed condition
        // that "passed" would silently hand a winner the wrong outcome.
        [Fact]
        public void UnknownStat_FailsRatherThanMatching()
        {
            var p = new Profile { userName = "Alice" };
            Assert.False(EventConditionSupport.Matches(p, Cond("corrpution", min: 0)));  // typo
            Assert.False(EventConditionSupport.Matches(p, Cond("", min: 0)));
            Assert.False(EventConditionSupport.Matches(p, Cond(null, min: 0)));
        }

        [Fact]
        public void NullProfile_NeverMatchesAProjectedCondition()
        {
            Assert.False(EventConditionSupport.Matches(null, Cond("corruption", max: -10)));
        }

        [Fact]
        public void StatNamesAreTrimmedAndCaseInsensitive()
        {
            Assert.True(EventConditionSupport.Matches(WithCorruption(-50), Cond("  Corruption ", max: -10)));
        }

        // =========================== Composition ===========================

        [Fact]
        public void NullCondition_IsNoConstraint()
        {
            Assert.True(EventConditionSupport.Matches(new Profile { userName = "Alice" }, null));
        }

        [Fact]
        public void MatchesAll_RequiresEveryCondition_EmptyIsUnconditional()
        {
            var p = WithCorruption(-40);
            p.currencies["rosequartz"] = 100;

            Assert.True(EventConditionSupport.MatchesAll(p, null));
            Assert.True(EventConditionSupport.MatchesAll(p, new List<EventCondition>()));
            Assert.True(EventConditionSupport.MatchesAll(p, new List<EventCondition>
            {
                Cond("corruption", max: -10), Cond("currency", "rosequartz", min: 50),
            }));
            // One failing condition sinks the whole set.
            Assert.False(EventConditionSupport.MatchesAll(p, new List<EventCondition>
            {
                Cond("corruption", max: -10), Cond("currency", "rosequartz", min: 500),
            }));
        }

        // =========================== Event-level helpers ===========================

        [Fact]
        public void HasAnyConditions_IsFalseForEveryPreConditionsEvent()
        {
            var ev = new RandomEvent
            {
                outcomes = new List<EventOutcome>
                {
                    new EventOutcome { weight = 1, resultText = "a" },
                    new EventOutcome { weight = 1, resultText = "b", conditions = new List<EventCondition>() },
                }
            };
            Assert.False(EventConditionSupport.HasAnyConditions(ev));
            Assert.False(EventConditionSupport.HasAnyConditions(null));
            Assert.False(EventConditionSupport.HasAnyConditions(new RandomEvent()));

            ev.outcomes[1].conditions.Add(Cond("corruption", max: -10));
            Assert.True(EventConditionSupport.HasAnyConditions(ev));
        }

        [Fact]
        public void EligibleOutcomes_FiltersToWhatThisWinnerQualifiesFor()
        {
            var corrupted = new EventOutcome { weight = 1, resultText = "dark", conditions = new List<EventCondition> { Cond("corruption", max: -10) } };
            var pure = new EventOutcome { weight = 1, resultText = "light", conditions = new List<EventCondition> { Cond("corruption", min: 10) } };
            var anyone = new EventOutcome { weight = 1, resultText = "plain" };
            var ev = new RandomEvent { outcomes = new List<EventOutcome> { corrupted, pure, anyone } };

            Assert.Equal(new List<EventOutcome> { corrupted, anyone }, EventConditionSupport.EligibleOutcomes(ev, WithCorruption(-50)));
            Assert.Equal(new List<EventOutcome> { pure, anyone }, EventConditionSupport.EligibleOutcomes(ev, WithCorruption(50)));
            Assert.Equal(new List<EventOutcome> { anyone }, EventConditionSupport.EligibleOutcomes(ev, WithCorruption(0)));
        }

        // =========================== Stored document shape ===========================
        //
        // Events are authored straight into Mongo (the random-event-builder writes the documents),
        // so the model has to survive the exact BSON that tool emits. The bounds are nullable on
        // purpose - an omitted min must read back as "unbounded", NOT as zero, or "the pure"
        // (min: 10, no max) would silently become "between 10 and 0" and match nobody.

        [Fact]
        public void StoredShape_OmittedBoundDeserializesAsUnboundedNotZero()
        {
            var doc = new BsonDocument
            {
                { "stat", "corruption" },
                { "key", "" },
                { "max", -10 },   // no "min" key at all, exactly as the builder writes it
            };

            EventCondition condition = BsonSerializer.Deserialize<EventCondition>(doc);

            Assert.False(condition.min.HasValue);
            Assert.Equal(-10, condition.max.Value);
            Assert.True(EventConditionSupport.Matches(WithCorruption(-9999), condition));
            Assert.False(EventConditionSupport.Matches(WithCorruption(0), condition));
        }

        [Fact]
        public void StoredShape_OutcomeWithoutConditionsKeyIsUnconditional()
        {
            var doc = new BsonDocument
            {
                { "weight", 1 },
                { "resultText", "Resolved!" },
                { "rewards", new BsonArray() },
                // No "conditions" key - every event authored before conditions existed looks
                // like this, and must keep behaving as a plain shared-roll outcome.
            };

            EventOutcome outcome = BsonSerializer.Deserialize<EventOutcome>(doc);

            Assert.Null(outcome.conditions);
            Assert.True(EventConditionSupport.MatchesAll(new Profile { userName = "Alice" }, outcome.conditions));
            Assert.False(EventConditionSupport.HasAnyConditions(
                new RandomEvent { outcomes = new List<EventOutcome> { outcome } }));
        }

        [Fact]
        public void StoredShape_RoundTripsThroughBsonWithBothBounds()
        {
            var original = new EventCondition { stat = "vice", key = "lustessence", min = 5, max = null };

            EventCondition restored = BsonSerializer.Deserialize<EventCondition>(original.ToBsonDocument());

            Assert.Equal("vice", restored.stat);
            Assert.Equal("lustessence", restored.key);
            Assert.Equal(5, restored.min.Value);
            Assert.False(restored.max.HasValue);
            // [BsonIgnoreIfNull] keeps the unbounded side out of the stored document entirely.
            Assert.False(original.ToBsonDocument().Contains("max"));
        }
    }
}
