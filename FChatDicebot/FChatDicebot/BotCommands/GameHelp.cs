using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            bool fromChannel = commandController.MessageCameFromChannel(channel);

            string messageString = "";

            if (fromChannel && thisChannel.AllowGames || !fromChannel)
            {
                IGame gametype = commandController.GetGameTypeForCommand(bot.DiceBot, channel, terms, out messageString);

                if (gametype != null)
                {
                    messageString = gametype.GetGameHelp();
                }
            }
            else
            {
                messageString = Name + " is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.";
            }

            if(fromChannel)
            {
                bot.SendMessageInChannel(messageString, channel);
            }
            else
            {
                bot.SendPrivateMessage(messageString, characterName);
            }
        }
    }
}
