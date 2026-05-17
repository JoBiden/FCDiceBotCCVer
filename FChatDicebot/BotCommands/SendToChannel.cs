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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string channelIdToSend = rawTerms[0];
            string channelLower = channelIdToSend.ToLower();
            string outputString = Utils.GetFullStringOfInputs(rawTerms);
            outputString = outputString.Replace(channelIdToSend, "").Trim();
            bool sendToChannel = false;

            switch(channelLower)
            {
                case "casino":
                case "vccasino":
                    channelIdToSend = BotMain.CasinoChannelId;// "adh-3fe0682b9b6bbe0acb62";
                    break;
                case "breakerworld":
                case "breaker":
                    channelIdToSend = BotMain.BreakerWorldChannelId;
                    break;
                case "kowloon":
                case "kcc":
                    channelIdToSend = BotMain.KowloonChannelId;
                    break;
                case "fateroom":
                case "fate":
                    channelIdToSend = BotMain.SevenMinutesFateRoomId;
                    break;
                case "fategu":
                case "suitengu":
                    channelIdToSend = BotMain.SuitenGuFateRoomId;
                    break;
                case "chessclub":
                case "chess":
                    channelIdToSend = BotMain.ChessClubChannelId;
                    break;
                case "testroom":
                case "testdicebot":
                case "test":
                    channelIdToSend = BotMain.TestDicebotChannelId;
                    break;
            }

            if (terms.Length < 2)
                outputString = "Error: not enough parameters.";
            else if (channelIdToSend == null || channelIdToSend.Length < 5)
                outputString = "Error: invalid channel id. (casino, breakerworld, kowloon, fateroom, [channel id])";
            else
                sendToChannel = true;

            string resultMessageString = "[b][ADMIN] Sending message to channel (" + channelIdToSend + "): [/b]" + outputString;
            
            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(resultMessageString, characterName);
            }
            else
            {
                bot.SendMessageInChannel(resultMessageString, channel);
            }

            if(sendToChannel)
            { 
                bot.SendMessageInChannel(outputString, channelIdToSend);
            }

        }
    }
}
