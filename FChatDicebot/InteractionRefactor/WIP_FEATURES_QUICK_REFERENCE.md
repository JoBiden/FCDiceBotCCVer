# Chateau Contract - WIP Features Quick Reference

## Involved Interactions (No cooldown, requires RP context)

### !climaxfor / !receiveclimaxfrom {recipient} {bodypart}
- Opt-in body part system (using = consenting to bot referencing it)
- Daily frequency tracking (first = full, repeats = surprised they can keep going)
- Awards context-dependent currencies
- Monster-type affects output

### !milk {recipient} {bodilyfluid}
- Uses daily frequency system like climax
- Draws from curated 'substance' identifier category
- May produce currencies/items based on fluid type
- Monster-type affects output

### !pledge {recipient} {interactiontype}
- Promise to do an interaction later
- Only Involved/Commitment/Consequence interactions pledgeable (not Casual)
- Fulfillment requires consent
- Can be fulfilled automatically if matching interaction happens
- Cancellable with a cost
- Tracked as "pledge honored" if fulfilled 1+ days later

## Commitment Interactions (With cooldowns, requires RP, meaningful commitment)

### !entitle {recipient} {"title"}
- 100 character limit
- User-given (with prefix/suffix marker) vs System-given (clean text)
- Multiple titles allowed
- Cooldown on receiving (probably)
- Cost to give (possibly bypassed by bonds)
- Cost to remove/change

### !breed {recipient} {monster} → !birth
- Monster type determines offspring (not parent genetics)
- Carrier decides when to !birth (min 1 day, no max)
- No cooldown, can carry multiple pregnancies
- Affects !milk text and !work routes
- Different offspring count formulas per monster
- Miniscule chance to create NPC (mod-facilitated)
- Tracks global births per monster type

### !train {recipient} {training}
- Progressive system (possibly infinite for some trainings)
- Cannot self-train
- Early stages: anyone can train
- Advanced stages: need trainer at your level or higher
- Unlocks !work routes
- Affects flavor text
- Permanent once acquired

### !corrupt / !purify {recipient} {amount}
- Single axis: corruption ↔ purity
- "Evil is sexy" theme
- Spreads through interactions (chance proportional to difference)
- Progressive with many titles
- Determines access to curses/blessings

## Consequence Interactions (Permanent effects, serious warnings, no take-backs)

### !infest {recipient} {parasite} → !cure
- Multiple parasites allowed
- Spreads deliberately (!infest) or randomly with consent prompt
- Requires trained player to !cure
- Affects interaction text and !work routes
- Visible indicator

### !odorize {recipient} {scent}
- Multiple scents allowed
- Potency decays each time noticed
- Must be refreshed over time
- Monster 'perfume' category for longer-lasting scents
- Only removed by natural decay (initially)

### !curse {recipient} {curse}
- Real mechanical effects (blocks interactions, changes outcomes)
- Can stack
- Persistent until removed by another player
- Access based on corruption level and monster type
- Blessings = curses with pure flavor (same mechanics)

### !break {recipient} {body/mind/hole}
- Duration based on break history (max 1 week)
- Blocks affected interactions completely
- Multiple parts can be broken
- Heals with time or player interaction
- Can prevent !work entirely

### !dose {recipient} {vice}
- Multiple vices allowed
- Addiction strengthens with doses
- Cravings triggered by interaction count
- Can be satisfied (worsens) or resisted (improves)
- Weighted toward worsening
- Strong addiction forces specific !work jobs
- Consent given at initial dose

## Common Themes
- **No hard time limits** (respects busy lives)
- **Consent is fundamental** (always required for interactions affecting others)
- **Monster types matter** (affect many interactions)
- **Work integration** (many systems unlock/affect job routes)
- **Multiple simultaneous effects** (can stack most things)
- **Player-driven healing** (most negative effects require help from others)
- **Daily resets** (for frequency-based systems)
- **50k character limit** (be mindful in dossier and outputs)
