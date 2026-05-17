# Database and Persistence

FCDiceBot uses a dual-storage system combining MongoDB for dynamic data and JSON files for configuration.

## Storage Architecture

### Dual Storage System

```
┌─────────────────────────────────────────────────────────┐
│                    FCDiceBot Data                        │
└───────────────┬─────────────────────┬───────────────────┘
                │                     │
                ▼                     ▼
    ┌─────────────────────┐  ┌──────────────────────┐
    │   MongoDB (Primary)  │  │  JSON Files (Legacy) │
    │   ChateauDb          │  │  C:\BotData\DiceBot\ │
    └─────────────────────┘  └──────────────────────┘
                │                     │
                ▼                     ▼
    ┌─────────────────────┐  ┌──────────────────────┐
    │ - Profiles          │  │ - Account Settings   │
    │ - Interactions      │  │ - Channel Settings   │
    │ - Pending Commands  │  │ - Chip Piles         │
    │ - Duties            │  │ - Roll Tables        │
    │ - Identifiers       │  │ - Card Decks         │
    └─────────────────────┘  └──────────────────────┘
```

## MongoDB Storage

### Connection

**Default Connection String:**
```
mongodb://localhost:27017
```

**Database Name:** `ChateauDb`

**Location:** `FChatDicebot/Database/Chateaudatabase.cs`

```csharp
const string connectionString = "mongodb://localhost:27017";
var client = new MongoClient(connectionString);
var database = client.GetDatabase("ChateauDb");
```

### Collections

#### 1. RegisteredProfiles

**Purpose:** User profiles with stats, timers, and currencies

**Document Structure:**

```json
{
  "_id": ObjectId("..."),
  "userName": "Alice",
  "displayName": "Alice the Great",
  "counts": {
    "kiss": 42,
    "kissgive": 25,
    "kisstake": 17,
    "cuddle": 15
  },
  "characteristics": {
    "mark": "collar (by Bob)",
    "species": "human",
    "job": "maid",
    "employer": "Carol",
    "objectified": null,
    "petrified": null
  },
  "lists": {
    "bonds": ["Bob (owner)", "Carol (master)"],
    "employees": ["Dave", "Eve"]
  },
  "timers": {
    "ratelimit_kiss": {
      "TimeSet": ISODate("2025-11-22T10:30:00Z")
    },
    "work": {
      "TimeSet": ISODate("2025-11-22T08:00:00Z")
    },
    "cashout": {
      "TimeSet": ISODate("2025-11-20T12:00:00Z")
    }
  },
  "currencies": {
    "tokens": 500,
    "gold": 100,
    "silver": 50
  },
  "jobExperience": {
    "maid": 150,
    "chef": 50
  },
  "titles": [
    {
      "titleText": "·First Kiss·",
      "grantedBy": "system",
      "dateGranted": ISODate("2025-11-01T12:00:00Z")
    },
    {
      "titleText": "Best Friend",
      "grantedBy": "Bob",
      "dateGranted": ISODate("2025-11-15T14:30:00Z")
    }
  ],
  "displayedTitleSlots": [0, 1, -1, -1, -1, -1, -1, -1, -1]
}
```

**Indexes:**
- `userName` (unique)

**Key Methods:**

```csharp
Profile GetProfile(string userName)
void SetProfile(Profile profile)
void RegisterUserChateau(string userName, int startingChips)
void IncrementCount(string userName, string countName, int amount)
bool IncrementCountWithRateLimit(string userName, string countName, TimeSpan rateLimit)
```

#### 2. Interactions

**Purpose:** Historical record of all completed interactions

**Document Structure:**

```json
{
  "_id": ObjectId("..."),
  "initiator": "Alice",
  "recipient": "Bob",
  "type": "kiss",
  "identifier": "",
  "investmentLevel": "casual",
  "extraParameters": [],
  "interactionTime": ISODate("2025-11-22T12:34:56Z")
}
```

**Indexes:**
- `initiator`
- `recipient`
- `type`
- `interactionTime`

**Key Methods:**

```csharp
void AddInteraction(Interaction interaction)
List<Interaction> GetInteractionsByInitiator(string initiator)
List<Interaction> GetInteractionsByRecipient(string recipient)
List<Interaction> GetInteractionsByType(string type)
List<Interaction> GetInteractionsBetween(string user1, string user2)
```

**Queries:**

```csharp
// Get all kisses
var kisses = GetInteractionsByType("kiss");

// Get all interactions Alice initiated
var aliceInteractions = GetInteractionsByInitiator("Alice");

// Get all interactions between Alice and Bob
var pairInteractions = GetInteractionsBetween("Alice", "Bob");
```

#### 3. PendingCommands

**Purpose:** Consent workflow - interactions awaiting consent

**Document Structure:**

```json
{
  "_id": ObjectId("..."),
  "recipient": "Bob",
  "Interaction": {
    "initiator": "Alice",
    "recipient": "Bob",
    "type": "kiss",
    "identifier": "",
    "investmentLevel": "casual",
    "interactionTime": ISODate("2025-11-22T12:30:00Z")
  },
  "TimeIssued": ISODate("2025-11-22T12:30:00Z")
}
```

**Expiration:** 10 minutes

**Indexes:**
- `recipient`
- `TimeIssued`

**Key Methods:**

```csharp
void AddPendingCommand(PendingCommand command)
List<PendingCommand> GetPendingCommands(string recipient)
void DeletePendingCommand(ObjectId id)
void CleanExpiredPendingCommands() // Removes commands older than 10 minutes
```

**Cleanup:**

Expired commands are removed when querying:

```csharp
var pending = GetPendingCommands("Bob");
// Automatically removes any older than 10 minutes
```

#### 4. Duties

**Purpose:** Job system - task definitions

**Document Structure:**

```json
{
  "_id": ObjectId("..."),
  "jobName": "maid",
  "taskDescriptions": [
    "Clean the manor",
    "Serve tea to guests",
    "Organize the library",
    "Polish silverware",
    "Prepare bedrooms"
  ],
  "baseReward": {
    "tokens": 50,
    "gold": 10
  },
  "experienceGrant": 10,
  "conditions": [
    {
      "conditionType": "random",
      "probability": 0.1,
      "effect": {
        "bonusTokens": 25,
        "message": "You found a tip!"
      }
    }
  ]
}
```

**Indexes:**
- `jobName` (unique)

**Key Methods:**

```csharp
Duty GetDuty(string jobName)
void AddDuty(Duty duty)
List<string> GetRandomTask(string jobName)
```

#### 5. PendingDuties

**Purpose:** Active work assignments

**Document Structure:**

```json
{
  "_id": ObjectId("..."),
  "userName": "Alice",
  "jobName": "maid",
  "taskDescription": "Clean the manor",
  "assignedTime": ISODate("2025-11-22T08:00:00Z"),
  "completed": false
}
```

**Indexes:**
- `userName`
- `completed`

**Key Methods:**

```csharp
PendingDuty GetPendingDuty(string userName)
void AssignDuty(string userName, string jobName, string task)
void CompleteDuty(ObjectId id)
```

#### 6. Commands

**Purpose:** Command metadata and usage tracking (optional)

**Document Structure:**

```json
{
  "_id": ObjectId("..."),
  "commandName": "kiss",
  "usageCount": 1523,
  "lastUsed": ISODate("2025-11-22T12:34:56Z"),
  "averageExecutionTime": 45.3
}
```

**Note:** This collection is optional and may not be actively used in all deployments.

#### 7. Identifiers

**Purpose:** Category-based identifiers for interactions

**Document Structure:**

```json
{
  "_id": ObjectId("..."),
  "category": "bodypart",
  "identifiers": [
    "collar",
    "wrist",
    "ankle",
    "forehead",
    "cheek",
    "neck",
    "shoulder"
  ]
}
```

**Categories:**
- `bodypart` - For marks
- `substance` - For feeding
- `attire` - For dressing
- `species` - For monsterization
- `object` - For objectification
- `plant` - For plant transformation

**Key Methods:**

```csharp
List<string> GetIdentifiers(string category)
void AddIdentifier(string category, string identifier)
bool ValidateIdentifier(string category, string identifier)
```

**Usage Example:**

```csharp
// When marking, validate body part:
var bodyParts = GetIdentifiers("bodypart");
if (!bodyParts.Contains(requestedPart)) {
    return "Invalid body part!";
}
```

## File-Based Storage

### Location

```
C:\BotData\DiceBot\
```

### Files

#### account_settings.txt

**Purpose:** Bot credentials and configuration

**Format:** JSON

**Structure:**

```json
{
  "AccountName": "your_flist_account",
  "CharacterName": "BotCharacterName",
  "Password": "your_password",
  "CName": "FCDiceBot",
  "AdminCharacters": ["AdminChar1", "AdminChar2"],
  "VelvetCuffClientId": "optional_vc_client_id",
  "VelvetCuffClientSecret": "optional_vc_secret"
}
```

**Security:** Contains sensitive credentials - keep secure

#### channel_settings.txt

**Purpose:** Per-channel configuration

**Format:** JSON

**Structure:**

```json
{
  "adh-channelid1": {
    "commandChar": "!",
    "enableChips": true,
    "enableGames": true,
    "enableTables": true,
    "enableSlots": true,
    "startingChips": 1000,
    "chipsClearanceLevel": 0,
    "greetNewUsers": false,
    "slotsMaxMultiplier": 100
  },
  "adh-channelid2": {
    "commandChar": "?",
    "enableChips": false,
    "enableGames": true,
    "enableTables": true,
    "enableSlots": false,
    "startingChips": 500,
    "chipsClearanceLevel": 1,
    "greetNewUsers": true,
    "slotsMaxMultiplier": 50
  }
}
```

#### saved_tables.txt

**Purpose:** User-created roll tables

**Format:** JSON

**Structure:**

```json
{
  "Alice": [
    {
      "tableName": "loot",
      "entries": [
        {"weight": 50, "result": "Common Item"},
        {"weight": 30, "result": "Uncommon Item"},
        {"weight": 15, "result": "Rare Item"},
        {"weight": 5, "result": "Legendary Item"}
      ]
    }
  ],
  "Bob": [
    {
      "tableName": "encounters",
      "entries": [
        {"weight": 40, "result": "Goblin"},
        {"weight": 30, "result": "Orc"},
        {"weight": 20, "result": "table:loot"},
        {"weight": 10, "result": "Dragon"}
      ]
    }
  ]
}
```

#### saved_chipPiles.txt

**Purpose:** Casino chip balances

**Format:** JSON

**Structure:**

```json
{
  "adh-channelid1": {
    "Alice": 1500,
    "Bob": 2300,
    "Carol": 800
  },
  "adh-channelid2": {
    "Dave": 5000,
    "Eve": 1200
  }
}
```

**Note:** Organized by channel, each user has a balance per channel

#### saved_decks.txt

**Purpose:** Custom card decks

**Format:** JSON

**Structure:**

```json
{
  "adh-channelid1": {
    "myddeck": [
      "Magic Sword",
      "Healing Potion",
      "Curse",
      "Gold Coin",
      "Ancient Scroll"
    ]
  }
}
```

#### saved_slots.txt

**Purpose:** Slot machine configurations

**Format:** JSON

**Structure:**

```json
{
  "fruits": {
    "symbols": ["Cherry", "Lemon", "Orange", "Plum", "Bell", "Seven"],
    "weights": [30, 25, 20, 15, 8, 2],
    "multipliers": {
      "Cherry": 3,
      "Lemon": 5,
      "Orange": 10,
      "Plum": 15,
      "Bell": 20,
      "Seven": 50
    }
  },
  "bondage": {
    "symbols": ["Rope", "Chain", "Collar", "Gag", "Cuffs", "Whip"],
    "weights": [30, 25, 20, 15, 8, 2],
    "multipliers": {
      "Rope": 3,
      "Chain": 5,
      "Collar": 10,
      "Gag": 15,
      "Cuffs": 20,
      "Whip": 50
    }
  }
}
```

#### saved_potions.txt

**Purpose:** User-saved potions

**Format:** JSON

**Structure:**

```json
{
  "Alice": [
    {
      "name": "swiftness",
      "fullName": "Shimmering Elixir of Swiftness",
      "effect": "Grants incredible speed for 1 hour",
      "components": {
        "base": "liquid",
        "appearance": "shimmering",
        "effect": "speed",
        "duration": "1 hour",
        "intensity": "incredible"
      }
    }
  ]
}
```

#### vc_chiporder_data.txt

**Purpose:** VelvetCuff payment transactions

**Format:** JSON

**Structure:**

```json
[
  {
    "transactionId": "abc123",
    "userName": "Alice",
    "chipAmount": 5000,
    "vcAmount": 50.00,
    "status": "pending",
    "createdTime": "2025-11-22T12:00:00Z",
    "channel": "adh-channelid1"
  }
]
```

**Lifecycle:**
1. Created when `!buychips` used
2. Polled every 30 seconds
3. Deleted when payment confirmed or expired

#### coupons_active.txt

**Purpose:** Active chip coupons

**Format:** JSON

**Structure:**

```json
[
  {
    "code": "WELCOME2024",
    "amount": 1000,
    "createdBy": "AdminChar",
    "createdTime": "2025-11-01T12:00:00Z",
    "used": false,
    "usedBy": null,
    "usedTime": null
  }
]
```

**Lifecycle:**
1. Created by admin: `!createcoupon`
2. Redeemed by user: `!redeem`
3. Marked as used (not deleted, for audit trail)

### Backup System

**Location:** `C:\BotData\DiceBot\ImmediateBackup\`

**When:** On bot startup, before any file modifications

**What:** All JSON files copied to backup directory

**Implementation:**

```csharp
// In BotMain.cs startup:
BackupDataFiles();

private void BackupDataFiles() {
    string backupDir = @"C:\BotData\DiceBot\ImmediateBackup\";
    Directory.CreateDirectory(backupDir);

    foreach (var file in Directory.GetFiles(@"C:\BotData\DiceBot\", "*.txt")) {
        string fileName = Path.GetFileName(file);
        File.Copy(file, Path.Combine(backupDir, fileName), overwrite: true);
    }
}
```

**Note:** Only keeps one backup (overwrites previous). For more robust backups, implement versioning or external backup system.

## Data Access Layer

### Adapter Pattern

**MonDB Static Adapter:**

**Location:** `FChatDicebot/MonDB.cs`

Provides static methods that delegate to an `IChateauDatabase` instance:

```csharp
public static class MonDB
{
    private static IChateauDatabase _database;

    public static void Initialize(IChateauDatabase database) {
        _database = database;
    }

    public static Profile getProfile(string userName) {
        return _database.GetProfile(userName);
    }

    public static void setProfile(Profile profile) {
        _database.SetProfile(profile);
    }

    // ... more delegation methods
}
```

**Why:** Allows existing code to use static calls while enabling dependency injection and testability.

### IChateauDatabase Interface

**Location:** `FChatDicebot/Database/Ichateaudatabase.cs`

Defines all database operations:

```csharp
public interface IChateauDatabase
{
    // Profile operations
    Profile GetProfile(string userName);
    void SetProfile(Profile profile);
    void RegisterUserChateau(string userName, int startingChips);

    // Interaction operations
    void AddInteraction(Interaction interaction);
    List<Interaction> GetInteractionsByInitiator(string initiator);
    List<Interaction> GetInteractionsByRecipient(string recipient);

    // Pending command operations
    void AddPendingCommand(PendingCommand command);
    List<PendingCommand> GetPendingCommands(string recipient);
    void DeletePendingCommand(ObjectId id);

    // Count and timer operations
    void IncrementCount(string userName, string countName, int amount);
    bool IncrementCountWithRateLimit(string userName, string countName, TimeSpan rateLimit);

    // Duty operations
    Duty GetDuty(string jobName);
    PendingDuty GetPendingDuty(string userName);
    void AssignDuty(string userName, string jobName, string task);

    // Identifier operations
    List<string> GetIdentifiers(string category);
    void AddIdentifier(string category, string identifier);
}
```

### ChateauDatabase Implementation

**Location:** `FChatDicebot/Database/Chateaudatabase.cs`

MongoDB implementation of `IChateauDatabase`:

```csharp
public class Chateaudatabase : IChateauDatabase
{
    private IMongoDatabase _database;
    private IMongoCollection<Profile> _profiles;
    private IMongoCollection<Interaction> _interactions;
    private IMongoCollection<PendingCommand> _pendingCommands;
    // ... more collections

    public Chateaudatabase() {
        var client = new MongoClient("mongodb://localhost:27017");
        _database = client.GetDatabase("ChateauDb");
        _profiles = _database.GetCollection<Profile>("RegisteredProfiles");
        _interactions = _database.GetCollection<Interaction>("Interactions");
        _pendingCommands = _database.GetCollection<PendingCommand>("PendingCommands");
        // ... initialize more collections
    }

    public Profile GetProfile(string userName) {
        return _profiles.Find(p => p.userName == userName).FirstOrDefault();
    }

    public void SetProfile(Profile profile) {
        _profiles.ReplaceOne(
            p => p.userName == profile.userName,
            profile,
            new ReplaceOptions { IsUpsert = true }
        );
    }

    // ... implement other methods
}
```

## Performance Considerations

### MongoDB Indexing

**Profiles:**
- Index on `userName` (unique)
- Enables O(log n) profile lookups

**Interactions:**
- Index on `initiator`
- Index on `recipient`
- Index on `type`
- Index on `interactionTime`
- Enables efficient history queries

### Query Optimization

**Fetch profiles once:**

```csharp
// Bad: Multiple profile fetches
var profile1 = MonDB.getProfile("Alice");
profile1.counts["kiss"]++;
MonDB.setProfile(profile1);

var profile2 = MonDB.getProfile("Alice");
profile2.counts["cuddle"]++;
MonDB.setProfile(profile2);

// Good: Single fetch and update
var profile = MonDB.getProfile("Alice");
profile.counts["kiss"]++;
profile.counts["cuddle"]++;
MonDB.setProfile(profile);
```

**Batch operations where possible:**

```csharp
// Update multiple users in one transaction
var batch = _profiles.BulkWrite(new[] {
    new ReplaceOneModel<Profile>(
        Builders<Profile>.Filter.Eq(p => p.userName, "Alice"),
        aliceProfile
    ),
    new ReplaceOneModel<Profile>(
        Builders<Profile>.Filter.Eq(p => p.userName, "Bob"),
        bobProfile
    )
});
```

### File I/O Optimization

**Load once, save when changed:**

```csharp
// Load chip piles at startup
private Dictionary<string, Dictionary<string, int>> ChipPiles = LoadChipPiles();

// Update in memory during operations
ChipPiles[channel][user] += amount;

// Save only when explicitly needed
SaveChipPiles();
```

**Avoid excessive saves:**

```csharp
// Bad: Save after every chip change
GiveChips("Alice", 100);
SaveChipPiles();
GiveChips("Bob", 200);
SaveChipPiles();

// Good: Save after batch of changes
GiveChips("Alice", 100);
GiveChips("Bob", 200);
SaveChipPiles();
```

## Data Integrity

### Transaction Safety

**MongoDB Transactions:**

For operations requiring atomicity:

```csharp
using (var session = _client.StartSession()) {
    session.StartTransaction();
    try {
        // Multiple operations
        _profiles.UpdateOne(session, ...);
        _interactions.InsertOne(session, ...);

        session.CommitTransaction();
    } catch {
        session.AbortTransaction();
        throw;
    }
}
```

**Note:** Current implementation doesn't use transactions extensively. Consider adding for critical operations.

### Validation

**Profile Validation:**

```csharp
public bool ValidateProfile(Profile profile) {
    if (string.IsNullOrWhiteSpace(profile.userName))
        return false;

    if (profile.counts == null)
        profile.counts = new Dictionary<string, int>();

    if (profile.timers == null)
        profile.timers = new Dictionary<string, CoolDown>();

    return true;
}
```

**Interaction Validation:**

```csharp
public bool ValidateInteraction(Interaction interaction) {
    if (string.IsNullOrWhiteSpace(interaction.initiator))
        return false;

    if (string.IsNullOrWhiteSpace(interaction.recipient))
        return false;

    if (string.IsNullOrWhiteSpace(interaction.type))
        return false;

    return true;
}
```

## Migrations and Schema Changes

### Adding Fields to Profiles

**Backward Compatible Approach:**

```csharp
// Old profile may not have "currencies" field
var profile = GetProfile("Alice");

if (profile.currencies == null) {
    profile.currencies = new Dictionary<string, int> {
        {"tokens", 0},
        {"gold", 0},
        {"silver", 0}
    };
}

profile.currencies["tokens"] += 50;
SetProfile(profile);
```

**Migration Script:**

```csharp
public void MigrateProfilesAddCurrencies() {
    var allProfiles = _profiles.Find(_ => true).ToList();

    foreach (var profile in allProfiles) {
        if (profile.currencies == null) {
            profile.currencies = new Dictionary<string, int> {
                {"tokens", 0},
                {"gold", 0},
                {"silver", 0}
            };
            SetProfile(profile);
        }
    }
}
```

### Renaming Fields

**Use MongoDB Update:**

```csharp
var update = Builders<Profile>.Update.Rename("oldFieldName", "newFieldName");
_profiles.UpdateMany(_ => true, update);
```

## Disaster Recovery

### MongoDB Backup

**Manual Backup:**

```bash
mongodump --db ChateauDb --out C:\Backups\MongoDB\2025-11-22\
```

**Restore:**

```bash
mongorestore --db ChateauDb C:\Backups\MongoDB\2025-11-22\ChateauDb\
```

**Automated Backup (Windows Task Scheduler):**

Create batch script `backup_mongodb.bat`:

```batch
@echo off
set BACKUP_DIR=C:\Backups\MongoDB\%DATE%
mongodump --db ChateauDb --out %BACKUP_DIR%
```

Schedule to run daily.

### File Backup

**Already implemented:** `ImmediateBackup` folder

**Enhanced Backup:**

```csharp
private void AdvancedBackup() {
    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    string backupDir = $@"C:\BotData\DiceBot\Backups\{timestamp}\";
    Directory.CreateDirectory(backupDir);

    foreach (var file in Directory.GetFiles(@"C:\BotData\DiceBot\", "*.txt")) {
        string fileName = Path.GetFileName(file);
        File.Copy(file, Path.Combine(backupDir, fileName));
    }
}
```

**Retention Policy:**

Keep last 7 days of backups:

```csharp
private void CleanOldBackups() {
    var backupDirs = Directory.GetDirectories(@"C:\BotData\DiceBot\Backups\");
    var oldBackups = backupDirs
        .Where(d => Directory.GetCreationTime(d) < DateTime.Now.AddDays(-7))
        .ToList();

    foreach (var dir in oldBackups) {
        Directory.Delete(dir, recursive: true);
    }
}
```

## See Also

- [Installation and Setup](Installation-and-Setup) - MongoDB setup
- [Architecture](Architecture) - Data access layer details
- [Interaction System](Interaction-System) - How interactions use database
- [Development Guide](Development-Guide) - Working with the database
