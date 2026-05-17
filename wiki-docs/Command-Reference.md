# Command Reference

Complete reference of all FCDiceBot commands organized by category.

**Note:** Default command prefix is `!` but can be configured per-channel.

## Legend

- `{required}` - Required parameter
- `[optional]` - Optional parameter
- `[user]Name[/user]` - User reference (F-List BBCode)
- **Admin** - Requires bot admin privileges
- **Op** - Requires channel op privileges

## General Commands

### Registration

#### !register
Register for Chateau features and receive starting chips.

**Usage:** `!register`

**Example:**
```
!register
→ Welcome to Chateau Contract! You received 1,000 starting chips.
```

**Effects:**
- Creates profile in database
- Grants starting chips (configurable per channel)
- Enables all Chateau interactions

## Dice Rolling

### Basic Rolling

#### !roll
Roll dice with complex expressions.

**Usage:** `!roll {expression}`

**Examples:**
```
!roll 1d20
!roll 3d6+5
!roll 2d10+1d6
!roll 4d6>4
```

**Operators:**
- `+` - Add
- `-` - Subtract
- `*` - Multiply
- `/` - Divide
- `>` - Count successes (>= value)
- `<` - Count failures (< value)

**Limits:**
- Max 200 dice
- Max 10M sides
- Max 400 total rolls

#### !fitd
Forged in the Dark dice pool system.

**Usage:** `!fitd {number}`

**Example:**
```
!fitd 3
→ Rolled [2, 5, 6]. Highest: 6 - Success!
```

#### !coinflip
Flip a coin.

**Usage:** `!coinflip` or `!flip`

**Example:**
```
!coinflip
→ Heads!
```

### Roll Tables

#### !addtable
Create a new roll table.

**Usage:** `!addtable {name}`

**Example:**
```
!addtable loot
→ Created table 'loot'
```

**Limit:** 4 tables per user

#### !addentry
Add entry to a roll table.

**Usage:** `!addentry {table} {weight} {result}`

**Example:**
```
!addentry loot 50 Common Item
→ Added 'Common Item' with weight 50 to 'loot'
```

**Limit:** 50 entries per table

#### !rolltable
Roll on a table.

**Usage:** `!rolltable {name}`

**Example:**
```
!rolltable loot
→ Result: Rare Item
```

#### !showtable
View table contents.

**Usage:** `!showtable {name}`

#### !removetable
Delete a table.

**Usage:** `!removetable {name}`

#### !removeentry
Remove entry from table.

**Usage:** `!removeentry {table} {entry}`

#### !listtables
List all your tables.

**Usage:** `!listtables`

## Card System

### Drawing Cards

#### !drawcard
Draw cards from a deck.

**Usage:** `!drawcard {amount} {decktype}`

**Deck Types:**
- `playing` - Standard 52-card deck
- `tarot` - 78-card tarot deck
- `uno` - Uno deck
- `manythings` - Deck of Many Things
- `custom` - User-defined deck

**Example:**
```
!drawcard 5 playing
→ You drew: Ace of Spades, 7 of Hearts, King of Diamonds, 2 of Clubs, Queen of Spades
```

#### !showhand
Show your current hand.

**Usage:** `!showhand`

#### !playcard
Play a card from your hand.

**Usage:** `!playcard {card}`

**Example:**
```
!playcard Ace of Spades
→ You played: Ace of Spades
```

#### !discardcard
Discard a card.

**Usage:** `!discardcard {card}`

#### !hidecard
Move card to hidden play area.

**Usage:** `!hidecard {card}`

#### !burncard
Remove card from game permanently.

**Usage:** `!burncard {card}`

### Deck Management

#### !shuffledeck
Shuffle a deck.

**Usage:** `!shuffledeck {decktype}`

#### !resetdeck
Return all cards to deck and shuffle.

**Usage:** `!resetdeck {decktype}`

**Example:**
```
!resetdeck playing
→ All cards returned to deck and shuffled!
```

#### !deckstatus
View deck status.

**Usage:** `!deckstatus {decktype}`

#### !viewdiscard
View discard pile.

**Usage:** `!viewdiscard {decktype}`

### Custom Decks

#### !createcustom
Create a custom deck.

**Usage:** `!createcustom {name}`

#### !addcustomcard
Add card to custom deck.

**Usage:** `!addcustomcard {deckname} {cardname}`

**Limit:** 200 cards per deck

### Dealer Commands

#### !dealercard
Draw cards to dealer's hand.

**Usage:** `!dealercard {amount} {decktype}`

#### !dealerplay
Dealer plays a card.

**Usage:** `!dealerplay {card}`

## Casino & Chips

### Chip Management

#### !showchips
View chip balances.

**Usage:** `!showchips` or `!chips`

**Example:**
```
!showchips
→ Chip Balances:
  Alice: 1,500
  Bob: 2,300
```

#### !givechips
Transfer chips to another user.

**Usage:** `!givechips [user]Name[/user] {amount}`

**Example:**
```
!givechips [user]Bob[/user] 500
→ You gave Bob 500 chips. New balance: 1,000
```

### Betting

#### !bet
Add chips to the pot.

**Usage:** `!bet {amount}`

**Example:**
```
!bet 100
→ Alice bets 100 chips. Pot: 100
```

#### !claimpot
Claim the pot.

**Usage:** `!claimpot`

**Requires:** Channel op or mutual agreement

#### !showpot
View current pot.

**Usage:** `!showpot` or `!pot`

### VelvetCuff Integration

#### !buychips
Purchase chips with real currency (VelvetCuff).

**Usage:** `!buychips {amount}`

**Example:**
```
!buychips 5000
→ Payment request created. Complete payment at: [URL]
```

**Limit:** Max 50,000 chips per transaction

#### !cashout
Convert chips to VelvetCuff currency.

**Usage:** `!cashout {amount}`

**Limits:**
- Min: 1,000 chips
- Max: 100,000 chips
- Cooldown: 72 hours

**Example:**
```
!cashout 10000
→ You cashed out 10,000 chips for 100 VelvetCuff tokens!
```

### Coupons

#### !createcoupon (Admin)
Create chip coupon.

**Usage:** `!createcoupon {code} {amount}`

**Example:**
```
!createcoupon WELCOME2024 1000
→ Coupon created: WELCOME2024 for 1,000 chips
```

#### !redeem
Redeem a chip coupon.

**Usage:** `!redeem {code}`

**Example:**
```
!redeem WELCOME2024
→ You redeemed WELCOME2024 for 1,000 chips!
```

### Slots

#### !slots
Play slot machine.

**Usage:** `!slots {bet}`

**Example:**
```
!slots 100
→ [Cherry] [Cherry] [Cherry]
→ You won 300 chips! (3x multiplier)
```

**Cooldown:** 5 minutes

#### !slotsinfo
View slot configuration.

**Usage:** `!slotsinfo`

### Admin Commands

#### !clearchips (Admin)
Reset all chip balances to starting amount.

**Usage:** `!clearchips`

## Games

### Game Management

#### !joingame
Join or create a game session.

**Usage:** `!joingame {gametype}`

**Game Types:**
- `highroll` - Highest roll wins
- `poker` - Texas Hold'em
- `blackjack` - Classic 21
- `roulette` - Casino roulette
- `bottlespin` - Spin the bottle
- `kingsgame` - King's game
- `liarsdice` - Liar's dice
- `rps` - Rock Paper Scissors
- `slamroll` - Competitive rolling
- `pokergame` - Alternative poker

**Example:**
```
!joingame poker
→ Alice created a poker game! Others can !joingame poker
```

#### !startgame
Start a game session.

**Usage:** `!startgame {gametype}`

**Example:**
```
!startgame poker
→ Poker game started! Players: Alice, Bob, Carol
```

#### !gamecommand
Execute game-specific action.

**Usage:** `!gamecommand {gametype} {action} [parameters]`

**Examples:**
```
!gamecommand poker bet 100
!gamecommand blackjack hit
!gamecommand roulette bet red 100
!gamecommand rps rock
```

#### !gamestatus
View game status.

**Usage:** `!gamestatus {gametype}`

#### !leavegame
Leave a game session.

**Usage:** `!leavegame {gametype}`

#### !cancelgame (Op)
Cancel a game session.

**Usage:** `!cancelgame {gametype}`

## Chateau Interactions

### Consent Commands

#### !consent
Accept a pending interaction.

**Usage:** `!consent` or `!accept`

**Example:**
```
!consent
→ Mwah! Alice and Bob share a kiss, cute.
```

#### !reject
Reject a pending interaction.

**Usage:** `!reject` or `!deny`

**Example:**
```
!reject
→ Bob rejected the interaction.
```

### Casual Interactions (1 hour cooldown)

#### !kiss
Share a kiss.

**Usage:** `!kiss [user]Name[/user]`

**Example:**
```
!kiss [user]Bob[/user]
→ Alice wants to give Bob a smooch! Do you !consent?
```

#### !cuddle
Cuddle together.

**Usage:** `!cuddle [user]Name[/user]`

#### !handhold
Hold hands.

**Usage:** `!handhold [user]Name[/user]`

#### !spank
Playful spanking.

**Usage:** `!spank [user]Name[/user]`

#### !bully
Playful teasing.

**Usage:** `!bully [user]Name[/user]`

### Involved Interactions (30 min cooldown)

#### !feed
Feed something to someone.

**Usage:** `!feed [user]Name[/user] {item}`

**Example:**
```
!feed [user]Bob[/user] chocolate
→ Alice wants to feed Bob chocolate! Do you !consent?
```

#### !dressup
Dress someone in attire.

**Usage:** `!dressup [user]Name[/user] {attire}`

**Example:**
```
!dressup [user]Bob[/user] maid outfit
```

#### !golden
Golden shower interaction.

**Usage:** `!golden [user]Name[/user]`

#### !payment
Give/receive payment.

**Usage:** `!payment [user]Name[/user] {amount}`

**Example:**
```
!payment [user]Bob[/user] 50
→ Alice wants to pay Bob 50 tokens! Do you !consent?
```

### Commitment Interactions (24 hour cooldown)

#### !mark
Place ownership mark.

**Usage:** `!mark [user]Name[/user] {bodypart}`

**Example:**
```
!mark [user]Bob[/user] collar
→ Alice wants to mark Bob's collar! Do you !consent?
```

**Effect:** Adds mark to recipient's profile

#### !unmark
Remove a mark.

**Usage:** `!unmark [user]Name[/user]`

#### !entitle
Grant a title.

**Usage:** `!entitle [user]Name[/user] "{title}"`

**Example:**
```
!entitle [user]Bob[/user] "Best Friend"
→ Alice wants to grant Bob the title 'Best Friend'! Do you !consent?
```

#### !bond
Create relationship bond.

**Usage:** `!bond [user]Name[/user] {bondtype}`

**Bond Types:** pet, slave, master, owner, servant, etc.

**Example:**
```
!bond [user]Bob[/user] pet
```

#### !employ
Hire someone for a job.

**Usage:** `!employ [user]Name[/user] {job}`

**Example:**
```
!employ [user]Bob[/user] maid
→ Alice wants to employ Bob as a maid! Do you !consent?
```

**Effect:** Sets job, enables !work command

### Consequence Interactions (24 hour cooldown)

#### !rename
Change someone's display name.

**Usage:** `!rename [user]Name[/user] "{newname}"`

**Example:**
```
!rename [user]Bob[/user] "Bobert the Great"
→ Warning: This will change their display name. Do you !consent?
```

**Effect:** Permanent until renamed again

#### !monsterize
Transform into a monster/species.

**Usage:** `!monsterize [user]Name[/user] {species}`

**Example:**
```
!monsterize [user]Bob[/user] dragon
```

#### !petrify
Turn to stone.

**Usage:** `!petrify [user]Name[/user]`

**Effect:** Status: petrified

#### !plant
Transform into a plant.

**Usage:** `!plant [user]Name[/user] {planttype}`

**Effect:** Status: plant

#### !objectify
Transform into an object.

**Usage:** `!objectify [user]Name[/user] {object}`

**Effect:** Status: object

#### !consume
Consume/absorb someone.

**Usage:** `!consume [user]Name[/user]`

**Effect:** Status: consumed (temporary removal)

#### !restore
Reverse transformations.

**Usage:** `!restore [user]Name[/user]`

**Example:**
```
!restore [user]Bob[/user]
→ Alice wants to restore Bob! Do you !consent?
```

### Job System

#### !work
Perform daily duty (if employed).

**Usage:** `!work`

**Example:**
```
!work
→ You perform your duty as a maid: Clean the manor
→ You earned 50 tokens!
```

**Cooldown:** 24 hours

#### !volunteer
Try other jobs without being employed.

**Usage:** `!volunteer`

**Example:**
```
!volunteer
→ You try your hand at being a chef: Prepare a meal
→ You earned 10 tokens!
```

**Note:** Volunteers earn less than employees

### Statistics & Profiles

#### !stats
View statistics.

**Usage:** `!stats` or `!stats [user]Name[/user]`

**Example:**
```
!stats
→ Statistics for Alice:
  Kisses: 42 (25 given, 17 received)
  Cuddles: 15 (8 given, 7 received)
  Total interactions: 60
```

#### !dossier
View interaction history.

**Usage:** `!dossier [user]Name[/user]`

**Example:**
```
!dossier [user]Alice[/user]
→ Dossier for Alice:
  - Kissed Bob (5 times)
  - Employed by Carol (maid)
  - Transformed into dragon by Dave
```

#### !profile
View detailed profile.

**Usage:** `!profile` or `!profile [user]Name[/user]`

**Shows:**
- Display name
- Titles
- Marks
- Job and employer
- Species/transformations
- Bonds
- Currencies

### Title System

#### !showtitles
View all your titles.

**Usage:** `!showtitles`

#### !settitle
Display a title in a slot.

**Usage:** `!settitle {slot} {title}`

**Slots:** 1-9

**Example:**
```
!settitle 1 ·First Kiss·
→ Title slot 1 set to: ·First Kiss·
```

#### !cleartitle
Clear a title slot.

**Usage:** `!cleartitle {slot}`

## Potion System

#### !generatepotion
Generate a random potion.

**Usage:** `!generatepotion`

**Example:**
```
!generatepotion
→ You brewed: Shimmering Elixir of Swiftness
→ Effect: Grants incredible speed for 1 hour
```

#### !savepotion
Save the last generated potion.

**Usage:** `!savepotion {name}`

**Limit:** 6 saved potions per user

#### !showpotion
View a saved potion.

**Usage:** `!showpotion {name}`

#### !listpotions
List all your saved potions.

**Usage:** `!listpotions`

## Bot Administration

### Channel Management

#### !joinchannel (Admin)
Join a channel.

**Usage:** `!joinchannel {channelid}`

**Example:**
```
!joinchannel adh-example
→ Joining channel adh-example...
```

#### !leavechannel (Admin)
Leave a channel.

**Usage:** `!leavechannel {channelid}`

### Bot Control

#### !shutdown (Admin)
Shut down the bot.

**Usage:** `!shutdown`

#### !setstatus (Admin)
Set bot status.

**Usage:** `!setstatus {status} {message}`

**Status Values:**
- `online`
- `busy`
- `dnd`
- `away`
- `looking`

**Example:**
```
!setstatus online "FCDiceBot ready!"
```

### Data Management

#### !clearchips (Admin)
Reset all chip balances.

**Usage:** `!clearchips`

#### !backup (Admin)
Trigger manual backup.

**Usage:** `!backup`

**Note:** May not be implemented in all versions

## Command Aliases

Many commands have shorter aliases:

| Full Command | Alias |
|-------------|-------|
| `!showchips` | `!chips` |
| `!showpot` | `!pot` |
| `!coinflip` | `!flip` |
| `!consent` | `!accept` |
| `!reject` | `!deny` |

## Tips

### User References

Always use F-List BBCode format for user references:

```
[user]CharacterName[/user]
```

The bot automatically parses these tags.

### Quoting

Use quotes for multi-word parameters:

```
!rename [user]Bob[/user] "Bobert the Great"
!entitle [user]Bob[/user] "Best Friend Forever"
```

### Case Sensitivity

Commands are **case-insensitive**:

```
!KISS [user]Bob[/user]    ← Works
!Kiss [user]Bob[/user]    ← Works
!kiss [user]Bob[/user]    ← Works
```

User names are **case-sensitive** (as per F-List):

```
!kiss [user]Bob[/user]    ← Correct
!kiss [user]bob[/user]    ← May not work if character is "Bob"
```

### Cooldowns

If you see "try again in X minutes", you've hit a rate limit:

- Casual interactions: 1 hour
- Involved interactions: 30 minutes
- Commitment/Consequence: 24 hours
- Slots: 5 minutes
- Cashout: 72 hours

### Permissions

Some commands require special permissions:

- **Bot Admin:** Defined in account_settings.txt
- **Channel Op:** F-List channel operators
- **Registered:** Must use `!register` first

## See Also

- [Interaction System](Interaction-System) - Detailed interaction mechanics
- [Dice and Games](Dice-and-Games) - Game rules and dice systems
- [Installation and Setup](Installation-and-Setup) - Channel configuration
