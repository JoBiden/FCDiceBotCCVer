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
    //NOTE: admin only used to manually edit settings for every channel at once
    public class ShowChannelsJoined : ChatBotCommand
    {
        public ShowChannelsJoined()
        {
            Name = "showchannelsjoined";
            RequireBotAdmin = true;
            RequireChannelAdmin = true;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedChannels;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string channels = Utils.PrintList(bot.ChannelsJoined);
            string output = "List of flist channels joined (by receieving CDS): " + channels;

            if (commandController.MessageCameFromChannel(address))
                bot.SendMessageInChannel(output, address);
            else
                bot.SendPrivateMessage(output, address);
        }
    }

}