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
                "!feedback [sub]!suggestion[/sub]",
                "!setmark",
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
            string messageText = "These are all of the commands native to the [user]Chateau Contract[/user] bot, as of [b]June 22nd 2026.[/b] For detailed description of their use, please see the [user]Chateau Contract[/user] profile or use !help [command] Commands in subtext are alternate names of the same command - all documentation will be for the first listed names.\n\n" +
                    "[u]Does not require channel[/u]\n" +
                    Utils.sortedListDisplayText(generalCommands) + "\n" +
                    "[i]Recovery Commands:[/i] " + Utils.sortedListDisplayText(recoveryCommands) + "\n\n" +
                    "[u]Requires channel[/u]\n" +
                    Utils.sortedListDisplayText(roomCommands) + "\n" +
                    "[i]Casual Interactions:[/i] " + Utils.sortedListDisplayText(casualCommands) + "\n" +
                    "[i]Involved Interactions:[/i] " + Utils.sortedListDisplayText(involvedCommands) + "\n" +
                    "[i]Commitment Interactions:[/i] " + Utils.sortedListDisplayText(commitmentCommands) + "\n" +
                    "[i]Consequence Interactions:[/i] " + Utils.sortedListDisplayText(consequenceCommands) + "\n" +

                    "\n[b]Channel Op only Commands:[/b]\n" +
                    "None yet :)\n" +

                    "\nFor full information on dice commands, see the profile [user]Dice Bot[/user]. [color=yellow]The [user]Dice Bot[/user] profile is maintained for the core [user]Dice Bot[/user] and not all of the commands described may be present in the Chateau Contract bot.[/color]\n";

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

            // Command name and aliases
            sb.Append("[b]!");
            sb.Append(cmd.Name);
            sb.Append("[/b]");

            if (cmd.Aliases != null && cmd.Aliases.Length > 0)
            {
                sb.Append("[sub]");
                sb.Append(string.Join(", ", cmd.Aliases.Select(a => "!" + a)));
                sb.Append("[/sub]");
            }
            sb.AppendLine("");

            // Category
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

            // Usage
            if (!string.IsNullOrEmpty(cmd.Usage))
            {
                sb.AppendLine("[u]Usage:[/u]");
                sb.AppendLine(cmd.Usage);
                sb.AppendLine("");
            }

            // Identifier list (if applicable)
            if (!string.IsNullOrEmpty(cmd.IdentifierCategory))
            {
                List<Model.Identifier> identifiers = MonDB.getIdentifiers(cmd.IdentifierCategory);
                if (identifiers != null && identifiers.Count > 0)
                {
                    sb.Append("[u]Available ");
                    sb.Append(cmd.IdentifierCategory.EndsWith("y") ? (cmd.IdentifierCategory.TrimEnd('y') + "ie") : cmd.IdentifierCategory);
                    sb.AppendLine("s:[/u]");
                    sb.AppendLine(Utils.sortedListDisplayText(identifiers.Select(i => i.type).ToList()));
                    sb.AppendLine("");
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
                sb.Append("[u]Cooldown:[/u] ");
                sb.Append(cooldownDuration);
                if (!string.IsNullOrEmpty(cooldownAppliesTo))
                {
                    sb.Append(" (applies to ");
                    sb.Append(cooldownAppliesTo);
                    sb.Append(")");
                }
                if (cmd.Category == "Casual Interaction")
                {
                    sb.Append(" [sub](Casual command cooldowns are only for incrementing dossier counts, and can still be performed at any time)[/sub]");
                }
                else if (cmd.Category == "Involved Interaction")
                {
                    sb.Append(" [sub](Involved command cooldowns are only for incrementing dossier counts, and can still be performed at any time[/sub])");
                }
                sb.AppendLine("\n");
            }

            // Channel requirement
            if (cmd.RequireChannel)
            {
                sb.Append("[u]Channel Required:[/u] Yes\n\n");
            }
            else
            {
                sb.Append("[u]Channel Required:[/u] No\n\n");
            }

            // Related commands
            if (cmd.RelatedCommands != null && cmd.RelatedCommands.Length > 0)
            {
                sb.Append("[u]Related Commands:[/u] ");
                sb.Append(string.Join(", ", cmd.RelatedCommands.Select(c => "!" + c)));
                sb.Append("\n");
            }

            return sb.ToString();
        }
    }
}
