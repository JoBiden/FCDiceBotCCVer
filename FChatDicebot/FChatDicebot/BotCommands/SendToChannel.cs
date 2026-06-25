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
    public class SendToChannel : ChatBotCommand
    {
        public SendToChannel()
        {
            Name = "sendtochannel";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string resultMessageString = "Error: Command requires at least two parameters.";

            if (rawTerms.Length >= 2)
            {
                string channelIdToSend = rawTerms[0];
                string outputString = Utils.GetFullStringOfInputs(rawTerms);
                outputString = outputString.Replace(channelIdToSend, "").Trim();

                bool sendToChannel = false;

                channelIdToSend = commandController.GetChannelFromInputs(rawTerms, out string errorMessage);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    //outputString = errorMessage;
                    resultMessageString = errorMessage;
                }
                else
                {
                    sendToChannel = true;
                    resultMessageString = "[b][ADMIN] Sending message to channel (" + channelIdToSend + "): [/b]" + outputString;
                }


                if (sendToChannel)
                {
                    bot.SendMessageInChannel(outputString, new MessageAddress() { channel = channelIdToSend, guild = address.guild, character = address.character });
                }
            }

            if (!commandController.MessageCameFromChannel(address))
            {
                bot.SendPrivateMessage(resultMessageString, address);
            }
            else
            {
                bot.SendMessageInChannel(resultMessageString, address);
            }


        }
    }
}
