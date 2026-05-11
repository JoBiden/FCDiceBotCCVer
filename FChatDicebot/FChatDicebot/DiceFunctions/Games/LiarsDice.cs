using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class LiarsDice : IGame
    {
        public string GetGameName()
        {
            return "LiarsDice";
        }

        public int GetMaxPlayers()
        {
            return 10;
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
            return true;
        }

        public int GetMinimumMsBetweenGames()
        {
            return 0;
        }

        public string GetGameHelp()
        {
            string thisGameCommands = "setante, newround, shuffleplayers, showplayers, adddie #, removedie #, setstartdice, forcepenalty (die/dare/strip/token)" +
                    "\n(as current player only): bet # (face as text) (i.e.: !gc bet 4 threes), challenge, penalty (die/dare/strip/token)";
            string thisGameStartupOptions = "# (sets ante amount), sides4 (use 4 sided dice), sides6 (use 6 sided dice), sides8 (use 8 sided dice), tokens (allow tokens), sameside (allow increasing # with same side for betting), increasingside (req die face >= existing for betting), increasingnumber (req die number >= existing for betting), startDice3 - startDice8 (set starting dice count), 1swild (a 1 counts as any #)" +
                    "\nThe default rules are: 5 dice with 6 sides, increasing number, no wilds, no tokens";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, false, false);
        }

        public string GetStartingDisplay()
        {
            return "[eicon]dbliardice1[/eicon][eicon]dbliardice2[/eicon]";
        }

        public string GetEndingDisplay()
        {
            return "";
        }

        public string GameStatus(GameSession session)
        {
            string fullStatus = "";
            if (session.LiarsDiceData != null)
            {
                if(session.LiarsDiceData.LiarsDicePlayers != null && session.LiarsDiceData.LiarsDicePlayers.Count > 0)
                {
                    string playersString = "";
                    foreach (LiarsDicePlayer player in session.LiarsDiceData.LiarsDicePlayers)
                    {
                        if (!string.IsNullOrEmpty(playersString))
                        {
                            playersString += ", ";
                        }

                        playersString += player.ToString();
                    }
                    fullStatus = "Current Players: " + playersString;
                }
                if(session.LiarsDiceData.CurrentBet != null)
                {
                    if(!string.IsNullOrEmpty(fullStatus))
                        fullStatus += "\n";
                    fullStatus += session.LiarsDiceData.CurrentBet.ToString();
                }
                if (session.LiarsDiceData.currentPlayerIndex >= 0)
                {
                    if (!string.IsNullOrEmpty(fullStatus))
                        fullStatus += "\n";
                    fullStatus += "The current player is: " + session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.currentPlayerIndex].PlayerName;
                }
                if (session.LiarsDiceData.currentPlayerPenaltyIndex >= 0)
                {
                    if (!string.IsNullOrEmpty(fullStatus))
                        fullStatus += "\n";
                    fullStatus += "The current penalty player is: " + session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.currentPlayerPenaltyIndex].PlayerName;
                }
            }
            
            return fullStatus;
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            //dice sides 2-8
            //set sameside higher die vs higher side or die
            //set tokens as penalty option
            //set ante

            messageString = "";

            if(!session.LiarsDiceData.RulesSet)
            {
                session.LiarsDiceData.RulesSet = true;

                string betTypeMessage = "";
                if(terms.Contains("sameside"))
                {
                    session.LiarsDiceData.BettingRule = LiarsDiceBettingRule.SameSidesHigherNumber;
                    betTypeMessage = "(bet same sides higher number)";
                }
                else if (terms.Contains("increasingside"))
                {
                    session.LiarsDiceData.BettingRule = LiarsDiceBettingRule.HigherSidesOrHigherNumber_PriorityFace;
                    betTypeMessage = "(bet higher sides or number, increasing dice face)";
                }
                else if (terms.Contains("increasingnumber"))
                {
                    session.LiarsDiceData.BettingRule = LiarsDiceBettingRule.HigherSidesOrHigherNumber_PriorityNumber;
                    betTypeMessage = "(bet higher sides or number, increasing dice number)";
                }

                string diceSidesMessage = "(Die Type: d6)";
                if(terms.Contains("sides6"))
                {
                    session.LiarsDiceData.DiceSides = 6;
                }
                else if(terms.Contains("sides4"))
                {
                    session.LiarsDiceData.DiceSides = 4;
                    diceSidesMessage = "(Die Type: d4)";
                }
                else if (terms.Contains("sides8"))
                {
                    session.LiarsDiceData.DiceSides = 8;
                    diceSidesMessage = "(Die Type: d8)";
                }

                string wildMessage = "(no wilds)";
                if(terms.Contains("wild1s") || terms.Contains("wild") || terms.Contains("1swild"))
                {
                    session.LiarsDiceData.WildOnes = true;
                    wildMessage = "(1s are wild)";
                }

                if (terms.Contains("startdice5"))
                {
                    session.LiarsDiceData.StartingDice = 5;
                }
                else if (terms.Contains("startdice6"))
                {
                    session.LiarsDiceData.StartingDice = 6;
                }
                else if (terms.Contains("startdice7"))
                {
                    session.LiarsDiceData.StartingDice = 7;
                }
                else if (terms.Contains("startdice3"))
                {
                    session.LiarsDiceData.StartingDice = 3;
                }
                else if (terms.Contains("startdice4"))
                {
                    session.LiarsDiceData.StartingDice = 4;
                }
                string startingDiceMessage = "(" + session.LiarsDiceData.StartingDice + " starting dice)";

                string tokensMessage = "";
                if (terms.Contains("token") || terms.Contains("tokens"))
                {
                    session.LiarsDiceData.TokensAvailable = true;
                    tokensMessage += "(tokens available)";
                }

                string settingsMessage = betTypeMessage + "";
                if (!string.IsNullOrEmpty(settingsMessage))
                    settingsMessage += " ";
                settingsMessage += diceSidesMessage + " " + wildMessage + " " + startingDiceMessage + (tokensMessage.Length > 0 ? " " : "") + tokensMessage + (ante > 0? " (ante set to " + ante + ")" : "");

                messageString = settingsMessage;
            }

            return true;
        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            string messageString = "";
            if (session.LiarsDiceData != null && session.LiarsDiceData.LiarsDicePlayers != null)
            {
                int thisPlayerIndex = GetIndexOfPlayerName(characterName, session.LiarsDiceData.LiarsDicePlayers);

                if(session.LiarsDiceData.CurrentBet.BettingPlayerIndex == thisPlayerIndex)
                {
                    session.LiarsDiceData.CurrentBet = null;
                    messageString += " (this player had the current bet, so the bet was removed)";
                }

                if (session.LiarsDiceData.currentPlayerIndex > thisPlayerIndex)
                {
                    session.LiarsDiceData.currentPlayerIndex -= 1;
                }

                if (session.LiarsDiceData.currentPlayerPenaltyIndex > thisPlayerIndex)
                {
                    session.LiarsDiceData.currentPlayerPenaltyIndex -= 1;
                }

                session.LiarsDiceData.LiarsDicePlayers = session.LiarsDiceData.LiarsDicePlayers.Where(a => a.PlayerName != characterName).ToList();

                if (session.LiarsDiceData.LiarsDicePlayers.Count <= session.LiarsDiceData.currentPlayerIndex)
                {
                    session.LiarsDiceData.currentPlayerIndex -= 1;
                }

                if (session.LiarsDiceData.LiarsDicePlayers.Count <= session.LiarsDiceData.currentPlayerPenaltyIndex)
                {
                    session.LiarsDiceData.currentPlayerPenaltyIndex -= 1;
                }

                if(session.LiarsDiceData.currentPlayerIndex >= 0)
                {
                    LiarsDicePlayer currentActivePlayerNow = session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.currentPlayerIndex];
                    if (!currentActivePlayerNow.Active || currentActivePlayerNow.Eliminated)
                    {
                        MoveActiveTurnToNextPlayer(new System.Random(), session);
                    }
                }
                if(session.LiarsDiceData.currentPlayerPenaltyIndex >= 0)
                {
                    LiarsDicePlayer currentActivePlayerNow = session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.currentPlayerPenaltyIndex];
                    if (!currentActivePlayerNow.Active || currentActivePlayerNow.Eliminated)
                    {
                        session.LiarsDiceData.currentPlayerPenaltyIndex = -1;
                    }
                }
            }

            messageString += "\n" + CurrentPlayerTurn(session);

            return messageString;
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            string outputString = "";
            
            //set up starting data 
            foreach (string player in playerNames)
            {
                session.LiarsDiceData.LiarsDicePlayers.Add(new LiarsDicePlayer() { 
                    PlayerDice = session.LiarsDiceData.StartingDice, 
                    PlayerName = player, 
                    PlayerTokens = 10, 
                    Active = true });
            }

            //shuffle players
            ShufflePlayers(botMain.DiceBot.random, session);

            //start the new round (roll dice, declare turn order, prompt current player for action)
            string newRoundString = StartNewRound(botMain, session);

            session.State = DiceFunctions.GameState.GameInProgress;

            return outputString + newRoundString;
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {

        }

        private string StartNewRound(BotMain botMain, GameSession session)
        {
            session.LiarsDiceData.CurrentBet = null;

            RollEveryPlayerDice(botMain, session);
            session.LiarsDiceData.currentPlayerPenaltyIndex = -1;

            if (session.LiarsDiceData.winningPlayerForRound >= 0)
            {
                session.LiarsDiceData.currentPlayerIndex = session.LiarsDiceData.winningPlayerForRound;
                session.LiarsDiceData.winningPlayerForRound = -1;
            }
            if (session.LiarsDiceData.currentPlayerIndex < 0)
            {
                MoveActiveTurnToNextPlayer(botMain.DiceBot.random, session);
            }

            string anteString = "";
            if(session.Ante > 0)
            {
                anteString = "\n";
                for (int i = 0; i < session.Players.Count; i++)
                {
                    if (!string.IsNullOrEmpty(anteString))
                    {
                        anteString += "\n";
                    }

                    string betstring = "";

                    betstring = botMain.DiceBot.BetChips(session.Players[i], session.ChannelId, session.Ante, false) + "\n";

                    anteString += betstring;
                }
            }

            //declare turn order
            string turnOrder = GetPlayerList(session);

            string currentPlayerTurn = CurrentPlayerTurn(session);
            string botMessage = "[b]" + GetGameName() + "[/b]: A new round has started.\nTurn Order: " + turnOrder + anteString + "\n[color=yellow]Rolling player dice...[/color]\n\n" + currentPlayerTurn;
            return botMessage;
        }

        private string CurrentPlayerTurn(GameSession session)
        {
            string currentPlayerToPlay = " (player turn not set)";
            string moveDescription = "";
            if(session.LiarsDiceData.currentPlayerPenaltyIndex >= 0)
            {
                currentPlayerToPlay = session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.currentPlayerPenaltyIndex].PlayerName;
                moveDescription = "'s penalty. " + GetPenaltiesAvailable(session);
            }
            else
            {
                currentPlayerToPlay = session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.currentPlayerIndex].PlayerName;
                moveDescription = "'s turn. (bet or challenge)";
            }

            string rtn = "[b]" + GetGameName() + "[/b]: it is now " + currentPlayerToPlay + moveDescription; 
            return rtn;
        }

        private void RollEveryPlayerDice(BotMain botMain, GameSession session)
        {
            foreach(LiarsDicePlayer p in session.LiarsDiceData.LiarsDicePlayers)
            {
                DiceRoll d = new DiceRoll(p.PlayerName, session.ChannelId, botMain.DiceBot) { DiceRolled = p.PlayerDice, DiceSides = session.LiarsDiceData.DiceSides };
                d.Roll(botMain.DiceBot.random);
                string rollResult = d.ResultString(DiceRollFormat.OjEicon6, false);
                p.ThisRoundDice = d;

                botMain.SendPrivateMessage("Your [b]Liar's Dice[/b] rolls for this round: " + rollResult, p.PlayerName);
            }
        }

        private string GetAllDiceString(GameSession session, bool playerNames, bool includeRollText)
        {
            string allDiceString = "";
            foreach (LiarsDicePlayer p in session.LiarsDiceData.LiarsDicePlayers)
            {
                if(!string.IsNullOrEmpty(allDiceString))
                {
                    allDiceString += "\n";
                }

                string rollResult = p.ThisRoundDice.ResultString(DiceRollFormat.OjEicon6, false);

                if (!includeRollText)
                    rollResult = rollResult.Substring(rollResult.IndexOf('{'));

                allDiceString += (playerNames ? ( Utils.GetCharacterUserTags(p.PlayerName) + ": ") : "") + rollResult;
            }
            return allDiceString;
        }

        private int GetIndexOfPlayerName(string player, List<LiarsDicePlayer> players)
        {
            int index = 0;
            foreach(LiarsDicePlayer p in players)
            {
                if (p.PlayerName == player)
                    return index;
                else
                    index++;
            }
            return -1;
        }

        private string GetPenaltiesAvailable(GameSession session)
        {
            if (session.Ante > 0)
                return "(die)";
            else if(!session.LiarsDiceData.TokensAvailable)
                return "(die, dare, strip)";
            else
                return "(die, dare, strip, token)";
        }

        private string ChallengeCurrentBet(DiceBot diceBot, GameSession session, string bettingPlayer, string character)
        {
            string challengeResult = "";
            string penaltiesAvailable = GetPenaltiesAvailable(session);

            if(DiceBetIsTrue(session))
            {
                session.LiarsDiceData.winningPlayerForRound = GetIndexOfPlayerName(bettingPlayer, session.LiarsDiceData.LiarsDicePlayers);
                session.LiarsDiceData.currentPlayerPenaltyIndex = GetIndexOfPlayerName(character, session.LiarsDiceData.LiarsDicePlayers);

                string penaltyPlayerString = Utils.GetCharacterUserTags(character) + (session.Ante > 0 ? " will lose one die." : " must now pick their penalty! " + penaltiesAvailable);
                challengeResult = "The current bet was [b]TRUE[/b]. " + Utils.GetCharacterUserTags(character) + "'s challenge failed.\n" + penaltyPlayerString;
            }
            else
            {
                session.LiarsDiceData.winningPlayerForRound = GetIndexOfPlayerName(character, session.LiarsDiceData.LiarsDicePlayers);
                session.LiarsDiceData.currentPlayerPenaltyIndex = GetIndexOfPlayerName(bettingPlayer, session.LiarsDiceData.LiarsDicePlayers);

                string penaltyPlayerString = Utils.GetCharacterUserTags(bettingPlayer) + (session.Ante > 0 ? " will lose one die." : " must now pick their penalty! " + penaltiesAvailable);
                challengeResult = "The current bet was [b]FALSE[/b]. " + Utils.GetCharacterUserTags(character) + "'s challenge succeeded.\n" + penaltyPlayerString;
            }

            if(session.Ante > 0)
            {
                challengeResult += "\n" + ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Die, false);
            }

            return challengeResult;
        }

        private bool DiceBetIsTrue(GameSession session)
        {
            LiarsDiceBet currentBet = session.LiarsDiceData.CurrentBet;

            string allDice = GetAllDiceString(session, false, false);

            int count1 = allDice.Count(f => (f == '1'));
            int count2 = allDice.Count(f => (f == '2'));
            int count3 = allDice.Count(f => (f == '3'));
            int count4 = allDice.Count(f => (f == '4'));
            int count5 = allDice.Count(f => (f == '5'));
            int count6 = allDice.Count(f => (f == '6'));
            int count7 = allDice.Count(f => (f == '7'));
            int count8 = allDice.Count(f => (f == '8'));

            int bonus = 0;
            if(session.LiarsDiceData.WildOnes)
            {
                bonus = count1;
            }

            switch(currentBet.DiceFace)
            {
                case 1:
                    return count1 + bonus >= currentBet.DiceNumber;
                case 2:
                    return count2 + bonus >= currentBet.DiceNumber;
                case 3:
                    return count3 + bonus >= currentBet.DiceNumber;
                case 4:
                    return count4 + bonus >= currentBet.DiceNumber;
                case 5:
                    return count5 + bonus >= currentBet.DiceNumber;
                case 6:
                    return count6 + bonus >= currentBet.DiceNumber;
                case 7:
                    return count7 + bonus >= currentBet.DiceNumber;
                case 8:
                    return count8 + bonus >= currentBet.DiceNumber;
            }

            return false;
        }

        private void ShufflePlayers(System.Random r, GameSession session)
        {
            if (session.LiarsDiceData.LiarsDicePlayers == null || session.LiarsDiceData.LiarsDicePlayers.Count == 0)
                return;

            int playerCount = session.LiarsDiceData.LiarsDicePlayers.Count;

            List<int> newPositions = new List<int>();
            for (int i = 0; i < playerCount; i++)
            {
                int swapPos = r.Next(playerCount);
                SwapPositions(session.LiarsDiceData.LiarsDicePlayers, i, swapPos);
            }

            session.LiarsDiceData.currentPlayerIndex = -1;
        }

        private void MoveActiveTurnToNextPlayer(System.Random random, GameSession session)
        {
            if (session.LiarsDiceData == null || session.LiarsDiceData.LiarsDicePlayers == null || session.LiarsDiceData.LiarsDicePlayers.Count(a => a.Active && !a.Eliminated) <= 1)
                return;

            if(session.LiarsDiceData.currentPlayerIndex < 0)
                session.LiarsDiceData.currentPlayerIndex = random.Next(session.LiarsDiceData.LiarsDicePlayers.Count);

            AdvanceActivePlayerOneSpotOnRoster(session);

            while(!session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.currentPlayerIndex].Active || session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.currentPlayerIndex].Eliminated)
            {
                AdvanceActivePlayerOneSpotOnRoster(session);
            }
        }

        private void AdvanceActivePlayerOneSpotOnRoster(GameSession session)
        {
            if (session.LiarsDiceData.currentPlayerIndex >= session.LiarsDiceData.LiarsDicePlayers.Count - 1)
                session.LiarsDiceData.currentPlayerIndex = 0;
            else
                session.LiarsDiceData.currentPlayerIndex = session.LiarsDiceData.currentPlayerIndex + 1;
        }

        private LiarsDiceBet GetBetFromCommands(string[] terms, int creatorIndex)
        {
            if (terms.Length < 3)
                return null;

            List<int> allNumbers = Utils.GetAllNumbersFromInputs(terms);

            int numberOfDice = -1;
            int dieFace = -1;

            if(allNumbers.Count == 2)
            {
                numberOfDice = allNumbers[0];
                dieFace = allNumbers[1];
            }
            else
            {
                numberOfDice = Utils.GetNumberFromInputs(terms);

                dieFace = -1;
                if (terms.Contains("one") || terms.Contains("ones"))
                    dieFace = 1;
                if (terms.Contains("two") || terms.Contains("twos"))
                    dieFace = 2;
                if (terms.Contains("three") || terms.Contains("threes"))
                    dieFace = 3;
                if (terms.Contains("four") || terms.Contains("fours"))
                    dieFace = 4;
                if (terms.Contains("five") || terms.Contains("fives"))
                    dieFace = 5;
                if (terms.Contains("six") || terms.Contains("sixs") || terms.Contains("sixes"))
                    dieFace = 6;
                if (terms.Contains("seven") || terms.Contains("sevens"))
                    dieFace = 7;
                if (terms.Contains("eight") || terms.Contains("eights"))
                    dieFace = 8;
                if (terms.Contains("nine") || terms.Contains("nines"))
                    dieFace = 9;
                if (terms.Contains("ten") || terms.Contains("tens"))
                    dieFace = 10;
            }
            
            if(numberOfDice > 0 && dieFace > 0)
            {
                return new LiarsDiceBet() { 
                    BettingPlayerIndex = creatorIndex,
                    DiceFace = dieFace,
                    DiceNumber = numberOfDice
                };
            }

            return null;
        }

        public void SwapPositions(List<LiarsDicePlayer> thesePlayers, int pos, int pos2)
        {
            LiarsDicePlayer dp = thesePlayers[pos];
            thesePlayers[pos] = thesePlayers[pos2];
            thesePlayers[pos2] = dp;
        }

        private void MarkPlayerActive(GameSession session, int playerIndex, bool active)
        {
            if (playerIndex < 0 || playerIndex >= session.LiarsDiceData.LiarsDicePlayers.Count - 1)
                return;

            session.LiarsDiceData.LiarsDicePlayers[playerIndex].Active = active;
        }

        private string ApplyPenalty(DiceBot diceBot, GameSession session, int playerIndex, PenaltyType penalty, bool listChoice)
        {
            LiarsDicePlayer player = session.LiarsDiceData.LiarsDicePlayers[playerIndex];
            string outputString = Utils.GetCharacterUserTags(player.PlayerName);
            switch(penalty)
            {
                case PenaltyType.Die:
                    {
                        player.PlayerDice -= 1;
                        if(listChoice)
                            outputString += " has chosen to lose a [b]DIE[/b] for their penalty for this round.\n" + Utils.GetCharacterUserTags(player.PlayerName);

                        if(player.PlayerDice <= 0)
                        {
                            outputString += " reached zero dice and has been eliminated!";
                            player.Eliminated = true;
                        }
                        else
                        {
                            outputString += " has " + player.PlayerDice + " dice remaining.";
                        }
                    }
                    break;
                case PenaltyType.Dare:
                    {
                        if(listChoice)
                            outputString += " has chosen to perform a [b]DARE[/b] for their penalty for this round.";
                    }
                    break;
                case PenaltyType.Strip:
                    {
                        if(listChoice)
                            outputString += " has chosen to [b]STRIP[/b] one item for their penalty for this round.";
                    }
                    break;
                case PenaltyType.Token:
                    {
                        player.PlayerTokens -= 1;

                        if(listChoice)
                            outputString += " has chosen to lose a [b]TOKEN[/b] for their penalty for this round.\n" + Utils.GetCharacterUserTags(player.PlayerName);

                        if (player.PlayerTokens <= 0)
                        {
                            outputString += " reached zero tokens and has been eliminated!";
                            player.Eliminated = true;
                        }
                        else
                        {
                            outputString += " has " + player.PlayerTokens + " tokens remaining.";
                        }
                    }
                    break;
            }

            session.LiarsDiceData.currentPlayerPenaltyIndex = -1;

            if(GameIsComplete(session))
            {
                outputString += FinishGame(session, diceBot);
                
            }

            return outputString;
        }

        private bool GameIsComplete(GameSession session)
        {
            return session.LiarsDiceData.LiarsDicePlayers.Count(a => !a.Eliminated) <= 1;
        }

        private LiarsDicePlayer GetGameWinner(GameSession session)
        {
            if (GameIsComplete(session))
            {
                return session.LiarsDiceData.LiarsDicePlayers.FirstOrDefault(a => !a.Eliminated);
            }
            else
                return null;
        }

        private string FinishGame(GameSession session, DiceBot diceBot)
        {
            string returnString = "";
            LiarsDicePlayer winner = GetGameWinner(session);
            if (winner != null)
                returnString += "\n\n[b]Liar's Dice[/b]: The game has finished. " + Utils.GetCharacterUserTags(winner.PlayerName) + " wins!";

            if(session.Ante > 0)
            {
                returnString += "\n" + diceBot.ClaimPot(winner.PlayerName, session.ChannelId, 1);
            }

            diceBot.RemoveGameSession(session.ChannelId, session.CurrentGame);

            return returnString;
        }

        private string GetPlayerList(GameSession session)
        {
            string rtn = "";
            foreach(LiarsDicePlayer p in session.LiarsDiceData.LiarsDicePlayers)
            {
                if (!string.IsNullOrEmpty(rtn))
                    rtn += ", ";

                rtn += p.ToString();
            }
            return rtn;
        }

        public string GetCurrentActivePlayerName(GameSession session)
        {
            if (session.LiarsDiceData.LiarsDicePlayers == null || session.LiarsDiceData.currentPlayerIndex < 0 || session.LiarsDiceData.currentPlayerIndex > session.LiarsDiceData.LiarsDicePlayers.Count - 1)
                return "invalid data";

            return session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.currentPlayerIndex].PlayerName;
        }

        public string GetCurrentActivePenaltyPlayerName(GameSession session)
        {
            if (session.LiarsDiceData.LiarsDicePlayers == null || session.LiarsDiceData.currentPlayerPenaltyIndex < 0 || session.LiarsDiceData.currentPlayerPenaltyIndex > session.LiarsDiceData.LiarsDicePlayers.Count - 1)
                return "invalid data";

            return session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.currentPlayerPenaltyIndex].PlayerName;
        }

        public void ResetGameRound(GameSession session)
        {
            session.State = GameState.Unstarted;
        }

        public string AcceptBet(BotMain botMain, string character, GameSession session, LiarsDiceBet bet)
        {
            string returnString = "";
            session.LiarsDiceData.CurrentBet = bet;
            MoveActiveTurnToNextPlayer(botMain.DiceBot.random, session);
            returnString = Utils.GetCharacterUserTags(character) + " has bet that there are " + bet.ToString();
            returnString += "\n" + CurrentPlayerTurn(session);

            return returnString;
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, GameSession session, string[] terms, string[] rawTerms)
        {
            if(session.LiarsDiceData.LiarsDicePlayers.Count(a => a.PlayerName == character) < 1)
            {
                return "Game commands for " + GetGameName() + " can only be used by characters who are playing the game.";
            }

            bool characterIsCurrentActivePlayer = character == GetCurrentActivePlayerName(session);
            bool characterIsCurrentActivePenaltyPlayer = character == GetCurrentActivePenaltyPlayerName(session);

            string returnString = "";
            if (terms.Contains("newround") || terms.Contains("startround"))
            {
                if(session.State != GameState.GameInProgress)
                {
                    returnString = "Failed: newround requires the game to be in progress to function. Use !startgame instead";
                }
                else if(session.LiarsDiceData.currentPlayerPenaltyIndex >= 0)
                {
                    returnString = "Failed: A new round cannot be started while a player has a penalty that needs to be chosen (use !gc penalty TYPE) or forced (!gc forcepenalty TYPE)";
                }
                else
                {
                    string newRoundString = StartNewRound(botMain, session);
                    returnString = newRoundString;
                }
            }
            else if (terms.Contains("shuffleplayers") || terms.Contains("rearrangeplayers") || terms.Contains("newplayerorder"))
            {
                ShufflePlayers(botMain.DiceBot.random, session);

                returnString = "The players have been shuffled.";
            }
            else if (terms.Contains("showplayers"))
            {
                string playerList = GetPlayerList(session);

                returnString = playerList;
            }
            else if (terms.Contains("bet") || terms.Contains("challenge"))
            {
                if (session.State != GameState.GameInProgress)
                {
                    returnString = "Failed: this command requires the game to be in progress to function.";
                }
                else if (session.LiarsDiceData.currentPlayerPenaltyIndex >= 0)
                {
                    returnString = "The active player needs to select their penalty before continuing.";
                }
                else if(!characterIsCurrentActivePlayer)
                {
                    returnString = "It is currently " + Utils.GetCharacterUserTags(GetCurrentActivePlayerName(session)) + "'s turn.";
                }
                else
                {
                    if(terms.Contains("bet"))
                    {
                        //determine what the bet is
                        LiarsDiceBet bet = GetBetFromCommands(terms, session.LiarsDiceData.currentPlayerIndex);

                        if(bet != null)
                        {
                            if(bet.DiceFace > session.LiarsDiceData.DiceSides || bet.DiceFace < 1)
                            {
                                returnString = "Invalid bet. The dice faces must be within 1 to " + session.LiarsDiceData.DiceSides + ".";
                            }
                            else if(bet.DiceNumber < 1)
                            {
                                returnString = "Invalid bet. You must bet at least 1 die has the chosen face.";
                            }
                            else
                            {
                                if (session.LiarsDiceData.CurrentBet == null)
                                {
                                    returnString = AcceptBet(botMain, character, session, bet);
                                }
                                else if(session.LiarsDiceData.BettingRule == LiarsDiceBettingRule.SameSidesHigherNumber)
                                {
                                    if (session.LiarsDiceData.CurrentBet.DiceNumber >= bet.DiceNumber)
                                    {
                                        returnString = "Invalid bet. A new bet must have a higher number of dice than the existing bet.";
                                    }
                                    else if (session.LiarsDiceData.CurrentBet.DiceFace != bet.DiceFace)
                                    {
                                        returnString = "Invalid bet. A new bet must use the same dice face as the existing bet.";
                                    }
                                    else
                                    {
                                        returnString = AcceptBet(botMain, character, session, bet);
                                    }
                                }
                                else if(session.LiarsDiceData.BettingRule == LiarsDiceBettingRule.HigherSidesOrHigherNumber_PriorityFace)
                                {
                                    int betDiceFaceScore = bet.DiceFace;
                                    int existingDiceFaceScore = session.LiarsDiceData.CurrentBet.DiceFace;

                                    if (session.LiarsDiceData.CurrentBet.DiceNumber >= bet.DiceNumber && existingDiceFaceScore == betDiceFaceScore)
                                    {
                                        returnString = "Invalid bet. A new bet with the same dice face must have a higher number of dice than the existing bet.";
                                    }
                                    else if (existingDiceFaceScore > betDiceFaceScore)
                                    {
                                        returnString = "Invalid bet. A new bet must use the same or a higher dice face as the existing bet.";
                                    }
                                    else
                                    {
                                        returnString = AcceptBet(botMain, character, session, bet);
                                    }
                                }
                                else if (session.LiarsDiceData.BettingRule == LiarsDiceBettingRule.HigherSidesOrHigherNumber_PriorityNumber)
                                {
                                    int betDiceFaceScore = bet.DiceFace;
                                    int existingDiceFaceScore = session.LiarsDiceData.CurrentBet.DiceFace;

                                    if (session.LiarsDiceData.CurrentBet.DiceNumber == bet.DiceNumber && existingDiceFaceScore >= betDiceFaceScore)
                                    {
                                        returnString = "Invalid bet. A new bet with the same dice number must have a higher dice face than the existing bet.";
                                    }
                                    else if (session.LiarsDiceData.CurrentBet.DiceNumber > bet.DiceNumber)
                                    {
                                        returnString = "Invalid bet. A new bet must use the same or a higher number of dice as the existing bet.";
                                    }
                                    else
                                    {
                                        returnString = AcceptBet(botMain, character, session, bet);
                                    }
                                }
                                
                            }

                            
                        }
                        else
                        {
                            returnString = "Invalid bet input. Please input as '!gc bet 3 threes', '!gc bet 2 fives', or '!gc bet 3 2', etc";
                        }
                    }
                    else if(terms.Contains("challenge"))
                    {
                        //make sure there's an existing bet
                        if (session.LiarsDiceData.CurrentBet != null && session.LiarsDiceData.CurrentBet.DiceNumber > 0 && session.LiarsDiceData.CurrentBet.DiceFace > 0)
                        {
                            string allDiceString = GetAllDiceString(session, true, true);

                            string challengeResultString = ChallengeCurrentBet(diceBot, session, session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.CurrentBet.BettingPlayerIndex].PlayerName , character);

                            returnString = Utils.GetCharacterUserTags(character) + " has chosen to [b]Challenge[/b] the current bet ([b]" + session.LiarsDiceData.CurrentBet.ToString() + "[/b])!\n[color=yellow]Revealing Dice Rolls...[/color]\n\n" + allDiceString + "\n" + challengeResultString;
                        }
                        else
                        {
                            returnString = "There is currently no bet to challenge. You must make a bet.";
                        }
                    }
                }
            }
            else if (terms.Contains("penalty"))
            {
                if (session.State != GameState.GameInProgress)
                {
                    returnString = "Failed: this command requires the game to be in progress to function.";
                }
                else if (session.LiarsDiceData.currentPlayerPenaltyIndex <= -1)
                {
                    returnString = "There is not currently a player who needs to choose a penalty.";
                }
                else if (!characterIsCurrentActivePenaltyPlayer)
                    returnString = "Only the player recieving a penalty can decide their own penalty.";
                else
                {
                    if (terms.Contains("die"))
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Die, true);
                    }
                    else if (terms.Contains("dare"))
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Dare, true);
                    }
                    else if (terms.Contains("strip"))
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Strip, true);
                    }
                    else if (terms.Contains("token") && session.LiarsDiceData.TokensAvailable)
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Token, true);
                    }
                    else
                    {
                        string penaltiesAvailable = GetPenaltiesAvailable(session);

                        returnString = "Penalty input invalid, please select a penalty type. " + penaltiesAvailable;
                    }
                }
            }
            else if (terms.Contains("forcepenalty"))
            {
                if (session.State != GameState.GameInProgress)
                {
                    returnString = "Failed: this command requires the game to be in progress to function.";//for " + GetGameName() + " only work while the game is running.";
                }
                else if (session.LiarsDiceData.currentPlayerPenaltyIndex <= -1)
                {
                    returnString = "There is not currently a player who needs to choose a penalty.";
                }
                else
                {
                    if (terms.Contains("die"))
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Die, true);
                    }
                    else if (terms.Contains("dare"))
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Dare, true);
                    }
                    else if (terms.Contains("strip"))
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Strip, true);
                    }
                    else if (terms.Contains("token") && session.LiarsDiceData.TokensAvailable)
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Token, true);
                    }
                    else
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Die, true);
                    }
                }
            }
            else if (terms.Contains("adddie"))
            {
                if (session.State != GameState.GameInProgress)
                {
                    returnString = "Failed: this command requires the game to be in progress to function.";//for " + GetGameName() + " only work while the game is running.";
                }
                else
                {
                    int playerNumber = Utils.GetNumberFromInputs(terms);
                    playerNumber -= 1;
                    if (playerNumber >= 0 && playerNumber <= session.Players.Count)
                    {
                        session.LiarsDiceData.LiarsDicePlayers[playerNumber].PlayerDice += 1;
                        returnString = "Added 1 die for " + Utils.GetCharacterUserTags(session.LiarsDiceData.LiarsDicePlayers[playerNumber].PlayerName) + "." +
                            "(" + session.LiarsDiceData.LiarsDicePlayers[playerNumber].PlayerDice + " dice remaining)";
                    }
                    else
                    {
                        returnString = "Player number invalid. Input a player number between 1 and the number playing.";
                    }
                }
            }
            else if (terms.Contains("removedie"))
            {
                if (session.State != GameState.GameInProgress)
                {
                    returnString = "Failed: this command requires the game to be in progress to function.";//for " + GetGameName() + " only work while the game is running.";
                }
                else
                {
                    int playerNumber = Utils.GetNumberFromInputs(terms);
                    playerNumber -= 1;
                    if (playerNumber >= 0 && playerNumber <= session.Players.Count)
                    {
                        returnString = ApplyPenalty(diceBot, session, playerNumber, PenaltyType.Die, false);
                    }
                    else
                    {
                        returnString = "Player number invalid. Input a player number between 1 and the number playing.";
                    }
                }
            }
            else if (terms.Contains("setstartdice") || terms.Contains("startdice"))
            {
                int amount = Utils.GetNumberFromInputs(terms);
                if (amount >= 20)
                {
                    returnString = "Failed: start dice cannot exceed 20.";
                }
                else if (amount >= 1)
                {
                    session.LiarsDiceData.StartingDice = amount;
                    returnString = "Set the starting dice for this session to [b]" + amount + "[/b].";
                }
                else
                {
                    returnString = "Failed: start dice need to be at least 1.";
                }
            }
            else
            {
                returnString = "A command for " + GetGameName() + " was not found.";
            }

            return returnString;
        }
    }

    public class LiarsDiceData
    {
        public bool RulesSet;
        public LiarsDiceBet CurrentBet;
        public int DiceSides = 6;
        public int StartingDice = 5;
        public bool TokensAvailable = false;
        public bool WildOnes = false;
        public LiarsDiceBettingRule BettingRule = LiarsDiceBettingRule.HigherSidesOrHigherNumber_PriorityNumber;

        public List<LiarsDicePlayer> LiarsDicePlayers = new List<LiarsDicePlayer>();

        public int currentPlayerIndex = -1;
        public int currentPlayerPenaltyIndex = -1;

        public int winningPlayerForRound = -1;
    }

    public class LiarsDicePlayer 
    {
        public string PlayerName;
        public int PlayerDice;
        public int PlayerTokens;
        public bool Active;
        public bool Eliminated;

        public DiceRoll ThisRoundDice;

        public override string ToString()
        {
            return PlayerName + " (" + PlayerDice + " dice)" + (Active? "" : " (inactive)");
        }
    }

    public class LiarsDiceBet
    {
        public int DiceNumber;
        public int DiceFace;
        public int BettingPlayerIndex;

        public override string ToString()
        {
            return DiceNumber + " dice showing " + DiceFace;
        }
    }

    public enum PenaltyType
    {
        NONE,
        Die,
        Dare,
        Strip,
        Token
    }

    public enum LiarsDiceBettingRule
    {
        NONE,
        SameSidesHigherNumber,
        HigherSidesOrHigherNumber_PriorityFace,
        HigherSidesOrHigherNumber_PriorityNumber
    }
}
