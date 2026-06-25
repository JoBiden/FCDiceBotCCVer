using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class GameHelp : ChatBotCommand
    {
        public GameHelp()
        {
            Name = "gamehelp";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            bool fromChannel = commandController.MessageCameFromChannel(address);

            string messageString = "";

            if (fromChannel && thisChannel.AllowGames || !fromChannel)
            {
                IGame gametype = commandController.GetGameTypeForCommand(bot.DiceBot, address, terms, out messageString);

                if (gametype != null)
                {
                    messageString = gametype.GetGameHelp();
                }
            }
            else
            {
                messageString = Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.";
            }

            if(fromChannel)
            {
                bot.SendMessageInChannel(messageString, address);
            }
            else
            {
                bot.SendPrivateMessage(messageString, address);
            }
        }
    }
}
