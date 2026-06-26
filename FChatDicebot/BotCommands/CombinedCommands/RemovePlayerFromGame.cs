using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands.Base
{
    public class RemovePlayerFromGame
    {
        public static void Run(BotMain bot, BotCommandController commandController,
            string[] rawTerms, string[] terms, MessageAddress address,
            UserGeneratedCommand command, string removedCharacterName, string originCommandName)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            if (thisChannel.AllowGames)
            {
                string messageString = "";

                IGame gametype = commandController.GetGameTypeForCommand(bot.DiceBot, address, terms, out messageString);

                if (gametype != null)
                {
                    GameSession sesh = bot.DiceBot.GetGameSession(address, gametype, false);
                    if (sesh != null)
                    {
                        if (!sesh.Players.Contains(removedCharacterName))
                        {
                            messageString = TextFormat.GetCharacterUserTags(removedCharacterName) + " has not joined " + sesh.CurrentGame.GetGameName() + ".";
                        }
                        else
                        {
                            messageString = bot.DiceBot.LeaveGame(new MessageAddress() { character = removedCharacterName, channel = address.channel, guild = address.guild }, gametype);

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

                bot.SendMessageInChannel(messageString, address);
            }
            else
            {
                bot.SendMessageInChannel(originCommandName + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
        }
    }
}
