using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//NOTE: game currently unfinished and does not function
namespace FChatDicebot.DiceFunctions
{
    public class PrizeRoll : IGame
    {
        public string GetGameName()
        {
            return "PrizeRoll";
        }

        public int GetMaxPlayers()
        {
            return 20;
        }

        public int GetMinPlayers()
        {
            return 1;
        }

        public bool AllowAnte()
        {
            return false;
        }

        public bool UsesFlatAnte()
        {
            return false;
        }

        public bool KeepSessionDefault()
        {
            return false;
        }

        public int GetMinimumMsBetweenGames()
        {
            return 1 * 60 * 1000; //1 minute test start
        }

        public string GetGameHelp()
        {
            string thisGameCommands = "(none)";
            string thisGameStartupOptions = "(none)" +
                "\nThe default rules are: (this game is unfinished and will not function)";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, true, false);
        }

        public string GetStartingDisplay()
        {
            return "~Prize Roll~";
        }

        public string GetEndingDisplay()
        {
            return "";
        }

        public string GameStatus(GameSession session)
        {
            return "";
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            messageString = "";
            return true;
        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            return "";
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            return "(game run cancelled - game unfinished)";
            List<string> currentPlayerNames = new List<string>();

            foreach(string s in playerNames)
            {
                currentPlayerNames.Add(s);
            }

            string outputString = "";
            bool highestRollFound = false;
            bool addedAntes = false;

            List<string> smallPrizePlayerNames = new List<string>();

            while (!highestRollFound)
            {
                int highestRoll = 0;
                List<DiceRoll> finishedRolls = new List<DiceRoll>();

                foreach (string s in currentPlayerNames)
                {
                    DiceRoll d = new DiceRoll(s, session.ChannelId, diceBot) { DiceRolled = 1, DiceSides = 100 };

                    d.Roll(r);

                    finishedRolls.Add(d);

                    if (d.Total > 49)
                        smallPrizePlayerNames.Add(s);
                }

                for (int i = 0; i < currentPlayerNames.Count; i++)
                {
                    if (!string.IsNullOrEmpty(outputString))
                    {
                        outputString += "\n";
                    }

                    string betstring = "";

                    if(!addedAntes && session.Ante > 0)
                    {
                        betstring = diceBot.BetChips(currentPlayerNames[i], session.ChannelId, session.Ante, false) + "\n";
                    }

                    string thisPlayerRollString = betstring + "[i]" + Utils.GetCharacterUserTags(currentPlayerNames[i]) + "[/i] rolling: " + finishedRolls[i].ResultString();
                    outputString += thisPlayerRollString;

                    if (highestRoll < finishedRolls[i].Total)
                    {
                        highestRoll = (int) finishedRolls[i].Total;
                    }
                }

                addedAntes = true;

                //reroll if there's a tie
                if(finishedRolls.Where(a => a.Total == highestRoll).Count() > 1)
                {
                    outputString += "\n[color=yellow]Tie roll![/color] Rerolling...";
                    //get indexes of highest rollers and remove everyone else
                    List<int> removeIndex = new List<int>();
                    for(int i =0; i < currentPlayerNames.Count; i++)
                    {
                        if(finishedRolls[i].Total != highestRoll)
                        {
                            removeIndex.Add(i);
                        }
                    }
                    if(removeIndex.Count > 0)
                    {
                        removeIndex = removeIndex.OrderByDescending(a => a).ToList();
                        foreach(int removed in removeIndex)
                        {
                            currentPlayerNames.RemoveAt(removed);
                            finishedRolls.RemoveAt(removed);
                        }
                    }
                }
                    //end the loop because a highest roll was found
                else
                {
                    highestRollFound = true;

                    DiceRoll highestDiceRoll = finishedRolls.FirstOrDefault(a => a.Total == highestRoll);
                    int highestDiceRollIndex = finishedRolls.IndexOf(highestDiceRoll);

                    string playerNameWinner = currentPlayerNames[highestDiceRollIndex];

                    string finishingPrizeString = "";
                    if(session.Ante > 0)
                    {
                        finishingPrizeString = "\n" + diceBot.ClaimPot(playerNameWinner, session.ChannelId, 1);//false, false);
                    }

                    finishingPrizeString += "\n" + diceBot.AddChips(playerNameWinner, session.ChannelId, 1000, false);

                    smallPrizePlayerNames.Remove(playerNameWinner);

                    if(smallPrizePlayerNames.Count > 0 )
                    {
                        finishingPrizeString += "\n[b]Prizes for rolling 50 or more:[/b]";
                        foreach (string pName in smallPrizePlayerNames)
                        {
                            if (pName != playerNameWinner)
                            {
                                finishingPrizeString += "\n" + diceBot.AddChips(pName, session.ChannelId, 200, false);
                            }
                        }
                    }


                    outputString += "\n[b]" + Utils.GetCharacterUserTags(playerNameWinner) + " wins![/b]" + finishingPrizeString;
                }
            }

            session.State = DiceFunctions.GameState.Finished;

            return outputString;
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {

        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, GameSession session, string[] terms, string[] rawTerms)
        {
            return GetGameName() + " has no valid GameCommands";
        }
    }
}
