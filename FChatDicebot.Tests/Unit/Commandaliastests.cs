using FChatDicebot.BotCommands;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.InteractionProcessors;
using FChatDicebot.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FChatDicebot.Tests.Unit
{
    /// <summary>
    /// Guards the alias wiring. <see cref="ChatBotCommand.Aliases"/> is the single source of
    /// truth: the dispatcher resolves through it and !help prints from it, so an alias is added
    /// by putting one string in that array. These tests pin the three ways that can go wrong —
    /// an alias that doesn't route, an alias that shadows a real command, and two commands
    /// claiming the same alias — none of which has a visible symptom until a resident types it.
    /// </summary>
    [Collection("Database")]
    public class CommandAliasTests
    {
        private readonly BotCommandController _controller;

        // The fixture guarantees MonDB.Initialize has run before the reflection-based command
        // loader instantiates the database-backed commands.
        public CommandAliasTests(TestDatabaseFixture fixture)
        {
            _controller = new BotCommandController(null);
        }

        [Fact]
        public void EveryDeclaredAlias_ResolvesToTheCommandThatDeclaredIt()
        {
            var broken = new List<string>();

            foreach (ChatBotCommand cmd in _controller.BotCommands)
            {
                foreach (string alias in cmd.Aliases ?? new string[0])
                {
                    ChatBotCommand resolved = _controller.FindCommandByName(alias);
                    if (!ReferenceEquals(resolved, cmd))
                    {
                        broken.Add("!" + alias + " (declared on !" + cmd.Name + ") resolved to "
                            + (resolved == null ? "nothing" : "!" + resolved.Name));
                    }
                }
            }

            Assert.True(broken.Count == 0, "Aliases that don't dispatch: " + string.Join("; ", broken));
        }

        [Fact]
        public void NoAlias_ShadowsARealCommandName()
        {
            var names = new HashSet<string>(
                _controller.BotCommands.Where(c => !string.IsNullOrEmpty(c.Name)).Select(c => c.Name),
                StringComparer.OrdinalIgnoreCase);

            var shadowed = _controller.BotCommands
                .SelectMany(c => (c.Aliases ?? new string[0]).Select(a => new { Command = c, Alias = a }))
                .Where(x => names.Contains(x.Alias))
                .Select(x => "!" + x.Alias + " on !" + x.Command.Name)
                .ToList();

            // The dispatcher ignores such an alias rather than misrouting, but the resident is
            // still promised a shorthand in !help that lands on a different command.
            Assert.True(shadowed.Count == 0,
                "Aliases that collide with an existing command name: " + string.Join("; ", shadowed));
        }

        [Fact]
        public void NoAlias_IsClaimedByTwoCommands()
        {
            var duplicates = _controller.BotCommands
                .SelectMany(c => (c.Aliases ?? new string[0]).Select(a => new { Command = c, Alias = a }))
                .GroupBy(x => x.Alias, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Select(x => x.Command).Distinct().Count() > 1)
                .Select(g => "!" + g.Key + " claimed by " + string.Join(" and ", g.Select(x => "!" + x.Command.Name)))
                .ToList();

            Assert.True(duplicates.Count == 0, "Aliases claimed twice: " + string.Join("; ", duplicates));
        }

        [Theory]
        // The long-standing aliases, which used to each need a delegating command class.
        [InlineData("hug", "cuddle")]
        [InlineData("dress", "dressup")]
        [InlineData("hire", "employ")]
        [InlineData("profile", "dossier")]
        [InlineData("bio", "dossier")]
        [InlineData("balance", "bank")]
        [InlineData("money", "bank")]
        [InlineData("stats", "statistics")]
        [InlineData("commands", "help")]
        [InlineData("suggestion", "feedback")]
        [InlineData("whatis", "identifier")]
        [InlineData("list", "category")]
        [InlineData("identifiers", "category")]
        [InlineData("w", "work")]
        [InlineData("v", "volunteer")]
        [InlineData("c", "consent")]
        [InlineData("accept", "consent")]
        [InlineData("refuse", "no")]
        [InlineData("decline", "no")]
        [InlineData("o", "oops")]
        [InlineData("withdraw", "oops")]
        [InlineData("cancel", "oops")]
        [InlineData("gc", "gamecommand")]
        [InlineData("g", "gamecommand")]
        // Declared in an Aliases array but never given a class, so it was dead until the array
        // started routing.
        [InlineData("collection", "bottles")]
        public void Alias_DispatchesToItsCanonicalCommand(string alias, string canonicalName)
        {
            ChatBotCommand resolved = _controller.FindCommandByName(alias);

            Assert.NotNull(resolved);
            Assert.Equal(canonicalName, resolved.Name);
        }

        [Fact]
        public void EveryCommandInTheHelpListing_Resolves()
        {
            // A name here that matches nothing prints as a plain "!whatever" with its alias
            // subtext silently missing — no error, just a quietly wrong listing.
            var unresolved = ChateauHelp.AllListedCommands
                .Where(n => _controller.FindCommandByName(n) == null)
                .ToList();

            Assert.True(unresolved.Count == 0,
                "!help lists commands that don't exist: " + string.Join(", ", unresolved));
        }

        [Theory]
        [InlineData("cuddle", "!cuddle [sub]!hug[/sub]")]
        [InlineData("bank", "!bank [sub]!balance, !money[/sub]")]
        [InlineData("kiss", "!kiss")]
        public void HelpListing_PrintsAliasesFromTheCommand(string commandName, string expected)
        {
            Assert.Equal(expected, ChateauHelp.ListEntry(_controller, commandName));
        }

        [Fact]
        public void InteractionAliases_AreAlsoRecognisedBySeteicon()
        {
            // The eicon map keys on the typed token, and its extra folds (climaxfor -> climax,
            // pay -> both payment directions) mean it can't just be generated from Aliases. So
            // it's a second place to touch: adding an alias to an interaction command without
            // adding it here leaves "!seteicon hug" refusing a verb "!hug" accepts.
            var missing = new List<string>();

            foreach (ChatBotCommand cmd in _controller.BotCommands)
            {
                if (string.IsNullOrEmpty(cmd.Name)) continue;
                if (!InteractionEiconSupport.TryResolveTokenToVerbKeys(cmd.Name, out string[] _)) continue;

                foreach (string alias in cmd.Aliases ?? new string[0])
                {
                    if (!InteractionEiconSupport.TryResolveTokenToVerbKeys(alias, out string[] _))
                        missing.Add("!" + alias + " (alias of the interaction !" + cmd.Name + ")");
                }
            }

            Assert.True(missing.Count == 0,
                "Interaction aliases missing from InteractionEiconSupport.TokenToVerbKeys: "
                + string.Join("; ", missing));
        }
    }
}
