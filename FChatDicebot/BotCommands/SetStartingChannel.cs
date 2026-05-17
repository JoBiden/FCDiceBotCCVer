using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands
{
    public class SetStartingChannel : ChatBotCommand
    {
        public SetStartingChannel()
        {
            Name = "setstartingchannel";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedChannels;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings existing = bot.GetChannelSettings(channel);

            string sendMessage = "[b]Added[/b] " + channel + " to list of startup channels.";

            if (existing.StartupChannel)
            {
                sendMessage = "[b]Removed[/b] " + channel + " from list of startup channels.";
            }
            existing.StartupChannel = !existing.StartupChannel;

            commandController.SaveChannelSettingsToDisk();

            bot.SendMessageInChannel(sendMessage, channel);
        }
    }
}