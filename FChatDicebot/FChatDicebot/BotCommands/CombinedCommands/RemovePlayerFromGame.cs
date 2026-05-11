using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands.Base
{
    public class RemovePlayerFromGame
    {
        public static void Run(BotMain bot, BotCommandController commandController, 
            string[] rawTerms, string[] terms, string characterName, string channel, 
            UserGeneratedCommand command, string removedCharacterName, string originCommandName)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            if (thisChannel.AllowGames)
            {
                string messageString = "";

                IGame gametype = commandController.GetGameTypeForCommand(bot.DiceBot, channel, terms, out messageString);

                if (gametype != null)
                {
                    GameSession sesh = bot.DiceBot.GetGameSession(channel, gametype, false);
                    if (sesh != null)
                    {
                        if (!sesh.Players.Contains(removedCharacterName))
                        {
                            messageString = Utils.GetCharacterUserTags(removedCharacterName) + " has not joined " + sesh.CurrentGame.GetGameName() + ".";
                        }
                        else
                        {
                            messageString = bot.DiceBot.LeaveGame(removedCharacterName, channel, gametype);

                            messageString += "\n" + sesh.Players.Count + " / " + sesh.CurrentGame.GetMaxPlayers() + " players ready.";

                            if (sesh.CurrentGame.GetType() == typeof(Roulette))
                            {
                                sesh.RemoveRouletteBet(removedCharacterName);
                            }

                            if (sesh.Players.Count >= sesh.CurrentGame.GetMinPlayers())
                            {
                                messageString += "[b] (Ready to start!)[/b]";
                            }
                        }
                    }
                    else
                    {
                        messageString = "Failed: Game session for " + gametype.GetGameName() + " not found or created.";
                    }
                }

                bot.SendMessageInChannel(messageString, channel);
            }
            else
            {
                bot.SendMessageInChannel(originCommandName + " is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", channel);
            }
        }
    }
}
