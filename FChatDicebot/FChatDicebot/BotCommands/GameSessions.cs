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
    public class GameSessions : ChatBotCommand
    {
        public GameSessions()
        {
            Name = "gamesessions";
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

                List<GameSession> channelGameSessions = bot.DiceBot.GameSessions.Where(a => a.GetChannelKey().ToLower() == address.GetChannelKey().ToLower()).ToList();

                if(channelGameSessions.Count == 0)
                {
                    messageString = "No game sessions active for this channel.";
                }
                else
                {
                    messageString = "Current game sessions active in this channel:";
                    foreach(GameSession session in channelGameSessions)
                    {
                        if(!string.IsNullOrEmpty(messageString))
                        {
                            messageString += "\n";
                        }
                        messageString += session.GetStatus();
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
