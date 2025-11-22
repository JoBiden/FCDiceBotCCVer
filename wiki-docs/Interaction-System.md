# Interaction System (Chateau Contract)

The Chateau Contract interaction system is the most complex feature of FCDiceBot, providing a consent-based roleplay interaction framework with persistence, statistics tracking, and transformative effects.

## Overview

The interaction system allows users to interact with each other in roleplay scenarios, with all interactions:
- **Requiring consent** from the recipient
- **Being tracked** in a database
- **Having cooldowns** to prevent spam
- **Granting achievements** and titles
- **Creating lasting effects** (for some interactions)

## Investment Levels

Interactions are organized into four investment levels, each with different cooldowns and effects:

### 1. Casual Interactions

**Cooldown:** 1 hour between same interaction
**Effects:** None (just for fun and statistics)
**Location:** `InteractionProcessors/Casual/`

| Interaction | Description | Example Message |
|-------------|-------------|-----------------|
| `!kiss` | Share a kiss | "Mwah! Alice and Bob share a kiss, cute." |
| `!cuddle` | Cuddle together | "Alice and Bob cuddle together, aww..." |
| `!handhold` | Hold hands | "Alice and Bob hold hands, how sweet!" |
| `!spank` | Playful spanking | "Alice spanks Bob, ouch!" |
| `!bully` | Playful teasing | "Alice bullies Bob a bit!" |

**Characteristics:**
- Simple, non-committal
- No lasting game effects
- Can be repeated hourly
- Count toward statistics and achievements

### 2. Involved Interactions

**Cooldown:** 30 minutes between same interaction
**Effects:** Temporary (visual/cosmetic)
**Location:** `InteractionProcessors/Involved/`

| Interaction | Description | Parameters |
|-------------|-------------|------------|
| `!feed` | Feed something to someone | Item to feed |
| `!dressup` | Dress someone in attire | Attire type |
| `!golden` | Golden shower | N/A |
| `!payment` | Give/receive payment | Token amount |

**Characteristics:**
- Involve more roleplay investment
- May have temporary visual effects
- Tracked in profile characteristics
- 30-minute cooldown

### 3. Commitment Interactions

**Cooldown:** Daily (24 hours)
**Effects:** Lasting relationships and ownership
**Location:** `InteractionProcessors/Commitment/`

| Interaction | Description | Parameters | Effect |
|-------------|-------------|------------|--------|
| `!mark` | Place ownership mark | Body part | Adds mark to profile |
| `!entitle` | Grant a title | Title text | Adds title to recipient |
| `!bond` | Create relationship | Bond type | Adds to bonds list |
| `!employ` | Hire for a job | Job name | Sets job, enables work |

**Characteristics:**
- Significant roleplay commitment
- Creates lasting game state
- Daily cooldown prevents abuse
- Can be reversed with counterparts

**Mark Example:**

```
Alice: !mark [user]Bob[/user] collar
Bob: !consent
Bot: Alice has marked Bob's collar!
```

Bob's profile now shows: `Mark: collar (by Alice)`

**Bond Types:**
- Pet
- Slave
- Master
- Owner
- Servant
- And many more...

### 4. Consequence Interactions

**Cooldown:** Daily (24 hours)
**Effects:** Transformative, permanent until reversed
**Location:** `InteractionProcessors/Consequence/`

| Interaction | Description | Parameters | Effect |
|-------------|-------------|------------|--------|
| `!rename` | Change someone's display name | New name | Changes display name |
| `!monsterize` | Transform into monster | Species | Changes species |
| `!petrify` | Turn to stone | N/A | Status: petrified |
| `!plant` | Transform into plant | Plant type | Status: plant |
| `!objectify` | Transform into object | Object type | Status: object |
| `!consume` | Consume/absorb someone | N/A | Status: consumed |

**Characteristics:**
- Permanent transformation until reversed
- Cannot be undone without counterpart command
- Daily cooldown (serious commitment)
- May restrict future interactions

**Rename Example:**

```
Alice: !rename [user]Bob[/user] "Bobert the Great"
Bob: !consent
Bot: Bob is now known as Bobert the Great!
```

Bob's `displayName` changes. All future interactions use the new name.

**Monsterize Example:**

```
Alice: !monsterize [user]Bob[/user] dragon
Bob: !consent
Bot: Bob has been transformed into a dragon!
```

Bob's species is now "dragon" in their profile.

## Consent Workflow

All interactions follow a strict consent workflow to ensure both parties agree.

### Step 1: Initiate Interaction

**Command Format:**
```
!{interaction} [user]RecipientName[/user] {parameters}
```

**Example:**
```
!kiss [user]Bob[/user]
```

**What Happens:**
1. Bot validates recipient exists
2. Bot checks if initiator is registered
3. Bot creates `PendingCommand` in database
4. Bot sends consent request to channel

**Consent Request Message:**

```
Alice wants to give Bob a smooch! Do you !consent?
```

### Step 2: Recipient Responds

**Options:**

#### Option A: Consent
```
!consent
```
or
```
!accept
```

#### Option B: Reject
```
!reject
```
or
```
!deny
```

#### Option C: Ignore
- Pending interaction expires after 10 minutes
- No action taken

### Step 3: Process Interaction

**On Consent:**

1. Fetch pending interaction from database
2. Get appropriate processor from registry
3. Execute `ProcessInteraction()`:
   - Validate interaction is still valid
   - Check rate limits
   - Increment statistics counters
   - Apply effects
   - Save to interaction history
   - Delete pending command
4. Generate completion message
5. Check for achievement unlocks
6. Send message to channel

**On Rejection:**

1. Delete pending command
2. Send rejection message
3. No statistics updated
4. No effects applied

### Step 4: Completion

**Success Message:**

```
Mwah! Alice and Bob share a kiss, cute.
```

**With Achievement:**

```
Mwah! Alice and Bob share a kiss, cute.

Achievement Unlocked!
Alice earned: ·First Kiss·
```

**Rate Limit Message:**

```
Alice has already kissed someone recently. Try again in 45 minutes.
```

## Rate Limiting System

### How It Works

Each interaction type has a rate limit based on investment level:

| Investment Level | Cooldown |
|------------------|----------|
| Casual | 1 hour |
| Involved | 30 minutes |
| Commitment | 24 hours |
| Consequence | 24 hours |

Rate limits are **per interaction type**, not global. You can:
- Kiss someone, then cuddle someone else (both casual)
- Cannot kiss twice within 1 hour

### Implementation

**Database Storage:**

Rate limits are stored in the profile's `timers` dictionary:

```csharp
profile.timers["ratelimit_kiss"] = new CoolDown {
    TimeSet = DateTime.Now
};
```

**Check on Interaction:**

```csharp
var rateLimit = TimeSpan.FromHours(1); // Casual
var lastKiss = profile.timers.GetValueOrDefault("ratelimit_kiss");

if (lastKiss != null && DateTime.Now.Subtract(lastKiss.TimeSet) < rateLimit) {
    // Still on cooldown
    return "Rate limited";
}
```

### Per-User Rate Limiting

**Important:** Rate limit applies to BOTH initiator and recipient

If Alice kissed Carol 10 minutes ago:
- Alice cannot kiss anyone for another 50 minutes
- Bob CAN kiss Alice (Bob hasn't kissed anyone recently)

This prevents one user from being spammed by multiple users in quick succession.

## Statistics Tracking

All interactions are counted and stored.

### Count Types

Interactions track both:
- **Initiator count:** "give" count (e.g., `kissgive`)
- **Recipient count:** "take" count (e.g., `kisstake`)
- **Total count:** Combined (e.g., `kiss`)

### Example Profile Counts

```json
{
  "userName": "Alice",
  "counts": {
    "kiss": 42,
    "kissgive": 25,
    "kisstake": 17,
    "cuddle": 15,
    "cuddle give": 8,
    "cuddletake": 7,
    "rename": 3,
    "renamegive": 3,
    "renametake": 0
  }
}
```

### Interaction History

Every completed interaction is saved to the `Interactions` collection:

```csharp
{
  "initiator": "Alice",
  "recipient": "Bob",
  "type": "kiss",
  "investmentLevel": "casual",
  "identifier": "",
  "interactionTime": "2025-11-22T12:34:56Z"
}
```

**Uses:**
- Dossiers (view interaction history)
- Statistics queries
- Achievement tracking
- Relationship analysis

## Processor Architecture

The interaction system uses a modular processor architecture for extensibility.

### IInteractionProcessor Interface

```csharp
public interface IInteractionProcessor
{
    string InteractionType { get; }        // "kiss", "cuddle", etc.
    string InvestmentLevel { get; }        // "casual", "involved", etc.

    string ProcessInteraction(PendingCommand command);

    ValidationResult ValidateInteraction(
        string initiator,
        string recipient,
        string identifier);

    string GetConsentWarning(
        Profile initiatorProfile,
        Profile recipientProfile,
        string identifier);

    string GetCompletionMessage(
        Profile initiatorProfile,
        Profile recipientProfile,
        string identifier);
}
```

### Base Class: InteractionProcessorBase

Provides common functionality:

```csharp
public abstract class InteractionProcessorBase : IInteractionProcessor
{
    protected IChateauDatabase Database { get; set; }

    protected void IncrementBothCounts(
        string initiator,
        string recipient,
        string countLabel);

    protected string IncrementBothCountsWithRateLimit(
        string initiator,
        string recipient,
        string countLabel,
        TimeSpan rateLimit);

    protected void SaveInteraction(Interaction interaction);

    protected string FormatMessage(string template, params object[] args);
}
```

### Example Processor: KissProcessor

**Location:** `InteractionProcessors/Casual/KissProcessor.cs`

```csharp
public class KissProcessor : InteractionProcessorBase
{
    public override string InteractionType => "kiss";
    public override string InvestmentLevel => "casual";

    public override string ProcessInteraction(PendingCommand command)
    {
        var rateLimit = TimeSpan.FromHours(1);

        string result = IncrementBothCountsWithRateLimit(
            command.Interaction.initiator,
            command.Interaction.recipient,
            "kiss",
            rateLimit
        );

        if (!string.IsNullOrEmpty(result)) {
            return result; // Rate limited
        }

        SaveInteraction(command.Interaction);
        MonDB.removePendingInteraction(command.Id);

        return "kiss";
    }

    public override string GetCompletionMessage(
        Profile initiatorProfile,
        Profile recipientProfile,
        string identifier)
    {
        string[] descriptors = new[] {
            "cute.", "that's kind of lewd...", "hot!", "aww...", "nice!"
        };

        string descriptor = descriptors[Utils.Random.Next(descriptors.Length)];

        return $"Mwah! {initiatorProfile.displayName} and " +
               $"{recipientProfile.displayName} share a kiss, {descriptor}";
    }
}
```

### Registry System

**Location:** `InteractionProcessors/InteractionProcessorRegistry.cs`

```csharp
public static class InteractionProcessorRegistry
{
    private static Dictionary<string, IInteractionProcessor> _processors
        = new Dictionary<string, IInteractionProcessor>();

    public static void Initialize()
    {
        RegisterProcessor(new KissProcessor());
        RegisterProcessor(new CuddleProcessor());
        // ... register all processors
    }

    public static IInteractionProcessor GetProcessor(string interactionType)
    {
        return _processors.GetValueOrDefault(interactionType.ToLower());
    }
}
```

## Special Features

### Easter Eggs

Some interactions have special cases:

**Queen Contract:**

If a user has the "Queen Contract" title, cuddles produce a special message:

```csharp
if (initiatorProfile.HasTitle("Queen Contract") ||
    recipientProfile.HasTitle("Queen Contract"))
{
    return "Corrupted Rin appears and cuddles both of you instead!";
}
```

### Validation

Some interactions require specific conditions:

**Mark Processor:**

Before marking, checks if a mark identifier is set:

```csharp
public override ValidationResult ValidateInteraction(
    string initiator,
    string recipient,
    string identifier)
{
    if (string.IsNullOrWhiteSpace(identifier))
    {
        return new ValidationResult {
            IsValid = false,
            ErrorMessage = "You must specify where to mark them!"
        };
    }

    return new ValidationResult { IsValid = true };
}
```

### Warnings

Some interactions warn the recipient:

**Rename Processor:**

```csharp
public override string GetConsentWarning(
    Profile initiatorProfile,
    Profile recipientProfile,
    string identifier)
{
    return $"Warning: {initiatorProfile.displayName} wants to rename you to " +
           $"\"{identifier}\". This will change how you appear in all future " +
           $"interactions until you are renamed again. Do you !consent?";
}
```

## Title System

Interactions can grant titles (achievements).

### System Titles

Granted automatically by the system:

Format: `·Title Name·` (with dots)

**Examples:**
- `·First Kiss·` - First kiss interaction
- `·Kiss Collector·` - 100 kisses
- `·Heartbreaker·` - 1000 kisses

**Implementation:**

```csharp
ChateauSystemTitles.CheckAndGrantTitles(profile, "kiss", count);
```

### Player-Granted Titles

Granted by other players via `!entitle`:

Format: `Title Name` (no dots)

**Example:**

```
Alice: !entitle [user]Bob[/user] "Best Friend"
Bob: !consent
Bot: Alice has granted Bob the title: Best Friend
```

### Title Slots

Each profile has **9 title display slots**:

```csharp
public int[] displayedTitleSlots { get; set; } // Indices into titles list
```

**Commands:**
- `!settitle {slot} {title}` - Display a title in a slot
- `!cleartitle {slot}` - Clear a slot
- `!showtitles` - View all your titles

**Display:**

When showing a profile, titles are displayed:

```
Alice
·First Kiss· | Best Friend | ·Kiss Collector·
```

## Job System

Jobs are assigned via `!employ` (commitment interaction).

### How It Works

**Employ:**

```
Alice: !employ [user]Bob[/user] maid
Bob: !consent
Bot: Alice has employed Bob as a maid!
```

**Effects:**
- Bob's profile: `characteristics["job"] = "maid"`
- Bob's profile: `characteristics["employer"] = "Alice"`
- Bob can now use `!work`

### Work Command

**Daily Duty:**

```
Bob: !work
Bot: Bob performs their duty as a maid: [random task]
Bob has earned 50 tokens!
```

**Implementation:**
1. Fetch duty for job type
2. Check if already worked today (cooldown)
3. Complete the duty
4. Grant rewards (tokens, experience)
5. Set work cooldown (24 hours)

**Duty Database:**

```csharp
{
  "jobName": "maid",
  "taskDescriptions": [
    "Clean the manor",
    "Serve tea to guests",
    "Organize the library"
  ],
  "rewards": {
    "tokens": 50,
    "experience": 10
  }
}
```

### Volunteer

Try other jobs without being employed:

```
Bob: !volunteer
Bot: Bob tries their hand at being a chef: [random task]
Bob earned 10 tokens! (Volunteers earn less than employees)
```

## Transformation System

Consequence interactions can transform users.

### Types of Transformations

**Species Change (Monsterize):**

```csharp
profile.characteristics["species"] = "dragon";
```

**Object Transformation:**

```csharp
profile.characteristics["objectified"] = "statue";
profile.characteristics["objectifiedBy"] = "Alice";
```

**Status Effects:**

```csharp
profile.characteristics["petrified"] = "true";
profile.characteristics["petrifiedBy"] = "Alice";
```

### Reversal

Most transformations can be reversed:

**Commands:**
- `!restore [user]Name[/user]` - Reverse transformations
- `!unmark [user]Name[/user]` - Remove marks

**Example:**

```
Alice: !restore [user]Bob[/user]
Bob: !consent
Bot: Bob has been restored to their original form!
```

**Implementation:**

```csharp
profile.characteristics.Remove("species");
profile.characteristics.Remove("objectified");
profile.characteristics.Remove("petrified");
// etc.
```

## Querying Interactions

### Dossier Command

View interaction history:

```
!dossier [user]Alice[/user]
```

**Output:**

```
Dossier for Alice:
- Kissed Bob (5 times)
- Cuddled Carol (3 times)
- Marked by Dave (collar)
- Employed by Eve (maid)
- Transformed into dragon by Frank
```

### Statistics Command

View your statistics:

```
!stats
```

or

```
!stats [user]Alice[/user]
```

**Output:**

```
Statistics for Alice:
Kisses: 42 (25 given, 17 received)
Cuddles: 15 (8 given, 7 received)
Renames: 3 (3 given, 0 received)
Total interactions: 60
```

## See Also

- [Command Reference](Command-Reference) - Full list of interaction commands
- [Database and Persistence](Database-and-Persistence) - How interactions are stored
- [Development Guide](Development-Guide) - Adding new interactions
- [Architecture](Architecture) - Processor pattern details
