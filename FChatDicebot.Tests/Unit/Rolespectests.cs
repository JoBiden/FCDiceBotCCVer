using FChatDicebot.InteractionProcessors;
using FChatDicebot.InteractionProcessors.Involved;
using FChatDicebot.Model;
using System;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Tests for RoleSpec — the declaration of which party an interaction is about, and how
    /// that flips for processors registered under two verb keys.
    ///
    /// Pure functions over strings and profiles, so no database fixture is needed. The
    /// behavior these pin was previously hand-written inside ClimaxforProcessor and
    /// SourceDrinkProcessor; their own suites still assert the domain-named wrappers, and
    /// these assert the shared shape underneath.
    /// </summary>
    public class RoleSpecTests
    {
        // -------------------------------------------------------------------
        // Fixed — the ordinary one-verb case
        // -------------------------------------------------------------------

        [Fact]
        public void Fixed_IsNotInvertible()
        {
            Assert.False(RoleSpec.Fixed().IsInvertible);
        }

        [Fact]
        public void Fixed_ActorIsAlwaysTheInitiator_WhateverVerbIsPassed()
        {
            var spec = RoleSpec.Fixed();

            Assert.Equal("Alice", spec.ResolveActor("spank", "Alice", "Bob"));
            Assert.Equal("Alice", spec.ResolveActor("anything-at-all", "Alice", "Bob"));
            Assert.Equal("Alice", spec.ResolveActor(null, "Alice", "Bob"));
        }

        [Fact]
        public void Fixed_CounterpartIsAlwaysTheRecipient()
        {
            Assert.Equal("Bob", RoleSpec.Fixed().ResolveCounterpart("spank", "Alice", "Bob"));
        }

        [Fact]
        public void Fixed_ReportsNoVerbs()
        {
            Assert.Empty(RoleSpec.Fixed().Verbs);
        }

        // -------------------------------------------------------------------
        // Invertible — construction
        // -------------------------------------------------------------------

        [Fact]
        public void Invertible_RequiresBothVerbs()
        {
            Assert.Throws<ArgumentException>(() => RoleSpec.Invertible(null, "climax"));
            Assert.Throws<ArgumentException>(() => RoleSpec.Invertible("climaxfor", ""));
        }

        [Fact]
        public void Invertible_ReportsBothVerbs_PrimaryFirst()
        {
            var spec = RoleSpec.Invertible("climaxfor", "climax");

            Assert.Equal(new[] { "climaxfor", "climax" }, spec.Verbs.ToArray());
        }

        // -------------------------------------------------------------------
        // Invertible — resolution
        // -------------------------------------------------------------------

        [Fact]
        public void Invertible_PrimaryVerb_ActorIsInitiator()
        {
            var spec = RoleSpec.Invertible("climaxfor", "climax");

            Assert.Equal("Alice", spec.ResolveActor("climaxfor", "Alice", "Bob"));
            Assert.Equal("Bob", spec.ResolveCounterpart("climaxfor", "Alice", "Bob"));
        }

        [Fact]
        public void Invertible_InvertedVerb_ActorIsRecipient()
        {
            var spec = RoleSpec.Invertible("climaxfor", "climax");

            Assert.Equal("Bob", spec.ResolveActor("climax", "Alice", "Bob"));
            Assert.Equal("Alice", spec.ResolveCounterpart("climax", "Alice", "Bob"));
        }

        [Fact]
        public void Invertible_VerbMatchIsCaseInsensitive()
        {
            var spec = RoleSpec.Invertible("drinkfrom", "forcedrink");

            Assert.True(spec.IsInverted("FORCEDRINK"));
            Assert.Equal("Bob", spec.ResolveActor("ForceDrink", "Alice", "Bob"));
        }

        [Fact]
        public void Invertible_UnknownOrMissingVerb_ReadsAsPrimary()
        {
            // Lenient on purpose: a pending consent record written before a verb existed
            // should still resolve to something sane at completion time rather than throw.
            var spec = RoleSpec.Invertible("drinkfrom", "forcedrink");

            Assert.Equal("Alice", spec.ResolveActor("something-else", "Alice", "Bob"));
            Assert.Equal("Alice", spec.ResolveActor(null, "Alice", "Bob"));
            Assert.Equal("Alice", spec.ResolveActor("", "Alice", "Bob"));
        }

        [Fact]
        public void Invertible_ActorAndCounterpartAreAlwaysMirrors()
        {
            var spec = RoleSpec.Invertible("drinkfrom", "forcedrink");

            foreach (string verb in new[] { "drinkfrom", "forcedrink", "unknown", null })
            {
                string actor = spec.ResolveActor(verb, "Alice", "Bob");
                string counterpart = spec.ResolveCounterpart(verb, "Alice", "Bob");

                Assert.NotEqual(actor, counterpart);
                Assert.Contains(actor, new[] { "Alice", "Bob" });
                Assert.Contains(counterpart, new[] { "Alice", "Bob" });
            }
        }

        // -------------------------------------------------------------------
        // ResolveActorProfile — the base class's redirect helper
        // -------------------------------------------------------------------

        [Fact]
        public void ResolveActorProfile_FollowsTheVerb()
        {
            var spec = RoleSpec.Invertible("climaxfor", "climax");
            var alice = new Profile { userName = "Alice" };
            var bob = new Profile { userName = "Bob" };

            Assert.Same(alice, spec.ResolveActorProfile("climaxfor", alice, bob));
            Assert.Same(bob, spec.ResolveActorProfile("climax", alice, bob));
        }

        [Fact]
        public void ResolveActorProfile_EitherProfileNull_FallsBackToInitiator()
        {
            // Matches what every hand-written override did — a half-loaded pair is not the
            // moment to start guessing a direction.
            var spec = RoleSpec.Invertible("climaxfor", "climax");
            var alice = new Profile { userName = "Alice" };

            Assert.Same(alice, spec.ResolveActorProfile("climax", alice, null));
            Assert.Null(spec.ResolveActorProfile("climax", null, alice));
        }

        [Fact]
        public void ResolveActorProfile_FixedSpec_AlwaysInitiator()
        {
            var spec = RoleSpec.Fixed();
            var alice = new Profile { userName = "Alice" };
            var bob = new Profile { userName = "Bob" };

            Assert.Same(alice, spec.ResolveActorProfile("whatever", alice, bob));
        }

        // -------------------------------------------------------------------
        // The two shipped pairs declare the shape their own helpers promise
        // -------------------------------------------------------------------

        [Fact]
        public void ClimaxRoles_MatchResolveClimaxer()
        {
            foreach (string verb in new[] { ClimaxforProcessor.ClimaxforType, ClimaxforProcessor.ClimaxType })
            {
                Assert.Equal(
                    ClimaxforProcessor.ResolveClimaxer(verb, "Alice", "Bob"),
                    ClimaxforProcessor.ClimaxRoles.ResolveActor(verb, "Alice", "Bob"));
                Assert.Equal(
                    ClimaxforProcessor.ResolvePartner(verb, "Alice", "Bob"),
                    ClimaxforProcessor.ClimaxRoles.ResolveCounterpart(verb, "Alice", "Bob"));
            }
        }

        [Fact]
        public void ClimaxSelfTargetCollapse_IsNotTheSpecsJob()
        {
            // The solo-climax rule lives in ResolveClimaxer, not RoleSpec: source-drink has no
            // self-target path and must not inherit a collapse it never wanted.
            Assert.Equal("Alice", ClimaxforProcessor.ResolveClimaxer(ClimaxforProcessor.ClimaxType, "Alice", "Alice"));
            Assert.Null(ClimaxforProcessor.ResolvePartner(ClimaxforProcessor.ClimaxType, "Alice", "Alice"));

            Assert.Equal("Alice", ClimaxforProcessor.ClimaxRoles.ResolveActor(ClimaxforProcessor.ClimaxType, "Alice", "Alice"));
        }

        [Fact]
        public void DrinkRoles_MatchResolveDrinker()
        {
            foreach (string verb in new[] { SourceDrinkProcessor.DrinkFromType, SourceDrinkProcessor.ForceDrinkType })
            {
                Assert.Equal(
                    SourceDrinkProcessor.ResolveDrinker(verb, "Alice", "Bob"),
                    SourceDrinkProcessor.DrinkRoles.ResolveActor(verb, "Alice", "Bob"));
                Assert.Equal(
                    SourceDrinkProcessor.ResolveSource(verb, "Alice", "Bob"),
                    SourceDrinkProcessor.DrinkRoles.ResolveCounterpart(verb, "Alice", "Bob"));
            }
        }

        [Fact]
        public void ShippedPairs_DeclareTheVerbsTheyAreRegisteredUnder()
        {
            Assert.Equal(
                new[] { ClimaxforProcessor.ClimaxforType, ClimaxforProcessor.ClimaxType },
                ClimaxforProcessor.ClimaxRoles.Verbs.ToArray());
            Assert.Equal(
                new[] { SourceDrinkProcessor.DrinkFromType, SourceDrinkProcessor.ForceDrinkType },
                SourceDrinkProcessor.DrinkRoles.Verbs.ToArray());
        }
    }
}
