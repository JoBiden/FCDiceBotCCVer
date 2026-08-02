# FCDiceBot Wiki

Welcome to the FCDiceBot documentation! This is a comprehensive chatbot for F-List chat with support for dice rolling, card games, casino features, and the extensive Chateau Contract roleplay interaction system.

## Quick Links

- **[Getting Started](Installation-and-Setup)** - Install and configure the bot
- **[Architecture Overview](Architecture)** - Understand how the system works
- **[Command Reference](Command-Reference)** - List of all available commands
- **[Interaction System](Interaction-System)** - Chateau Contract interactions
- **[Dice and Games](Dice-and-Games)** - Dice rolling and game features
- **[F-List Integration](F-List-Integration)** - How the bot connects to F-List
- **[Database and Persistence](Database-and-Persistence)** - Data storage system
- **[Development Guide](Development-Guide)** - Adding features and commands
- **[Style Guide](Style-Guide)** - Rules for user-facing text
- **[Documentation Guide](README)** - How to keep these docs accurate
- **specs/** - Per-feature design + as-shipped documentation

## What is FCDiceBot?

FCDiceBot is a feature-rich chatbot designed for F-List chat that provides:

### Core Features (legacy dicebot)

- **Dice Rolling System** - Advanced dice rolling with complex expressions, up to 200 dice
- **Card Deck Management** - Playing cards, Tarot, Uno, and custom decks
- **Casino Chip Economy** - Betting, pots, chip transfers, and real currency integration
- **Game Sessions** - 15 games including poker, blackjack, roulette, chess, mafia, and more
- **Roll Tables** - User-created weighted random tables
- **Slot Machines** - Customizable slot games with payout multipliers

### Chateau Contract System

An extensive roleplay interaction system featuring:

- **Consent-based Interactions** - every interaction needs the recipient's `!consent` (decline with `!no`, withdraw with `!oops`), including multi-person group casuals
- **Investment Levels** - Casual, Involved, Commitment, and Consequence interactions, plus themed Recovery commands
- **Status Effects** - scents, corruption, addictions, parasites, and curses that echo into other interactions
- **Economy** - currencies, `!work` duties, employment with employer kickbacks, milk bottles, pledges
- **Titles and Statistics** - system achievements, player-granted titles, dossiers, and Chateau-wide reports
- **Transformations** - species changes, petrification, objectification, breeding, and their reversals

## Technology

- **Language:** C# (.NET Framework 4.8)
- **Database:** MongoDB (Chateau) + JSON file storage (legacy dicebot)
- **Communication:** WebSocket (F-List chat protocol)
- **Libraries:** Newtonsoft.Json, websocket-sharp, MongoDB.Driver
- **Tests:** xUnit (`FChatDicebot.Tests`, needs local MongoDB)

## Architecture Highlights

- **Command Pattern** - ~210 commands automatically discovered via reflection
- **Strategy Pattern** - Modular interaction processors for extensibility
- **Rate Limiting** - per-interaction cooldowns, declared once per processor
- **Message Queue** - Respects F-List's rate limits (1.5s between messages)
- **Multi-channel Support** - Per-channel configuration and state management

## Quick Start

1. Follow the [Installation Guide](Installation-and-Setup)
2. Configure your F-List account credentials
3. Set up MongoDB
4. Run the bot and join channels
5. Start using commands with the `!` prefix (configurable per-channel)

## Bot Commands

All commands use the `!` prefix by default (configurable per-channel):

- `!roll 3d6` - Roll dice
- `!register` - Register for a chip pile (legacy dicebot, per channel)
- `!joinchateau` - Register with the Chateau Contract system
- `!help` - Command list; `!help {command}` for details
- `!kiss [user]Name[/user]` - Chateau interaction example

See the [Command Reference](Command-Reference) for the complete list.
