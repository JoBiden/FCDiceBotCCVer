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
    public class GameStatus : ChatBotCommand
    {
        public GameStatus()
        {
            Name = "gamestatus";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            if (thisChannel.AllowGames)
            {
                string messageString = "";

                if (terms.Length < 1)
                {
                    messageString = "Error: This command requires a game name.";
                }
                else
                {
                    IGame gametype = commandController.GetGameTypeForCommand(bot.DiceBot, address, terms, out messageString);

                    if(gametype != null)
                    {
                        GameSession sesh = bot.DiceBot.GetGameSession(address, gametype, false);

                        if (sesh != null)
                        {
                            double secondsRemain = bot.DiceBot.GetSecondsRemainingOnCountdownTimer(address, gametype.GetGameName());

                            messageString = sesh.GetStatus(secondsRemain);
                        }
                        else
                        {
                            messageString = "Game session for " + gametype.GetGameName() + " not found or created.";
                        }
                    }
                }

                bot.SendMessageInChannel(messageString, address);
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
        }
    }
}
