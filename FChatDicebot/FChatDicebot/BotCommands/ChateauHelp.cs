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
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            List<string> generalCommands = new List<string>() 
            { 
                "!dossier",
                "!botinfo",
                "!uptime",
                "!help",
                "!category",
                "!identifier",
                "!joinchateau",
                "!modmessage"
            };

            List<string> roomCommands = new List<string>()
            {
                "!consent"
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
                "!golden"
            };

            List<string> commitmentCommands = new List<string>()
            {
                "!monsterize",
                "!petrify",
                "!plant",
                "!objectify",
                "!consume"
            };

            List<string> consequenceCommands = new List<string>()
            {
                "!rename"
            };
            string messageText = "These are all of the commands native to the [user]Chateau Contract[/user] bot, as of [b]September 24th 2023.[/b] For detailed description of their use, please see the [user]Chateau Contract[/user] profile.\n\n" +
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
                    "None yet :)\n";
            }

            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(messageText + "\nMost of [user]Chateau Contract[/user]'s functions are designed for use in the [session=Château Contract]adh-ac1885cd73f31adfaefb[/session] channel. Be sure to !joinchateau if you plan to stick around ♥", characterName);
            }
            else
            {
                bot.SendMessageInChannel(messageText, channel);
            }
        }
    }
}
