using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class HighRoll : IGame
    {
        public string GetGameName()
        {
            return "HighRoll";
        }

        public int GetMaxPlayers()
        {
            return 20;
        }

        public int GetMinPlayers()
        {
            return 2;
        }

        public bool AllowAnte()
        {
            return true;
        }

        public bool UsesFlatAnte()
        {
            return true;
        }

        public bool KeepSessionDefault()
        {
            return false;
        }

        public int GetMinimumMsBetweenGames()
        {
            return 0;
        }

        public string GetGameHelp()
        {
            string thisGameCommands = "setante #, setsplit split:#-# (sets the pot split), setsides # (set the # of sides on the die)";
            string thisGameStartupOptions = "# (sets session ante), split:#-# (sets the pot split), sides:# (set the # of sides on the die)\n" +
                "Setting the split: use split:70-30 to set to 70% first place 30% second place. Can set a prize for the top 2-3 rolls to #(1st)-#(2nd)-#(3rd). Totals up to 100, up to 3 prizes" +
                "\nThe default rules are: Highest roller on 1d100 wins (100% of) the pot, no ante, reroll ties";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, false, false);
        }

        public string GetStartingDisplay()
        {
            return FChatDicebot.TextFormat.Emoji("dbhighroll1") + FChatDicebot.TextFormat.Emoji("dbhighroll2");
        }

        public string GetEndingDisplay()
        {
            return "";
        }

        public string GameStatus(GameSession session)
        {
            return "Die used: [b]1d" + session.HighRollData.DieSides +"[/b], Prizes: [b]" + (session.HighRollData.PotSplits.Count < 2? "Winner takes all.": string.Join("-", session.HighRollData.PotSplits)) +"[/b]";
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            messageString = "";
            if (!session.HighRollData.RulesSet)
            {
                messageString = SetSplits(terms, session);

                string dieString = "";
                foreach (string s in terms)
                {
                    if (s.StartsWith("sides:"))
                    {
                        string justNumber = s.Replace("sides:", "");
                        int dieSides = 0;
                        int.TryParse(justNumber, out dieSides);
                        if (dieSides >= 2 && dieSides < DiceBot.MaximumSides)
                        {
                            session.HighRollData.DieSides = dieSides;
                            dieString = "Set the die type for this session to [b]1d" + dieSides + "[/b]. ";
                        }
                        else
                        {
                            //returnString = "Failed: no positive die sides amount was found (must be between 2 and " + DiceBot.MaximumSides + ").";
                        }
                    }
                    if(string.IsNullOrEmpty(dieString))
                    {

                    }
                }

                messageString = dieString + "(ante: " + session.Ante + ") " + messageString;
            }

            return true;
        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            return "";
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            int dieSides = session.HighRollData.DieSides;

            session.HighRollData.Scores = new List<HighRollScore>();
            foreach(string s in playerNames)
            {
                session.HighRollData.Scores.Add(new HighRollScore() { PlayerName = s });
            }

            string outputString = "";

            // Commit every player's stake into the bag up front (affordability was checked at
            // join; if anyone can no longer cover it, abort and refund rather than half-collect).
            if (session.HasWagerStakes)
            {
                string commitError;
                if (!Wager.WagerGameSupport.CommitAllStakes(diceBot, session, out commitError))
                {
                    session.State = GameState.Unstarted;
                    return "Session for HighRoll failed to start: " + commitError;
                }
            }

            foreach (HighRollScore playerScore in session.HighRollData.Scores)
            {
                DiceRoll d = new DiceRoll(new MessageAddress() { character = playerScore.PlayerName, channel = session.ChannelId, guild = session.GuildId }, botMain) { DiceRolled = 1, DiceSides = dieSides };
                //playerScore.PlayerName, session.ChannelId, diceBot)

                d.Roll(r);
                playerScore.FirstDiceRoll = d;
            }

            for (int i = 0; i < session.HighRollData.Scores.Count; i++)
            {
                if (!string.IsNullOrEmpty(outputString))
                {
                    outputString += "\n";
                }

                HighRollScore currentPlayerScore =  session.HighRollData.Scores[i];

                // Stakes were already committed up front by CommitAllStakes.
                string betstring = "";
                bool successfulBet = true;

                string thisPlayerRollString = (string.IsNullOrEmpty(betstring) ? TextFormat.GetCharacterUserTags(currentPlayerScore.PlayerName) : betstring) ;

                if(successfulBet)
                {
                    thisPlayerRollString += " [i]Rolling...[/i] " + currentPlayerScore.FirstDiceRoll.ResultString();
                    currentPlayerScore.PlayerScore = currentPlayerScore.FirstDiceRoll.Total;
                }
                else
                {
                }

                outputString += thisPlayerRollString;
            }

            session.HighRollData.Scores = session.HighRollData.Scores.Where(a => !a.CannotAfford).ToList();
            if(session.HighRollData.Scores.Count() == 0)
            {
                session.State = DiceFunctions.GameState.Finished;
                outputString += "\nFailed: There are too few people able to afford the bet for Highroll: Cancelling session";
                return outputString;
            }
            //handle ties
            bool tie = TiedRolls(session);
            int tieRollsCount = 0;
            while(tie)
            {
                tieRollsCount += 1;
                if (tieRollsCount > 10)
                    break;

                outputString += "\n[color=yellow]Tie rolls![/color] Rerolling...";
                var groupedRollsByNumber = session.HighRollData.Scores
                        .GroupBy(card => card.PlayerScore);
                var twoPlusSameNumber = groupedRollsByNumber.Where(group => group.Count() >= 2);

                string allAdditonalRolls = "";
                foreach(var groupOfTwoPlus in twoPlusSameNumber)
                {
                    foreach(var playerScore in groupOfTwoPlus)
                    {
                        MessageAddress playerChipPileAddress = new MessageAddress() { character = playerScore.PlayerName, channel = session.ChannelId, guild = session.GuildId };
                        DiceRoll diceRoll = new DiceRoll(playerChipPileAddress, botMain) { DiceRolled = 1, DiceSides = dieSides };
                            //playerScore.PlayerName, session.ChannelId, diceBot) { DiceRolled = 1, DiceSides = dieSides };
                        diceRoll.Roll(r);
                        allAdditonalRolls += "\n[i]" + TextFormat.GetCharacterUserTags(playerScore.PlayerName) + "'s score is " + playerScore.PlayerScore + ".[/i] Rolling: " + diceRoll.ResultString();
                        playerScore.PlayerScore += ((double)diceRoll.Total) / Math.Pow(dieSides * 10, tieRollsCount);
                    }
                }
                outputString += allAdditonalRolls;
                tie = TiedRolls(session);
            }

            session.HighRollData.Scores = session.HighRollData.Scores.OrderByDescending(a => a.PlayerScore).ToList();

            string resultsString = "\n[color=yellow][b]Results:[/b][/color]";

            // Award the bag to the top finishers, splitting each currency column by PotSplits.
            Dictionary<string, string> awarded = null;
            if (session.HasWagerStakes)
            {
                var winners = new List<string>();
                for (int i = 0; i < session.HighRollData.Scores.Count && i < session.HighRollData.PotSplits.Count; i++)
                    winners.Add(session.HighRollData.Scores[i].PlayerName);
                awarded = Wager.WagerGameSupport.AwardPotSplit(diceBot, session, winners, session.HighRollData.PotSplits);
            }

            for (int i = 0; i < session.HighRollData.Scores.Count && i < 3; i++)
            {
                string placeString = "first place!";
                switch(i)
                {
                    case 1: placeString = "second place."; break;
                    case 2: placeString = "third place."; break;
                }
                string betstring = "";
                if (awarded != null && awarded.TryGetValue(session.HighRollData.Scores[i].PlayerName, out string won))
                    betstring = " (won " + won + ")";
                resultsString += "\n[b]" + TextFormat.GetCharacterUserTags(session.HighRollData.Scores[i].PlayerName) + " got " + placeString + "[/b]" + betstring;
            }

            if(session.HighRollData.Scores.Count >= 6)
            {
                resultsString += "\n...";
            }
            if (session.HighRollData.Scores.Count >= 5)
            {
                resultsString += "\n[b]" + TextFormat.GetCharacterUserTags(session.HighRollData.Scores[session.HighRollData.Scores.Count - 2].PlayerName) + " got [i]second-to-last[/i] place.[/b]";
            }
            if (session.HighRollData.Scores.Count > 3)//session.HighRollData.PotSplits.Count())
            {
                resultsString += "\n[b]" + TextFormat.GetCharacterUserTags(session.HighRollData.Scores[session.HighRollData.Scores.Count - 1].PlayerName) + " got [i]last[/i] place.[/b]";
            }

            outputString += resultsString;

            session.State = DiceFunctions.GameState.Finished;

            return outputString;
        }

        public bool TiedRolls(GameSession session)
        {
            var groupedRollsByNumber = session.HighRollData.Scores
                    .GroupBy(card => card.PlayerScore);
            var twoPlusSameNumber = groupedRollsByNumber.Where(group => group.Count() >= 2).Count();//.OrderByDescending(a => GetPokerNumberScoreForCard(a.ElementAt(0)));

            return twoPlusSameNumber > 0;
        }

        public string SetSplits(string[] terms, GameSession session)
        {
            string messageString = "";
            session.HighRollData.PotSplits = new List<int>();
            foreach (string s in terms)
            {
                if (s.StartsWith("split:") || s.StartsWith("splits:"))
                {
                    string justSplit = s.Replace("split:", "").Replace("splits:", "");
                    string[] splits = justSplit.Split('-');
                    if (splits != null && splits.Count() > 1)
                    {
                        int runningTotal = 0;
                        int numberSplits = 0;
                        foreach (string qt in splits)
                        {
                            int output = 0;
                            numberSplits += 1;
                            if (numberSplits > 3)
                                break;
                            int.TryParse(qt, out output);
                            output = Utils.Clamp(output, 0, 100);
                            if (output + runningTotal > 100)
                            {
                                output = 100 - runningTotal;
                            }
                            if (output > 0)
                            {
                                runningTotal += output;
                                session.HighRollData.PotSplits.Add(output);
                                if (runningTotal >= 100)
                                    break;
                            }
                        }
                        if(session.HighRollData.PotSplits.Count > 0 && session.HighRollData.PotSplits.Sum() < 100)
                        {
                            session.HighRollData.PotSplits[0] += 100 - session.HighRollData.PotSplits.Sum();
                        }
                    }
                }
            }
            
            if(session.HighRollData.PotSplits.Count < 2)
            {
                session.HighRollData.PotSplits = new List<int>() { 100 };
            }

            session.HighRollData.RulesSet = true;
            if (session.HighRollData.PotSplits.Count <= 1)
            {
                messageString = "Prizes set: [b]Winner takes all[/b]";
            }
            else
            {
                messageString = "Prizes set: [b]" + string.Join("-", session.HighRollData.PotSplits) + " split[/b]";
            }
            return messageString;
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {

        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, MessageAddress address, GameSession session, string[] terms, string[] rawTerms)
        {
            //help, keepsession, ante can be standardized across games
            string returnString = "";
            
            if (terms.Contains("setsplit") || terms.Contains("split"))
            {
                string result = SetSplits(terms, session);
                returnString = result;
            }
            else if(terms.Contains("setsides") || terms.Contains("die"))
            {
                int amount = Utils.GetNumberFromInputs(terms);
                if (amount >= 2 && amount < DiceBot.MaximumSides)
                {
                    session.HighRollData.DieSides = amount;
                    returnString = "Set the die type for this session to [b]1d" + amount + "[/b].";
                }
                else
                {
                    returnString = "Failed: no positive die sides amount was found (must be between 2 and " + DiceBot.MaximumSides + ").";
                }
            }
            else { returnString += "Failed: No such command exists for " + GetGameName(); }

            return returnString;
        }
    }

    public class HighRollData
    {
        public bool RulesSet;
        public int DieSides = 100;
        public List<int> PotSplits = new List<int>() { 100 };
        public List<HighRollScore> Scores;
    }

    public class HighRollScore
    {
        public string PlayerName;
        public double PlayerScore;
        public DiceRoll FirstDiceRoll;
        public bool CannotAfford;
    }

}
