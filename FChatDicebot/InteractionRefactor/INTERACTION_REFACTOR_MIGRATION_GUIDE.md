# Interaction Strategy Pattern - Migration Guide

## What This Refactor Does

Transforms the interaction processing system from a single massive switch statement into individual, testable, maintainable classes for each interaction type.

## Files Created

### Core System Files:
1. **InteractionProcessors/IInteractionProcessor.cs** - Interface that all processors implement
2. **InteractionProcessors/InteractionProcessorBase.cs** - Base class with common functionality
3. **InteractionProcessors/InteractionProcessorRegistry.cs** - Registry that manages all processors

### Example Processors (Migrated):
4. **InteractionProcessors/Casual/KissProcessor.cs** - Kiss interaction
5. **InteractionProcessors/Casual/CuddleProcessor.cs** - Cuddle interaction
6. **InteractionProcessors/Casual/HandholdProcessor.cs** - Handhold interaction
7. **InteractionProcessors/Commitment/MarkProcessor.cs** - Mark interaction

### Updated Files:
8. **ChateauInteractionHandler_NEW.cs** - Updated version that supports both systems

## Installation Steps

### Step 1: Add New Files to Your Project

1. Create the `InteractionProcessors` directory in your project root
2. Create subdirectories: `Casual/` and `Commitment/`
3. Copy all the new files from `/home/claude/InteractionProcessors/` to your project

### Step 2: Update Your .csproj File

Add these lines to include the new files in compilation:

```xml
<Compile Include="InteractionProcessors\IInteractionProcessor.cs" />
<Compile Include="InteractionProcessors\InteractionProcessorBase.cs" />
<Compile Include="InteractionProcessors\InteractionProcessorRegistry.cs" />
<Compile Include="InteractionProcessors\Casual\KissProcessor.cs" />
<Compile Include="InteractionProcessors\Casual\CuddleProcessor.cs" />
<Compile Include="InteractionProcessors\Casual\HandholdProcessor.cs" />
<Compile Include="InteractionProcessors\Commitment\MarkProcessor.cs" />
```

### Step 3: Replace ChateauInteractionHandler.cs

**IMPORTANT: Backup your current file first!**

```bash
# Backup
cp ChateauInteractionHandler.cs ChateauInteractionHandler_BACKUP.cs

# Replace with new version
cp ChateauInteractionHandler_NEW.cs ChateauInteractionHandler.cs
```

### Step 4: Update ChateauConsent.cs (Optional but Recommended)

The `getInteractionMessage` method in ChateauConsent should be updated to use the new processors.

Change this part (around line 65):
```csharp
channelMessage += getInteractionMessage(toConsent.pendingInteraction.type, 
    toConsent.pendingInteraction.identifier, 
    toConsent.pendingInteraction.initiator, 
    toConsent.pendingInteraction.recipient);
```

To this:
```csharp
// Try new processor first
var processor = InteractionProcessors.InteractionProcessorRegistry.GetProcessor(toConsent.pendingInteraction.type);
if (processor != null)
{
    Profile initProfile = MonDB.getProfile(toConsent.pendingInteraction.initiator);
    Profile recipProfile = MonDB.getProfile(toConsent.pendingInteraction.recipient);
    channelMessage += processor.GetCompletionMessage(initProfile, recipProfile, toConsent.pendingInteraction.identifier);
}
else
{
    // Fallback to old method
    channelMessage += getInteractionMessage(toConsent.pendingInteraction.type, 
        toConsent.pendingInteraction.identifier, 
        toConsent.pendingInteraction.initiator, 
        toConsent.pendingInteraction.recipient);
}
```

## How to Test

### Test 1: Test Migrated Interactions

Test that kiss, cuddle, handhold, and mark still work:

1. Have two test characters in the channel
2. Try `!kiss [user]Character[/user]`
3. Target character does `!consent`
4. Verify the message appears correctly
5. Repeat for cuddle, handhold, and mark

### Test 2: Test Non-Migrated Interactions

Test that old interactions still work:

1. Try any interaction NOT migrated (spank, bully, rename, etc.)
2. Verify they still work exactly as before

### Test 3: Verify Backward Compatibility

The system should work identically to before, just with cleaner code under the hood.

## Benefits You'll See Immediately

1. **Easier to Add New Interactions** - Just create a new processor class
2. **Easier to Test** - Test each interaction in isolation
3. **Easier to Find Code** - Each interaction in its own file
4. **Easier to Modify** - Change one interaction without touching others
5. **No More Merge Conflicts** - Multiple people can work on different interactions

## How to Migrate More Interactions

To migrate another interaction (e.g., "spank"):

1. Create `InteractionProcessors/Casual/SpankProcessor.cs`
2. Implement the interface (copy KissProcessor as a template)
3. Add to registry in `InteractionProcessorRegistry.Initialize()`
4. Comment out the case in the switch statement
5. Test it

Example template:

```csharp
public class SpankProcessor : InteractionProcessorBase
{
    public override string InteractionType => "spank";
    public override string InvestmentLevel => "casual";

    public override string ProcessInteraction(PendingCommand command)
    {
        string initiator = command.pendingInteraction.initiator;
        string recipient = command.pendingInteraction.recipient;

        // Your logic here (copied from switch statement)
        IncrementDifferentCounts(initiator, recipient, "spankgive", "spanktake");
        MonDB.removePendingInteraction(command.Id);

        return "spank";
    }

    public override string GetCompletionMessage(Profile initiatorProfile, Profile recipientProfile, string identifier)
    {
        // Your message logic here (copied from ChateauConsent.getInteractionMessage)
        var spankDescriptors = new List<string> { ... };
        return $"{initiatorProfile.displayName} winds up and gives {recipientProfile.displayName} {GetRandomDescriptor(spankDescriptors)}";
    }
}
```

## Rollback Plan

If something goes wrong:

```bash
# Restore original file
cp ChateauInteractionHandler_BACKUP.cs ChateauInteractionHandler.cs

# Remove the new InteractionProcessors directory
# Rebuild your project
```

The old code is still there - we just added a new layer on top.

## Next Steps After This Refactor

1. Migrate remaining casual interactions (spank, bully)
2. Migrate involved interactions (dressup, feed, golden, payment)
3. Migrate commitment interactions (bond, consume, employ)
4. Migrate consequence interactions (rename, monsterize, petrify, plant, objectify)
5. Start implementing WIP features using the new system

## Questions?

This refactor is backwards compatible - both old and new systems work side by side. You can migrate interactions gradually at your own pace.
