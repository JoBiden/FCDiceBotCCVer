using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;

namespace FChatDicebot.BotCommands
{
    public class ChateauHelp : ChatBotCommand
    {
        public ChateauHelp()
        {
            Name = "help";
            Aliases = new string[] { };
            Category = "General";
            ShortDescription = "Get help about bot commands";
            LongDescription = "View general help for all commands, or get detailed help for a specific command.\n\nWithout arguments: Shows a categorized list of all available commands.\nWith a command name: Shows detailed help for that specific command including usage, cooldown, and related commands.";
            Usage = "!help\nor\n!help [commandname]";
            RelatedCommands = new string[] { "botinfo", "dossier" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
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
                "!dossier [sub]!profile[/sub] ",
                "!work [sub]!w[/sub]",
                "!volunteer [sub]!v[/sub]",
                "!bank",
                "!botinfo",
                "!uptime",
                "!help",
                "!category [sub]!list[/sub] ",
                "!identifier [sub]!whatis[/sub]",
                "!joinchateau",
                "!modmessage",
                "!setmark"
            };

            List<string> roomCommands = new List<string>()
            {
                "!consent [sub]!c[/sub] "
            };

            List<string> casualCommands = new List<string>()
            {
                "!kiss",
                "!handhold",
                "!cuddle",
                "!spank",
                "!bully"
            };

            List<string> involvedCommands = new List<string>()
            {
                "!dressup",
                "!feed",
                "!golden",
                "!pay"
            };

            List<string> commitmentCommands = new List<string>()
            {
                "!monsterize",
                "!petrify",
                "!plant",
                "!objectify",
                "!consume",
                "!mark",
                "!employ",
                "!bond"
            };

            List<string> consequenceCommands = new List<string>()
            {
                "!rename"
            };
            string messageText = "These are all of the commands native to the [user]Chateau Contract[/user] bot, as of [b]November 4th 2025.[/b] For detailed description of their use, please see the [user]Chateau Contract[/user] profile. Commands in subtext are alternate names of the same command - all documentation will be for the first listed names.\n\n" +
                    "[u]Does not require channel[/u]\n" +
                    Utils.sortedListDisplayText(generalCommands) + "\n\n" +         
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
                    "!namechange [old profile in user tag] \"new profile in quotes\" - updates the database to reflect a user who has changed their Flist username. CaSe SeNsItIvE \n";
            }

            if (commandController.MessageCameFromChannel(channel))
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
                sb.Append(" (aliases: ");
                sb.Append(string.Join(", ", cmd.Aliases.Select(a => "!" + a)));
                sb.Append(")");
            }
            else
            {
                sb.Append(" (aliases: none)");
            }
            sb.Append("\n");

            // Category
            if (!string.IsNullOrEmpty(cmd.Category))
            {
                sb.Append("Category: ");
                sb.Append(cmd.Category);
                sb.Append("\n\n");
            }

            // Long description
            if (!string.IsNullOrEmpty(cmd.LongDescription))
            {
                sb.Append(cmd.LongDescription);
                sb.Append("\n\n");
            }
            else if (!string.IsNullOrEmpty(cmd.ShortDescription))
            {
                sb.Append(cmd.ShortDescription);
                sb.Append("\n\n");
            }

            // Usage
            if (!string.IsNullOrEmpty(cmd.Usage))
            {
                sb.Append("[u]Usage:[/u]\n");
                sb.Append(cmd.Usage);
                sb.Append("\n\n");
            }

            // Identifier list (if applicable)
            if (!string.IsNullOrEmpty(cmd.IdentifierCategory))
            {
                List<Identifier> identifiers = MonDB.getIdentifiers(cmd.IdentifierCategory);
                if (identifiers != null && identifiers.Count > 0)
                {
                    sb.Append("[u]Available ");
                    sb.Append(cmd.IdentifierCategory);
                    sb.Append("s:[/u]\n");
                    sb.Append(Utils.sortedListDisplayText(identifiers.Select(i => i.type).ToList()));
                    sb.Append("\n\n");
                }
            }

            // Cooldown information
            if (!string.IsNullOrEmpty(cmd.CooldownDuration))
            {
                sb.Append("[u]Cooldown:[/u] ");
                sb.Append(cmd.CooldownDuration);
                if (!string.IsNullOrEmpty(cmd.CooldownAppliesTo))
                {
                    sb.Append(" (applies to ");
                    sb.Append(cmd.CooldownAppliesTo);
                    sb.Append(")");
                }
                sb.Append("\n\n");
            }
            else
            {
                sb.Append("[u]Cooldown:[/u] None\n\n");
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
