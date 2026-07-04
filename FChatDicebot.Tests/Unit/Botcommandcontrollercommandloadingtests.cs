using FChatDicebot.Tests.Fixtures;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Regression guard for command loading vs MonDB initialization order. The DI-style
    /// commands (ChateauDossier, ChateauBondtree, ChateauFamilytree) resolve
    /// MonDB.GetDatabase() in the reflection-friendly parameterless constructor the
    /// command loader uses, so they only load when MonDB was initialized BEFORE
    /// BotCommandController is constructed. BotMain.Run was once ordered the other way
    /// around, which made the loader silently skip all three commands on a live bot
    /// (the loader logs-and-continues per command type by design, so nothing crashed —
    /// !dossier just stopped existing).
    /// </summary>
    [Collection("Database")]
    public class BotCommandControllerCommandLoadingTests
    {
        // The fixture parameter is what matters: it guarantees MonDB.Initialize has run
        // (pointing at the test database) before the controller loads command types.
        public BotCommandControllerCommandLoadingTests(TestDatabaseFixture fixture)
        {
        }

        [Theory]
        [InlineData("dossier")]
        [InlineData("bondtree")]
        [InlineData("familytree")]
        public void DatabaseBackedCommands_LoadWhenMonDbIsInitializedFirst(string commandName)
        {
            var controller = new BotCommandController(null);

            Assert.NotNull(controller.FindCommandByName(commandName));
        }
    }
}
