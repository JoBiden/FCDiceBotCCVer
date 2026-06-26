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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string messageString = "[b]" + DiceBot.DiceBotCharacter + " Admin Message:[/b] " + Utils.GetFullStringOfInputs(rawTerms);

            string resultMessageString = "[b](Admin) Sending message to all occupied channels: [/b]" + messageString;

            if (!commandController.MessageCameFromChannel(address))
            {
                bot.SendPrivateMessage(resultMessageString, address);
            }
            else
            {
                bot.SendMessageInChannel(resultMessageString, address);
            }

            foreach (string channelCode in bot.ChannelsJoined)
            {
                bot.SendMessageInChannel(messageString, address);
            }
        }
    }
}
