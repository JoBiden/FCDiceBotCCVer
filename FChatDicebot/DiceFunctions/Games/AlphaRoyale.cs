using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class AlphaRoyale : IGame
    {
        public const int WaitPerPostMs = 1500;
        public const int MorningNewsSeconds = 10;
        public const int FirstRoundSeconds = 30;
        public const int SecondRoundSeconds = 30;
        public const int FinalRoundSeconds = 20;
        //public const int IntroductionRoundSeconds = 60;
        public const int IntermissionSeconds = 9;

        #region IGameRequiredData
        public string GetGameName()
        {
            return "AlphaRoyale";
        }

        public int GetMaxPlayers()
        {
            return 20;
        }

        public int GetMinPlayers()
        {
            return 8;
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
            return true;
        }

        public int GetMinimumMsBetweenGames()
        {
            return 60000;
        }
        #endregion

        public string GetGameHelp()
        {
            string thisGameCommands = "setante #, setsplit split:#-# (sets the pot split), hideeliminations OR showeliminations (sets whether elimination text will be hidden or shown), capture OR lethal (set elimination verbs to capture or death)";
            string thisGameStartupOptions = "# (sets session ante), split:#-# (sets the pot split), sides:# (set the # of sides on the die), hideeliminations (hides elimination text for participants), capture OR lethal (set elimination verbs to capture or death)\n" +
                "Setting the split: use split:70-30 to set to 70% first place 30% second place. Can set a prize for the top 2-3 rolls to #(1st)-#(2nd)-#(3rd). Totals up to 100, up to 3 prizes" +
                "\nThe default rules are: Highest roller on 1d100 wins (100% of) the pot, no ante, reroll ties";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, false, false);
        }

        public string GetStartingDisplay()
        {
            return FChatDicebot.TextFormat.Emoji("dbalpha1") + FChatDicebot.TextFormat.Emoji("dbalpha2");
        }

        public string GetEndingDisplay()
        {
            return "";
        }

        public string GameStatus(GameSession session)
        {
            string messageString = "Prizes: [b]" + (session.AlphaRoyaleData.PotSplits.Count < 2 ? "Winner takes all." : string.Join("-", session.AlphaRoyaleData.PotSplits)) + "[/b], ";
            //string anteString = session.Ante > 0 ? " (ante: " + session.Ante + ") " + messageString : " (no bets placed)";
            return messageString + (session.AlphaRoyaleData.HideEliminations ? "Eliminations text is hidden" : "Elimination text is shown") + ", " + (session.AlphaRoyaleData.UseCaptureElimination ? "Capture Eliminations" : "Lethal Eliminations");
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            messageString = "";
            if (!session.AlphaRoyaleData.RulesSet)
            {
                messageString = SetSplits(terms, session);

                string captureString = " (elimination is capture)";
                string hideEliminations = "";
                if (terms.Contains("capture") || terms.Contains("capturedeath") || terms.Contains("captureelimination"))
                {
                    session.AlphaRoyaleData.UseCaptureElimination = true;
                    captureString = " (elimination is capture)";
                }

                if (terms.Contains("lethal") || terms.Contains("lethaldeath") || terms.Contains("lethalelimination"))
                {
                    session.AlphaRoyaleData.UseCaptureElimination = false;
                    captureString = " (elimination is death)";
                }

                if (terms.Contains("hideeliminations"))
                {
                    session.AlphaRoyaleData.HideEliminations = true;
                    hideEliminations = " (eliminations aren't shown)";
                }

                //string dieString = "";
                //foreach (string s in terms)
                //{

                //    if (s.StartsWith("sides:"))
                //    {
                //        string justNumber = s.Replace("sides:", "");
                //        int dieSides = 0;
                //        int.TryParse(justNumber, out dieSides);
                //        if (dieSides >= 2 && dieSides < DiceBot.MaximumSides)
                //        {
                //            session.HighRollData.DieSides = dieSides;
                //            dieString = "Set the die type for this session to [b]1d" + dieSides + "[/b]. ";
                //        }
                //        else
                //        {
                //            //returnString = "Failed: no positive die sides amount was found (must be between 2 and " + DiceBot.MaximumSides + ").";
                //        }
                //    }
                //    if (string.IsNullOrEmpty(dieString))
                //    {

                //    }
                //}

                string anteString = session.Ante > 0 ? " (ante: " + session.Ante + ") " + messageString : " (no bets placed)";
                messageString = captureString + hideEliminations + anteString;// + " " + messageString;
            }

            return true;
        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            return "";
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            //set up the list of players
            foreach (string player in playerNames)
            {
                session.AlphaRoyaleData.AlphaRoyalePlayers.Add(new AlphaRoyalePlayer()
                {
                    PlayerName = player,
                    Eliminated = false,
                    PlayersEliminated = 0,
                    WonGame = false
                });
            }

            string bettingString = "";
            if (session.Ante > 0)
            {
                for (int i = 0; i < session.AlphaRoyaleData.AlphaRoyalePlayers.Count; i++)
                {
                    //if (!string.IsNullOrEmpty(bettingString))
                    //{
                    //    bettingString += "\n";
                    //}

                    AlphaRoyalePlayer currentPlayer = session.AlphaRoyaleData.AlphaRoyalePlayers[i];

                    string betstring = "";

                    MessageAddress playerChipPileAddress = new MessageAddress() { character = currentPlayer.PlayerName, channel = session.ChannelId, guild = session.GuildId };
                    ChipPile playerPile = diceBot.GetChipPile(playerChipPileAddress);//playerChipPileAddress currentPlayerScore.PlayerName, session.ChannelId) ;
                    if (playerPile.Chips >= session.Ante)
                        betstring = diceBot.BetChips(playerChipPileAddress, session.Ante, false);// + "\n";
                    else
                    {
                        currentPlayer.CannotAfford = true;
                        currentPlayer.Placement = 99;
                        betstring = TextFormat.GetCharacterUserTags(currentPlayer.PlayerName) + " cannot afford the ante to join and has been removed.";
                        //currentPlayerScore.PlayerScore = -1;
                    }

                    string thisPlayerRollString = (string.IsNullOrEmpty(betstring) ? TextFormat.GetCharacterUserTags(currentPlayer.PlayerName) : betstring);

                    bettingString += thisPlayerRollString;
                    bettingString += "\n";
                }
            }

            string allFaces = session.AlphaRoyaleData.GetRemainingPlayersIcons();
            session.AlphaRoyaleData.RoundNumber = 1;

            //take the list of players from the data
            List<AlphaRoyalePlayer> alphaRoyalePlayers = session.AlphaRoyaleData.GetRemainingPlayers();

            string outputString = "";

            //outputString += "🔥 **ALPHA ROYALE BEGINS!** 🔥\n\n";
            string lethality = session.AlphaRoyaleData.UseCaptureElimination ? "Who will get captured and face humiliation, " : "Who's biting the dust, ";
            outputString += "They're here: And they're ready to fight to the end! " + lethality + "and who will come out on top?\n";
            outputString += bettingString;
            outputString += "[b]----- ROUND 1 -----[/b]\n";
            outputString += "Starting Players: " + allFaces;

            session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "Phase", DoubleTime.GetCurrentTimestampSeconds() + FirstRoundSeconds);
            //show icons for every player

            return outputString;
        }

        private void HandleElimination(System.Random r, GameSession session, ref string outputString)
        {
            List<AlphaRoyalePlayer> remainingPlayers =
                session.AlphaRoyaleData.GetRemainingPlayers();

            // Pick eliminated player
            AlphaRoyalePlayer eliminated =
                remainingPlayers[r.Next(remainingPlayers.Count)];

            // Pick aggressor (must be different)
            AlphaRoyalePlayer aggressor;
            do
            {
                aggressor = remainingPlayers[r.Next(remainingPlayers.Count)];
            }
            while (aggressor.PlayerName == eliminated.PlayerName);

            // Apply elimination
            eliminated.Eliminated = true;
            eliminated.Placement = remainingPlayers.Count;
            aggressor.PlayersEliminated++;

            // Flavor text
            string flavorText = GetFlavorTextForMurder(r, eliminated.PlayerName, session.AlphaRoyaleData.UseCaptureElimination);

            if (!session.AlphaRoyaleData.HideEliminations)
            {
                if (!string.IsNullOrEmpty(outputString))
                    outputString += "Eliminations: \n";
                outputString += TextFormat.GetCharacterUserTags(eliminated.PlayerName) + " was " + flavorText + " by " + TextFormat.GetCharacterUserTags(aggressor.PlayerName) + "!\n";
            }
            else
            {
                if (!string.IsNullOrEmpty(outputString))
                    outputString += ", ";
                else
                    outputString += "Eliminations: ";
                outputString += TextFormat.GetCharacterUserTags(eliminated.PlayerName);
            }

        }

        public string SetSplits(string[] terms, GameSession session)
        {
            string messageString = "";
            session.AlphaRoyaleData.PotSplits = new List<int>();
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
                                session.AlphaRoyaleData.PotSplits.Add(output);
                                if (runningTotal >= 100)
                                    break;
                            }
                        }
                        if (session.AlphaRoyaleData.PotSplits.Count > 0 && session.AlphaRoyaleData.PotSplits.Sum() < 100)
                        {
                            session.AlphaRoyaleData.PotSplits[0] += 100 - session.AlphaRoyaleData.PotSplits.Sum();
                        }
                    }
                }
            }

            if (session.AlphaRoyaleData.PotSplits.Count < 2)
            {
                session.AlphaRoyaleData.PotSplits = new List<int>() { 100 };
            }

            session.AlphaRoyaleData.RulesSet = true;
            if (session.AlphaRoyaleData.PotSplits.Count <= 1)
            {
                messageString = "Prizes set: [b]Winner takes all[/b]";
            }
            else
            {
                messageString = "Prizes set: [b]" + string.Join("-", session.AlphaRoyaleData.PotSplits) + " split[/b]";
            }
            return messageString;
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {
            List<QueuedAction> triggeredActions = session.GetTriggeredActions(currentTime);
            if (triggeredActions != null && triggeredActions.Count() > 0)
            {
                for (int i = 0; i < triggeredActions.Count(); i++)
                {
                    QueuedAction currentAction = triggeredActions[i];
                    session.RemoveQueuedAction(currentAction);

                    switch (currentAction.QueuedActionType)
                    {
                        case QueuedActionType.AdvanceGamePhase:
                            string newPhaseResult = StartNewPhase(session.AlphaRoyaleData.RoundNumber, botMain, session);
                            botMain.SendMessageInChannel(newPhaseResult, session.GetMessageAddress());
                            break;
                    }
                }
            }
        }

        private string StartNewPhase(int roundNumber, BotMain botMain, GameSession session)
        {
            string returnString = "";
            double currentTime = DoubleTime.GetCurrentTimestampSeconds();
            session.AlphaRoyaleData.RoundNumber = roundNumber;//.CurrentGamePhase = newPhase;

            List<MafiaPlayer> livingPlayers = session.MafiaData.GetLivingPlayers();

            string allActions = "";
            switch (roundNumber)
            {
                case 1:
                    // ---------- ROUND 1 ----------
                    allActions = "";

                    int targetPlayers = Math.Max(session.AlphaRoyaleData.GetRemainingPlayers().Count() / 2, 4);
                    while (session.AlphaRoyaleData.GetRemainingPlayers().Count > targetPlayers)
                    {
                        HandleElimination(session.AlphaRoyaleData.Random, session, ref allActions);
                    }

                    session.AlphaRoyaleData.RoundNumber = 2;

                    session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "Phase", currentTime + IntermissionSeconds);
                    returnString = allActions;
                    break;
                case 2:
                    //round 2 start intermission
                    allActions = "\n[b]----- ROUND 2 -----[/b]\n";

                    allActions += "Remaining Players: " + session.AlphaRoyaleData.GetRemainingPlayersIcons();

                    session.AlphaRoyaleData.RoundNumber++;

                    session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "Phase", currentTime + FirstRoundSeconds);

                    returnString = allActions;
                    break;
                case 3:
                    allActions = "";
                    // ---------- ROUND 2 ----------
                    while (session.AlphaRoyaleData.GetRemainingPlayers().Count > 2)
                    {
                        HandleElimination(session.AlphaRoyaleData.Random, session, ref allActions);
                    }

                    session.AlphaRoyaleData.RoundNumber++;

                    session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "Phase", currentTime + IntermissionSeconds);
                    returnString = allActions;
                    break;
                case 4:
                    allActions = "\n[b]----- FINAL ROUND -----[/b]\n";

                    allActions += "Remaining Players: " + session.AlphaRoyaleData.GetRemainingPlayersIcons();

                    session.AlphaRoyaleData.RoundNumber++;

                    session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "Phase", currentTime + FinalRoundSeconds);

                    returnString = allActions;
                    break;
                case 5:

                    allActions = "";
                    // ---------- FINAL ROUND ----------
                    while (session.AlphaRoyaleData.GetRemainingPlayers().Count > 1)
                    {
                        HandleElimination(session.AlphaRoyaleData.Random, session, ref allActions);
                    }

                    // Declare winner
                    AlphaRoyalePlayer winner = session.AlphaRoyaleData.GetRemainingPlayers()[0];
                    winner.WonGame = true;

                    //allActions += "\n🏆 **WINNER:** " + TextFormat.GetCharacterIconTags(winner.PlayerName) + " 🏆\n";

                    session.AlphaRoyaleData.RoundNumber++;

                    session.AddQueuedAction(QueuedActionType.AdvanceGamePhase, "Phase", currentTime + IntermissionSeconds);

                    returnString = allActions;
                    break;
                case 6:
                    returnString = FinishGame(session, botMain.DiceBot);// FinishGame()

                    break;
            }

            return returnString;
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
            else if (terms.Contains("hideeliminations") || terms.Contains("showeliminations"))
            {
                if (terms.Contains("hideeliminations"))
                    session.AlphaRoyaleData.HideEliminations = true;
                if (terms.Contains("showeliminations"))
                    session.AlphaRoyaleData.HideEliminations = false;

                string result = "elimination text will be hidden.";
                if (!session.AlphaRoyaleData.HideEliminations)
                    result = "elimination text will be shown.";
                returnString = result;
            }
            else if (terms.Contains("lethal") || terms.Contains("capture") || terms.Contains("captureeliminations") || terms.Contains("lethaleliminations"))
            {
                if (terms.Contains("lethal") || terms.Contains("lethaleliminations"))
                    session.AlphaRoyaleData.UseCaptureElimination = false;
                if (terms.Contains("capture") || terms.Contains("captureliminations"))
                    session.AlphaRoyaleData.UseCaptureElimination = true;

                string result = "eliminations are now lethal.";
                if (session.AlphaRoyaleData.UseCaptureElimination)
                    result = "elimination are now done by capture.";
                returnString = result;
            }
            else { returnString += "Failed: No such command exists for " + GetGameName(); }

            return returnString;
        }

        public static string GetFlavorTextForMurder(System.Random random, string murderedPlayerName, bool usingCaptureString)
        {
            string flavorText = "";

            int flavorRoll = random.Next(16);
            switch (flavorRoll)
            {
                case 0:
                    flavorText = usingCaptureString? "wrapped up in a perfect mummy costume" : "riddled with bullets";
                    break;
                case 1:
                    flavorText = usingCaptureString ? "chained to the wall" : "pushed down an elevator shaft";
                    break;
                case 2:
                    flavorText = usingCaptureString ? "stuffed in the closet" : "beaten to death with a club";
                    break;
                case 3:
                    flavorText = usingCaptureString ? "kidnapped and taken to the dungeon" : "impaled on a metal fencepost";
                    break;
                case 4:
                    flavorText = usingCaptureString ? "wrapped up tight in leather straps" : "stabbed several times with a knife";
                    break;
                case 5:
                    flavorText = usingCaptureString ? "shackled and collared" : "decapitated with a sword";
                    break;
                case 6:
                    flavorText = usingCaptureString ? "put in a dog cage" : "run over with a car";
                    break;
                case 7:
                    flavorText = usingCaptureString ? "hypnotized and left docile" : "poisoned in their evening tea";
                    break;
                case 8:
                    flavorText = usingCaptureString ? "drugged and carried off" : "crushed with a falling piano";
                    break;
                case 9:
                    flavorText = usingCaptureString ? "locked in a latex gimp suit" : "burned to death in a fire";
                    break;
                case 10:
                    flavorText = usingCaptureString ? "bound and gagged" : "drowned in a pool";
                    break;
                case 11:
                    flavorText = usingCaptureString ? "sealed in a vacuum bed" : "mauled by a wild animal released";
                    break;
                case 12:
                    flavorText = usingCaptureString ? "drugged and carried off" : "shot through the heart by an arrow";
                    break;
                case 13:
                    flavorText = usingCaptureString ? "locked in a cute little cage" : "suffocated by a pillow";
                    break;
                case 14:
                    flavorText = usingCaptureString ? "given a free shibari lesson" : "blown to bits in an explosion";
                    break;
                case 15:
                    flavorText = usingCaptureString ? "chained to the bed" : "given a lethal injection";
                    break;
            }

            return flavorText;
        }

        private string FinishGame(GameSession session, DiceBot diceBot)
        {
            string returnString = "";

            returnString += "[b]Alpha Royale[/b] finished! " + TextFormat.GetCharacterIconTags(session.AlphaRoyaleData.GetRemainingPlayers()[0].PlayerName) + " has won! " + TextFormat.Emoji("confetti");

            session.State = DiceFunctions.GameState.Finished;

            List<AlphaRoyalePlayer> playersRanked = session.AlphaRoyaleData.AlphaRoyalePlayers.Where(ab => !ab.CannotAfford).OrderBy(a => a.Placement).ToList();

            //session.AlphaRoyaleData.Scores = session.HighRollData.Scores.OrderByDescending(a => a.PlayerScore).ToList();

            string resultsString = "\n[color=yellow][b]Results:[/b][/color]";
            int originalPot = diceBot.GetChipPile(new MessageAddress() { character = DiceBot.PotPlayerAlias, channel = session.ChannelId, guild = session.GuildId }).Chips;// DiceBot.PotPlayerAlias, session.ChannelId).Chips;
            for (int i = 0; i < playersRanked.Count && i < 4; i++)
            {
                string placeString = "first place!";
                switch (i)
                {
                    case 1: placeString = "second place"; break;
                    case 2: placeString = "third place"; break;
                    case 3: placeString = "fourth place"; break;
                }
                if (i < playersRanked.Count)// - 1)
                {
                    AlphaRoyalePlayer player = playersRanked[i];
                    string betstring = "";
                    string eliminationsString = player.PlayersEliminated > 0? " with [b]" + player.PlayersEliminated + "[/b] elimination" + TextFormat.SIfPlural(player.PlayersEliminated) : "";
                    if (session.Ante > 0)
                    {
                        if (session.AlphaRoyaleData.PotSplits.Count() >= i + 1)
                        {
                            int amount = (int)Math.Ceiling(originalPot * ((double)session.AlphaRoyaleData.PotSplits[i]) / 100);
                            MessageAddress addr = new MessageAddress() { character = player.PlayerName, channel = session.ChannelId, guild = session.GuildId };
                            betstring = " " + diceBot.ClaimPot(addr, 1, amount);
                        }
                    }
                    resultsString += "\n[b]" + TextFormat.GetCharacterUserTags(player.PlayerName) + " got " + placeString + "[/b]" + eliminationsString + "." + betstring;
                }
            }

            if (playersRanked.Count >= 6)
            {
                resultsString += "\n...";
            }
            if (playersRanked.Count >= 5)
            {
                string eliminationsString = playersRanked[playersRanked.Count - 2].PlayersEliminated > 0 ? " with [b]" + playersRanked[playersRanked.Count - 2].PlayersEliminated + "[/b] elimination" + TextFormat.SIfPlural(playersRanked[playersRanked.Count - 2].PlayersEliminated) : "";

                resultsString += "\n[b]" + TextFormat.GetCharacterUserTags(playersRanked[playersRanked.Count - 2].PlayerName) + " got [i]second-to-last[/i] place[/b]" + eliminationsString + ".";
            }
            if (playersRanked.Count > 4)
            {
                resultsString += "\n[b]" + TextFormat.GetCharacterUserTags(playersRanked[playersRanked.Count - 1].PlayerName) + " got [i]last[/i] place.[/b]";
            }

            return returnString + resultsString;
        }
    }

    public class AlphaRoyaleData
    {
        public Random Random = new Random();
        public bool RulesSet;
        public int RoundNumber;
        public List<int> PotSplits = new List<int>() { 100 };

        public MafiaSetupRules SetupRules = MafiaSetupRules.Basic;
        public bool RevealRolesOnDeath = false;
        public bool UseCaptureElimination = true;
        public bool HideEliminations = false;

        public List<AlphaRoyalePlayer> AlphaRoyalePlayers = new List<AlphaRoyalePlayer>();

        public List<MafiaRole> ThisSessionAvailableRoles = new List<MafiaRole>();

        public List<AlphaRoyalePlayer> GetRemainingPlayers()
        {
            return AlphaRoyalePlayers.Where(a => !a.Eliminated && !a.CannotAfford).ToList();
        }

        public string GetRemainingPlayersIcons()
        {
            string rtn = "";
            var players = GetRemainingPlayers();
            foreach (var player in players)
            {
                rtn += TextFormat.GetCharacterIconTags(player.PlayerName);
            }
            return rtn;
        }
    }

    public class AlphaRoyalePlayer
    {
        public string PlayerName;

        public bool Eliminated;
        public bool CannotAfford;
        public int Placement = 0;
        public int PlayersEliminated = 0;

        public bool WonGame;

        public string Print(bool revealRole, bool showChastitydeaths)
        {
            string roleStr = revealRole ? " ()" : "";
            string voteStr = "";
            return PlayerName + roleStr + (Eliminated ? " (" + Mafia.GetDeadString(showChastitydeaths) + ")" : "") + voteStr;
        }
    }

}
