using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class Help : ChatBotCommand
    {
        public Help()
        {
            Name = "help";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string helpLocation = Utils.IsDiscordMessage(address) ? "see the website https://monstergenerator.javissoft.com/Home/DiceBot" : "see the profile [user]" + bot.AccountSettings.CharacterName + "[/user]";

            string messageText = "[b]General Commands:[/b] [i]Can be sent to " + bot.AccountSettings.CharacterName + " as a DM (Does not require channel)[/i]\n" +
                    "!roll, !fitd, !showlastroll, !coinflip, !fen, !joinchannel, !botinfo, !uptime, !help, !directory, !rachel\n" +
                    "!savetable, !savetablesimple, !deletetable, !tableinfo, !mytables, !showtables, !savecustomdeck, !savecustomdecksimple, !showdecks, !mydecks, !slotsinfo,\n" +
                    "!deletepotion, !showpotions, !tablejson, !deckjson\n" +
                    "[i]Requires channel[/i]\n" +
                    "!leavethischannel, !drawcard, !resetdeck, !shuffledeck, !shufflediscardintodeck, !endhand, !showhand, !showcardpiles, !movecard\n" +
                    "!discardcard, !takefromdiscard, !deckinfo, !playcard, !takefromplay, !discardfromplay, !playfromdiscard, !hidecard, !revealcard\n" +
                    "!register, !addchips, !showchips, !givechips, !bet, !claimpot, !removepile, !takechips, !redeemchips, !removechips\n" +
                    "!rolltable, !slots, !slotsinfo, !work, !showprofile, !potionjson, !showpotionprices\n" +
                    "!generatemonster, !showmonster\n" +
                    "!generatepotion, !generatepotioninfo, !revealpotion, !droppotion, !savepotion,\n" +
                    "!joingame, !leavegame, !cancelgame, !startgame, !gamestatus, !gamecommand, !gc, !g, !showgames\n" +
                    "[b]Channel Op only Commands:[/b]\n" +
                    "!setstartingchannel, !updatesetting, !viewsettings, !removefromgame, !addtogame, !removeallchipsoveramount\n" +
                    "!savetable, !savetablesimple, !deletetable, !tableinfo, !mytables, !showtables, !savecustomdeck, !mydecks, !savecustomdecksimple, !savejobslist, !deletejobslist\n" +
                    "!tablejson, !deckjson, !jobslistjson\n" +
                    "For full information on commands, " + helpLocation + ".";

            if(Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, command.characterName))
            {
                messageText += "\n[b]Admin only Commands [/b](no channel req except testviewdeck and testops)\n" +
                    "!TestChar, !TestOps, !TestViewDeck,\n!SendToChannel, !SendAllChannels, !SetStatus, !GenerateChipsCode, !UpdateSettingAll, !TestExtractPotions, " +
                    "!RemoveOldData, !TestSlotRolls, !TestVcConnection, !ForceGiveChips, !TestSetHand, !testresetcooldowns, !testkiss, !unlockdice\n" +
                    "!RestrictChips, !removeallpiles, !AuditChannels, !ShowChannelsJoined, !ViewStartupChannels";
            }

            if (!commandController.MessageCameFromChannel(address))
            {
                bot.SendPrivateMessage(messageText + "\nMost of [user]" + bot.AccountSettings.CharacterName + "[/user]'s functions require it to be in a [b]channel[/b]. If you don't have a channel with the bot in it, use !directory to find a suitable channel.", address);
            }
            else
            {
                bot.SendMessageInChannel(messageText, address);
            }
        }
    }
}
