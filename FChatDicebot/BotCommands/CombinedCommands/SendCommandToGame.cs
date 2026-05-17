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
    public class SendCommandToGame
    {
        public static void Run(BotMain bot, BotCommandController commandController,
            string[] rawTerms, string[] terms, string characterName, string channel,
            UserGeneratedCommand command, string originCommandName, string gameName)
        {
            //sent from dice bot in private - figure out the channel and game session

            List<GameSession> possibleSessions = bot.DiceBot.GameSessions.Where(a => a.CurrentGame.GetGameName() == gameName 
                && a.Players.Count(b => b == characterName) > 0).ToList();

            //apply this command to existing game
            string responseMessage = "";
            if (possibleSessions == null || possibleSessions.Count == 0)
            {
                responseMessage = "Failed: no relevant game session found";
            }
            else if (possibleSessions.Count > 1)
            {
                responseMessage = "Failed: Multiple game sessions of this type were found. You may only play one session at a time for games which require whispered commands";
            }
            else
            {
                GameSession relevantSession = possibleSessions[0];

                switch(gameName)
                {
                    case "RockPaperScissors":
                        {
                            GameSymbol usedSymbol = GameSymbol.NONE;
                            //get symbol from inputs
                            if (originCommandName == "rock")
                                usedSymbol = GameSymbol.Rock;
                            else if (originCommandName == "paper")
                                usedSymbol = GameSymbol.Paper;
                            else if (originCommandName == "scissors")
                                usedSymbol = GameSymbol.Scissors;
                            else if (originCommandName == "lizard")
                                usedSymbol = GameSymbol.Lizard;
                            else if (originCommandName == "spock")
                                usedSymbol = GameSymbol.Spock;

                            RockPaperScissors sessionOfRps = (RockPaperScissors)relevantSession.CurrentGame;
                            sessionOfRps.ApplySymbolToPlayer(bot, relevantSession, characterName, usedSymbol);
                            responseMessage += "You've sent a " + sessionOfRps.GetThrowString(usedSymbol) + "!";
                        }
                        break;
                    case "Mafia":
                        {
                            Mafia sessionOfMafia = (Mafia)relevantSession.CurrentGame;
                            bool secondaryTarget = originCommandName == "usepower2" || originCommandName == "usepowersecondary";
                            responseMessage +=  sessionOfMafia.ActivatePowerAtNight(bot, rawTerms, characterName, relevantSession, secondaryTarget);
                        }
                        break;
                }
                
            }

            if (commandController.MessageCameFromChannel(channel))
            {
                bot.SendMessageInChannel(responseMessage, channel);
            }
            else
            {
                bot.SendPrivateMessage(responseMessage, characterName);
            }
        }
    }
}
