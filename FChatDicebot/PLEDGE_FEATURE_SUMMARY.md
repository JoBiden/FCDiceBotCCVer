# !pledge Feature Implementation Summary

## Overview
Implemented the **!pledge** feature as described in WIP_FEATURES_DESIGN.md. This is an Involved-tier interaction that allows users to promise to perform an interaction with someone at a later time.

## Implementation Date
November 22, 2025

## Features Implemented

### Core Functionality
1. **!pledge {recipient} {interactiontype}** - Create a pledge
   - Creates a promise to perform an interaction later
   - Does NOT require consent (it's just a promise)
   - Only works for Involved, Commitment, and Consequence interactions (Casual interactions cannot be pledged)
   - Validates that the interaction type exists

2. **!viewpledges** - View all active pledges
   - Shows pledges you've made to others
   - Shows pledges others have made to you
   - Displays how long ago each pledge was made
   - Private message to the user

3. **!fulfill {recipient} {interactiontype}** - Fulfill a pledge
   - Explicitly fulfills a specific pledge
   - Creates a pending interaction that requires consent
   - Marks pledge as "fulfilled" after consent is given
   - Tracks "pledge honored" status if fulfilled 1+ days after creation

4. **!cancelpledge {recipient} {interactiontype}** - Cancel a pledge
   - Allows pledger to cancel their pledge
   - Costs 10 favor to cancel (represents the dramatic RP moment of breaking a promise)
   - Updates pledge status to "cancelled"

### Database Layer
- **Pledge Model** - New data model with fields:
  - pledger, pledgee, interactionType, identifier, investmentLevel
  - pledgeTime, fulfilledTime, status, pledgeHonored
- **Database Methods** - Full CRUD operations for pledges:
  - AddPledge, GetPledge, GetPledgesByPledger, GetPledgesByPledgee
  - GetActivePledges, UpdatePledge, DeletePledge

### Integration
- **ChateauInteractionHandler** - Updated to detect pledge fulfillment
  - Checks for pledgeId in interaction extraParameters
  - Marks pledge as fulfilled after successful interaction
  - Calculates and sets "pledge honored" status for pledges fulfilled 1+ days after creation

## Files Created
- `FChatDicebot/BotCommands/ChateauPledge.cs`
- `FChatDicebot/BotCommands/ChateauViewPledges.cs`
- `FChatDicebot/BotCommands/ChateauFulfill.cs`
- `FChatDicebot/BotCommands/ChateauCancelPledge.cs`

## Files Modified
- `FChatDicebot/Model/ChateauDB.cs` - Added Pledge model
- `FChatDicebot/Database/Ichateaudatabase.cs` - Added pledge interface methods
- `FChatDicebot/Database/Chateaudatabase.cs` - Implemented pledge database methods
- `FChatDicebot/MonDB.cs` - Added pledge static wrapper methods
- `FChatDicebot/ChateauInteractionHandler.cs` - Added pledge fulfillment detection

## Technical Details

### Pledge Status States
- **active** - Pledge is active and can be fulfilled
- **fulfilled** - Pledge was successfully completed
- **cancelled** - Pledge was cancelled by the pledger

### Pledge Honored
A pledge is marked as "honored" when:
- It is fulfilled (consented to and completed) at least 1 day after it was created
- This recognizes follow-through and commitment

### Command Auto-Discovery
Commands are automatically discovered via reflection in BotCommandController.
No manual registration required.

## Design Decisions

### Cancellation Cost
Set to 10 favor as the cost to cancel a pledge. This:
- Makes breaking a pledge meaningful but not prohibitive
- Represents the dramatic RP moment the design doc describes
- Can be adjusted if needed during testing

### Identifier Support
Currently, pledges support storing an identifier but the !pledge command doesn't yet accept identifiers as parameters. This can be added in a future enhancement when needed.

### Automatic Fulfillment
The design doc mentions "Method 2: Automatic fulfillment" where regular interactions could automatically fulfill matching pledges. This is NOT implemented in this version but could be added as an enhancement. The explicit !fulfill command (Method 1) is fully functional.

## Future Enhancements (Not Yet Implemented)

1. **Dossier Integration** - Add pledge summary to !dossier output
   - Show total number of active pledges
   - Show names of people involved in pledges
   - Keep output concise due to 50k character limit

2. **Automatic Fulfillment** - When someone uses a regular interaction command, check for matching pledges and offer to fulfill them automatically

3. **Identifier Parameters** - Allow !pledge to specify identifiers (e.g., `!pledge [user]Name[/user] feed chocolate`)

4. **Multiple Pledge Handling** - Better UX when multiple pledges of the same type exist to the same person

5. **Pledge Release** - Allow recipients to release someone from a pledge without cost

## Testing Recommendations

1. Test pledge creation with various interaction types
2. Verify Casual interactions are rejected
3. Test !viewpledges with multiple pledges
4. Test !fulfill with consent flow
5. Test pledge honored status calculation (1+ days)
6. Test cancellation with sufficient/insufficient favor
7. Test edge cases (non-existent users, invalid interaction types, etc.)

## Notes

- All pledge operations are channel-based (RequireChannel = true)
- Pledge system respects the existing consent framework
- Fulfilling a pledge goes through the normal interaction processor system
- Error handling includes graceful fallback if pledge fulfillment tracking fails
