using FChatDicebot.Tests.Fixtures;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Guards which commands opt in to bare-name targeting. The flag is derived from Usage
    /// (every targeting command documents the [user] slot) with an explicit override where
    /// Usage can't say it — the pending-lifecycle verbs document "{name}" and the short
    /// aliases document nothing. A command that silently loses the opt-in stops accepting
    /// bare names with no other visible symptom, so it's worth pinning down.
    /// </summary>
    [Collection("Database")]
    public class ChatBotCommandAcceptsRecipientTests
    {
        private readonly BotCommandController _controller;

        // The fixture guarantees MonDB.Initialize has run before the reflection-based
        // command loader instantiates the database-backed commands.
        public ChatBotCommandAcceptsRecipientTests(TestDatabaseFixture fixture)
        {
            _controller = new BotCommandController(null);
        }

        [Theory]
        // Derived from the [user] slot in Usage
        [InlineData("cuddle")]
        [InlineData("hug")]
        [InlineData("dose")]
        [InlineData("pay")]
        [InlineData("entitle")]
        [InlineData("dossier")]
        [InlineData("bank")]
        [InlineData("breed")]
        [InlineData("pledge")]
        [InlineData("climax")]
        [InlineData("corrupt")]
        [InlineData("lap")]
        // Explicit, because Usage documents "{name}" or nothing at all
        [InlineData("consent")]
        [InlineData("no")]
        [InlineData("oops")]
        [InlineData("c")]
        [InlineData("accept")]
        public void TargetingCommands_AcceptBareNames(string commandName)
        {
            var command = _controller.FindCommandByName(commandName);

            Assert.NotNull(command);
            Assert.True(command.ResolvesRecipient(), commandName + " should accept a bare recipient name");
        }

        [Theory]
        [InlineData("help")]
        [InlineData("roll")]
        [InlineData("statistics")]
        [InlineData("joinchateau")]
        [InlineData("category")]
        [InlineData("work")]
        public void NonTargetingCommands_DoNotTryToInferNames(string commandName)
        {
            var command = _controller.FindCommandByName(commandName);

            Assert.NotNull(command);
            Assert.False(command.ResolvesRecipient(), commandName + " does not target anyone");
        }
    }
}
