using FChatDicebot.InteractionProcessors;
using FChatDicebot.Model;
using FChatDicebot.Tests.Builders;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace FChatDicebot.Tests.Unit.InteractionProcessors
{
    /// <summary>
    /// Locks the "every consent prompt names !no" rule. The reminder is appended once, by
    /// <see cref="InteractionProcessorBase.GetConsentWarning"/> /
    /// <see cref="InteractionProcessorBase.GetGroupConsentWarning"/>, so these tests guard the
    /// helper's behaviour and the structural fact that no processor can slip past the wrapper.
    /// </summary>
    [Collection("Database")]
    public class ConsentDeclineHintTests
    {
        // The registry builds every processor with its parameterless constructor, which
        // resolves MonDB.GetDatabase() — so this class needs the database fixture even though
        // none of the prompts under test read from it.
        public ConsentDeclineHintTests(TestDatabaseFixture fixture)
        {
            fixture.Reset();
        }

        // -------------------------------------------------------------------
        // WithDeclineHint
        // -------------------------------------------------------------------

        [Fact]
        public void WithDeclineHint_AppendsHintAfterThePrompt()
        {
            Assert.Equal(
                "Alice wants to pet Bob. Do you !consent to some scritches? (or !no)",
                ConsentWarningText.WithDeclineHint("Alice wants to pet Bob. Do you !consent to some scritches?"));
        }

        [Fact]
        public void WithDeclineHint_IsIdempotent()
        {
            string once = ConsentWarningText.WithDeclineHint("Do you !consent?");
            string twice = ConsentWarningText.WithDeclineHint(once);

            Assert.Equal(once, twice);
        }

        [Fact]
        public void WithDeclineHint_HintAlreadyMidString_NotAppendedAgain()
        {
            // What a fragment-carrying prompt looks like: the reminder sits with the question
            // and status-effect fragments trail it.
            string composed = "Do you !consent? (or !no) [musk lingers]";

            Assert.Equal(composed, ConsentWarningText.WithDeclineHint(composed));
        }

        [Fact]
        public void WithDeclineHint_BespokeVariant_SuppressesTheDefault()
        {
            string composed = "The first to !consent gets to be under Alice! "
                + ConsentWarningText.DeclineHintVariant("to opt out entirely");

            Assert.Equal("The first to !consent gets to be under Alice! (or !no to opt out entirely)", composed);
            Assert.Equal(composed, ConsentWarningText.WithDeclineHint(composed));
        }

        [Fact]
        public void WithDeclineHint_CollapsesTrailingWhitespaceBeforeTheHint()
        {
            Assert.Equal("Do you !consent? (or !no)", ConsentWarningText.WithDeclineHint("Do you !consent?   "));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void WithDeclineHint_EmptyPrompt_LeftAlone(string prompt)
        {
            Assert.Equal(prompt, ConsentWarningText.WithDeclineHint(prompt));
        }

        // -------------------------------------------------------------------
        // Structural: nothing bypasses the shared entry points
        // -------------------------------------------------------------------

        [Fact]
        public void EveryRegisteredProcessor_InheritsTheSharedConsentEntryPoints()
        {
            var offenders = new List<string>();

            foreach (var processor in DistinctRegisteredProcessors())
            {
                Type type = processor.GetType();

                MethodInfo single = type.GetMethod("GetConsentWarning",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(Profile), typeof(Profile), typeof(string) },
                    null);
                if (single == null || single.DeclaringType != typeof(InteractionProcessorBase))
                {
                    offenders.Add(type.Name + ".GetConsentWarning");
                }

                MethodInfo group = type.GetMethod("GetGroupConsentWarning",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(Profile), typeof(IReadOnlyList<Profile>), typeof(string) },
                    null);
                if (group == null || group.DeclaringType != typeof(InteractionProcessorBase))
                {
                    offenders.Add(type.Name + ".GetGroupConsentWarning");
                }
            }

            Assert.True(offenders.Count == 0,
                "These processors shadow the shared consent entry point, so their prompts would "
                + "lose the '" + ConsentWarningText.DeclineHint + "' reminder. Override "
                + "BuildConsentWarning / BuildGroupConsentWarning instead: "
                + string.Join(", ", offenders));
        }

        // -------------------------------------------------------------------
        // Behavioural: the casual prompts (the ones that need no database) really carry it
        // -------------------------------------------------------------------

        [Theory]
        [InlineData("kiss", null)]
        [InlineData("cuddle", null)]
        [InlineData("handhold", null)]
        [InlineData("spank", null)]
        [InlineData("bully", null)]
        [InlineData("boobhat", null)]
        [InlineData("lick", null)]
        [InlineData("pet", null)]
        [InlineData("lap", "lap")]
        [InlineData("sit", "sit")]
        public void CasualConsentPrompts_EndWithTheDeclineHint(string interactionType, string identifier)
        {
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").Build();
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").Build();
            var carol = new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").Build();

            var processor = InteractionProcessorRegistry.GetProcessor(interactionType);
            Assert.NotNull(processor);

            string single = processor.GetConsentWarning(alice, bob, identifier);
            Assert.EndsWith(ConsentWarningText.DeclineHint, single);

            // The !sit lap stack closes with its own phrasing rather than the bare hint.
            string group = processor.GetGroupConsentWarning(alice, new List<Profile> { bob, carol }, identifier);
            Assert.EndsWith(")", group);
            Assert.Contains("(or !no", group);
        }

        [Fact]
        public void LapStackGroupPrompt_SpellsOutWhatDecliningMeans()
        {
            var alice = new ProfileBuilder().WithUserName("Alice").WithDisplayName("Alice").Build();
            var bob = new ProfileBuilder().WithUserName("Bob").WithDisplayName("Bob").Build();
            var carol = new ProfileBuilder().WithUserName("Carol").WithDisplayName("Carol").Build();

            string sit = InteractionProcessorRegistry.GetProcessor("sit")
                .GetGroupConsentWarning(alice, new List<Profile> { bob, carol }, "sit");

            Assert.Equal(
                "Alice wants to start a lap stack with Bob and Carol. The first to !consent "
                + "gets to be under Alice! (or !no to opt out entirely)",
                sit);
        }

        // -------------------------------------------------------------------
        // Ordering: the reminder stays with the question, trailing notes follow it
        // -------------------------------------------------------------------

        [Fact]
        public void BreakConsentPrompt_ClampNoteIsSmallPrintAfterTheReminder()
        {
            string clamped = FChatDicebot.InteractionProcessors.Consequence.BreakProcessor.ComposeConsentText(
                "Alice", "Bob", "arm", 1, FChatDicebot.InteractionProcessors.Consequence.BreakProcessor.ClampDirection.Min);

            Assert.Contains("Do you !consent? (or !no) [sub]Duration was adjusted to the minimum of", clamped);
            Assert.EndsWith("[/sub]", clamped);
            Assert.DoesNotContain("(Duration was adjusted", clamped);
        }

        [Fact]
        public void BreakConsentPrompt_NoClamp_EndsWithTheReminder()
        {
            string unclamped = FChatDicebot.InteractionProcessors.Consequence.BreakProcessor.ComposeConsentText(
                "Alice", "Bob", "arm", 3, FChatDicebot.InteractionProcessors.Consequence.BreakProcessor.ClampDirection.None);

            Assert.EndsWith(ConsentWarningText.DeclineHint, unclamped);
        }

        private static IEnumerable<IInteractionProcessor> DistinctRegisteredProcessors()
        {
            // Lapsit / corruption / climax each register one instance under two verb keys.
            return InteractionProcessorRegistry.GetAllInteractionTypes()
                .Select(InteractionProcessorRegistry.GetProcessor)
                .Where(p => p != null)
                .GroupBy(p => p.GetType())
                .Select(g => g.First());
        }
    }
}
