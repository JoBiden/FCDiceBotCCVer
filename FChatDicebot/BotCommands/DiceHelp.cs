using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;

namespace FChatDicebot.BotCommands
{
    public class DiceHelp : ChatBotCommand
    {
        public DiceHelp()
        {
            Name = "dicehelp";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string messageText = "These commands are native to the dice bot that Chateau Contract is built on top of. They should still work, but Contract does not maintain them, so no promises!\n" + 
                    "[b]General Commands:[/b] [i]Does not require channel[/i]\n" +
                    "!roll, !fitd, !coinflip, !fen, !joinchannel\n!botinfo, !uptime, !help, !rachel\n" +
                    "!savetable, !savetablesimple, !deletetable, !tableinfo, !mytables, !showtables, !savecustomdeck, !savecustomdecksimple, !slotsinfo\n" +
                    "[i]Requires channel[/i]\n" +
                    "!drawcard, !resetdeck, !shuffledeck, !shufflediscardintodeck, !endhand, !showhand, !showcardpiles, !movecard\n" +
                    "!discardcard, !takefromdiscard, !deckinfo, !playcard, !takefromplay, !discardfromplay, !playfromdiscard, !hidecard, !revealcard\n" +
                    "!register, !addchips, !showchips, !givechips, !bet, !claimpot, !removepile, !takechips, !redeemchips, !removechips\n" +
                    "!rolltable, !generatepotion, !generatepotioninfo, !showpotion, !slots, !slotsinfo\n" +
                    "!leavethischannel\n" +
                    "!joingame, !leavegame, !cancelgame, !startgame, !gamestatus, !gamecommand, !gc, !g\n" +
                    "[b]Channel Op only Commands:[/b]\n" +
                    "!removeallpiles, !setstartingchannel, !updatesetting, !viewsettings, !removefromgame, !addtogame\n" +
                    "!savetable, !savetablesimple, !deletetable, !tableinfo, !mytables, !showtables, !savecustomdeck, !savecustomdecksimple\n" +
                    "For full information on dice commands, see the profile [user]Dice Bot[/user]. [b]The profile is maintained for the core Dice Bot and not all of the commands described may be present in the Chateau Contract bot.[/b]";

            if(Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, command.characterName))
            {
                messageText += "\n[b]Admin only Commands [/b](no channel req except testviewdeck and testops)\n" +
                    "!TestChar, !TestOps, !TestViewDeck,\n!SendToChannel, !SendAllChannels, !SetStatus, !GenerateChipsCode, !UpdateSettingAll, " +
                    "!RemoveOldData, !TestSlotRolls, !TestVcConnection, !ForceGiveChips, !TestSetHand";
            }

            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(messageText + "\nMost of [user]Dice Bot[/user]'s functions require it to be in a [b]channel[/b]. You can invite it to a private channel and then use [b]!joinchannel[/b] to test commands.", characterName);
            }
            else
            {
                bot.SendMessageInChannel(messageText, channel);
            }
        }
    }
}
