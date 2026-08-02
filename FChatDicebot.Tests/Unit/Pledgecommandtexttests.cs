using FChatDicebot;
using FChatDicebot.BotCommands;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.Tests.Fixtures;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// The private error !fulfill and !abandonpledge hand back when the resident's input
    /// doesn't match one of their pledges. Pure helper, but the registry builds its
    /// processors with the parameterless constructor (which resolves MonDB), so the class
    /// still needs the database fixture.
    /// </summary>
    [Collection("Database")]
    public class PledgeCommandTextTests
    {
        public PledgeCommandTextTests(TestDatabaseFixture fixture)
        {
            fixture.Reset();
        }

        [Fact]
        public void NoMatchingPledgeText_KnownType_NamesTheVerbAndTheRecipient()
        {
            var processor = InteractionProcessorRegistry.GetProcessor("feed");

            string message = ChateauInteractionHandler.noMatchingPledgeText(processor, "feed", "Bob");

            Assert.Contains("feed", message);
            Assert.Contains("Bob", message);
        }

        [Fact]
        public void NoMatchingPledgeText_PointsAtTheCommandThatListsPledges()
        {
            var processor = InteractionProcessorRegistry.GetProcessor("feed");

            string message = ChateauInteractionHandler.noMatchingPledgeText(processor, "feed", "Bob");

            // Derived from the command itself so a second rename can't leave this stale the
            // way "!viewpledges" was left behind.
            Assert.Contains("!" + new ChateauPledges().Name, message);
            Assert.DoesNotContain("!viewpledges", message);
        }

        [Fact]
        public void NoMatchingPledgeText_UnknownType_AnswersTheTypoInsteadOfThrowing()
        {
            // The real production path: a misspelled verb resolves to no processor at all,
            // and both commands used to dereference a null while building this very message.
            var processor = InteractionProcessorRegistry.GetProcessor("cuddel");
            Assert.Null(processor);

            string message = ChateauInteractionHandler.noMatchingPledgeText(processor, "cuddel", "Bob");

            Assert.Contains("cuddel", message);
            Assert.Contains("spelled it correctly", message);
        }

        [Fact]
        public void NoMatchingPledgeText_UnknownType_MatchesWhatPledgeSaysAboutTheSameTypo()
        {
            // !pledge, !fulfill and !abandonpledge take the same argument, so a misspelling
            // gets one answer.
            Assert.Equal(
                ChateauInteractionHandler.interactionTypeNotFoundText("cuddel"),
                ChateauInteractionHandler.noMatchingPledgeText(null, "cuddel", "Bob"));
        }
    }
}
