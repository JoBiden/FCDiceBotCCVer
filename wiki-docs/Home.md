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

## What is FCDiceBot?

FCDiceBot is a feature-rich chatbot designed for F-List chat that provides:

### Core Features

- **Dice Rolling System** - Advanced dice rolling with complex expressions, up to 200 dice
- **Card Deck Management** - Playing cards, Tarot, Uno, and custom decks
- **Casino Chip Economy** - Betting, pots, chip transfers, and real currency integration
- **Game Sessions** - 10+ games including poker, blackjack, roulette, and more
- **Roll Tables** - User-created weighted random tables
- **Slot Machines** - Customizable slot games with payout multipliers

### Chateau Contract System

An extensive roleplay interaction system featuring:

- **Consent-based Interactions** - All interactions require recipient consent
- **Investment Levels** - Casual, Involved, Commitment, and Consequence interactions
- **Rate Limiting** - Prevents spam with cooldown timers
- **Job System** - Employment, daily duties, and experience tracking
- **Title System** - Achievements and player-granted titles
- **Transformation System** - Species changes, object transformations, consumption
- **Relationship System** - Marks, bonds, and ownership relationships
- **Statistics Tracking** - Complete history of all interactions

## Technology

- **Language:** C# (.NET Framework)
- **Database:** MongoDB (primary) + JSON file storage (legacy)
- **Communication:** WebSocket (F-List chat protocol)
- **Libraries:** Newtonsoft.Json, websocket-sharp, MongoDB.Driver

## Architecture Highlights

- **Command Pattern** - 143+ commands automatically discovered via reflection
- **Strategy Pattern** - Modular interaction processors for extensibility
- **Rate Limiting** - Per-user cooldowns on interactions
- **Message Queue** - Respects F-List's rate limits (1.5s between messages)
- **Multi-channel Support** - Per-channel configuration and state management

## Getting Help

- Check the relevant wiki page for your topic
- Review the [Command Reference](Command-Reference) for command syntax
- See the [Development Guide](Development-Guide) for contributing

## Quick Start

1. Follow the [Installation Guide](Installation-and-Setup)
2. Configure your F-List account credentials
3. Set up MongoDB
4. Run the bot and join channels
5. Start using commands with the `!` prefix (configurable per-channel)

## Bot Commands

All commands use the `!` prefix by default (configurable per-channel):

- `!roll 3d6` - Roll dice
- `!register` - Register for Chateau features
- `!help` - Get help (if implemented)
- `!kiss [user]Name[/user]` - Chateau interaction example

See the [Command Reference](Command-Reference) for the complete list.
