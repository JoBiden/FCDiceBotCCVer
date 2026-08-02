# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

FCDiceBot is a C# / .NET Framework 4.8 chatbot for F-List chat (F-Chat). It has two halves:

- **Legacy dicebot** — dice rolling, card decks, casino chips, and channel games (`FChatDicebot/DiceFunctions/`). Stable; rarely changed.
- **Chateau Contract** — a consent-based social interaction framework (interactions, currencies, titles, pledges, status effects) backed by MongoDB. Nearly all active development happens here.

Canonical documentation lives in `wiki-docs/` (Architecture, Development-Guide, Interaction-System, Command-Reference, Database-and-Persistence). Feature specs live in `wiki-docs/specs/`: shipped specs carry a `**Status:** Implemented.` line; pre-implementation specs go in the `Future-*` / `Infrastructure` subfolders and follow the template described in `wiki-docs/specs/README.md`.

## Build and test

```
dotnet build FChatDicebot.sln
dotnet test FChatDicebot.Tests\FChatDicebot.Tests.csproj
dotnet test FChatDicebot.Tests\FChatDicebot.Tests.csproj --filter "FullyQualifiedName~Bondprocessortests"
```

- Tests (xUnit) require a **local MongoDB at `mongodb://localhost:27017`** and use the `ChateauDb_Test` database (`FChatDicebot.Tests/TestConfiguration.cs`). Test parallelization is disabled assembly-wide on purpose — the processor registry and `MonDB` are process-wide statics that bind to a database exactly once (see the comment in `FChatDicebot.Tests/Fixtures/Testdatabasefixture.cs`).
- DB-touching tests join the `Database` collection and use `TestDatabaseFixture` (call `Reset()` for isolation); pure helpers get plain unit tests.
- `FChatDicebot.csproj` is a legacy non-SDK project that **globs `**\*.cs`** — a new source file builds with no csproj edit. Don't add files through the VS Solution Explorer (it can expand the wildcard back into explicit entries). `Model\MonsterGenerator\**` is deliberately excluded: unported code that cannot compile.
- NuGet packages are committed under `packages\`. The test project is SDK-style.
- Running the bot for real needs `C:\BotData\DiceBot\account_settings.txt` (JSON of `SavedData/AccountSettings.cs`) plus MongoDB. Run modes: `-flist` (default), `-discord`, `-both`, `-none`.

## Architecture

Message flow: `Program.cs` → `BotMain` (WebSocket to F-Chat, 200ms run loop, outgoing message queue rate-limited to one send per 1.5s) → `BotCommandController` → command class → (for interactions) processor.

- **Commands** (`FChatDicebot/BotCommands/`, ~240 classes): subclass `ChatBotCommand`; discovered by reflection at startup, no registration needed. Metadata fields on the base class (`Category`, `Usage`, `CooldownDuration`, descriptions) drive `!help`. Bare-name targeting is derived from `Usage` containing `[user]`; set `AcceptsRecipient` explicitly only where `Usage` can't express it. **Aliases are separate delegating command classes** — the `Aliases` array is display-only and does not route.
- **Interactions** (`FChatDicebot/InteractionProcessors/`): two-phase consent flow. The initiating command writes a `PendingCommand` to Mongo and posts a consent prompt; the target's `!consent` resolves the processor from `InteractionProcessorRegistry` and runs it (`!no` declines, `!oops` withdraws). Adding an interaction = processor class (under `Casual/`, `Involved/`, `Commitment/`, or `Consequence/`) + command class + a `RegisterProcessor` line in `InteractionProcessorRegistry.Initialize()` + tests.
- **Group interactions**: `GroupInteractionResolver` + per-processor `GroupSpec`.
- **Cross-interaction hooks**: `IStatusEffectContributor` (+ `StatusEffectRegistry`) contributes status lines to readouts; `IPostInteractionEffect` (+ `PostInteractionEffectRegistry`) lets a completed interaction affect other parties.
- **Persistence**: MongoDB database `ChateauDb` behind `IChateauDatabase` (`FChatDicebot/Database/`), accessed almost everywhere through the static adapter `MonDB`. **`MonDB.Initialize(...)` must run before anything touches the DB** — `GetDatabase()` throws otherwise, and processors bind to whatever database is set when they are first constructed. Legacy dicebot state (chips, decks, tables, channel settings) is JSON files under `C:\BotData\DiceBot\`.

## User-facing text rules

- `wiki-docs/Style-Guide.md` is the canonical first-draft reference for consent prompts, completion messages, errors, and help fields.
- Before declaring work done, list every changed user-facing string (before → after) for the owner's approval — even one-word changes.
- Single sources of truth — never inline these:
  - Cooldown/consent warning text: `CooldownSpec` / `ConsentWarningText`. In processors, override `BuildConsentWarning`, **never** `GetConsentWarning` — the base entry point appends the "(or !no)" decline reminder.
  - Readout formatting and section colors: `ReadoutText` / `ReadoutDomain`. Route new readouts through it.
  - Themed display helpers: `ViceText.ViceName` for vices, `ScentText` for scents, `ParasiteText` for parasites.
  - `Utils.*ToText` helpers read `Identifier.displayText` from the DB, so new identifiers render with no code change.
- Never expose "UTC day" to players — say "Chateau day" or just "day".

## Conventions

- Branch from `main`; PRs target `main`. Never merge PRs or push to `main` directly — merging is the owner's call.
- Update the docs a change invalidates: the relevant `wiki-docs/specs/*.md` as-shipped notes, and `wiki-docs/Command-Reference.md` when help text or usage changes.
- `scripts/` holds operational tooling: `feedback-triage/` (resident-feedback pipeline — its playbooks are the actual automation instructions; ledger writes go through `ft.ps1`, and nothing writes the `Feedback` collection directly), `work-duty-builder/` and `random-event-builder/` (local web UIs launched via their `run.ps1`), plus one-off mongosh backfill scripts.
