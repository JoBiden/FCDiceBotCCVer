# Dice and Games

FCDiceBot provides extensive dice rolling and game features for tabletop gaming and casual play.

## Dice Rolling System

### Basic Dice Rolls

**Command:** `!roll {expression}`

**Examples:**

```
!roll 1d20         → Roll 1 twenty-sided die
!roll 3d6          → Roll 3 six-sided dice
!roll 2d10+5       → Roll 2d10 and add 5
!roll 1d20-2       → Roll 1d20 and subtract 2
!roll 4d6*2        → Roll 4d6 and multiply by 2
!roll 2d8/2        → Roll 2d8 and divide by 2
```

### Complex Expressions

**Supports Multiple Operations:**

```
!roll 1d20+1d6+5    → Roll 1d20, add 1d6, add 5
!roll 2d20>15       → Roll 2d20, count successes (rolls >= 15)
!roll 3d6<4         → Roll 3d6, count failures (rolls < 4)
```

**Operators:**
- `+` - Addition
- `-` - Subtraction
- `*` - Multiplication
- `/` - Division
- `>` - Count successes (rolls greater than or equal to value)
- `<` - Count failures (rolls less than value)

### Limitations

**Safety Limits:**
- **Maximum Dice:** 200 per roll
- **Maximum Sides:** 10,000,000
- **Maximum Total Rolls:** 400 (across all dice in expression)

**Why:** Prevents server overload from malicious or accidental excessive rolls

**Examples of Limits:**

```
!roll 201d6         → Error: Too many dice (max 200)
!roll 3d10000001    → Error: Too many sides (max 10,000,000)
!roll 50d20+50d20+50d20+50d20+50d20+50d20+50d20+50d20+50d20
                    → Error: Too many total rolls (max 400)
```

### Special Roll Types

#### Forged in the Dark (FitD)

**Command:** `!fitd {number}`

Rolls a pool of d6 dice (Blades in the Dark / Forged in the Dark system):

- Takes the **highest** roll
- 1-3: Failure
- 4-5: Partial Success
- 6: Success
- Multiple 6s: Critical Success

**Example:**

```
!fitd 3
→ Rolled 3d6: [2, 5, 6]
→ Highest: 6 - Success!
```

#### Coin Flip

**Command:** `!coinflip` or `!flip`

Simple heads or tails:

```
!coinflip
→ Heads!
```

### Roll Tables

Create custom weighted random tables.

#### Creating a Table

**Command:** `!addtable {name}`

Then add entries:

**Command:** `!addentry {table} {weight} {result}`

**Example:**

```
!addtable loot
!addentry loot 50 Common Item
!addentry loot 30 Uncommon Item
!addentry loot 15 Rare Item
!addentry loot 5 Legendary Item
```

**Weights:** Higher weight = more likely

#### Rolling on a Table

**Command:** `!rolltable {name}`

```
!rolltable loot
→ Result: Uncommon Item
```

**How It Works:**
1. Sum all weights (50+30+15+5 = 100)
2. Roll 1d100
3. Map result to weighted ranges:
   - 1-50: Common Item
   - 51-80: Uncommon Item
   - 81-95: Rare Item
   - 96-100: Legendary Item

#### Table Management

**Commands:**
- `!showtable {name}` - View table entries and weights
- `!removetable {name}` - Delete a table
- `!removeentry {table} {entry}` - Remove specific entry
- `!listtables` - Show all your tables

**Limits:**
- **4 tables per user**
- **50 entries per table**

#### Nested Tables

Tables can reference other tables:

```
!addtable encounters
!addentry encounters 40 goblin
!addentry encounters 30 orc
!addentry encounters 20 table:loot
!addentry encounters 10 dragon
```

When "table:loot" is rolled, it rolls on the loot table.

## Card Deck System

### Deck Types

| Type | Description | Cards |
|------|-------------|-------|
| `playing` | Standard 52-card deck | A-K in 4 suits |
| `tarot` | Tarot deck | 78 cards (Major + Minor Arcana) |
| `uno` | Uno deck | Uno cards |
| `manythings` | Deck of Many Things | Custom magical deck |
| `custom` | User-defined | Up to 200 cards |

### Basic Card Commands

#### Draw Cards

**Command:** `!drawcard {amount} {decktype}`

```
!drawcard 5 playing
→ You drew: Ace of Spades, 7 of Hearts, King of Diamonds, 2 of Clubs, Queen of Spades
```

**Note:** Cards go to your private hand

#### Show Hand

**Command:** `!showhand`

```
!showhand
→ Your hand: Ace of Spades, 7 of Hearts, King of Diamonds, 2 of Clubs, Queen of Spades
```

#### Play Card

**Command:** `!playcard {card}`

Moves card from hand to "in play" (visible to all):

```
!playcard Ace of Spades
→ You played: Ace of Spades
```

#### Discard Card

**Command:** `!discardcard {card}`

Moves card to discard pile:

```
!discardcard 2 of Clubs
→ You discarded: 2 of Clubs
```

### Deck Management

#### Shuffle Deck

**Command:** `!shuffledeck {decktype}`

```
!shuffledeck playing
→ The playing deck has been shuffled!
```

#### Reset Deck

**Command:** `!resetdeck {decktype}`

Returns all cards (from all players' hands, play, and discard) to the deck and shuffles:

```
!resetdeck playing
→ All cards returned to deck and shuffled!
```

#### View Deck Status

**Command:** `!deckstatus {decktype}`

```
!deckstatus playing
→ Playing Deck: 42 cards remaining, 10 in play, 0 in discard
```

### Card Collections

Each deck has multiple collections:

| Collection | Description | Visibility |
|------------|-------------|------------|
| Deck | Draw pile | Hidden |
| Hand | Player's hand | Private |
| In Play | Active cards | Public |
| Hidden In Play | Private play area | Private |
| Discard | Discard pile | Public |
| Burn | Removed from game | Hidden |

**Commands:**
- `!hidecard {card}` - Move to Hidden In Play
- `!burncard {card}` - Remove from game permanently
- `!viewdiscard` - See discard pile
- `!viewburn` - See burned cards (admin only)

### Custom Decks

Create your own deck:

**Command:** `!createcustom {name}`

Then add cards:

**Command:** `!addcustomcard {deckname} {cardname}`

**Example:**

```
!createcustom myddeck
!addcustomcard myddeck "Magic Sword"
!addcustomcard myddeck "Healing Potion"
!addcustomcard myddeck "Curse"
```

**Limits:**
- **200 cards per custom deck**
- **Deck name must be unique per channel**

### Dealer System

Special "the_dealer" player for house/GM cards:

**Command:** `!dealercard {amount} {decktype}`

Cards go to dealer's hand instead of player's hand.

**Command:** `!dealerplay {card}`

Dealer plays a card (visible to all).

## Casino Chip System

### Registration

**Command:** `!register`

Creates your profile and grants starting chips (default: 1000, configurable per channel).

### Chip Commands

#### View Chips

**Command:** `!showchips` or `!chips`

```
!showchips
→ Chip Balances:
  Alice: 1,500
  Bob: 2,300
  Carol: 800
```

#### Transfer Chips

**Command:** `!givechips [user]Name[/user] {amount}`

```
!givechips [user]Bob[/user] 500
→ You gave Bob 500 chips. New balance: 1,000
```

### Betting System

#### Place Bet

**Command:** `!bet {amount}`

Adds chips to the pot:

```
!bet 100
→ Alice bets 100 chips. Pot: 100
```

#### Claim Pot

**Command:** `!claimpot`

Claims the current pot (requires channel op or mutual agreement):

```
!claimpot
→ Alice claims the pot of 500 chips!
```

#### View Pot

**Command:** `!showpot` or `!pot`

```
!showpot
→ Current pot: 500 chips
```

### Buying Chips (VelvetCuff Integration)

**Command:** `!buychips {amount}`

Initiates a VelvetCuff payment transaction:

```
!buychips 5000
→ Payment request created. Please complete payment at: [VelvetCuff URL]
→ Transaction ID: abc123
```

**Process:**
1. Bot creates VelvetCuff transaction
2. User completes payment on VelvetCuff site
3. Bot polls transaction status every 30 seconds
4. On payment confirmed, chips are added
5. Confirmation message sent

**Limits:**
- **Minimum:** 1 chip
- **Maximum:** 50,000 chips per transaction

### Cashing Out

**Command:** `!cashout {amount}`

Convert chips to VelvetCuff currency:

```
!cashout 10000
→ You cashed out 10,000 chips for 100 VelvetCuff tokens!
```

**Limits:**
- **Minimum:** 1,000 chips
- **Maximum:** 100,000 chips
- **Cooldown:** 72 hours between cashouts

### Chip Coupons

**Admin Command:** `!createcoupon {code} {amount}`

Creates a redeemable coupon:

```
!createcoupon WELCOME2024 1000
→ Coupon created: WELCOME2024 for 1,000 chips
```

**User Command:** `!redeem {code}`

```
!redeem WELCOME2024
→ You redeemed WELCOME2024 for 1,000 chips!
```

**Note:** Coupons are one-time use

### Admin Commands

**Clear All Chips:** `!clearchips` (Bot admin only)

Resets all chip balances to starting amount.

## Game System

### Available Games

1. **HighRoll** - Highest roll wins
2. **Poker** - Texas Hold'em poker
3. **Blackjack** - Classic 21 game
4. **Roulette** - Casino roulette wheel
5. **BottleSpin** - Spin the bottle
6. **KingsGame** - King selects actions
7. **LiarsDice** - Hidden dice bidding
8. **RockPaperScissors** - RPS tournament
9. **SlamRoll** - Competitive dice rolling
10. **Pokergame** - Alternative poker variant

### Game Flow

#### 1. Create/Join Game

**Command:** `!joingame {gametype}`

First player creates the session, others join:

```
Alice: !joingame poker
→ Alice created a poker game! Others can !joingame poker

Bob: !joingame poker
→ Bob joined the poker game! Players: Alice, Bob
```

#### 2. Start Game

**Command:** `!startgame {gametype}`

```
Alice: !startgame poker
→ Poker game started! Players: Alice, Bob, Carol
```

#### 3. Game Actions

**Command:** `!gamecommand {gametype} {action} {parameters}`

Actions vary per game:

**Poker:**
```
!gamecommand poker bet 100
!gamecommand poker fold
!gamecommand poker call
!gamecommand poker raise 200
```

**Blackjack:**
```
!gamecommand blackjack hit
!gamecommand blackjack stand
!gamecommand blackjack double
```

**Roulette:**
```
!gamecommand roulette bet red 100
!gamecommand roulette bet 17 50
```

#### 4. Game Status

**Command:** `!gamestatus {gametype}`

```
!gamestatus poker
→ Poker Game Status:
  Players: Alice (1500 chips), Bob (2000 chips), Carol (1200 chips)
  Pot: 300 chips
  Current turn: Alice
```

#### 5. Leave Game

**Command:** `!leavegame {gametype}`

```
!leavegame poker
→ You left the poker game.
```

#### 6. Cancel Game

**Command:** `!cancelgame {gametype}` (Channel op or creator only)

```
!cancelgame poker
→ Poker game cancelled. Bets returned.
```

### Game Details

#### HighRoll

**Objective:** Roll highest

**Flow:**
1. Join game
2. Start game
3. `!gamecommand highroll roll` - Everyone rolls
4. Highest roll wins

**Default:** 1d100, configurable

#### Poker

**Objective:** Best poker hand wins

**Flow:**
1. Join game (2-8 players)
2. Start game - Deal hole cards
3. Betting round
4. Flop (3 community cards)
5. Betting round
6. Turn (1 community card)
7. Betting round
8. River (1 community card)
9. Final betting
10. Showdown - Best hand wins

**Hand Rankings:**
- Royal Flush
- Straight Flush
- Four of a Kind
- Full House
- Flush
- Straight
- Three of a Kind
- Two Pair
- Pair
- High Card

#### Blackjack

**Objective:** Get 21 or closest without going over

**Flow:**
1. Join game
2. Bet chips
3. Start game - Deal 2 cards to each player and dealer
4. Players take turns:
   - Hit (take card)
   - Stand (keep hand)
   - Double (double bet, take 1 card)
5. Dealer plays (hits until 17+)
6. Compare hands

**Payouts:**
- Blackjack (21 with 2 cards): 3:2
- Win: 1:1
- Push (tie): Return bet
- Lose: Lose bet

#### Roulette

**Objective:** Bet on where the ball lands

**Bets:**
- Number (0-36): 35:1
- Red/Black: 1:1
- Odd/Even: 1:1
- High (19-36)/Low (1-18): 1:1
- Dozens (1-12, 13-24, 25-36): 2:1
- Columns: 2:1

**Flow:**
1. Join game
2. Place bets: `!gamecommand roulette bet {type} {amount}`
3. Spin: `!gamecommand roulette spin`
4. Payouts calculated

#### Bottle Spin

**Objective:** Spin the bottle to select random player

**Flow:**
1. Join game (3+ players)
2. Start game
3. `!gamecommand bottlespin spin`
4. Random player selected

**Uses:** Truth or Dare, King's Game selection, etc.

#### King's Game

**Objective:** King gives orders

**Flow:**
1. Join game (4+ players recommended)
2. Start game
3. Draw cards - One player draws King
4. King issues command: `!gamecommand kingsgame order {command}`
5. Rotate to next round

#### Liar's Dice

**Objective:** Bluff about dice rolls

**Flow:**
1. Join game (2+ players)
2. Start game - Each player rolls hidden dice
3. First player bids: `!gamecommand liarsdice bid {quantity} {face}`
   - Example: "Three 5s" (claiming at least three dice showing 5)
4. Next player either:
   - Raises bid
   - Calls "liar"
5. If called, reveal dice:
   - Bid true: Caller loses a die
   - Bid false: Bidder loses a die
6. Last player with dice wins

#### Rock Paper Scissors

**Objective:** Classic RPS

**Flow:**
1. Join game (2 players)
2. Start game
3. Both choose: `!gamecommand rps {rock|paper|scissors}`
4. Reveal and determine winner

**Tournament Mode:**

Multiple players, elimination brackets:

```
!joingame rps_tournament
!startgame rps_tournament
```

### Game Sessions

**Per Channel, Per Game Type:**

Each channel can have one active session per game type:

- Channel #1: Poker game
- Channel #1: Blackjack game (simultaneous)
- Channel #2: Poker game (different session)

**Session State:**

Stored in `GameSessions` dictionary:

```csharp
GameSessions["channel"]["poker"] = new PokerSession {
    Players = ["Alice", "Bob", "Carol"],
    Pot = 500,
    CurrentTurn = "Alice",
    CommunityCards = ["Ace of Spades", "7 of Hearts", "King of Diamonds"],
    // ... game-specific state
};
```

## Slot Machine System

### Playing Slots

**Command:** `!slots {bet}`

```
!slots 100
→ [Cherry] [Cherry] [Cherry]
→ You won 300 chips! (3x multiplier)
```

### Slot Configuration

**Default Configurations:**
- **Fruits:** Classic fruit symbols
- **Bondage:** Themed symbols

**Admin Command:** `!setslots {config}`

### Symbol Sets

**Fruits Configuration:**
- Cherry: 3x
- Lemon: 5x
- Orange: 10x
- Plum: 15x
- Bell: 20x
- Seven: 50x

**Multipliers:** Based on matching symbols

**Channel Limits:**

Per-channel max multiplier setting prevents excessive payouts:

```json
{
  "slotsMaxMultiplier": 100
}
```

If a 500x win is rolled but max is 100x, payout is capped at 100x.

### Cooldown

**Default:** 5 minutes between spins per user

Prevents slot spam and chip inflation.

## Potion Generation

### Generate Random Potion

**Command:** `!generatepotion`

```
!generatepotion
→ You brewed: Shimmering Elixir of Swiftness
→ Effect: Grants incredible speed for 1 hour
```

**Components:**
- Base substance (liquid, powder, paste, etc.)
- Appearance (shimmering, murky, glowing, etc.)
- Effect type (strength, speed, healing, etc.)
- Duration
- Intensity

### Save Potion

**Command:** `!savepotion {name}`

After generating a potion, save it:

```
!savepotion swiftness
→ Potion saved as "swiftness"
```

**Limit:** 6 saved potions per user

### View Saved Potion

**Command:** `!showpotion {name}`

```
!showpotion swiftness
→ Shimmering Elixir of Swiftness
→ Effect: Grants incredible speed for 1 hour
```

### List Potions

**Command:** `!listpotions`

```
!listpotions
→ Your saved potions:
  1. swiftness
  2. healing
  3. strength
```

## See Also

- [Command Reference](Command-Reference) - Complete command list
- [Installation and Setup](Installation-and-Setup) - Channel configuration
- [Architecture](Architecture) - DiceBot implementation details
