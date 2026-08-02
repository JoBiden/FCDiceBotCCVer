# Command Reference

Reference of all FCDiceBot commands, organized by category.

**The bot itself is the authoritative reference:** `!help` lists every command with help metadata, `!help {command}` shows its usage, cooldown, and related commands — all generated from the command classes, so it can't drift. This page is the browsable index; when a command here disagrees with `!help`, trust `!help` and fix this page.

**Note:** Default command prefix is `!` but can be configured per-channel.

## Legend

- `{required}` - Required parameter
- `[optional]` - Optional parameter
- `[user]Name[/user]` - User reference (F-List BBCode)
- **Admin** - Requires bot admin privileges
- **Op** - Requires channel op privileges

## Naming someone

Anywhere a command below shows a `[user]Name[/user]` slot, you can also just type the name:
`!cuddle bob smith` works the same as `!cuddle [user]Bob Smith[/user]`. Capitalisation doesn't
matter, a renamed resident's current name works as well as their F-Chat handle, and an
unambiguous first name is enough (`!cuddle queen` finds Queen Contract). Group casuals take
several bare names in a row — `!cuddle bob smith jane doe` — though everyone after the first
has to be named in full.

A `[user]` tag is still the unambiguous way to say who you mean, and it always wins over
name-guessing. Type the start of a name and press Tab to have F-Chat complete it into one.

---

# Chateau Contract Commands

## Getting Started

| Command | Usage | What it does |
|---------|-------|--------------|
| `!joinchateau` | `!joinchateau` | Register your character with the Chateau system |
| `!help` (`!commands`) | `!help` or `!help {command}` | Command list / detailed help |
| `!identifier` (`!whatis`) | `!identifier {identifier}` | Information about an identifier (a bodypart, substance, monster, …) |
| `!category` (`!list`, `!identifiers`) | `!category {category}` or `!category` | List all identifiers in a category |
| `!botinfo` / `!uptime` | | Bot version / time online |
| `!modmessage` | `!modmessage [category]` | View moderator announcements |
| `!feedback` (`!suggestion`) | `!feedback <your idea or bug>` | Send the staff an idea or a bug report |

## Consent Lifecycle

Every interaction is a request until the target answers. All three verbs accept `all`, a number from your pending list, or a name.

| Command | Usage | What it does |
|---------|-------|--------------|
| `!consent` (`!c`, `!accept`) | `!consent` / `!consent all` / `!consent {number}` / `!consent {name}` | Accept a pending interaction. With several pending, you're PM'd a numbered list |
| `!no` (`!refuse`, `!decline`) | `!no` / `!no all` / `!no {number}` / `!no {name}` | Decline a pending interaction. In a group, the shared moment still resolves with whoever else consented |
| `!oops` (`!o`, `!withdraw`, `!cancel`) | `!oops` / `!oops all` / `!oops {number}` / `!oops {name}` | Withdraw an interaction **you** started (calls off a whole group) |

Pending requests expire after 10 minutes.

## Casual Interactions

All casual interactions are group-capable (name several residents) and share a 30-minute recording cooldown — hitting it doesn't block the fun, it just doesn't make the dossier.

| Command | Usage | What it does |
|---------|-------|--------------|
| `!kiss` | `!kiss [user]Name[/user]` | Give another resident (or residents) a kiss |
| `!cuddle` (`!hug`) | `!cuddle [user]Name[/user]` | Cuddle with another resident (or residents) |
| `!handhold` | `!handhold [user]Name[/user]` | Hold hands |
| `!spank` | `!spank [user]Name[/user]` | Spank another resident (or residents) |
| `!bully` | `!bully [user]Name[/user]` | Bully another resident (or residents) |
| `!pet` | `!pet [user]Name[/user]` | Pet another resident (or residents) |
| `!lick` | `!lick [user]Name[/user]` | Give another resident (or residents) a lick |
| `!boobhat` | `!boobhat [user]Name[/user]` | Put your chest on another resident's head |
| `!sit` | `!sit [user]Name[/user]` | Take a seat on another resident's lap (starts a lap *stack* — others can race to consent) |
| `!lap` | `!lap [user]Name[/user]` | Pull another resident onto your lap |

## Involved Interactions

| Command | Usage | What it does |
|---------|-------|--------------|
| `!feed` | `!feed [user]Name[/user] {substance}` | Feed another resident |
| `!dressup` (`!dress`) | `!dressup [user]Name[/user] {attire}` | Dress another resident, or yourself, in specific attire |
| `!golden` | `!golden [user]Name[/user] {bodypart}` | Give another resident a golden shower |
| `!milk` | `!milk [user]Name[/user] {substance}` | Milk a substance from another resident (produces numbered bottles; per-recipient daily cooldown) |
| `!climax` | `!climax [user]Name[/user]` | Bring another resident, or yourself, to orgasm |
| `!climaxfor` | `!climaxfor [user]Name[/user]` | Bring yourself to orgasm, solo or for another resident |
| `!pay` | see below | Transfer currency or bottles |

### !pay

Transfer currency, or pass along bottles from your collection. A negative amount bills the other resident instead.

**Usage:** `!pay [user]Name[/user] {amount} {currency}` or `!pay [user]Name[/user] {amount} bottles {substance}` or `!pay [user]Name[/user] bottles #12 #13`

Bottles keep their number, their donor and their corrupt/pure tag when they change hands. Naming bottles by number is the only way to pass along an empty; an amount always means full bottles.

```
!pay [user]Bob[/user] bottles #142 #143
→ Alice is going to pass Bob 2 bottles: two of the milk from Carol (corrupt)! Do you !consent to receiving them? (or !no)
```

## Commitment Interactions

Lasting relationships, transformations, and state. Cooldowns are typically daily; the consent prompt and `!help` state each one exactly.

| Command | Usage | What it does |
|---------|-------|--------------|
| `!mark` | `!mark [user]Name[/user] {bodypart}` | Place your mark upon another resident's body |
| `!entitle` | `!entitle [user]Name[/user] "{title}"` | Grant a custom title |
| `!bond` | `!bond [user]Name[/user] {bondtype}` | Declare your bond with another resident |
| `!employ` (`!hire`) | `!employ [user]Name[/user] {job}` | Employ someone (or yourself) to do jobs for the Chateau; enables `!work` |
| `!train` | `!train [user]Name[/user] {training}` | Train with another resident in a skill |
| `!breed` | `!breed [user]Name[/user] {monster}` (or `random`) | Breed another resident with new monster life |
| `!birth` | `!birth` or `!birth {index}` | Birth a pregnancy that has finished gestating |
| `!corrupt` | `!corrupt [user]Name[/user] {amount}` | Push another resident toward corruption (daily magnitude quota) |
| `!purify` | `!purify [user]Name[/user] {amount}` | Push another resident toward purity (same quota) |
| `!petrify` | `!petrify [user]Name[/user] {location}` | Turn another resident into a statue (browse with `!statues`) |
| `!plant` | `!plant [user]Name[/user] {plant}` | Transform another resident into a plant |
| `!objectify` | `!objectify [user]Name[/user] {object}` | Transform someone into an object |
| `!consume` | `!consume [user]Name[/user] {bodypart}` | Consume/devour another resident |

## Consequence Interactions

Heavier, longer cooldowns (typically weekly, often per-axis: one `!dose` per vice per recipient per week). Several leave status effects that echo into other interactions.

| Command | Usage | What it does |
|---------|-------|--------------|
| `!rename` | `!rename [user]Name[/user] "{newname}"` | Change another resident's official name on the records |
| `!monsterize` | `!monsterize [user]Name[/user] {monster}` | Transform someone into a monster |
| `!odorize` | `!odorize [user]Name[/user] {scent}` | Saturate another resident with a lingering scent |
| `!break` | `!break [user]Name[/user] {bodypart} {days?}` | Break a bodypart for several days |
| `!dose` | `!dose [user]Name[/user] {vice}` | Hook another resident on an addictive vice |
| `!infest` | `!infest [user]Name[/user] {parasite}` | Infest someone with new parasitic life |
| `!curse` | `!curse [user]Name[/user] {curse}` | Place a curse — a disabler or a modifier |

## Recovery Commands

Self-targeted reversals, each with its own cost or time gate.

| Command | Usage | What it does |
|---------|-------|--------------|
| `!purge` | `!purge {parasite}` | Purge a parasite, at a cost (free if caught within the spread grace) |
| `!cleanse` | `!cleanse {curse}` | Cleanse a curse, at a cost |
| `!rest` | `!rest {bodypart}` | Skip today's `!work` to heal broken bodyparts one day faster |
| `!detox` | `!detox {vice}` | Break an addiction, at a cost |
| `!wash` | `!wash {scent}` | Wash off one scent layer (one per day) |

(`!purify` and `!birth` above complete the reversal set for corruption and pregnancies.)

## Pledges

| Command | Usage | What it does |
|---------|-------|--------------|
| `!pledge` | `!pledge [user]Name[/user] {interactiontype}` | Promise to perform an interaction in the future |
| `!fulfill` | `!fulfill [user]Name[/user] {interactiontype}` | Perform a promised interaction |
| `!pledges` | `!pledges` or `!pledges [user]Name[/user]` | View active pledges |
| `!abandonpledge` | `!abandonpledge [user]Name[/user] {interactiontype}` | Abandon an active pledge |

## Economy & Jobs

| Command | Usage | What it does |
|---------|-------|--------------|
| `!work` (`!w`) | `!work`, then `!w {choice number}` | Perform your job duties for currency (daily; PM'd a choice of chores) |
| `!volunteer` (`!v`) | `!volunteer {job}`, then `!v {choice number}` | Try a job you're not employed in (separate daily timer) |
| `!bank` (`!balance`, `!money`) | `!bank` or `!bank [user]Name[/user]` | See accumulated currencies |
| `!business` | `!business` | See what your employees have earned you (25% MANOR kickback from their `!work`) |
| `!sell` | `!sell {amount} {substance}` | Sell full bottles to the Chateau (the bottle number leaves your collection) |

### Bottle Collection

Bottles come from `!milk`. Every bottle carries a permanent number, the resident it came from, and a corrupt/pure tag if the donor was far enough one way or the other.

| Command | Usage | What it does |
|---------|-------|--------------|
| `!bottles` (`!collection`) | `!bottles` / `!bottles {substance}` / `!bottles {substance} [user]Name[/user]` | Look over the bottles you're holding (private reply) |
| `!drink` | `!drink` / `!drink {substance}` / `!drink #{number}` | Drink one bottle (channel-only). Drinking empties the bottle but keeps its number as a record. Corrupt/pure bottles shift you (up to 3/day); an addicting substance quiets the craving |

## Profile, Titles & Personalization

| Command | Usage | What it does |
|---------|-------|--------------|
| `!dossier` (`!profile`, `!bio`) | `!dossier` or `!dossier [user]Name[/user]` | A resident's public record: interactions, holdings, titles, lasting effects |
| `!titles` | `!titles` or `!titles [user]Name[/user]` | View all earned and granted titles |
| `!settitle` | `!settitle {slot} "{title}"` | Display a title in one of 9 dossier slots (or clear a slot) |
| `!seteicon` | see below | Pin your own eicon to an interaction or bodypart |
| `!setmark` | `!setmark [eicon]YourMark[/eicon]` | Legacy alias for `!seteicon mark` |
| `!random` | `!random <keyword/answer>` | Join the ambient random event happening in this channel |

### !seteicon

Pin one of your own eicons to an interaction, or to one of your bodyparts.

**Usage:** `!seteicon {interaction} [eicon]YourEicon[/eicon]` or `!seteicon {bodypart} [eicon]YourEicon[/eicon]`
Leave the eicon off to clear it (`!seteicon ass`), or send `!seteicon` alone to list everything you've set.

**Interaction eicons** show on that interaction's completion message. Mutual interactions (`!kiss`, `!cuddle`, `!handhold`, `!bond`) and group interactions show every participant's; `!climax`/`!climaxfor` show the one climaxing; `!pet` shows the one being petted; everything else shows the initiator's.

**Bodypart eicons** show whenever an interaction involves that part of your body. Any identifier in the `bodypart` category works — `!category bodypart` lists them.

| Interaction | Part | Whose eicon shows |
|---|---|---|
| `!mark`, `!golden`, `!break` | the typed part | the recipient's |
| `!consume` | the typed part | the initiator's |
| `!spank` | ass | the recipient's |
| `!feed` | mouth | the recipient's |
| `!milk` | breast | the recipient's |
| `!lick` | tongue | the initiator's |
| `!boobhat` | breast | the initiator's |
| `!handhold` | hand | both |

Bodypart eicons come after the interaction eicons on the message. A part you haven't set an eicon for simply shows nothing.

## Information Readouts

Chateau-wide reports, all read-only:

| Command | What it shows |
|---------|---------------|
| `!statistics` (`!stats`) | Chateau-wide statistics across every interaction |
| `!populations` | Current monsterized population by species |
| `!birthrates` | Every monster ever born, by species |
| `!flora` | Every plant ever cultivated |
| `!parasites` | Every parasite ever spread, current and lifetime |
| `!statues` | All currently petrified characters (`!statues {location}`) |
| `!payroll` | Current workforce and lifetime duties completed |
| `!economics` | Full per-currency wealth across all residents |
| `!bondtree` | Everyone connected to a resident by bonds, N degrees out |
| `!familytree` | A resident's family bonds, N degrees out |

## Chateau Admin Commands

| Command | Usage | What it does |
|---------|-------|--------------|
| `!namechange` (Admin) | `!namechange [user]OldUsername[/user] ...` | Update the database when a user changes their F-List username |
| `!setidentifiereicon` (Admin) | `!setidentifiereicon {identifier} [eicon]TheEicon[/eicon]` | Set the Chateau's bot-wide eicon for an identifier (shown in `!whatis`); leave the eicon off to clear |
| `!feedbacklist` (Admin) | `!feedbacklist [count]` | View recent `!feedback` submissions |

---

# Legacy Dicebot Commands

The dicebot half predates the Chateau help system; the commands below with usage strings carry full `!help` metadata, the rest respond to `!dicehelp`, `!gamehelp`, and their own error messages. All are stable and rarely change.

## Dice

| Command | Usage | What it does |
|---------|-------|--------------|
| `!roll` | `!roll {dice notation}` | Roll dice using standard notation (`3d6+5`, `2d20>15`, …). Limits: 200 dice, 400 total rolls, 10,000,000 sides |
| `!fitd` | `!fitd {number of dice}` | Forged in the Dark style dice pool |
| `!coinflip` | `!coinflip` | Flip a coin |
| `!tipdie` | `!tipdie {from}>{to}` | Adjust one die from the last roll |
| `!showlastroll` | `!showlastroll` | Show the last dice roll made |
| `!rolltable` | `!rolltable {table name}` | Roll on a saved random table |
| `!fen` | `!fen {FEN string}` | Display a chess board from FEN notation |

Related (no help metadata): `!unlockdice`, `!unlockdiceall`, `!luckforecast`, `!dicehelp`.

## Roll Tables

`!savetable`, `!savetablesimple`, `!showtables`, `!mytables`, `!tableinfo`, `!tablejson`, `!deletetable` — create, inspect, and remove weighted random tables, rolled with `!rolltable`.

## Games

| Command | Usage | What it does |
|---------|-------|--------------|
| `!joingame` | `!joingame {game name}` or `!joingame {game name} {amount} {currency}` | Join or start a dice game (optionally wagering a Chateau currency) |
| `!startgame` | `!startgame {game name}` | Start the session once enough players joined |
| `!gamecommand` (`!gc`, `!g`) | `!gc {game name} {command}` | Send a game-specific command to the active session |
| `!gamestatus` | `!gamestatus {game name}` | Status of an active session |
| `!leavegame` | `!leavegame {game name}` | Leave a session you've joined |
| `!cancelgame` | `!cancelgame {game name}` | Cancel the active session |
| `!showgames` | `!showgames` | List every game type available |
| `!rock` / `!paper` / `!scissors` / `!lizard` / `!spock` | | Plays in an active Rock-Paper-Scissors game |

**Games available:** AlphaRoyale, Blackjack, BottleSpin, Chess, DungeonDelve, HighRoll, KingsGame, LiarsDice, Mafia, Poker, PokerGame, PrizeRoll, RockPaperScissors, Roulette, SlamRoll (see `!showgames` / `!gamehelp`).

Related: `!addtogame`, `!removefromgame`, `!gamesessions`, `!gamehelp` / `!helpgame`, `!endhand`.

## Cards & Decks

Draw/play flow: `!drawcard`, `!showhand`, `!playcard`, `!discardcard`, `!hidecard`, `!movecard`, `!revealcard`, `!playfromdiscard`, `!takefromdiscard`, `!takefromplay`, `!discardfromplay`.
Deck management: `!shuffledeck`, `!shufflediscardintodeck`, `!resetdeck`, `!deckinfo`, `!decklist`, `!deckjson`, `!showdecks`, `!showcardpiles`, `!cardinfo`.
Custom decks: `!savecustomdeck`, `!savecustomdecksimple`, `!mydecks`, `!deletecustomdeck`.

Deck types include playing cards, tarot, uno, and saved custom decks.

## Chips & Casino

Balances and transfers: `!register` (per-channel chip pile), `!showchips`, `!givechips`, `!bet`, `!claimpot`.
VelvetCuff integration: `!buychips`, `!cancelbuychips`, `!cashout` (min/max enforced), `!itembuy`.
Chip codes (coupons): `!generatechipscode` (admin), `!addchipscode`, `!redeemchips`.
Slots: `!slots`, `!slotsinfo` (cooldown and max multiplier are per-channel settings).
Admin pile management: `!addchips`, `!removechips`, `!takechips`, `!forcegivechips`, `!removepile`, `!removeallpiles`, `!removeallchipsoveramount`, `!restrictchips`.

## Potions & Powers

`!generatepotion`, `!generatepotioninfo`, `!savepotion`, `!showpotion`, `!showpotions`, `!showpotionmenu`, `!showpotionprices`, `!revealpotion`, `!deletepotion`, `!droppotion`, `!potionjson`, `!usepower`, `!usepower2`, `!usepowersecondary`.

## Bot Administration (legacy)

Channel management: `!joinchannel`, `!leavethischannel`, `!showchannelsjoined`, `!auditchannels`, `!setstartingchannel`, `!viewstartupchannels`, `!setchanneldescription`, `!sendtochannel`, `!sendallchannels`.
Settings: `!setstatus`, `!updatesetting`, `!updatesettingall`, `!viewsettings`, `!timeout`, `!removeolddata`.
Misc/diagnostic: `!showprofile`, `!showmonster`, `!generatemonster`, `!savejobslist`, `!deletejobslist`, `!jobslistjson`, `!directory`, and the `!test*` commands.

---

## Command Aliases

Many commands answer to a second name. Either name does exactly the same thing; the documentation is written under the full command.

| Full Command | Aliases |
|-------------|---------|
| `!bank` | `!balance`, `!money` |
| `!bottles` | `!collection` |
| `!category` | `!list`, `!identifiers` |
| `!consent` | `!c`, `!accept` |
| `!cuddle` | `!hug` |
| `!dossier` | `!profile`, `!bio` |
| `!dressup` | `!dress` |
| `!employ` | `!hire` |
| `!feedback` | `!suggestion` |
| `!gamecommand` | `!gc`, `!g` |
| `!help` | `!commands` |
| `!identifier` | `!whatis` |
| `!no` | `!refuse`, `!decline` |
| `!oops` | `!o`, `!withdraw`, `!cancel` |
| `!statistics` | `!stats` |
| `!volunteer` | `!v` |
| `!work` | `!w` |

`!help` prints this same list from the commands themselves, so it's always current there.

## Tips

### Case Sensitivity

Commands are **case-insensitive** (`!KISS`, `!Kiss`, `!kiss` all work). Bare-name targeting is case-insensitive too.

### Cooldowns

If an interaction tells you the clerks were "still busy processing", you've hit a recording cooldown. Each interaction states its own cooldown in its consent prompt and in `!help {command}` — casuals recover in minutes, commitment acts in a day, consequences in a week. Slots and `!work` have their own timers.

### Permissions

Some commands require special permissions:

- **Bot Admin:** Defined in account_settings.txt (`AdminCharacters`)
- **Channel Op:** F-List channel operators
- **Registered:** Chateau commands need `!joinchateau`; chip commands need `!register` in that channel

## See Also

- [Interaction System](Interaction-System) - Detailed interaction mechanics
- [Dice and Games](Dice-and-Games) - Game rules and dice systems
- [Installation and Setup](Installation-and-Setup) - Channel configuration
