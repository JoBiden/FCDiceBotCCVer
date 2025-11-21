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
    }
}
