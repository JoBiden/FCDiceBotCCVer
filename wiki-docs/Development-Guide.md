# Development Guide

This guide explains how to extend and customize FCDiceBot by adding new features, commands, and interactions.

When your change invalidates any wiki page, update it in the same PR — see [README](README.md) for the documentation rules.

## Development Environment Setup

### Prerequisites

1. **.NET Framework 4.8 developer pack** (the main project is a legacy non-SDK csproj) and the **dotnet CLI** (the test project is SDK-style)
2. **MongoDB** running locally at `mongodb://localhost:27017` (required by the bot *and* the tests)
3. **Git**
4. **F-List account** (only for running the bot live)

### Getting Started

```
git clone <repo>
dotnet build FChatDicebot.sln
dotnet test FChatDicebot.Tests\FChatDicebot.Tests.csproj
```

NuGet packages are committed under `packages\`. Visual Studio works too, but **don't add files through the VS Solution Explorer** — `FChatDicebot.csproj` globs `**\*.cs`, and the IDE can expand that wildcard back into explicit entries. A new `.cs` file builds with no csproj edit. (`Model\MonsterGenerator\**` is deliberately excluded: unported code that cannot compile.)

Running the bot for real needs `C:\BotData\DiceBot\account_settings.txt` (JSON of `SavedData/AccountSettings.cs`) plus MongoDB. Run modes: `-flist` (default), `-discord`, `-both`, `-none`.

### Project Structure

```
FCDiceBotCCVer/
├── FChatDicebot.sln
├── FChatDicebot/
│   ├── BotMain.cs                      # Core bot logic, run loop, MonDB.Initialize
│   ├── BotCommandController.cs         # Command discovery + dispatch
│   ├── MonDB.cs                        # Static DB adapter
│   ├── BotCommands/                    # ~210 command classes (one per command)
│   │   └── Base/ChatBotCommand.cs      # Base class + help metadata fields
│   ├── InteractionProcessors/          # Consent-based interactions
│   │   ├── IInteractionProcessor.cs
│   │   ├── InteractionProcessorBase.cs
│   │   ├── InteractionProcessorRegistry.cs
│   │   ├── CooldownSpec.cs / ConsentWarningText.cs
│   │   ├── GroupInteractionResolver.cs / GroupSpec.cs
│   │   ├── StatusEffectRegistry.cs / PostInteractionEffectRegistry.cs
│   │   └── Casual/ Involved/ Commitment/ Consequence/
│   ├── Database/                       # IChateauDatabase + Mongo implementation
│   ├── DiceFunctions/                  # Legacy dicebot (dice, decks, chips, games)
│   ├── Model/ChateauDB.cs              # Profile, Interaction, PendingCommand, ...
│   └── SavedData/                      # JSON-file config classes
├── FChatDicebot.Tests/                 # xUnit tests (SDK-style project)
│   ├── Fixtures/Testdatabasefixture.cs
│   ├── Unit/  Integration/  Builders/
│   └── TestConfiguration.cs            # ChateauDb_Test @ localhost:27017
├── scripts/                            # Operational tooling (see CLAUDE.md)
└── wiki-docs/                          # This documentation
```

## Adding a New Command

Create one file in `BotCommands/`. Reflection discovers it at startup; the csproj glob picks it up at build. There is no registration step.

The `Run` signature takes a `MessageAddress` (character + channel), not separate name/channel strings:

```csharp
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauWave : ChatBotCommand
    {
        public ChateauWave()
        {
            Name = "wave";
            Aliases = new string[] { };          // extra names that also dispatch here
            Category = "Casual Interaction";     // groups the command in !help
            ShortDescription = "Wave at another resident";
            LongDescription = "Longer explanation shown by !help wave.";
            Usage = "!wave [noparse][user]NameInUserTag[/user][/noparse]";
            RelatedCommands = new string[] { "kiss", "consent" };
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController,
            string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            string target = commandController.GetUserNameFromCommandTerms(rawTerms);
            // ... validate, act, respond via bot.SendMessageInChannel / bot.SendPrivateMessage
        }
    }
}
```

### Command metadata

The base-class fields on `ChatBotCommand` drive `!help` and dispatch — see `BotCommands/Base/ChatBotCommand.cs` for the authoritative list and doc comments. The ones that trip people up:

| Field | Notes |
|-------|-------|
| `Aliases` | Single source of truth for aliases — they **route**, not just display. One array entry per alias; never a delegating class. A name already used by another command's `Name` is ignored. |
| `Usage` | Also drives bare-name targeting: a `[user]` slot in `Usage` makes the command accept a bare name. Set `AcceptsRecipient` explicitly only where `Usage` can't express it. |
| `CooldownDuration` / `CooldownAppliesTo` | For interactions, derive these from the processor's `CooldownSpec` (`FormatDuration()` / `FormatAppliesTo()`) — never hand-write strings that can drift. |
| `LockCategory` | One of `NONE`, `SavedTables`, `SavedChannels`, `ChannelDecks`, `ChannelScores`, `CharacterInventories`, `RPGData`, `ChannelOpsRequest`. |

User-facing strings (help text included) follow the [Style Guide](Style-Guide.md), and every changed string goes to the owner for wording approval before the work is called done.

## Adding a New Interaction

Interactions are the two-phase consent flow: an initiating command writes a `PendingCommand`, the target's `!consent` resolves the processor and runs it. Adding one means **four things**: processor class, command class, a registry line, and tests.

### Step 1: Processor class

Put it under `Casual/`, `Involved/`, `Commitment/`, or `Consequence/`. Model it on a real neighbor of the same tier — `KissProcessor` for a symmetric casual, `SpankProcessor` for a directional one, `MarkProcessor` for an identifier-taking commitment. The skeleton:

```csharp
public class WaveProcessor : InteractionProcessorBase
{
    public override string InteractionType => "wave";
    public override string InvestmentLevel => "casual";
    private static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(30);

    // Both constructors: DI for tests, parameterless for the registry.
    public WaveProcessor(IChateauDatabase database) : base(database) { }
    public WaveProcessor() : base() { }

    public override string ProcessInteraction(PendingCommand command)
    {
        string initiator = command.pendingInteraction.initiator;
        string recipient = command.pendingInteraction.recipient;
        _lastRateLimitMessage = IncrementBothCountsWithRateLimit(initiator, recipient, "wave", RateLimit);
        Database.DeletePendingCommand(command.Id);
        return "wave";
    }

    // Override the builder, NEVER GetConsentWarning — the non-virtual entry point
    // appends the "(or !no)" decline reminder to whatever you return here.
    protected override string BuildConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier)
    {
        return initiatorProfile.displayName + " wants to wave at you, "
            + recipientProfile.displayName + ". Do you !consent to a greeting?";
    }

    public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
    {
        return $"{initiatorProfile.displayName} waves at {recipientProfile.displayName}. How friendly!";
    }
}
```

Things a real processor may also need (all defined on `InteractionProcessorBase` / `IInteractionProcessor` — read those files, they are heavily commented):

- **`CooldownRule`** (a `CooldownSpec`) for commitment/consequence tiers — the single source of truth for the cooldown's period, who it binds, and the derived help/consent-warning text.
- **`GroupSpec`** to opt a casual into multi-target groups (`GroupSpec.Symmetric(...)` etc.).
- **`ValidateInteraction`** when an identifier or precondition must be checked before the consent prompt goes out.
- **`GetInteractionVerb(VerbTense)`** when the verb isn't a regular `-ed`/`-s` form (pattern: `BullyProcessor`).
- **Status effects**: compose with `ComposeConsentWarning(baseWarning, effects.ConsentWarnings)` so the decline reminder stays attached to the question.
- **Eicons**: `EiconAppliesToBothParties`, `BodypartEiconRule`.

### Step 2: Register it

Add one line in `InteractionProcessorRegistry.Initialize()`:

```csharp
RegisterProcessor(new WaveProcessor());
```

### Step 3: Command class

Model it on the initiating command of a neighbor (e.g. `BotCommands/ChateauKiss.cs`): build an `Interaction`, wrap it in a `PendingCommand` (`pendingInteraction`, `awaitingConsentFrom`), save via `MonDB.addPendingCommand`, then let **the processor** produce the consent prompt:

```csharp
var processor = InteractionProcessorRegistry.GetProcessor("wave");
string message = processor.GetConsentWarning(initiatorProfile, recipientProfile, "");
bot.SendMessageInChannel(message, channel);
```

### Step 4: Tests

Add tests in `FChatDicebot.Tests` (see Testing below). Processor tests join the `Database` collection and construct the processor with the fixture's database.

## Working with the Database

Everything goes through `IChateauDatabase`; most legacy call sites use the static adapter `MonDB` (lowercase method names). The real API surface lives in `FChatDicebot/MonDB.cs` and `FChatDicebot/Database/Ichateaudatabase.cs` — check there rather than guessing. Frequently used:

```csharp
Profile profile = MonDB.getProfile(userName);        // null if not registered
MonDB.setProfile(userName, profile);                 // note: two arguments
MonDB.incrementCount(userName, "kiss");              // +1
MonDB.changeCount(userName, "kiss", 5);              // arbitrary delta
MonDB.changeCurrency(userName, "copper", -10);       // atomic currency change
MonDB.addPendingCommand(pending);
List<PendingCommand> waiting = MonDB.getPending(userName);
MonDB.removePendingInteraction(pending.Id);
MonDB.addInteraction(interaction);                   // history record
Identifier id = MonDB.getIdentifier("collar");       // throws-if-uninitialized path
Identifier idOrNull = MonDB.tryGetIdentifier("collar"); // non-throwing, for display fallbacks
```

Inside a processor, prefer the injected `Database` (an `IChateauDatabase`) over `MonDB` — that's what makes the processor testable.

Rate limits are stored in `profile.timers` as `CoolDown { timerStart, timerEnd }` (UTC), keyed `ratelimit_{countLabel}`. `IncrementCountWithRateLimit` refuses the increment and returns `false` while `timerEnd` is in the future.

## Testing

The test project **already exists**: `FChatDicebot.Tests` (xUnit, SDK-style). It requires a local MongoDB and uses the `ChateauDb_Test` database (`TestConfiguration.cs`).

```
dotnet test FChatDicebot.Tests\FChatDicebot.Tests.csproj
dotnet test FChatDicebot.Tests\FChatDicebot.Tests.csproj --filter "FullyQualifiedName~Bondprocessortests"
```

Rules of the road:

- **Parallelization is disabled assembly-wide on purpose.** The processor registry and `MonDB` are process-wide statics that bind to a database exactly once — see the comment block in `Fixtures/Testdatabasefixture.cs` before touching this.
- **DB-touching tests** join the `Database` collection, take `TestDatabaseFixture` in the constructor, and call `fixture.Reset()` for isolation:

```csharp
[Collection("Database")]
public class WaveProcessorTests
{
    private readonly TestDatabaseFixture _fixture;

    public WaveProcessorTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
    }

    [Fact]
    public void ProcessInteraction_IncrementsBothCounts()
    {
        var processor = new WaveProcessor(_fixture.Database);
        // seed profiles via _fixture.Database, build a PendingCommand, assert counts
    }
}
```

- **Pure helpers** (text formatting, spec parsing) get plain unit tests with no fixture.
- `ConsentDeclineHintTests` fails the build if a processor shadows `GetConsentWarning` — that's intended.

## Best Practices

1. **Validate input** before acting; unrecognized targets should abort with a clear message, never silently self-target.
2. **Check registration** — `MonDB.getProfile` returns null for unregistered characters; route the error text through `ChateauInteractionHandler`'s helpers where one exists.
3. **Single sources of truth** — cooldown/consent text via `CooldownSpec`/`ConsentWarningText`; readout formatting via `ReadoutText`/`ReadoutDomain`; themed display via `ViceText`/`ScentText`/`ParasiteText`; identifier display via `Utils.*ToText`. Never inline these.
4. **Match the style guide** for every user-facing string, and list changed strings (before → after) for owner approval.
5. **Never say "UTC day"** to players — it's "Chateau day" or just "day".
6. **Use the right `LockCategory`** when touching shared legacy state (tables, decks, chips).

## Debugging

- `Utils.AddToLog(...)` writes to the bot log.
- Inspect Mongo directly with `mongosh`:

```
use ChateauDb
db.RegisteredProfiles.find({userName: "Alice"})
db.PendingCommands.find({awaitingConsentFrom: "Bob"})
```

- Breakpoints work normally under Visual Studio (run mode `-none` starts the bot without connecting to F-List, useful for startup debugging).

## Contributing

- Branch from `main`; PRs target `main`. Never merge PRs or push to `main` directly — merging is the owner's call.
- Update the docs a change invalidates: the relevant `wiki-docs/specs/*.md` as-shipped notes, and [Command-Reference](Command-Reference.md) when help text or usage changes. See [wiki-docs/README](README.md).

## See Also

- [Architecture](Architecture) - System design details
- [Database and Persistence](Database-and-Persistence) - Database operations
- [Interaction System](Interaction-System) - Interaction mechanics
- [Command Reference](Command-Reference) - Existing commands
- [Style Guide](Style-Guide.md) - User-facing text rules
