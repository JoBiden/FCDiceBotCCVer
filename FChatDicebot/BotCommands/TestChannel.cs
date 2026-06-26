using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands
{
    public class TestChannel : ChatBotCommand
    {
        public TestChannel()
        {
            Name = "testchannel";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings chan = bot.GetChannelSettings(address);
            string responseMessage = "";
            string channelNameLow  = address.channel.ToLower();
            if (channelNameLow == BotMain.CasinoChannelId)
            {
                responseMessage = "Match vc casino.";
            }
            if (channelNameLow == BotMain.ChessClubChannelId)
            {
                responseMessage = "Match vc chess club channel.";
            }
            if (channelNameLow == BotMain.TestDicebotChannelId)
            {
                responseMessage = "Match test dicebot channel.";
            }
            if (channelNameLow == BotMain.BreakerWorldChannelId)
            {
                responseMessage = "Match vc breaker world channel.";
            }
            if (channelNameLow == BotMain.SevenMinutesFateRoomId)
            {
                responseMessage = "Match seven minutes fate room channel.";
            }

            if (!chan.AllowChips)
            {
                responseMessage += "\nChips are not allowed under the settings for this channel.";
            }
            if (!chan.AllowGames)
            {
                responseMessage += "\nGames are not allowed under the settings for this channel.";
            }
            if (!chan.AllowSlots)
            {
                responseMessage += "\nSlots are not allowed under the settings for this channel.";
            }
            bot.SendMessageInChannel(responseMessage, address);
        }
    }
}
