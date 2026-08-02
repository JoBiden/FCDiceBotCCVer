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
                string requestedCommand = terms[0].ToLower();
                ChatBotCommand cmd = commandController.BotCommands.FirstOrDefault(c =>
                    c.Name.ToLower() == requestedCommand ||
                    (c.Aliases != null && c.Aliases.Any(a => a.ToLower() == requestedCommand)));

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

            // Original general help display
            List<string> generalCommands = new List<string>()
            {
                "!dossier [sub]!profile, !bio[/sub] ",
                "!bondtree",
                "!familytree",
                "!work [sub]!w[/sub]",
                "!volunteer [sub]!v[/sub]",
                "!bank [sub]!balance, !money[/sub]",
                "!titles",
                "!settitle",
                "!category [sub]!list[/sub] ",
                "!identifier [sub]!whatis[/sub]",
                "!pledges",
                "!abandonpledge",
                "!sell",
                "!statues",
                "!statistics [sub]!stats[/sub]",
                "!populations",
                "!flora",
                "!birthrates",
                "!parasites",
                "!payroll",
                "!economics",
                "!business",
                "!joinchateau",
                "!modmessage",
                "!random",
                "!feedback [sub]!suggestion[/sub]",
                "!seteicon",
                "!help [sub]!commands[/sub]",
                "!botinfo",
                "!uptime"
            };

            List<string> recoveryCommands = new List<string>()
            {
                "!cleanse",
                "!detox",
                "!purge",
                "!rest",
                "!wash"
            };

            List<string> roomCommands = new List<string>()
            {
                "!consent [sub]!c, !accept[/sub] ",
                "!no [sub]!refuse, !decline[/sub]",
                "!oops [sub]!o, !withdraw, !cancel[/sub]",
                "!pledge",
                "!fulfill"
            };

            List<string> casualCommands = new List<string>()
            {
                "!kiss",
                "!handhold",
                "!cuddle [sub]!hug[/sub]",
                "!spank",
                "!bully",
                "!boobhat",
                "!lick",
                "!pet",
                "!lap",
                "!sit"
            };

            List<string> involvedCommands = new List<string>()
            {
                "!dressup [sub]!dress[/sub]",
                "!feed",
                "!golden",
                "!pay",
                "!milk",
                "!climax",
                "!climaxfor"
            };

            List<string> commitmentCommands = new List<string>()
            {
                "!petrify",
                "!plant",
                "!objectify",
                "!consume",
                "!mark",
                "!employ [sub]!hire[/sub]",
                "!bond",
                "!entitle",
                "!corrupt",
                "!purify",
                "!breed",
                "!birth",
                "!train"
            };

            List<string> consequenceCommands = new List<string>()
            {
                "!rename",
                "!monsterize",
                "!curse",
                "!infest",
                "!dose",
                "!odorize",
                "!break"
            };

            List<string> dicebotCommands = new List<string>()
            {
                "!joingame",
                "!startgame",
                "!leavegame",
                "!cancelgame",
                "!gamestatus",
                "!gamecommand [sub]!gc, !g[/sub]",
                "!showgames",
                "!roll",
                "!rolltable",
                "!showlastroll",
                "!coinflip",
                "!fitd",
                "!tipdie",
                "!rock",
                "!paper",
                "!scissors",
                "!lizard",
                "!spock",
                "!fen"
            };
            string messageText = "These are all of the commands native to the [user]Chateau Contract[/user] bot, as of [b]June 29th 2026.[/b] For detailed description of their use, please see the [user]Chateau Contract[/user] profile or use !help [command] Commands in subtext are alternate names of the same command - all documentation will be for the first listed names.\n\n" +
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
