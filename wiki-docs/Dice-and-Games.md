# Dice and Games

FCDiceBot provides extensive dice rolling and game features for tabletop gaming and casual play. This is the **legacy dicebot** half of the codebase (`FChatDicebot/DiceFunctions/`): stable, rarely changed.

Exact command syntax is available in-app via `!dicehelp`, `!gamehelp {game}`, and `!help {command}` for the commands that carry help metadata. When this page and the bot disagree, the bot is right — fix this page.

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
!roll 1d20+1d6+5   → Multiple dice groups in one expression
!roll 2d20>15      → Count successes (rolls >= 15)
!roll 3d6<4        → Count failures (rolls < 4)
```

### Limits

Safety limits are constants on `DiceFunctions/DiceBot.cs`:

- **Maximum dice:** 200 per roll (`MaximumDice`)
- **Maximum sides:** 10,000,000 (`MaximumSides`)
- **Maximum total rolls:** 400 across the expression (`MaximumRolls`)

### Other dice commands

| Command | What it does |
|---------|--------------|
| `!fitd {number of dice}` | Forged in the Dark style d6 pool (highest die decides the outcome) |
| `!coinflip` | Heads or tails |
| `!tipdie {from}>{to}` | Adjust one die from the last roll |
| `!showlastroll` | Redisplay the last roll (`!showlastroll sort` to sort) |
| `!unlockdice` / `!unlockdiceall` | Unlock cosmetic dice |
| `!luckforecast` | Novelty luck reading (costs chips) |

## Roll Tables

User-created random tables, rolled with `!rolltable {table name}`.

- Table entries are keyed by **roll number with an optional range** (an entry can cover e.g. rolls 5–8); the table rolls across the entries' combined range and prints the matching entry.
- Entries can **trigger secondary rolls** on other tables, capped at `MaximumSecondaryTableRolls` (3). `!rolltable {name} nosecondary` suppresses them; `nolabel` hides the roll label.
- Tables are authored as JSON with `!savetable`, or in a simplified format with `!savetablesimple` — `!dicehelp` documents the formats. Inspect with `!showtables`, `!mytables`, `!tableinfo`, `!tablejson`; remove with `!deletetable`.
- Saved per user in the legacy JSON storage.

## Card Deck System

### Deck Types

`DeckType` in `DiceFunctions/Deck.cs`: Playing, Tarot, ManyThings (Deck of Many Things), Uno, BreakerRumble (+ Extra / Classic variants), Skipbo, and Custom (user-saved).

### Commands

Per-channel deck state, with hands, an in-play area, a hidden-play area, and a discard pile:

- **Draw/play flow:** `!drawcard`, `!showhand`, `!playcard`, `!discardcard`, `!hidecard` (hidden play), `!movecard`, `!revealcard`, `!playfromdiscard`, `!takefromdiscard`, `!takefromplay`, `!discardfromplay`, `!endhand`
- **Deck management:** `!shuffledeck`, `!shufflediscardintodeck`, `!resetdeck`, `!deckinfo`, `!decklist`, `!deckjson`, `!showdecks`, `!showcardpiles`, `!cardinfo`
- **Custom decks:** `!savecustomdeck` (JSON), `!savecustomdecksimple`, `!mydecks`, `!deletecustomdeck` — up to `MaximumCardsInDeck` (200) cards

## Casino Chip System

Chips are per-channel play money (channels can toggle them; see channel settings).

- `!register` — create your chip pile in this channel (grants the channel's starting chips)
- `!showchips` — view balances; `!givechips [user]Name[/user] {amount}` — transfer
- `!bet {amount}` — add chips to the pot; `!claimpot` — claim it
- **VelvetCuff integration:** `!buychips` (max `MaximumChipBuySize` 50,000 per order), `!cancelbuychips`, `!cashout` (min/max enforced; max `MaximumChipCashoutSize` 100,000), `!itembuy`. Orders are polled against the VelvetCuff API until paid or expired.
- **Chip codes:** `!generatechipscode` (admin) creates a redeemable code; `!redeemchips` / `!addchipscode` redeem it
- **Slots:** `!slots`, `!slotsinfo` — symbols, weights, multipliers, cooldown, and max multiplier are per-channel settings; jackpot state persists in Mongo (`SlotsJackpots`)
- **Admin pile management:** `!addchips`, `!removechips`, `!takechips`, `!forcegivechips`, `!removepile`, `!removeallpiles`, `!removeallchipsoveramount`, `!restrictchips`

## Game System

### Available Games

One `IGame` implementation per game in `DiceFunctions/Games/`:

AlphaRoyale, Blackjack, BottleSpin, Chess, DungeonDelve, HighRoll, KingsGame, LiarsDice, Mafia, Poker, PokerGame, PrizeRoll, RockPaperScissors, Roulette, SlamRoll

`!showgames` lists them live; `!gamehelp {game}` explains each game's commands and startup options.

### Game Flow

```
!joingame {game}                     ← join (or create) the channel's session
!joingame {game} {amount} {currency} ← optionally wager a Chateau currency
!startgame {game}                    ← start once enough players joined
!gamecommand {game} {command}        ← game-specific actions (!gc / !g for short)
!gamestatus {game}                   ← where things stand
!leavegame / !cancelgame             ← leave / cancel
```

Rock-Paper-Scissors has dedicated play commands: `!rock`, `!paper`, `!scissors`, `!lizard`, `!spock`. Chess positions can be displayed from FEN with `!fen`.

**Currency wagers:** games can be wagered in any Chateau currency (no exchange rates — the wager is consent-negotiated at `!joingame` time); settlement uses the atomic `changeCurrency` path.

Ops can manage sessions with `!addtogame` / `!removefromgame`; `!gamesessions` lists active sessions.

## Potion System

Novelty potion generation and use: `!generatepotion`, `!savepotion`, `!showpotion`, `!showpotions`, `!showpotionmenu`, `!showpotionprices`, `!revealpotion`, `!deletepotion`, `!droppotion`, `!usepower` (+ variants). Saved per user in legacy JSON storage.

## See Also

- [Command Reference](Command-Reference) - Full command index
- [Architecture](Architecture) - Where the dicebot sits in the codebase
- [Database and Persistence](Database-and-Persistence) - Legacy JSON storage details
