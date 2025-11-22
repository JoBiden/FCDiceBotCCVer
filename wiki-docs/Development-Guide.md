# Development Guide

This guide explains how to extend and customize FCDiceBot by adding new features, commands, and interactions.

## Development Environment Setup

### Prerequisites

1. **Visual Studio 2013 or later**
2. **MongoDB** (for testing)
3. **Git** (for version control)
4. **F-List account** (for testing)

### Getting Started

1. Clone the repository
2. Open `FChatDicebot/FCDicebot.sln` in Visual Studio
3. Restore NuGet packages
4. Build the solution
5. Set up test bot account and MongoDB

### Project Structure

```
FCDiceBotCCVer/
└── FChatDicebot/
    └── FChatDicebot/
        ├── BotMain.cs                      # Core bot logic
        ├── BotCommandController.cs         # Command dispatcher
        ├── BotCommands/                    # 143 command classes
        │   ├── Base/
        │   │   └── ChatBotCommand.cs       # Base class
        │   ├── ChateauKiss.cs
        │   ├── ChateauCuddle.cs
        │   └── ... (140+ more)
        ├── InteractionProcessors/          # Modular interactions
        │   ├── IInteractionProcessor.cs
        │   ├── InteractionProcessorBase.cs
        │   ├── InteractionProcessorRegistry.cs
        │   ├── Casual/
        │   ├── Involved/
        │   ├── Commitment/
        │   └── Consequence/
        ├── Database/
        │   ├── Ichateaudatabase.cs         # Database interface
        │   └── Chateaudatabase.cs          # MongoDB implementation
        ├── DiceFunctions/
        │   ├── DiceBot.cs                  # Dice and games
        │   └── Games/                      # 10+ game implementations
        ├── Model/
        │   └── ChateauDB.cs                # Data models
        ├── SavedData/                      # Configuration classes
        └── Utils.cs                        # Helper functions
```

## Adding a New Command

### Step 1: Create Command Class

Create a new file in `BotCommands/` directory:

**Example:** `BotCommands/ChateauWave.cs`

```csharp
using System;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands
{
    public class ChateauWave : ChatBotCommand
    {
        public ChateauWave()
        {
            Name = "wave";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.None;
        }

        public override void Run(
            BotMain bot,
            BotCommandController commandController,
            string[] rawTerms,
            string[] terms,
            string characterName,
            string channel,
            UserGeneratedCommand command)
        {
            // Get target user from terms
            string targetUser = commandController.GetUserNameFromCommandTerms(terms);

            if (string.IsNullOrEmpty(targetUser))
            {
                bot.SendMessageInChannel(
                    $"Usage: !wave [user]Name[/user]",
                    channel);
                return;
            }

            // Check if initiator is registered
            var initiatorProfile = MonDB.getProfile(characterName);
            if (initiatorProfile == null)
            {
                bot.SendMessageInChannel(
                    $"{characterName} is not registered. Use !register first.",
                    channel);
                return;
            }

            // Check if target exists
            var targetProfile = MonDB.getProfile(targetUser);
            if (targetProfile == null)
            {
                bot.SendMessageInChannel(
                    $"{targetUser} not found.",
                    channel);
                return;
            }

            // Create the interaction
            string message = $"{initiatorProfile.displayName} waves at " +
                           $"{targetProfile.displayName}. Hello!";

            bot.SendMessageInChannel(message, channel);

            // Optionally: Increment statistics
            MonDB.IncrementCount(characterName, "wave", 1);
            MonDB.IncrementCount(targetUser, "wavetake", 1);
        }
    }
}
```

### Step 2: Build and Test

1. Build the solution (F6)
2. The command is **automatically discovered** via reflection
3. Run the bot
4. Test in F-List: `!wave [user]TestUser[/user]`

### Command Properties

| Property | Description | Values |
|----------|-------------|--------|
| `Name` | Command trigger | String (e.g., "wave") |
| `RequireBotAdmin` | Bot admin only | true/false |
| `RequireChannelAdmin` | Channel op only | true/false |
| `RequireChannel` | Must be in channel | true/false |
| `LockCategory` | Thread safety lock | CommandLockCategory enum |

### Lock Categories

Prevent race conditions on shared data:

```csharp
public enum CommandLockCategory
{
    None,              // No lock needed
    SavedTables,       // Lock for roll tables
    SavedChannels,     // Lock for channel settings
    ChannelDecks,      // Lock for card decks
    ChannelScores,     // Lock for game scores
    ChipPiles          // Lock for chip operations
}
```

## Adding a New Interaction

Interactions use the processor pattern for modularity.

### Step 1: Create Processor Class

**Example:** `InteractionProcessors/Casual/WaveProcessor.cs`

```csharp
using System;
using FChatDicebot.Model;

namespace FChatDicebot.InteractionProcessors.Casual
{
    public class WaveProcessor : InteractionProcessorBase
    {
        public override string InteractionType => "wave";
        public override string InvestmentLevel => "casual";

        public override string ProcessInteraction(PendingCommand command)
        {
            // Set rate limit (1 hour for casual)
            var rateLimit = TimeSpan.FromHours(1);

            // Increment counts with rate limit check
            string result = IncrementBothCountsWithRateLimit(
                command.Interaction.initiator,
                command.Interaction.recipient,
                "wave",
                rateLimit
            );

            // If rate limited, return error message
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            // Save interaction to database
            SaveInteraction(command.Interaction);

            // Remove pending command
            MonDB.removePendingInteraction(command.Id);

            // Return interaction type (signals success)
            return "wave";
        }

        public override ValidationResult ValidateInteraction(
            string initiator,
            string recipient,
            string identifier)
        {
            // No special validation needed for wave
            return new ValidationResult { IsValid = true };
        }

        public override string GetConsentWarning(
            Profile initiatorProfile,
            Profile recipientProfile,
            string identifier)
        {
            // No special warning needed
            return null;
        }

        public override string GetCompletionMessage(
            Profile initiatorProfile,
            Profile recipientProfile,
            string identifier)
        {
            // Random descriptors
            string[] descriptors = new[]
            {
                "friendly!",
                "enthusiastic!",
                "shy...",
                "energetic!"
            };

            string descriptor = descriptors[
                Utils.Random.Next(descriptors.Length)
            ];

            return $"{initiatorProfile.displayName} waves at " +
                   $"{recipientProfile.displayName}, {descriptor}";
        }
    }
}
```

### Step 2: Register Processor

Edit `InteractionProcessors/InteractionProcessorRegistry.cs`:

```csharp
public static void Initialize()
{
    // Existing processors...
    RegisterProcessor(new KissProcessor());
    RegisterProcessor(new CuddleProcessor());

    // Add new processor
    RegisterProcessor(new WaveProcessor());

    // More processors...
}
```

### Step 3: Create Command to Trigger

**Example:** `BotCommands/ChateauWave.cs`

```csharp
public class ChateauWave : ChatBotCommand
{
    public ChateauWave()
    {
        Name = "wave";
        RequireChannel = true;
    }

    public override void Run(
        BotMain bot,
        BotCommandController commandController,
        string[] rawTerms,
        string[] terms,
        string characterName,
        string channel,
        UserGeneratedCommand command)
    {
        // Get target
        string targetUser = commandController.GetUserNameFromCommandTerms(terms);
        if (string.IsNullOrEmpty(targetUser))
        {
            bot.SendMessageInChannel(
                "Usage: !wave [user]Name[/user]",
                channel);
            return;
        }

        // Validate both users registered
        var initiatorProfile = MonDB.getProfile(characterName);
        var recipientProfile = MonDB.getProfile(targetUser);

        if (initiatorProfile == null || recipientProfile == null)
        {
            bot.SendMessageInChannel(
                "Both users must be registered.",
                channel);
            return;
        }

        // Create interaction
        var interaction = new Interaction
        {
            initiator = characterName,
            recipient = targetUser,
            type = "wave",
            identifier = "",
            investmentLevel = "casual",
            interactionTime = DateTime.Now
        };

        // Create pending command
        var pendingCommand = new PendingCommand
        {
            recipient = targetUser,
            Interaction = interaction,
            TimeIssued = DateTime.Now
        };

        // Save to database
        MonDB.addPendingCommand(pendingCommand);

        // Send consent request
        bot.SendMessageInChannel(
            $"{initiatorProfile.displayName} wants to wave at " +
            $"{recipientProfile.displayName}! Do you !consent?",
            channel);
    }
}
```

### Step 4: Test

1. Build and run
2. Use `!wave [user]TestUser[/user]`
3. Test consent workflow
4. Verify statistics tracking

## Adding a New Game

### Step 1: Implement IGame Interface

**Example:** `DiceFunctions/Games/CoinToss.cs`

```csharp
using System;
using System.Collections.Generic;

namespace FChatDicebot.DiceFunctions.Games
{
    public class CoinTossGame : IGame
    {
        public string GameName => "cointoss";

        public string StartGame(List<string> players, string channel)
        {
            if (players.Count < 2)
            {
                return "Need at least 2 players!";
            }

            // Initialize game state
            var session = DiceBot.GetGameSession(channel, GameName);
            session.Players = players;
            session.CurrentTurn = 0;
            session.GameState = "betting";

            return $"Coin Toss started! Players: {string.Join(", ", players)}. " +
                   $"Use !gamecommand cointoss bet {heads|tails} {amount}";
        }

        public string ProcessAction(
            string action,
            string player,
            string[] parameters,
            string channel)
        {
            var session = DiceBot.GetGameSession(channel, GameName);

            if (session == null)
            {
                return "No game in progress!";
            }

            switch (action.ToLower())
            {
                case "bet":
                    return ProcessBet(session, player, parameters, channel);

                case "flip":
                    return ProcessFlip(session, channel);

                default:
                    return "Unknown action. Use: bet, flip";
            }
        }

        private string ProcessBet(
            GameSession session,
            string player,
            string[] parameters,
            string channel)
        {
            // Validate parameters
            if (parameters.Length < 2)
            {
                return "Usage: !gamecommand cointoss bet {heads|tails} {amount}";
            }

            string choice = parameters[0].ToLower();
            if (choice != "heads" && choice != "tails")
            {
                return "Choose heads or tails!";
            }

            if (!int.TryParse(parameters[1], out int amount))
            {
                return "Invalid bet amount!";
            }

            // Check player has chips
            int playerChips = DiceBot.GetChips(player, channel);
            if (playerChips < amount)
            {
                return $"You don't have enough chips! Balance: {playerChips}";
            }

            // Store bet
            if (session.GameData == null)
            {
                session.GameData = new Dictionary<string, object>();
            }

            session.GameData[$"{player}_choice"] = choice;
            session.GameData[$"{player}_bet"] = amount;

            // Deduct chips
            DiceBot.AddChips(player, -amount, channel);

            return $"{player} bet {amount} chips on {choice}!";
        }

        private string ProcessFlip(GameSession session, string channel)
        {
            // Flip coin
            bool isHeads = Utils.Random.Next(2) == 0;
            string result = isHeads ? "heads" : "tails";

            string message = $"The coin lands on: {result.ToUpper()}!\n\n";

            // Calculate payouts
            int totalPayout = 0;

            foreach (var player in session.Players)
            {
                if (session.GameData.ContainsKey($"{player}_choice"))
                {
                    string choice = (string)session.GameData[$"{player}_choice"];
                    int bet = (int)session.GameData[$"{player}_bet"];

                    if (choice == result)
                    {
                        // Winner gets 2x bet
                        int payout = bet * 2;
                        DiceBot.AddChips(player, payout, channel);
                        message += $"{player} won {payout} chips!\n";
                        totalPayout += payout;
                    }
                    else
                    {
                        message += $"{player} lost {bet} chips.\n";
                    }
                }
            }

            // End game
            DiceBot.EndGameSession(channel, GameName);

            return message;
        }

        public string GetStatus(string channel)
        {
            var session = DiceBot.GetGameSession(channel, GameName);

            if (session == null)
            {
                return "No game in progress.";
            }

            string status = $"Coin Toss Game\nPlayers: " +
                          $"{string.Join(", ", session.Players)}\n";

            if (session.GameData != null)
            {
                status += "Bets:\n";
                foreach (var player in session.Players)
                {
                    if (session.GameData.ContainsKey($"{player}_choice"))
                    {
                        string choice = (string)session.GameData[$"{player}_choice"];
                        int bet = (int)session.GameData[$"{player}_bet"];
                        status += $"  {player}: {bet} on {choice}\n";
                    }
                }
            }

            return status;
        }

        public string EndGame(string channel)
        {
            DiceBot.EndGameSession(channel, GameName);
            return "Coin Toss game ended.";
        }
    }
}
```

### Step 2: Register Game

In `DiceBot.cs`, register the game:

```csharp
private static Dictionary<string, IGame> Games = new Dictionary<string, IGame>
{
    {"highroll", new HighRollGame()},
    {"poker", new PokerGame()},
    {"cointoss", new CoinTossGame()},  // Add new game
    // ... more games
};
```

### Step 3: Test

1. Build and run
2. `!joingame cointoss`
3. `!startgame cointoss`
4. `!gamecommand cointoss bet heads 100`
5. `!gamecommand cointoss flip`

## Working with the Database

### Fetching Profiles

```csharp
// Get a profile
Profile profile = MonDB.getProfile(userName);

if (profile == null)
{
    // User not registered
    return "User not found.";
}

// Access profile data
string displayName = profile.displayName;
int kissCount = profile.counts.GetValueOrDefault("kiss", 0);
```

### Updating Profiles

```csharp
// Get profile
Profile profile = MonDB.getProfile(userName);

// Modify
profile.counts["newcount"] = 1;
profile.characteristics["species"] = "dragon";
profile.currencies["tokens"] = 500;

// Save
MonDB.setProfile(profile);
```

### Incrementing Counts

```csharp
// Simple increment
MonDB.IncrementCount(userName, "kiss", 1);

// With rate limit (returns false if rate limited)
bool success = MonDB.IncrementCountWithRateLimit(
    userName,
    "kiss",
    TimeSpan.FromHours(1)
);

if (!success)
{
    return "Rate limited!";
}
```

### Saving Interactions

```csharp
var interaction = new Interaction
{
    initiator = "Alice",
    recipient = "Bob",
    type = "wave",
    identifier = "",
    investmentLevel = "casual",
    interactionTime = DateTime.Now
};

MonDB.addInteraction(interaction);
```

### Pending Commands

```csharp
// Create pending command
var pending = new PendingCommand
{
    recipient = "Bob",
    Interaction = interaction,
    TimeIssued = DateTime.Now
};

MonDB.addPendingCommand(pending);

// Get pending commands for user
List<PendingCommand> userPending = MonDB.getPending("Bob");

// Remove pending command
MonDB.removePendingInteraction(pending.Id);
```

## Best Practices

### 1. Input Validation

Always validate user input:

```csharp
// Check for null or empty
if (string.IsNullOrEmpty(targetUser))
{
    return "Target user required!";
}

// Validate numbers
if (!int.TryParse(amountStr, out int amount))
{
    return "Invalid amount!";
}

// Validate ranges
if (amount < 1 || amount > 10000)
{
    return "Amount must be between 1 and 10,000!";
}
```

### 2. Registration Checks

Always check if users are registered:

```csharp
var profile = MonDB.getProfile(userName);
if (profile == null)
{
    bot.SendMessageInChannel(
        $"{userName} is not registered. Use !register first.",
        channel);
    return;
}
```

### 3. Error Handling

Use try-catch for database operations:

```csharp
try
{
    var profile = MonDB.getProfile(userName);
    // ... operations
    MonDB.setProfile(profile);
}
catch (Exception ex)
{
    Utils.AddToLog($"Error: {ex.Message}", "ERROR");
    bot.SendMessageInChannel(
        "An error occurred. Please try again.",
        channel);
}
```

### 4. Thread Safety

Use appropriate locks for shared data:

```csharp
public ChateauExample()
{
    Name = "example";
    LockCategory = CommandLockCategory.ChipPiles; // Lock chips
}
```

### 5. Message Formatting

Keep messages concise and readable:

```csharp
// Good: Concise and clear
string message = $"{user} earned 50 tokens!";

// Bad: Too verbose
string message = $"Congratulations! The user named {user} has " +
                $"successfully earned a total of 50 tokens which have " +
                $"been added to their account balance!";
```

Use spoilers for long messages:

```csharp
if (message.Length > 500)
{
    message = $"[spoiler]{message}[/spoiler]";
}
```

### 6. Rate Limits

Always implement rate limits for interactions:

```csharp
// Check rate limit
var lastUse = profile.timers.GetValueOrDefault("ratelimit_action");
if (lastUse != null &&
    DateTime.Now.Subtract(lastUse.TimeSet) < TimeSpan.FromMinutes(30))
{
    return "Try again later!";
}

// Set new timer
profile.timers["ratelimit_action"] = new CoolDown
{
    TimeSet = DateTime.Now
};
MonDB.setProfile(profile);
```

## Testing

### Unit Testing Setup

Create test project:

1. Add new project: "FChatDicebot.Tests"
2. Add reference to main project
3. Install NUnit or xUnit via NuGet

### Example Test

```csharp
using NUnit.Framework;
using FChatDicebot.InteractionProcessors.Casual;

namespace FChatDicebot.Tests
{
    [TestFixture]
    public class WaveProcessorTests
    {
        [Test]
        public void ProcessInteraction_ValidInput_ReturnsSuccess()
        {
            // Arrange
            var processor = new WaveProcessor();
            var command = new PendingCommand
            {
                Interaction = new Interaction
                {
                    initiator = "Alice",
                    recipient = "Bob",
                    type = "wave",
                    investmentLevel = "casual"
                }
            };

            // Act
            string result = processor.ProcessInteraction(command);

            // Assert
            Assert.AreEqual("wave", result);
        }

        [Test]
        public void GetCompletionMessage_ValidProfiles_ReturnsMessage()
        {
            // Arrange
            var processor = new WaveProcessor();
            var alice = new Profile { displayName = "Alice" };
            var bob = new Profile { displayName = "Bob" };

            // Act
            string message = processor.GetCompletionMessage(alice, bob, "");

            // Assert
            Assert.IsTrue(message.Contains("Alice"));
            Assert.IsTrue(message.Contains("Bob"));
        }
    }
}
```

### Integration Testing

Test with real F-List connection:

1. Create test bot account
2. Create test channel
3. Use test characters
4. Automate commands via script

### Manual Testing Checklist

- [ ] Command executes without errors
- [ ] Proper error messages for invalid input
- [ ] Database updates correctly
- [ ] Rate limits work
- [ ] Consent workflow functions
- [ ] Achievement tracking works
- [ ] No memory leaks
- [ ] Thread safety verified

## Debugging

### Enable Verbose Logging

Add logging to your commands:

```csharp
Utils.AddToLog($"Processing command: {Name} for {characterName}", "DEBUG");
```

### Database Debugging

Check MongoDB directly:

```bash
mongo
use ChateauDb
db.RegisteredProfiles.find({userName: "Alice"})
db.Interactions.find({type: "kiss"}).count()
```

### Breakpoints

Set breakpoints in Visual Studio:

1. Click left margin next to line number
2. Run in Debug mode (F5)
3. Trigger command in F-List
4. Inspect variables

## Contributing

### Code Style

Follow existing code style:

- **Indentation:** Tabs (not spaces)
- **Braces:** Opening brace on same line
- **Naming:**
  - Classes: PascalCase
  - Methods: PascalCase
  - Fields: camelCase
  - Properties: PascalCase

### Git Workflow

1. Create feature branch: `git checkout -b feature/wave-interaction`
2. Make changes
3. Commit: `git commit -m "Add wave interaction"`
4. Push: `git push origin feature/wave-interaction`
5. Create pull request

### Documentation

Update wiki when adding features:

- Add command to Command Reference
- Update architecture docs if needed
- Add examples to relevant pages

## Common Issues

### "Profile not found"

**Cause:** User not registered

**Solution:** Ensure `!register` called first

### "Rate limited"

**Cause:** Cooldown not expired

**Solution:** Check timer in profile:

```csharp
var timer = profile.timers.GetValueOrDefault("ratelimit_kiss");
if (timer != null)
{
    var remaining = TimeSpan.FromHours(1) - DateTime.Now.Subtract(timer.TimeSet);
    // Show remaining time
}
```

### "MongoDB connection failed"

**Cause:** MongoDB not running

**Solution:**
```bash
net start MongoDB
```

### Reflection not finding command

**Cause:** Namespace or base class incorrect

**Solution:** Ensure:
- Namespace is `FChatDicebot.BotCommands`
- Inherits from `ChatBotCommand`

## See Also

- [Architecture](Architecture) - System design details
- [Database and Persistence](Database-and-Persistence) - Database operations
- [Interaction System](Interaction-System) - Interaction mechanics
- [Command Reference](Command-Reference) - Existing commands
