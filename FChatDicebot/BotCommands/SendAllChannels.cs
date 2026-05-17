using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class SendAllChannels : ChatBotCommand
    {
        public SendAllChannels()
        {
            Name = "sendallchannels";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string messageString = "[b][DICE BOT ADMIN MESSAGE]:[/b] " + Utils.GetFullStringOfInputs(rawTerms);

            string resultMessageString = "[b][ADMIN] Sending message to all occupied channels: [/b]" + messageString;
            
            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(resultMessageString, characterName);
            }
            else
            {
                bot.SendMessageInChannel(resultMessageString, channel);
            }

            foreach (string channelCode in bot.ChannelsJoined)
            {
                bot.SendMessageInChannel(messageString, channelCode);
            }
        }
    }
}
