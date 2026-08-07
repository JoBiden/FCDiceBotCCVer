using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class ChateauHelp : ChatBotCommand
    {
        /// <summary>The blocks of the no-arg !help listing, in the order they print.</summary>
        public enum HelpSection
        {
            /// <summary>The untitled block under "Does not require channel".</summary>
            General,
            Recovery,
            /// <summary>The untitled block under "Requires channel".</summary>
            Room,
            Casual,
            Involved,
            Commitment,
            Consequence,
            Dicebot
        }

        /// <summary>
        /// Which block of the general listing a command prints in, or null if it isn't listed.
        /// This is the only place membership is decided: the listing is read off the loaded
        /// command table, so a new command appears the moment it declares a <c>Category</c>,
        /// with no second list to remember. There used to be one — eight hand-maintained name
        /// arrays — and <c>!drinkfrom</c> / <c>!forcedrink</c> shipped missing from the listing
        /// because adding a command and listing it were two separate steps.
        ///
        /// A command goes unlisted when it has no <c>Category</c> (the legacy dicebot commands
        /// that were never migrated, which the closing line of the listing already accounts
        /// for), when it's admin-only (the admin block is written by hand and only shown to
        /// admins), or when it sets <see cref="ChatBotCommand.HideFromHelpListing"/>. An
        /// unrecognized <c>Category</c> also lands nowhere, which a test catches.
        /// </summary>
        public static HelpSection? SectionFor(ChatBotCommand cmd)
        {
            if (cmd == null || string.IsNullOrEmpty(cmd.Name) || string.IsNullOrEmpty(cmd.Category))
                return null;

            if (cmd.HideFromHelpListing || cmd.RequireBotAdmin || cmd.RequireChannelAdmin)
                return null;

            // Compared lowercased so a casing slip in a command's Category can't quietly drop it
            // out of the listing.
            switch (cmd.Category.ToLowerInvariant())
            {
                case "casual interaction": return HelpSection.Casual;
                case "involved interaction": return HelpSection.Involved;
                case "commitment interaction": return HelpSection.Commitment;
                case "consequence interaction": return HelpSection.Consequence;
                case "recovery": return HelpSection.Recovery;
                case "dicebot": return HelpSection.Dicebot;
                case "admin": return null;

                // Everything else is an ordinary command, split between the two untitled blocks
                // by the only thing that separates them: whether it can be used outside a
                // channel. That's why !drink sits with !consent rather than with !bottles.
                case "general":
                case "information":
                case "personalization":
                case "interaction":
                case "interaction support":
                    return cmd.RequireChannel ? HelpSection.Room : HelpSection.General;

                default:
                    return null;
            }
        }

        /// <summary>
        /// The command names one block prints. Unordered — <see cref="Utils.sortedListDisplayText"/>
        /// alphabetizes at display time, as it did when these were hand-written arrays.
        /// </summary>
        public static List<string> ListedNames(BotCommandController commandController, HelpSection section)
        {
            return commandController.BotCommands
                .Where(c => SectionFor(c) == section)
                .Select(c => c.Name)
                // A legacy dicebot class and a Chateau class can share a Name (see
                // FindCommandByName); the listing wants that name once.
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Every command the general listing prints, across all blocks.</summary>
        public static IEnumerable<string> AllListedCommands(BotCommandController commandController)
        {
            return Enum.GetValues(typeof(HelpSection))
                .Cast<HelpSection>()
                .SelectMany(s => ListedNames(commandController, s));
        }

        public ChateauHelp()
        {
            Name = "help";
            Aliases = new string[] { "commands" };
            Category = "General";
            ShortDescription = "Get help about bot commands";
            LongDescription = "View general help for all commands, or get detailed help for a specific command.\n\nWithout arguments: Shows a categorized list of all available commands.\nWith a command name: Shows detailed help for that specific command including usage, cooldown, and related commands.";
            Usage = "!help\nor\n!help {commandname}";
            RelatedCommands = new string[] { "botinfo", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            // Check if user is requesting help for a specific command
            if (terms.Length > 0)
            {
                // Same resolver the dispatcher uses, so "!help hug" and "!hug" can't disagree
                // about which command an alias means.
                string requestedCommand = terms[0].ToLower();
                ChatBotCommand cmd = commandController.FindCommandByName(requestedCommand);

                if (cmd != null)
                {
                    string detailedHelp = BuildDetailedCommandHelp(cmd);
                    bot.SendPrivateMessage(detailedHelp, characterName);
                    return;
                }
                else
                {
                    bot.SendPrivateMessage($"Command '{requestedCommand}' not found. Use !help to see all available commands.", characterName);
                    return;
                }
            }

            List<string> generalCommands = ListEntries(commandController, HelpSection.General);
            List<string> recoveryCommands = ListEntries(commandController, HelpSection.Recovery);
            List<string> roomCommands = ListEntries(commandController, HelpSection.Room);
            List<string> casualCommands = ListEntries(commandController, HelpSection.Casual);
            List<string> involvedCommands = ListEntries(commandController, HelpSection.Involved);
            List<string> commitmentCommands = ListEntries(commandController, HelpSection.Commitment);
            List<string> consequenceCommands = ListEntries(commandController, HelpSection.Consequence);
            List<string> dicebotCommands = ListEntries(commandController, HelpSection.Dicebot);
            string messageText = "These are all of the commands native to the [user]Chateau Contract[/user] bot, as of [b]August 2nd 2026.[/b] For detailed description of their use, please see the [user]Chateau Contract[/user] profile or use !help [command] Commands in subtext are alternate names of the same command - all documentation will be for the first listed names.\n\n" +
                    "[u]Does not require channel[/u]\n" +
                    Utils.sortedListDisplayText(generalCommands) + "\n" +
                    "[color=blue]Recovery Commands:[/color] " + Utils.sortedListDisplayText(recoveryCommands) + "\n\n" +
                    "[u]Requires channel[/u]\n" +
                    Utils.sortedListDisplayText(roomCommands) + "\n" +
                    "[color=green]Casual Interactions:[/color] " + Utils.sortedListDisplayText(casualCommands) + "\n" +
                    "[color=yellow]Involved Interactions:[/color] " + Utils.sortedListDisplayText(involvedCommands) + "\n" +
                    "[color=orange]Commitment Interactions:[/color] " + Utils.sortedListDisplayText(commitmentCommands) + "\n" +
                    "[color=red]Consequence Interactions:[/color] " + Utils.sortedListDisplayText(consequenceCommands) + "\n" +
                    "[color=cyan]Dice Bot Commands:[/color] " + Utils.sortedListDisplayText(dicebotCommands) + "\n" +

                    "\n[b]Channel Op only Commands:[/b]\n" +
                    "None yet :)\n" +

                    "\nAny dicebot commands not listed here have yet to be fully migrated, or were intentionally cut. They might work, but use at your own risk!\n";

            if(Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, command.characterName))
            {
                messageText += "\n[b]Admin only Commands [/b](no channel req)\n" +
                    "!namechange [noparse][user]old profile in user tag[/user][/noparse] \"new profile in quotes\" - updates the database to reflect a user who has changed their Flist username. CaSe SeNsItIvE \n" +
                    "!feedbacklist [count] - view recent !feedback / !suggestion submissions (newest first) \n";
            }

            if (commandController.MessageCameFromChannel(address))
            {
                bot.SendMessageInChannel("We've messaged the requested help directly to you, " + command.characterName + ". In the future you can message this profile directly to avoid cluttering the public channel. Thank you~", channel);
                
            }
            bot.SendPrivateMessage(messageText + "\nMost of [user]Chateau Contract[/user]'s functions are designed for use in the [session=Château Contract]adh-ac1885cd73f31adfaefb[/session] channel. Be sure to !joinchateau if you plan to stick around ♥", characterName);
        }

        /// <summary>Renders one listing line per command in the given block.</summary>
        private static List<string> ListEntries(BotCommandController commandController, HelpSection section)
        {
            return ListedNames(commandController, section)
                .Select(n => ListEntry(commandController, n))
                .ToList();
        }

        /// <summary>
        /// One listing entry: <c>!name</c>, followed by its aliases in subtext if it has any.
        /// A name that resolves to nothing is still printed — better a listed command with no
        /// alias subtext than a silently missing line if a command is ever renamed.
        /// </summary>
        public static string ListEntry(BotCommandController commandController, string commandName)
        {
            ChatBotCommand cmd = commandController.FindCommandByName(commandName);
            string[] aliases = cmd != null ? cmd.Aliases : null;

            if (aliases == null || aliases.Length == 0)
                return "!" + commandName;

            return "!" + commandName + " " + ReadoutText.Small(string.Join(", ", aliases.Select(a => "!" + a)));
        }

        private string BuildDetailedCommandHelp(ChatBotCommand cmd)
        {
            StringBuilder sb = new StringBuilder();

            // Command name and aliases. The name is this readout's Title line.
            sb.Append(ReadoutText.Title("!" + cmd.Name));

            if (cmd.Aliases != null && cmd.Aliases.Length > 0)
            {
                sb.Append(' ');
                sb.Append(ReadoutText.Small(string.Join(", ", cmd.Aliases.Select(a => "!" + a))));
            }
            sb.AppendLine("");

            // Category. These colours are deliberately NOT the ReadoutDomain palette: the tier
            // line is a stoplight (green -> yellow -> orange -> red) reading as escalating
            // seriousness, which is worth more here than palette consistency. Owner-confirmed.
            if (!string.IsNullOrEmpty(cmd.Category))
            {
                switch (cmd.Category)
                {
                    case "Casual Interaction":
                        sb.AppendLine("[color=green]Casual Interaction[/color]");
                        break;
                    case "Involved Interaction":
                        sb.AppendLine("[color=yellow]Involved Interaction[/color]");
                        break;
                    case "Commitment Interaction":
                        sb.AppendLine("[color=orange]Commitment Interaction[/color]");
                        break;
                    case "Consequence Interaction":
                        sb.AppendLine("[color=red]Consequence Interaction[/color]");
                        break;
                    case "Recovery":
                        sb.AppendLine("[color=blue]Recovery[/color]");
                        break;
                    case "Dicebot":
                        sb.AppendLine("[color=cyan]Dicebot[/color]");
                        break;
                    case "General":
                        sb.AppendLine("General");
                        break;
                    default:
                        sb.AppendLine(cmd.Category);
                        break;
                }
                sb.AppendLine("");
            }

            // Long description
            if (!string.IsNullOrEmpty(cmd.LongDescription))
            {
                sb.AppendLine(cmd.LongDescription);
                sb.AppendLine("");
            }
            else if (!string.IsNullOrEmpty(cmd.ShortDescription))
            {
                sb.AppendLine(cmd.ShortDescription);
                sb.AppendLine("");
            }

            // Usage. Field headings are Reference-domain sections (purple), consistent across
            // every field below.
            if (!string.IsNullOrEmpty(cmd.Usage))
            {
                sb.AppendLine(ReadoutText.Section("Usage", ReadoutDomain.Reference));
                sb.AppendLine(Indent(cmd.Usage));
            }

            // Identifier list (if applicable)
            if (!string.IsNullOrEmpty(cmd.IdentifierCategory))
            {
                List<Model.Identifier> identifiers = MonDB.getIdentifiers(cmd.IdentifierCategory);
                if (identifiers != null && identifiers.Count > 0)
                {
                    string categoryPlural = (cmd.IdentifierCategory.EndsWith("y")
                        ? cmd.IdentifierCategory.TrimEnd('y') + "ie"
                        : cmd.IdentifierCategory) + "s";
                    sb.AppendLine(ReadoutText.Section("Available " + categoryPlural, ReadoutDomain.Reference));
                    sb.AppendLine(Indent(Utils.sortedListDisplayText(identifiers.Select(i => i.type).ToList())));
                }
            }

            // Cooldown information. Prefer the structured CooldownSpec on the interaction's
            // processor (single source of truth) so this line can't drift from the consent
            // warning; fall back to the command's free-text fields for system commands and
            // aliases that have no processor of their own.
            string cooldownDuration = cmd.CooldownDuration;
            string cooldownAppliesTo = cmd.CooldownAppliesTo;
            if (!string.IsNullOrEmpty(cmd.Name))
            {
                var cooldownRule = InteractionProcessors.InteractionProcessorRegistry.GetProcessor(cmd.Name)?.CooldownRule;
                if (cooldownRule != null)
                {
                    cooldownDuration = cooldownRule.FormatDuration();
                    cooldownAppliesTo = cooldownRule.FormatAppliesTo();
                }
            }
            if (!string.IsNullOrEmpty(cooldownDuration))
            {
                sb.Append(ReadoutText.Section("Cooldown", ReadoutDomain.Reference)).Append(' ');
                sb.Append(ReadoutText.Num(cooldownDuration));
                if (!string.IsNullOrEmpty(cooldownAppliesTo))
                {
                    sb.Append(' ').Append(ReadoutText.Small("(applies to " + cooldownAppliesTo + ")"));
                }
                if (cmd.Category == "Casual Interaction")
                {
                    sb.Append(' ').Append(ReadoutText.Small("(Casual command cooldowns are only for incrementing dossier counts, and can still be performed at any time)"));
                }
                else if (cmd.Category == "Involved Interaction")
                {
                    // Closing bracket used to sit outside the [sub], rendering as a stray ")".
                    sb.Append(' ').Append(ReadoutText.Small("(Involved command cooldowns are only for incrementing dossier counts, and can still be performed at any time)"));
                }
                sb.AppendLine("");
            }

            // Channel requirement
            sb.Append(ReadoutText.Section("Channel required", ReadoutDomain.Reference))
              .Append(cmd.RequireChannel ? " Yes\n" : " No\n");

            // Related commands
            if (cmd.RelatedCommands != null && cmd.RelatedCommands.Length > 0)
            {
                sb.Append(ReadoutText.Footer("Related commands: "
                    + string.Join(", ", cmd.RelatedCommands.Select(c => "!" + c))));
                sb.Append("\n");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Indents every line of a multi-line help field so it sits under its section header
        /// the way rows do in the other readouts. <c>Usage</c> is frequently multi-line
        /// ("!bank\nor\n!bank [user]...[/user]"), so this can't just prepend once.
        /// </summary>
        private static string Indent(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return ReadoutText.RowIndent
                + text.Replace("\n", "\n" + ReadoutText.RowIndent);
        }
    }
}
