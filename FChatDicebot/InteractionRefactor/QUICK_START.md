# Interaction Strategy Pattern - Quick Reference Card

## What You're Getting

✅ A cleaner way to organize interaction code
✅ 4 interactions already migrated as examples  
✅ Complete backward compatibility
✅ Easy path to add all your WIP features

## Files to Download

All files are in `/mnt/user-data/outputs/InteractionRefactor/`

### Must Install:
- `InteractionProcessors/` folder (entire directory)
- `ChateauInteractionHandler_NEW.cs` (replaces your current one)

### Documentation:
- `REFACTOR_COMPLETE_SUMMARY.md` - Overview of everything
- `INTERACTION_REFACTOR_MIGRATION_GUIDE.md` - Step-by-step installation
- `REFACTORING_RECOMMENDATIONS.md` - Future refactor ideas
- `WIP_FEATURES_DESIGN.md` - All your WIP feature designs
- `WIP_FEATURES_QUICK_REFERENCE.md` - Quick lookup

## 3-Minute Install

```bash
# 1. Backup your current file
cp ChateauInteractionHandler.cs ChateauInteractionHandler_BACKUP.cs

# 2. Copy new files
cp -r InteractionProcessors/ YourProject/
cp ChateauInteractionHandler_NEW.cs YourProject/ChateauInteractionHandler.cs

# 3. Update .csproj to include new files (see migration guide)

# 4. Compile and test
```

## What Works Immediately

These 4 interactions now use the new system:
- !kiss
- !cuddle  
- !handhold
- !mark

All other interactions still work using the old system.

## To Add a New Interaction

```csharp
// 1. Create InteractionProcessors/YourCategory/YourProcessor.cs
public class YourProcessor : InteractionProcessorBase
{
    public override string InteractionType => "yourtype";
    public override string InvestmentLevel => "casual"; // or involved/commitment/consequence

    public override string ProcessInteraction(PendingCommand command)
    {
        // Your logic here
        MonDB.removePendingInteraction(command.Id);
        return "yourtype";
    }

    public override string GetCompletionMessage(Profile initiator, Profile recipient, string id)
    {
        return $"{initiator.displayName} did something to {recipient.displayName}!";
    }
}

// 2. Register in InteractionProcessorRegistry.Initialize():
RegisterProcessor(new YourProcessor());

// 3. Done!
```

## Key Advantages

| Old Way | New Way |
|---------|---------|
| Edit 225+ line switch | Create new 30-line file |
| Find your code in massive switch | Each interaction in own file |
| Risk breaking other interactions | Changes are isolated |
| Merge conflicts when collaborating | No conflicts possible |
| Hard to test individual interactions | Test each interaction separately |

## What to Read First

1. **START HERE:** `REFACTOR_COMPLETE_SUMMARY.md` (5 min read)
2. **THEN:** `INTERACTION_REFACTOR_MIGRATION_GUIDE.md` (10 min read)
3. **OPTIONAL:** `REFACTORING_RECOMMENDATIONS.md` (for future refactors)

## Testing Your Installation

```csharp
// In your channel:
!kiss [user]Someone[/user]
// They consent
!consent

// Should work exactly as before
// If it does: ✅ Installation successful!
```

## Red Flags (What Could Go Wrong)

❌ **Compile errors about missing using statements**
   → Add: `using FChatDicebot.InteractionProcessors;`

❌ **"No processor found" errors**
   → Check that Initialize() is being called
   → Check that processor is registered

❌ **Old behavior for migrated interactions**
   → Make sure you commented out the switch cases
   → Check ChateauInteractionHandler uses _NEW version

## Green Lights (What Success Looks Like)

✅ Project compiles with no errors
✅ kiss, cuddle, handhold, mark work identically
✅ Other interactions (spank, rename, etc.) still work
✅ No runtime errors in console
✅ You feel good about the code organization

## When to Use This

**Use new system for:**
- All new interactions
- WIP features you're implementing
- Interactions you need to modify

**Keep in old system:**
- Interactions working fine that you don't need to touch
- Can migrate them later when convenient

## Support

If you get stuck:
- Check the migration guide
- Look at the example processors (Kiss, Cuddle, Handhold, Mark)
- Copy their pattern

The pattern is simple once you see one example!

## Bottom Line

This refactor will save you **hours of time** when implementing your 11+ WIP features. Instead of fighting with a giant switch statement, you'll have clean, organized, easy-to-find code.

Time to install: **10-15 minutes**
Time saved: **Hours and hours**

Worth it? **Absolutely.**
