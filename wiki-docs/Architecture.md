# System Architecture

This page explains the overall architecture of FCDiceBot and how the major components interact.

For rules on keeping this page (and the rest of the wiki) accurate, see [README](README.md).

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Program.cs                            │
│      (Entry point; run modes -flist/-discord/-both/-none)    │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                        BotMain.cs                            │
│            (Core Bot Logic & Orchestration)                  │
│  - WebSocket connection to F-Chat                            │
│  - Outgoing message queue (rate-limited)                     │
│  - 200ms run loop                                            │
│  - MonDB.Initialize (must precede all DB access)             │
└──────┬──────────────┬──────────────┬────────────────────────┘
       │              │              │
       ▼              ▼              ▼
┌─────────────┐ ┌────────────┐ ┌──────────────────┐
│VelvetCuff   │ │BotCommand  │ │ChateauInteraction│
│Connection   │ │Controller  │ │Handler           │
└─────────────┘ └────────────┘ └──────────────────┘
       │              │              │
       ▼              ▼              ▼
┌─────────────┐ ┌────────────┐ ┌──────────────────┐
│BotWeb       │ │~210 Command│ │Interaction       │
│Requests     │ │Classes     │ │Processors        │
└─────────────┘ └────────────┘ └──────────────────┘
                     │              │
                     ▼              ▼
              ┌────────────┐ ┌──────────────┐
              │DiceBot     │ │MonDB         │
              │(Dice/Games)│ │(Database)    │
              └────────────┘ └──────────────┘
```

## Core Components

### 1. BotMain

**Location:** `FChatDicebot/BotMain.cs`

The heart of the bot, responsible for:

- **WebSocket Connection Management**
  - Connects to `wss://chat.f-list.net/chat2`
  - Handles authentication via F-List API tickets
  - Reconnects after `ReconnectTimeMs` (2 minutes) with SSL fallback

- **Message Queue System**
  - Rate-limited outgoing messages (`MinimumTimeBetweenMessages` = 1.5s, per F-List bot rules)
  - Scheduled future messages

- **Main Run Loop**
  - `TickTimeMiliseconds` = 200ms tick rate
  - Processes incoming messages, sends queued messages, updates game states
  - Polls VelvetCuff transactions
  - Periodic sweeps: group-interaction consent timeouts, ambient random events (`RandomEventEngine`)

- **Startup order:** `Run()` calls `MonDB.Initialize("mongodb://localhost:27017", "ChateauDb")` **before** constructing `BotCommandController` — processors bind to whatever database is set when they are first constructed, and `MonDB.GetDatabase()` throws if `Initialize` hasn't run.

- **File I/O Operations**
  - Loads and saves the legacy JSON data files under `C:\BotData\DiceBot\`
  - Copies them to `C:\BotData\DiceBot\ImmediateBackup` on startup

### 2. BotCommandController

**Location:** `FChatDicebot/BotCommandController.cs`

Command discovery, dispatch, and execution:

- **Reflection-based Command Discovery**
  - Scans the `BotCommands` namespace at startup; every `ChatBotCommand` subclass is loaded automatically, no registration needed (~210 commands as of mid-2026 — count them with the grep in [README](README.md) rather than trusting this number)
  - The `Aliases` array on each command routes as well as decorates: `FindCommandByName` dispatches on it, so an alias is one array entry, never a separate class

- **Permission System**
  - Bot admin checks (`AdminCharacters` in AccountSettings)
  - Channel admin checks (F-List ops)
  - Per-command requirements (`RequireBotAdmin`, `RequireChannelAdmin`, `RequireBotIsChannelAdmin`, `RequireChannel`)

- **Bare-name targeting**
  - Commands whose `Usage` documents a `[user]` slot accept a bare name too; a normalization pass over `rawTerms` resolves it (unrecognized names abort rather than silently self-targeting)

- **Thread Safety**
  - `CommandLockCategory`: `NONE`, `SavedTables`, `SavedChannels`, `ChannelDecks`, `ChannelScores`, `CharacterInventories`, `RPGData`, `ChannelOpsRequest`

### 3. ChateauInteractionHandler

**Location:** `FChatDicebot/ChateauInteractionHandler.cs`

Routes consented interactions to their processors and centralizes common error messages (user not found, not registered, invalid input).

### 4. InteractionProcessorRegistry

**Location:** `FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs`

Static registry of all interaction processors, keyed by interaction type. Every processor is registered explicitly in `Initialize()`. A few processors register under a second key because one processor serves two verbs:

- `LapsitProcessor` — `!sit` and `!lap`
- `CorruptionProcessor` — `!corrupt` and `!purify`
- `ClimaxforProcessor` — `!climaxfor` and `!climax`

**Processor folders** (under `FChatDicebot/InteractionProcessors/`):

| Folder | Processors |
|--------|------------|
| `Casual/` | kiss, cuddle, handhold, spank, bully, boobhat, lick, pet, lapsit |
| `Involved/` | feed, golden, dressup, milk, climaxfor, payment (give/receive) |
| `Commitment/` | mark, entitle, petrify, plant, objectify, consume, employ, bond, breed, train, corruption |
| `Consequence/` | monsterize, rename, odorize, break, dose, infest, curse |

### 5. Group interactions and cross-interaction hooks

- **`GroupInteractionResolver` + per-processor `GroupSpec`** — casual interactions can target several residents at once; each target gets their own pending seat, and the group resolves when everyone has consented/declined or the consent window (`PendingMinutesKeep`, 10 minutes) runs out.
- **`IStatusEffectContributor` + `StatusEffectRegistry`** — lets one interaction's lingering state (scents, corruption, parasites, curses…) contribute lines to other interactions' consent prompts, completions, and readouts.
- **`IPostInteractionEffect` + `PostInteractionEffectRegistry`** — lets a completed interaction affect other parties (e.g. parasite spread).

### 6. DiceBot

**Location:** `FChatDicebot/DiceFunctions/DiceBot.cs`

Legacy dice rolling and game management:

- **Dice Rolling** — complex expressions; limits are constants on `DiceBot` (`MaximumDice` 200, `MaximumRolls` 400, `MaximumSides` 10,000,000)
- **Card Deck Management** — playing/tarot/uno/custom decks, per-channel state, hands
- **Casino Chip System** — chip piles, betting, pots, VelvetCuff integration
- **Game Sessions** — one `IGame` implementation per game (`DiceFunctions/Games/`): AlphaRoyale, Blackjack, BottleSpin, Chess, DungeonDelve, HighRoll, KingsGame, LiarsDice, Mafia, Poker, PokerGame, PrizeRoll, RockPaperScissors, Roulette, SlamRoll

### 7. Database Layer

**Dual storage:**

- **MongoDB** (`ChateauDb`) behind `IChateauDatabase` (`FChatDicebot/Database/`), accessed almost everywhere through the static adapter `MonDB`. See [Database and Persistence](Database-and-Persistence.md) for the collection list and schemas.
- **JSON files** under `C:\BotData\DiceBot\` for legacy dicebot state (account settings, channel settings, chips, tables, decks, slots).

`MonDB.Initialize(connectionString, databaseName)` must run before anything touches the DB; `GetDatabase()` throws otherwise (deliberately fail-loud so a test or script can never silently write production data).

## Data Flow: User Input to Response

```
USER: types "!kiss [user]Target[/user]" (or "!kiss target") in a channel
  → F-List server sends MSG via WebSocket
  → BotMain.OnMessage(): parse message type, deserialize
  → BotMain.InterpretChatCommand(): extract command + terms
  → BotCommandController.RunChatBotCommand(): find ChateauKiss,
    check permissions, resolve bare-name target
  → ChateauKiss.Run(bot, commandController, rawTerms, terms, address, command):
    validate profiles, create PendingCommand in Mongo, post consent prompt
  → Channel: "Alice wants to give you a kiss, Bob. Do you !consent to a smooch? (or !no)"

TARGET: types "!consent" (or !no to decline; initiator can !oops to withdraw)
  → ChateauConsent.Run(): load pending command, sweep expired ones (10 min),
    resolve processor from InteractionProcessorRegistry
  → processor.ProcessInteraction(): increment counts (rate-limited),
    delete pending command
  → completion message assembled (status-effect fragments, eicons, titles)
  → BotMain queues the message; run loop sends it after the 1.5s gap
  → Channel: "Mwah! Alice and Bob share a kiss, cute."
```

## Design Patterns

- **Command** — each command is a `ChatBotCommand` subclass encapsulating metadata + execution.
- **Strategy / Registry** — `IInteractionProcessor` implementations looked up by type in `InteractionProcessorRegistry`.
- **Template Method** — `InteractionProcessorBase` owns the workflow (consent-warning assembly, status-effect composition, group handling); subclasses override the specific steps (`BuildConsentWarning`, `GetCompletionMessage`, …).
- **Adapter** — `MonDB` is a static facade over `IChateauDatabase`, enabling gradual migration to dependency injection.

## Rate Limiting

- **Messages:** 1.5s minimum between outgoing sends (F-List requirement).
- **Interactions:** per-interaction cooldowns, not one global scheme:
  - Casual and involved processors use a `RateLimit` constant (30 minutes as of mid-2026).
  - Commitment/consequence processors declare a structured `CooldownSpec` (`CooldownRule`) — period in days (typically 1 or 7), who it binds (initiator / recipient / pair / both), optional per-axis scope (e.g. per-vice per-recipient), or a daily magnitude quota (corrupt/purify). The spec is the single source of truth for both the consent-warning frequency clause and the `!help` cooldown fields.
  - Cooldowns are stored in the profile's `timers` dictionary as `CoolDown` records (`timerEnd`, UTC), keyed `ratelimit_{countLabel}` or per-direction keys such as `milk_give_<recipient>`.

## Extension Points

### Adding a New Command
1. Create a class in the `BotCommands` namespace inheriting `ChatBotCommand`
2. Set metadata in the constructor (`Name`, `Category`, `Usage`, `ShortDescription`, … — these drive `!help`)
3. Implement `Run(BotMain, BotCommandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)`
4. Discovery is automatic; the csproj globs `**\*.cs`, so no project-file edit either

### Adding a New Interaction
See the walkthrough in the [Development Guide](Development-Guide.md): processor class + command class + a `RegisterProcessor` line in `InteractionProcessorRegistry.Initialize()` + tests.

### Adding a New Game
1. Implement `IGame` (`FChatDicebot/DiceFunctions/Base/IGame.cs`)
2. Register it where `DiceBot` builds its game list
3. Players use the generic `!joingame` / `!startgame` / `!gamecommand` commands

## Configuration

- **`C:\BotData\DiceBot\account_settings.txt`** — JSON of `SavedData/AccountSettings.cs`: F-List account credentials, `CharacterName`, `AdminCharacters`, optional Discord/VelvetCuff credentials.
- **`C:\BotData\DiceBot\channel_settings.txt`** — per-channel command prefix, feature toggles (chips/games/tables/slots), starting chips, `AllowRandomEvents` opt-in.

## See Also

- [Installation and Setup](Installation-and-Setup) - How to deploy
- [Development Guide](Development-Guide) - How to extend
- [Database and Persistence](Database-and-Persistence) - Data storage details
- [Interaction System](Interaction-System) - Consent flow in depth
