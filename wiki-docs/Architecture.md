# System Architecture

This page explains the overall architecture of FCDiceBot and how the major components interact.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Program.cs                            │
│                    (Application Entry)                       │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                        BotMain.cs                            │
│            (Core Bot Logic & Orchestration)                  │
│  - WebSocket Management                                      │
│  - Message Queue Processing                                  │
│  - Connection Lifecycle                                      │
│  - File I/O & Persistence                                    │
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
│BotWeb       │ │143 Command │ │Interaction       │
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

### 1. BotMain (938 lines)

**Location:** `FChatDicebot/FChatDicebot/BotMain.cs`

The heart of the bot, responsible for:

- **WebSocket Connection Management**
  - Connects to `wss://chat.f-list.net/chat2`
  - Handles authentication via F-List API tickets
  - Manages reconnection with SSL fallback
  - 5-minute timeout with automatic reconnection

- **Message Queue System**
  - Rate-limited outgoing messages (1.5s minimum between sends)
  - Priority queue for important messages
  - Scheduled future messages

- **Main Run Loop**
  - 200ms tick rate
  - Processes incoming messages
  - Sends queued messages
  - Updates game states
  - Polls VelvetCuff transactions

- **Channel Management**
  - Tracks joined channels
  - Handles channel joins/leaves
  - Per-channel state management

- **File I/O Operations**
  - Loads and saves JSON configuration files
  - Automatic backup on startup

**Key Methods:**
- `Run()` - Initialize and start the bot
- `RunLoop()` - Main 200ms tick loop
- `OnMessage(string data)` - Handle incoming WebSocket messages
- `InterpretChatCommand()` - Parse user commands
- `SendMessageInChannel()` / `SendPrivateMessage()` - Send messages
- `GetNewApiTicket()` - Authenticate with F-List

### 2. BotCommandController (442 lines)

**Location:** `FChatDicebot/FChatDicebot/BotCommandController.cs`

Command discovery, dispatch, and execution:

- **Reflection-based Command Discovery**
  - Scans `BotCommands` namespace at startup
  - Automatically loads all command classes
  - No manual registration needed
  - Currently: 143 commands

- **Permission System**
  - Bot admin checks (defined in AccountSettings)
  - Channel admin checks (F-List ops)
  - Registered user checks (Chateau database)
  - Per-command requirements

- **Thread Safety**
  - Lock categories: SavedTables, SavedChannels, ChannelDecks, ChannelScores
  - Prevents race conditions on shared data

- **Helper Methods**
  - Parse `[user]Name[/user]` tags
  - Parse table references
  - Sanitize and lowercase input

**Key Methods:**
- `LoadChatBotCommands()` - Discover commands via reflection
- `RunChatBotCommand()` - Validate permissions and execute
- `GetUserNameFromCommandTerms()` - Parse user tags
- `SaveCharacterDataToDisk()` - Persist data

### 3. ChateauInteractionHandler

**Location:** `FChatDicebot/FChatDicebot/ChateauInteractionHandler.cs`

Routes interactions to processors or legacy handler:

- **Processor-based System** (New)
  - Modular interaction handling
  - Registry pattern for processor lookup
  - Strategy pattern for different interaction types

- **Legacy Switch Statement** (Deprecated)
  - Old monolithic interaction handling
  - Being gradually migrated to processors

- **Common Error Messages**
  - User not found
  - Not registered
  - Mark not set
  - Invalid input

### 4. InteractionProcessorRegistry

**Location:** `FChatDicebot/FChatDicebot/InteractionProcessors/InteractionProcessorRegistry.cs`

Manages all interaction processors:

- **Registry Pattern**
  - Central registration of all processors
  - Type-based lookup
  - Investment level filtering

- **Processor Categories**
  - Casual: kiss, cuddle, handhold, spank, bully
  - Involved: feed, golden, dressup
  - Commitment: mark, entitle
  - Consequence: rename, monsterize, petrify, plant, objectify, consume, employ, bond

- **Transaction Processors**
  - Payment (give/receive)

**Key Methods:**
- `Initialize()` - Register all processors
- `GetProcessor(string type)` - Get processor by interaction type
- `GetProcessorsByInvestmentLevel(string level)` - Filter by investment level

### 5. DiceBot

**Location:** `FChatDicebot/FChatDicebot/DiceFunctions/DiceBot.cs`

Dice rolling and game management:

- **Dice Rolling**
  - Supports complex expressions
  - Up to 200 dice, 400 rolls, 10M sides
  - Operators: +, -, *, /, >, <

- **Card Deck Management**
  - Multiple deck types (Playing, Tarot, Uno, Custom)
  - Per-channel deck state
  - Player hands and collections

- **Casino Chip System**
  - Chip balances
  - Betting and pots
  - VelvetCuff integration

- **Game Sessions**
  - 10+ game types
  - Per-channel, per-game sessions
  - Turn-based state management

### 6. Database Layer

**Dual Storage System:**

#### MongoDB (Primary)
**Location:** `FChatDicebot/FChatDicebot/Database/Chateaudatabase.cs`

- Database: "ChateauDb"
- Collections:
  - RegisteredProfiles - User data
  - Interactions - Historical records
  - PendingCommands - Consent workflow
  - Duties - Job definitions
  - PendingDuties - Active work
  - Identifiers - Category taxonomy

#### File Storage (Legacy)
**Location:** `C:\BotData\DiceBot\`

- JSON format
- Used for: account settings, channel settings, chips, tables, decks, slots, coupons

#### MonDB Adapter
**Location:** `FChatDicebot/FChatDicebot/MonDB.cs`

- Static facade over IChateauDatabase
- Backward compatibility layer
- Enables gradual migration to dependency injection

## Data Flow: User Input to Response

```
┌─────────────────────────────────────────────────────────────┐
│ USER: Types "!kiss [user]Target[/user]" in F-List channel   │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ F-List Server: Sends MSG message via WebSocket              │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ BotMain.OnMessage()                                          │
│  - Parse message type and content                            │
│  - Deserialize JSON                                          │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ BotMain.InterpretChatCommand()                               │
│  - Extract command: "kiss"                                   │
│  - Extract terms: ["[user]Target[/user]"]                    │
│  - Create UserGeneratedCommand                               │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ BotCommandController.RunChatBotCommand()                     │
│  - Find ChateauKiss command                                  │
│  - Check permissions                                         │
│  - Sanitize input                                            │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ ChateauKiss.Run()                                            │
│  - Parse target from [user] tags                             │
│  - Validate target exists                                    │
│  - Create PendingCommand                                     │
│  - Save to database                                          │
│  - Send consent request                                      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ Channel: "User wants to kiss Target! Do you !consent?"      │
└─────────────────────────────────────────────────────────────┘

                    [Target consents...]

┌─────────────────────────────────────────────────────────────┐
│ ChateauConsent.Run()                                         │
│  - Get pending interaction                                   │
│  - Get KissProcessor from registry                           │
│  - Execute ProcessInteraction()                              │
│  - Get completion message                                    │
│  - Check achievements                                        │
│  - Queue message                                             │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ BotMain.RunLoop()                                            │
│  - Wait 1.5s since last message                              │
│  - Dequeue message                                           │
│  - Send via WebSocket                                        │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ Channel: "Mwah! User and Target share a kiss, cute."        │
└─────────────────────────────────────────────────────────────┘
```

## Design Patterns

### 1. Command Pattern
Each command is a separate class inheriting from `ChatBotCommand`. Commands encapsulate their execution logic and metadata.

### 2. Strategy Pattern
`IInteractionProcessor` interface allows different interaction types to be handled by different strategies (processors).

### 3. Registry Pattern
`InteractionProcessorRegistry` maintains a registry of all processors and provides type-based lookup.

### 4. Adapter Pattern
`MonDB` serves as a static adapter over `IChateauDatabase`, enabling backward compatibility.

### 5. Template Method Pattern
`InteractionProcessorBase` provides template methods with common workflow, allowing subclasses to customize specific steps.

### 6. Facade Pattern
`BotMain` acts as a facade, simplifying access to complex subsystems.

### 7. Observer Pattern
WebSocket event handlers (`OnMessage`, `OnError`, `OnClose`) implement observer pattern for event-driven architecture.

### 8. Queue Pattern
`BotMessageQueue` implements a priority queue with rate limiting.

## Threading Model

### Main Thread
- WebSocket message handling
- Command execution
- Message queue processing
- Game state updates

### Thread Safety
- Locks on shared data (tables, channels, decks, scores)
- Lock categories defined per command
- Prevents concurrent modification

### Rate Limiting
- 1.5 second minimum between outgoing messages (F-List requirement)
- Per-user interaction cooldowns (1 hour casual, 30 min involved, daily for commitment/consequence)
- Database-backed timer system

## Extension Points

### Adding a New Command
1. Create class in `BotCommands` namespace
2. Inherit from `ChatBotCommand`
3. Implement `Run()` method
4. Automatic discovery via reflection

### Adding a New Interaction
1. Create processor class implementing `IInteractionProcessor`
2. Implement required methods
3. Register in `InteractionProcessorRegistry.Initialize()`
4. Create command class to initiate interaction

### Adding a New Game
1. Implement `IGame` interface
2. Add to `DiceBot` game management
3. Create game commands (join, start, action, etc.)

## Configuration

### Account Settings
`C:\BotData\DiceBot\account_settings.txt`
- F-List credentials
- Character name
- Admin list

### Channel Settings
`C:\BotData\DiceBot\channel_settings.txt`
- Per-channel command prefix
- Feature toggles (chips, games, slots)
- Starting chip amounts
- Slot multiplier limits

## Performance Considerations

### Message Rate Limiting
- F-List enforces rate limits
- Bot respects 1.5s between messages
- Queue prevents message loss

### Database Queries
- MongoDB indexed by userName
- Profile lookups are O(log n)
- Interaction history queries can be expensive for large datasets

### Memory Usage
- Deck state per channel
- Game sessions per channel per game type
- Chip balances loaded at startup
- Pending commands expire after 10 minutes

## Error Handling

### WebSocket Disconnection
- Automatic reconnection attempts
- 5-minute timeout
- SSL fallback if HTTPS connection fails

### Database Errors
- Graceful fallback to file-based storage where possible
- Error logging
- User-friendly error messages

### Command Errors
- Validation before execution
- Permission checks
- Clear error messages to users
- No crash on invalid input

## See Also

- [Installation and Setup](Installation-and-Setup) - How to deploy
- [Development Guide](Development-Guide) - How to extend
- [Database and Persistence](Database-and-Persistence) - Data storage details
