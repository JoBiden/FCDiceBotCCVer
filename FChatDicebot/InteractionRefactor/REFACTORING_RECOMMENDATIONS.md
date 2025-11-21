# Chateau Contract - Refactoring Recommendations

Based on analysis of the current codebase and planned WIP features, here are recommended refactors organized by priority and when to implement them.

---

## HIGH PRIORITY - Do These First (Before Implementing New Features)

### 1. Extract Interaction Processing Logic into Strategy Pattern

**Current Issue:**
- `ChateauInteractionHandler.addInteraction()` uses a massive switch statement (225+ lines)
- Every new interaction type requires editing this central function
- Difficult to test individual interactions in isolation
- Hard to maintain and prone to merge conflicts

**Recommendation:**
Create an interface-based system where each interaction type is its own class:

```csharp
public interface IInteractionProcessor
{
    string InteractionType { get; }
    string InvestmentLevel { get; } // "casual", "involved", "commitment", "consequence"
    string ProcessInteraction(PendingCommand command);
    bool ValidateInteraction(string initiator, string recipient, string identifier);
    string GetConsentWarning(Profile initiatorProfile, Profile recipientProfile, string identifier);
}
```

**Benefits:**
- Each interaction in its own file (easier to find and modify)
- Can test interactions independently
- Adding new interactions doesn't touch existing code
- Enables easier feature flags and A/B testing
- Follows Open/Closed Principle

**When:** Do this BEFORE implementing any WIP features that add new interactions.

**Estimated Effort:** 1-2 days to refactor existing interactions, then all future interactions become trivial to add.

---

### 2. Create a Cooldown/Timer Management System

**Current Issue:**
- Timers are manually set in each interaction case
- All use hardcoded durations (e.g., `AddDays(7)`, `AddDays(1)`)
- No centralized way to check if someone can perform an interaction
- Timer logic duplicated across multiple files

**Recommendation:**
Create a `TimerManager` class:

```csharp
public class TimerManager
{
    // Check if action can be performed
    public static bool CanPerformAction(string userName, string actionType);
    
    // Set a timer with configuration
    public static void SetTimer(string userName, string timerType, TimeSpan duration);
    
    // Get timer configuration from database or config
    public static TimerConfig GetTimerConfig(string interactionType, string investmentLevel);
    
    // Check remaining cooldown
    public static TimeSpan GetRemainingCooldown(string userName, string timerType);
}

public class TimerConfig
{
    public string InteractionType { get; set; }
    public TimeSpan DefaultDuration { get; set; }
    public bool AppliesToInitiator { get; set; }
    public bool AppliesToRecipient { get; set; }
}
```

**Benefits:**
- Easy to adjust cooldowns without code changes
- Can store cooldown configs in database for runtime changes
- Consistent cooldown checking across all interactions
- Easier to implement variable cooldowns (e.g., based on break history)

**When:** Do this BEFORE implementing new commitment/consequence interactions.

**Estimated Effort:** 1 day to implement, 1-2 days to refactor existing timer code.

---

### 3. Unified Text Generation System

**Current Issue:**
- Interaction flavor text is hardcoded in `ChateauConsent.getInteractionMessage()`
- Random descriptors are inline lists
- No way to customize text based on characteristics (monster type, corruption, etc.)
- Adding variations requires editing the switch statement

**Recommendation:**
Create a template-based text generation system:

```csharp
public class InteractionTextGenerator
{
    // Get text with variable substitution and random selection
    public static string GenerateText(string interactionType, InteractionContext context);
    
    // Load templates from database or files
    private static InteractionTemplate GetTemplate(string interactionType);
    
    // Apply conditional logic (if monster type X, use Y text)
    private static string ApplyConditionals(string template, InteractionContext context);
}

public class InteractionContext
{
    public Profile InitiatorProfile { get; set; }
    public Profile RecipientProfile { get; set; }
    public string Identifier { get; set; }
    public Dictionary<string, object> CustomData { get; set; }
}

public class InteractionTemplate
{
    public string InteractionType { get; set; }
    public List<string> BaseDescriptors { get; set; }
    public Dictionary<string, List<string>> ConditionalDescriptors { get; set; } // e.g., "monster:slime" -> special text
    public string Template { get; set; } // e.g., "{initiator} kisses {recipient}, {descriptor}"
}
```

**Benefits:**
- Easy to add text variations without code changes
- Can add monster-specific text for WIP features
- Can implement daily frequency text variations
- Writers can contribute without coding
- A/B test different text

**When:** Do this BEFORE implementing !climaxfor, !milk, or other text-heavy interactions.

**Estimated Effort:** 2-3 days to implement, 1-2 days to migrate existing text.

---

## MEDIUM PRIORITY - Do When Relevant

### 4. Profile Data Structure Enhancements

**Current Issue:**
- Profile uses generic `Dictionary<string, X>` for everything
- No type safety or validation
- Difficult to query complex data (e.g., "all characters with parasite X")

**Recommendation:**
Keep the flexible dictionary structure for extensibility, but add typed wrappers:

```csharp
public class ProfileExtensions
{
    // Strongly typed accessors
    public static int GetCorruptionLevel(this Profile profile);
    public static void SetCorruptionLevel(this Profile profile, int level);
    
    public static List<Parasite> GetParasites(this Profile profile);
    public static void AddParasite(this Profile profile, Parasite parasite);
    
    public static int GetDailyInteractionCount(this Profile profile, string interactionType);
    public static void IncrementDailyCount(this Profile profile, string interactionType);
    public static void ResetDailyCounts(this Profile profile);
    
    // Break tracking
    public static bool IsPartBroken(this Profile profile, string partType);
    public static DateTime GetBreakExpiry(this Profile profile, string partType);
    public static int GetBreakCount(this Profile profile, string partType);
}

// New structured data types
public class Parasite
{
    public string Name { get; set; }
    public string Infector { get; set; }
    public DateTime InfectedDate { get; set; }
    public int SpreadCount { get; set; }
}

public class Title
{
    public string Text { get; set; }
    public string GivenBy { get; set; } // null for system titles
    public DateTime AwardedDate { get; set; }
    public bool IsSystemTitle => GivenBy == null;
}
```

**When:** Do this when implementing first WIP feature that needs complex data (!corrupt, !infest, !entitle).

**Estimated Effort:** 1-2 days depending on scope.

---

### 5. Daily Reset System

**Current Issue:**
- No mechanism for daily resets (needed for !climaxfor, !milk frequency tracking)
- "Chateau day" concept not implemented

**Recommendation:**
Create a daily reset scheduler:

```csharp
public class ChateauDayManager
{
    // Check if it's a new Chateau day and process resets
    public static void ProcessDayRollover();
    
    // Reset all daily counts for all profiles
    private static void ResetDailyCounts();
    
    // Get current Chateau day number (for tracking)
    public static int GetCurrentChateauDay();
    
    // Check if action happened "today"
    public static bool WasToday(DateTime timestamp);
}
```

Call `ProcessDayRollover()` from `BotMain` periodic tick or at start of each command.

**When:** Do this BEFORE implementing !climaxfor or !milk.

**Estimated Effort:** 1 day.

---

### 6. Consent System Enhancement

**Current Issue:**
- Consent warnings are inline strings in each command
- No way to show detailed consequences before consent
- Difficult to add consent prompts for random effects (parasite spread, corruption spread)

**Recommendation:**
Create a `ConsentWarning` system:

```csharp
public class ConsentWarning
{
    public string WarningText { get; set; }
    public List<string> ConsequenceList { get; set; }
    public ConsentSeverity Severity { get; set; } // Info, Warning, Severe
    
    public string ToFormattedString()
    {
        string result = WarningText;
        if (ConsequenceList.Any())
        {
            result += "\n[b]Consequences:[/b]";
            foreach (var consequence in ConsequenceList)
            {
                result += "\n• " + consequence;
            }
        }
        return result;
    }
}

public enum ConsentSeverity
{
    Info,       // Casual interactions
    Warning,    // Commitment interactions
    Severe      // Consequence interactions
}
```

**When:** Do this BEFORE implementing consequence-tier WIP features.

**Estimated Effort:** 1 day, plus updating existing commands gradually.

---

## LOW PRIORITY - Nice to Have

### 7. Command Parameter Parser Refactor

**Current Issue:**
- Parameter parsing is manual and repetitive
- Each command parses `[user]` tags and identifiers independently
- No validation or error handling consistency

**Recommendation:**
Create a fluent parameter parser:

```csharp
var parameters = CommandParser.Parse(rawTerms)
    .RequireUser(out string recipient)
    .RequireIdentifier("bodypart", out string bodypart)
    .OptionalNumber(out int amount, defaultValue: 1)
    .Validate();

if (!parameters.IsValid)
{
    bot.SendPrivateMessage(parameters.GetErrorMessage(), characterName);
    return;
}
```

**When:** Do this when you're frustrated with parameter parsing for the 50th time.

**Estimated Effort:** 2-3 days.

---

### 8. Identifier Category Management

**Current Issue:**
- Identifiers stored in database but no tooling to manage them
- Adding new categories requires manual database insertion
- No way to bulk import/export identifiers

**Recommendation:**
Create admin commands or a simple CLI tool:
- `!admin addidentifier {type} {category} "description"`
- `!admin listidentifiers {category}`
- CSV import/export for identifiers

**When:** When you start implementing WIP features and need to add lots of identifiers.

**Estimated Effort:** 1-2 days.

---

## REFACTOR TIMING RECOMMENDATIONS

### Do in Advance (Highest Value):
1. **Interaction Strategy Pattern** - Will save enormous time on every new feature
2. **Timer Management System** - Needed by almost every WIP feature
3. **Daily Reset System** - Blocks !climaxfor and !milk implementation

### Do When First Needed:
4. **Text Generation System** - First needed for !climaxfor/!milk text variations
5. **Profile Extensions** - First needed for !corrupt or !infest
6. **Consent Warning System** - First needed for consequence interactions

### Do When Annoying:
7. **Parameter Parser** - Quality of life improvement
8. **Identifier Management** - Quality of life for content creation

---

## IMPLEMENTATION ORDER RECOMMENDATION

If you're planning to implement WIP features soon:

**Phase 1 - Foundation (Do First):**
1. Interaction Strategy Pattern
2. Timer Management System  
3. Daily Reset System
4. Text Generation System

**Phase 2 - As Needed:**
5. Profile Extensions (when implementing !corrupt, !infest, !entitle)
6. Consent Warning System (when implementing consequence interactions)

**Phase 3 - Polish:**
7. Parameter Parser
8. Identifier Management Tools

---

## NOTES ON BACKWARD COMPATIBILITY

All these refactors can be done incrementally:
- Old interaction types can remain in switch statement while new ones use strategy pattern
- Timer management can coexist with manual timer setting
- Text generation can be opt-in per interaction type

This means you can refactor gradually without breaking existing functionality.

---

## TESTING CONSIDERATIONS

After each refactor, you should be able to:
- Run existing interactions and verify they work identically
- Add a new interaction using the new system
- Confirm the new system is easier than the old way

If any refactor doesn't demonstrably improve developer experience, skip it or revise it.
