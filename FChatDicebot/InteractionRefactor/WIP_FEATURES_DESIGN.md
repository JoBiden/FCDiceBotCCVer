# Chateau Contract - WIP Features Design Document

This document captures design decisions and implementation plans for features marked as "WIP (not yet implemented)" in the Chateau profile.

---

## !climaxfor / !receiveclimaxfrom {recipient} {bodypart}
**Category:** Involved Interactions  
**Description:** Cum with/for someone

### Design Philosophy
- **No actual cooldown** - would disrupt RP flow
- **Opt-in detail system** - using a body part = consenting to bot referencing it
- Balance between personalization and avoiding data entry burden

### Command Structure
- `!climaxfor {recipient} {bodypart}` - initiator climaxes for/with recipient using specified body part
- `!receiveclimaxfrom {recipient} {bodypart}` - recipient climaxes for initiator using specified body part
- Two separate commands for role clarity

### Body Part System
- Draw from existing identifier database with 'bodypart' tag
- Create new **'climax' category** for body parts that make sense for this interaction
- Not all body part identifiers should be available (needs manual curation)

### Daily Frequency System (applies to both climax and milk)
- Track daily count (resets each Chateau day)
- **Flavor text variations:**
  - First time of day: particularly full/abundant
  - Subsequent times: surprised they can keep going
  - **No specific volume commentary** on repeat uses
- This allows RP to flow naturally without cooldown restrictions

### Stat Tracking Considerations
- Track: number of climaxes, types used, volumes
- Stats could affect output text
- **Concern:** Stat progression might conflict with player's character vision
- Need to think carefully about whether/how stats should progress

### Volume Mechanics
- **TBD** - could be time-based, cumulative, or based on other factors
- Both options have pros and cons, need further consideration

### Special Features
- Custom descriptions based on player's 'monster' characteristic (if they have one)
- Different output based on transformation states

### Currency/Rewards (context-dependent)
May award any of the following based on monster type and other factors:
- cummies
- ectoplasm
- eggs
- exposure
- pleasure
- pride
- sex
- soul
- stats

### Role Clarity
- **Initiator** = the one climaxing
- **Recipient** = the one being climaxed for/with
- Separate commands handle role reversal

---

## !milk {recipient} {bodilyfluid}
**Category:** Involved Interactions  
**Description:** Milk someone, whether that's actual milk, or something else!

### Cooldown Approach
- **No actual cooldown timer**
- Uses same daily frequency system as !climaxfor (see above)

### Bodily Fluid System
- Draw from existing **'substance' identifier category**
- Needs curation to filter for bodily fluids and milk-able substances
- Not all substances will be appropriate for milking

### Stat Tracking
- **Who is doing the milking** (initiator)
- **Who is being milked** (recipient)
- **What fluids are being milked**
- **Daily count** for flavor text (resets at Chateau day start)
- Frequency over time is NOT important beyond daily reset

### Future Interactions
- May be influenced by **curses, infestations, and other Consequence-tier effects**
- Will address these cross-interactions when implementing those systems
- Cross that bridge when we get there

### Rewards
- May produce **currencies/items depending on fluid type**
- Similar reward system to !climaxfor

### Special Features
- Custom descriptions based on **monster types and transformations**
- Output varies based on character state

---

## !pledge {recipient} {interactiontype}
**Category:** Involved Interactions  
**Description:** Promise someone that you will do a specific interaction, when the time is right

### Foundational Principle
**Any interaction involving another person must be consented to by the other person.** This is non-negotiable and applies to pledges as well.

### Purpose
- Store an interaction that hasn't been roleplayed out yet
- For interactions that shouldn't happen in the current moment
- Creates a visible promise that others can see in the bot
- Easy way to invoke the interaction when the moment is right

### Consent Requirements
- Creating a pledge: Does NOT require immediate consent (it's just a promise)
- Fulfilling a pledge: **DOES require consent** from the person the interaction was pledged to
- Same consent system as regular interactions applies

### Which Interactions Can Be Pledged
- **Casual interactions:** NOT pledgeable (not noteworthy enough)
- **Involved interactions:** Pledgeable
- **Commitment interactions:** Pledgeable
- **Consequence interactions:** Pledgeable

### Display and Visibility
- Full pledge details should be in their own command (e.g., `!pledges` or `!viewpledges`)
- In !dossier and other outputs: Show summary only due to 50,000 character limit
  - Display total number of pledges
  - Display names of people involved in pledges
  - NOT full context of each pledge

### Duration and Expiration
- Pledges remain **indefinitely** until fulfilled or cancelled
- No time limits or automatic expiration
- Design philosophy: People have busy lives, nothing should have forced time limits

### Fulfillment Process - Two Methods

#### Method 1: Explicit !fulfill command
- `!fulfill {pledgeidentifier}` or similar
- Direct way to invoke a pledged interaction

#### Method 2: Automatic fulfillment (additional feature)
- When someone uses a regular interaction command (e.g., `!kiss {recipient}`)
- Bot automatically checks if there's a matching pledge
- If match found, tack the pledge fulfillment onto the normal interaction
- Leverages existing !consent system that handles multiple interactions
- Fulfilled pledge + new interaction both require consent

### Tracking
- Track pledges that are fulfilled **at least a day after they're made**
- These should be marked as **'pledge honored'** or similar
- Shows commitment and follow-through

### Cancellation
- Pledges are **cancellable** by the pledger
- Cancellation has an **associated cost** (currency, stat penalty, or similar)
- Design rationale: Even if people leave pledges unfulfilled to avoid the cost, **intentionally breaking a pledge is a dramatic RP moment worth supporting**
- The act of turning your back on a pledge should be mechanically recognized

### Implementation Questions Still To Consider
- What is the exact cost for cancelling a pledge?
- How should the !fulfill command identify which pledge to fulfill if someone has multiple?
- Should recipients be able to decline/release someone from a pledge?

---

## !entitle {recipient} {"title in quotes"}
**Category:** Commitment Interactions (WIP)  
**Description:** Give someone a special title. Note that the title will have a prefix and/or suffix symbol to indicate it was given by a user

### Title Display
- Visible in **!dossier**
- **Character limit: 100 characters** (tight limit due to dossier space constraints)
- Multiple titles allowed per person

### Two Types of Titles

#### User-Given Titles
- Given via `!entitle {recipient} {"title"}`
- Have a **prefix/suffix marker** to indicate player-given status
  - Similar to ✦Title✦ format, but possibly with smaller visual footprint characters
  - Distinguishes user-given from system-given titles
- Different people can give different titles to the same person
- All user-given titles for a person are preserved

#### System-Given Titles
- Awarded by the bot automatically based on achievements
- **No prefix/suffix** - appear as clean text
- Same requirements for everyone (objective achievements)
- Examples of triggers:
  - Performing same interaction type multiple times
  - Having specific bonds
  - Having people employed by you
  - Achieving certain number of different characteristics
  - Other milestone achievements

### Cooldowns
- **Cooldown on RECEIVING a title** (probably)
- TBD: Exact cooldown duration
- Note: Still undecided between receiving vs giving cooldown

### Costs and Requirements
- **Cost to give a title** (currency, resources, or similar)
- Cost might be **bypassed based on existing Bonds**
  - Example: No cost to entitle your bonded partner/employee/etc.
- Creates meaningful decision about who to title

### Modification and Removal
- Titles can be **removed or changed**
- Removal/change has an **associated cost**
- Prevents frivolous title spam

### Implementation Questions Still To Consider
- Exact cooldown duration for receiving titles?
- Confirm: cooldown on receiving vs giving?
- What specific cost to give a title?
- What specific cost to remove/change a title?
- Which bonds bypass the title-giving cost?
- Exact prefix/suffix characters to use?
- How are multiple titles displayed in dossier?
- What are the specific system-title achievement criteria?
- Should there be a command to view all of someone's titles separately?

---

## !breed {recipient} {monster}
**Category:** Commitment Interactions (WIP)  
**Description:** Breed someone with new life. Will come with a !birth command to control the timing

### Monster Type Selection
- `{monster}` parameter specifies what type of offspring is created
- **RP determines offspring type, not parent genetics**
- Monster type can differ from either parent's species
- Design rationale: Roleplay scene context matters more than biological logic

### Birth Timing (!birth command)
- **Person carrying decides** when to give birth
- Uses `!birth` command when ready
- **Minimum time:** 1 day after breeding
- **Maximum time:** None - indefinite pregnancy possible
- Design philosophy: Passage of time determined by RP flow, not forced timers

### Cooldowns
- **No cooldown** for breeding someone
- **No cooldown** for being bred
- Someone can be bred **multiple times** before giving birth
- Can carry multiple pregnancies simultaneously

### Post-Birth Effects

#### Character Profile
- Record on character's profile indicating the birth
- Shows birth history/count

#### Global Statistics
- Track **total births of each monster type** globally
- Chateau-wide statistic visible to all

### Pregnancy State Effects

#### Interaction Modifications
- Being pregnant affects **!milk interaction text**
- Pregnancy-specific flavor text for milking

#### Work System Integration
- Some **!work scenarios** only accessible while pregnant
- Pregnancy opens special work routes
- Adds variety and pregnancy-specific RP opportunities

### Offspring Count Formula
- **Different formulas per monster type**
- Some monsters have single births, others have litters/clutches/swarms
- Formula determined by monster identifier properties

### NPC Creation System
- **Extremely rare chance** to create a Chateau NPC from birth
- Chance is "almost miniscule"
- Different chance formulas per monster type
- **All NPC creations must be facilitated by moderator or bot owner**
- Design rationale: Keeps NPC creation special and controlled
- Ensures quality and prevents NPC spam

### Priority System
- **Monster type of offspring** is primary factor
- More important than monster type of either parent
- Offspring monster type drives all formulas and effects

### Implementation Questions Still To Consider
- Exact offspring count formulas for each monster type
- Exact NPC creation chance formulas for each monster type
- How is pregnancy state displayed in dossier?
- Should there be a !pregnancies command to view active pregnancies?
- What specific !work routes are pregnancy-only?
- Can pregnancy be terminated early, or only through !birth?
- Should multiple simultaneous pregnancies have any special interactions?

---

## !train {recipient} {training}
**Category:** Commitment Interactions (WIP)  
**Description:** Train someone in special techniques and skills

### Training System Type
- {training} from a list of **identifiers**
- **Progression-based** rather than binary
- Some trainings may be **infinitely progressable**
- Note: Work experience system may already cover some progression use cases

### Session Requirements
- Multiple sessions needed for progression (not binary)
- **Cannot train yourself** - must have someone train you
- Can be flavored as:
  - Mutual practice between peers
  - Traditional mentor/student arrangement
  - Other training scenarios

### Multiple Training
- Can be trained in **multiple things simultaneously**
- No limit on concurrent trainings

### Trainer Qualifications - Tiered System

#### Early Stages (Bootstrapping Solution)
- **Anyone can train early stages**
- Solves the "first master" problem
- Allows new trainings to spread organically
- Low levels accessible to everyone

#### Advanced Stages
- Must train with someone **equal to or better** than you
- Trainer must have at least your level in that training
- Prevents unqualified training at higher levels
- Creates natural mentor hierarchy

### Training Effects

#### Existing: Work System Integration
- Training **unlocks new !work routes** (already implemented)
- Different trainings open different work opportunities

#### Planned: Flavor Text Modifications
- Training affects flavor text in various interactions
- "Nice to have" feature for future implementation
- Adds depth and recognition of character skills

### Permanence
- Training is **permanent once acquired**
- Cannot be lost or reduced
- Builds over character's lifetime

### Cooldown
- TBD: Whether there's a cooldown between training sessions
- TBD: If so, is cooldown per-training or global?

### Implementation Questions Still To Consider
- Exact progression stages for each training identifier?
- Is there a cooldown between training sessions?
- What specific flavor text changes based on training?
- Should there be a !trainings command to view your training levels?
- Can training be displayed in dossier, and how?
- Are there any costs associated with training?
- Should training sessions require RP context like other Commitment interactions?
- Do some trainings have maximum levels while others are infinite?
- What determines which trainings are infinite vs capped?

---

## !corrupt {recipient} {amount} / !purify {recipient} {amount}
**Category:** Commitment Interactions (WIP)  
**Description:** Corrupt (and possibly purify as well) someone else

### Command Structure
- **!corrupt** - increases corruption
- **!purify** - separate command for flavor reasons
- Both mechanically similar but thematically opposite
- Purification = negative corruption levels

### Amount Parameter - Two Options Under Consideration

#### Option 1: Numeric Range
- Open to any whole number within a certain range
- Direct control over corruption amount
- More precise but less flavorful

#### Option 2: Named Categories
- 'Light', 'Moderate', 'Intense' (or similar tiers)
- Randomly assigned values in background within each tier's range
- More flavorful but less precise control
- **Currently undecided between these approaches**

### Corruption/Purity Scale
- **Single axis system**
- Cannot have both corruption AND purity simultaneously
- Progression scale (likely negative to positive)
- Example: -100 (pure) ← 0 (neutral) → +100 (corrupt)

### Corruption Theme
- **"Evil is sexy" aesthetic**
- NOT about actual immoral behavior
- Emphasizes temptation, indulgence, darkness as attractive
- Stays within roleplay-appropriate boundaries

### Progression System
- Corruption is **progressive**
- Exact progression mechanics TBD
- Not yet decided what the progression looks like
- Will involve multiple stages/thresholds

### Spreading Mechanics
- Corruption/purity has a **chance to spread** through interactions
- Spreads through **almost any interaction** (not just !corrupt command)
- Spread chance **proportional to difference** between participants
  - Large difference = higher spread chance
  - Example: Very corrupt person interacting with pure person = high spread chance
  - Similar levels = low/no spread chance
- Creates organic corruption/purification through regular RP

### Visual Indicators
- Text-based system (no actual visual indicators)
- **Titles are primary indicator**
- Many fun corruption/purity themed titles
- Titles change based on corruption level/stage

### Source Tracking
- **Number of corruption sources is irrelevant**
- Only total corruption level matters
- Not tracking who corrupted whom

### Implementation Questions Still To Consider
- Numeric range vs named categories for amount parameter?
- Exact corruption scale (what are min/max values)?
- What are the progression stages/thresholds?
- Exact spread chance formula based on difference?
- Which interactions can spread corruption?
- What titles correspond to which corruption levels?
- Does corruption affect any interaction flavor text beyond titles?
- Is there a cooldown on using !corrupt or !purify?
- Should there be a cost to corrupt/purify someone?

---

## !infest {recipient} {parasite}
**Category:** Consequence Interactions (WIP)  
**Description:** Infest someone with a parasite. Parasites will have a chance to spread in some fashion

### Parasite Naming System - Two Options Under Consideration

#### Option 1: Predefined Identifier List
- Create curated list of parasite types
- Standard identifiers like other systems

#### Option 2: Player-Named Parasites
- Named by or after the **first person to infest someone** with that parasite type
- Creates unique, personalized parasites
- More creative freedom but harder to balance
- **Currently undecided between these approaches**

### Spreading Mechanics

#### Deliberate Spread
- **!infest command** always spreads parasites intentionally
- Direct method of spreading infestation

#### Random Spread
- **Small chance** to spread through other interactions
- Only a **small subset of interactions** can spread parasites
- Design rationale: Not everyone wants to opt into this system

#### Consent for Random Spread
- User gets a chance to **explicitly accept or deny** random infestation
- Prompt before infestation becomes official
- Protects player agency for unexpected infestations
- Only applies to random spread (not deliberate !infest which requires normal consent)

### Multiple Infestations
- Someone **can have multiple parasites** simultaneously
- Different parasites can coexist

### Removal/Cure System

#### Requirements
- **Requires interaction from another player** (cannot self-cure)
- Possible integration with **!purify** (purification side of !corrupt)

#### Devoted !cure Command
- Separate **!cure** command for parasite removal
- Only effective when used by someone with:
  - Specific **training** levels
  - Specific **work experience** in relevant jobs
- Creates role for "healers" in the community
- Prevents trivial parasite removal

### Parasite Effects

#### Differential Effects (Ideal)
- **Different parasites have different effects**
- Challenge: How to respect agency of parasite creators?
- Want creators to have input on their parasite's effects
- TBD: Exact system for this

#### Interaction Text Changes
- Affects interaction text **only for infested person**
- Does NOT affect other party's text unless spreading to them
- Keeps effects contained to willing participants

#### Work System Integration
- May unlock **new !work routes** specific to infestation
- Parasites create unique opportunities
- Provides tangible benefit

#### Other Benefits
- System doesn't currently have room for other tangible benefits
- Mechanical benefits limited to work routes for now

### Visibility
- **Visible "infested" indicator**
- Shows which parasites someone has
- Public information in dossier or similar

### Implementation Questions Still To Consider
- Predefined list vs player-named parasites?
- Which interactions can randomly spread parasites?
- Exact random spread chance formula?
- How do players define effects for their custom parasites?
- Exact prompt text for accepting/denying random infestation?
- Can !cure remove all parasites or just one at a time?
- What training/job levels allow effective !cure usage?
- Is there a cost to use !cure?
- Does !cure have a cooldown?
- Should there be different removal difficulties for different parasites?

---

## !odorize {recipient} {scent}
**Category:** Consequence Interactions (WIP)  
**Description:** Make someone completely smell, so much it gets noticed when other interactions are happening

### Scent Selection
- {scent} from **identifier list**
- Can be **customized/named after the initiator**
- Creates personalized scent signatures

### Scent Quality
- **Not overtly repugnant** (within acceptable RP bounds)
- Some scents are **not traditionally pleasant**
- Range from pleasant to unusual/musky/earthy but not offensive

### Potency and Decay System
- Scents have a **chance to be noticed** in most interactions
- **Slowly decreases in potency** each time noticed
- Natural decay system prevents permanent overwhelming scents
- Must be **refreshed over time** to maintain potency
- Base implementation: only gradual removal

### Multiple Scents
- Someone can have **multiple scents simultaneously**
- **Multiple scents can be noticed** in same interaction
- Scents stack rather than replace
- Creates complex scent profiles

### Removal System

#### Base System (Initial Implementation)
- Scents **only wear off naturally** through decay
- No active removal at start

#### Future Considerations
- Possible **!bathe interaction** to remove scents faster
- Certain **job paths** might remove odors
- Some jobs might **add** offensive odors (miners, sewage workers, etc.)
- These are future enhancements, not initial implementation

### Monster Type Integration - Perfume Category

#### Perfume Quality
- Certain monster types have **'perfume' category** trait
- Two possible integration approaches (TBD):
  - **Option 1:** Only monsters with perfume can customize scent names
  - **Option 2:** Perfume monsters maintain scents longer/indefinitely

#### Advanced Monster Scent System
- Further **sub-categorize monsters** by:
  - Different types of perfumes/odors they produce
  - Different strengths of smell
- Creates monster-specific scent interactions
- Some monsters naturally more fragrant than others

### Interaction Integration
- Scent mentions appear in **most interactions** (high coverage)
- Chance-based system (not every single interaction)
- Adds flavor without being overwhelming

### Implementation Questions Still To Consider
- Exact potency decay rate per notice?
- Exact chance for scent to be noticed in interactions?
- Which interactions can notice scents?
- How is customization/naming done (UI/command)?
- Exact mechanics for perfume monster scent longevity?
- Which monsters have perfume category?
- What are the perfume subcategories?
- Should there be a maximum number of simultaneous scents?
- Is there a cost to !odorize someone?
- Does !odorize have a cooldown?
- How long until a scent fully decays naturally?

---

## !curse {recipient} {curse}
**Category:** Consequence Interactions (WIP)  
**Description:** Curse someone in a consequential way

### Curse Creation System
- **Ideally custom curses** created by players
- **Challenge:** Difficult to implement meaningfully with mechanical effects
- May need to use predefined identifier list instead
- TBD: Balance between customization and mechanical functionality

### Mechanical Effects
- Curses have **real mechanical implications** (not just flavor)
- Examples of curse effects:
  - **Chastity curse:** Changes !climax into denial text
  - **Interaction blocking:** Completely removes option to engage in specific interactions
  - When blocked interaction attempted: **Overt notification** explaining the curse prevents it

### Curse Stacking
- Curses **can stack**
- Multiple curses can affect one person simultaneously
- Creates complex cursed states

### Duration and Persistence
- Curses are **constant and persistent**
- Do NOT fade over time
- Must be **actively removed** through interaction
- Similar to parasite removal system

### Removal System
- Requires **interaction from another player**
- Cannot self-remove curses
- Similar mechanics to !cure for parasites
- May integrate with purification system

### Access System - Multiple Factors

#### Corruption/Purification Based Access
- Available curses determined by **corruption level**
- Negative corruption (purity) = access to "blessings"
- Positive corruption = access to "curses"
- Same mechanical effects, different flavor

#### Monster Type Access
- Certain **monster types** can access specific curses **for free**
- Bypasses corruption requirements for thematic curses
- Creates monster-specific curse abilities

### Blessings vs Curses - Unified System
- **Mechanically identical** in function
- **Flavored differently** based on caster's corruption value:
  - Negative corruption (pure) → "Blessing" flavor
  - Positive corruption (corrupt) → "Curse" flavor
- Same effects, different narrative framing
- Example: Chastity as blessing (purity) vs chastity as curse (corruption)

### Implementation Questions Still To Consider
- Can custom curses be implemented meaningfully?
- What predefined curses/blessings should exist?
- Exact corruption thresholds for curse/blessing access?
- Which monster types get which free curses?
- How does curse removal work (separate command, purify, cure)?
- Who can remove curses (anyone, trained people, specific corruption levels)?
- Is there a cost to curse someone?
- Does !curse have a cooldown?
- Should curses have escalating effects over time, or stay constant?
- Can the same curse be applied multiple times for stacking intensity?
- How are active curses displayed in dossier?

---

## !break {recipient} {body/mind/hole}
**Category:** Consequence Interactions (WIP)  
**Description:** Break a part of someone so thoroughly, that part can't be used for awhile

### Break Categories
- **body** - physical body broken
- **mind** - mental/psychological breaking
- **hole** - specific orifice/hole broken
- These are the three main categories

### Duration System - Variable Based on History
- Duration varies based on:
  - **How many times** that part has been broken before
  - **How recently** it was broken
- Repeated breaking = longer recovery time
- Recent breaks = longer recovery on re-break
- **Maximum duration: 1 week**
- Design rationale: Don't want to lock players out too long

### Mechanical Effects
- Being broken **completely changes** interaction outcomes
- Affected interactions include:
  - **!breed** - cannot breed with broken parts
  - **!climax** - cannot climax with broken parts
  - Other interactions using that body part
- Similar to curse system - **overt notification** when attempted
- Explains why the interaction cannot proceed normally

### Multiple Breaks
- **Multiple parts can be broken simultaneously**
- Can have body + mind + hole all broken at once
- Creates severely debilitated state

### Healing/Recovery System

#### Passive Recovery
- **Time heals all wounds**
- Parts automatically recover after duration expires
- Natural healing is guaranteed

#### Active Recovery
- **Interactions with certain players** can heal breaks
- Similar to parasite/curse removal systems
- Requires another player's help
- May require specific training/skills

### Progression System
- **Start with only fully broken state**
- No stages initially (not damaged/broken/shattered)
- Design rationale: Stages are extra writing for limited benefit
- Could add stages later if desired

### Work System Integration
- Breaking can **affect work options**
- Some jobs **prevented entirely** when broken
- Depends on which part is broken and what job requires

### Consent and Warnings
- **Heavy consequence interaction**
- As with all Consequence tier interactions:
  - **Clear warning** before !consent
  - **Explicit explanation** of what breaking means
  - Player must be fully informed before accepting

### Implementation Questions Still To Consider
- Exact duration formula based on break history?
- Base duration for first break of each part?
- How much does duration increase per repeated break?
- How much does recency affect duration?
- Which interactions are affected by which break types?
- Who can heal breaks (anyone, trained healers, specific corruption levels)?
- Is there a separate !heal or !mend command?
- Is there a cost to heal someone?
- Does healing have a cooldown?
- Which jobs are prevented by which break types?
- Should there be a visible "broken" indicator in dossier?
- Can a part be broken while still recovering from previous break?

---

## !dose {recipient} {vice}
**Category:** Consequence Interactions (WIP)  
**Description:** Dose someone with an addictive substance. Will add text about cravings and their satisfaction

### Vice System
- {vice} is an **identifier category**
- Many substances and smells fall under 'vice' category
- Draws from existing substance/scent identifiers

### Craving Mechanics
- Cravings triggered based on **number of interactions**
- Similar to how !odorize potency works
- Interaction-count based rather than time-based
- Creates dynamic craving system

### Addiction Progression
- Addiction **gets stronger with more doses**
- Progressive system that escalates
- Not binary - multiple addiction levels

### Multiple Vices
- Can be addicted to **multiple vices simultaneously**
- Different vices tracked separately
- Creates complex addiction profiles

### Craving Resolution - Two Outcomes

#### Forcibly Satisfied
- Craving is **forcibly satisfied** during interaction
- Makes addiction **worse**
- Increases addiction level/intensity
- Reinforces the vice

#### Successfully Resisted
- Player **resists the craving**
- Makes addiction **less intense**
- Decreases addiction level
- Path to recovery

### Progression Weighting
- System **weighted toward getting worse**
- More likely to satisfy than resist
- **Exception: Very first stages** may be easier to resist
- Creates realistic addiction difficulty
- Recovery is possible but challenging

### Craving Text Effects
- **Frequency** of craving text based on addiction level
- **Intensity** of craving text based on addiction level
- Higher addiction = more frequent and intense mentions
- Scales with addiction severity

### Work System Integration - Forced Jobs
- **Strong enough vice** forces specific job choices
- Overrides player's normal job selection
- **Negates choice** and explains why
- Jobs that would satisfy the vice
- Example: Drug addiction forces dealer/user jobs

### Consent Philosophy
- **Surface level:** Appears to violate consent by forcing jobs
- **Actual implementation:** Players opt in with full agency
- Consent given when **initially accepting the dose**
- Clear warning that vices can force behaviors
- Players choose to enter addiction system knowingly
- All consequences flow from that initial informed choice

### Implementation Questions Still To Consider
- Exact number of interactions between craving triggers?
- How many addiction levels/stages exist?
- Exact weighting for satisfy vs resist chances?
- What determines if a craving is satisfied or resisted (random, player choice, interaction type)?
- Which jobs are associated with which vices?
- At what addiction level do forced jobs begin?
- Can addictions be fully cured, or only reduced?
- Is there a separate command to actively try to resist/overcome addiction?
- Do any trainings or skills help resist cravings?
- Should there be a visible addiction indicator in dossier?
- How does initial !dose consent warning explain the system?
- Can someone dose themselves (relapse)?

---

## Document Status
**Last Updated:** November 4, 2025  
**Status:** Complete initial brainstorming session
**Progress:** All WIP features documented with design decisions

### Features Documented:
**Involved Interactions:**
- !climaxfor / !receiveclimaxfrom
- !milk
- !pledge

**Commitment Interactions:**
- !entitle
- !breed (with !birth)
- !train
- !corrupt / !purify

**Consequence Interactions:**
- !infest (with !cure)
- !odorize
- !curse
- !break
- !dose

### Next Steps:
- Review and refine design decisions
- Prioritize features for implementation
- Create detailed technical specifications
- Begin identifier category curation
- Define exact formulas and mechanics
- Implement features one at a time
