using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class LiarsDice : IGame
    {
        #region gameProperties
        public const int MAX_DICE = 20;

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
        #endregion
        public string GetGameHelp()
        {
            string thisGameCommands = "setante, newround, shuffleplayers, showplayers, adddie (playername), removedie (playername), addtoken (playername), removetoken (playername), setturn (playername), wild1s, setsides #, setstartdice #, setstartingtokens #, setmode (elimination/ penalty/ dicepenalty/ tokenpenalty), forcepenalty (die/dare/strip/token)" +
                    "\n(as current player only): bet # (face as text) (i.e.: !gc bet 4 threes), challenge, penalty (die/dare/strip/token/forfeit)";
            string thisGameStartupOptions = "# (sets ante amount), sides:# (use dice with # sides), tokens:# (set starting tokens (lives) number), sameside (allow increasing # with same side for betting), increasingside (req die face >= existing for betting), increasingnumber (req die number >= existing for betting), startDice:# (set starting dice amount to #), 1swild (a 1 counts as any #), elimination, penalty, dicepenalty, tokenpenalty (sets the starting game penalty mode)" +
                    "\nThe default rules are: Penalty mode elimination, 5 dice with 6 sides, increasing number, no wilds, no tokens";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, false, false);
        }

        public string GetStartingDisplay()
        {
            return FChatDicebot.TextFormat.Emoji("dbliardice1") + FChatDicebot.TextFormat.Emoji("dbliardice2");
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
                //print rules
                string diceSidesMessage = "Die Type: d" + session.LiarsDiceData.DiceSides;

                string gameMode = "Mode: " + GetGameModeString(session);
                string wilds = session.LiarsDiceData.WildOnes ? "1s are wild" : "no wilds";

                string startingDice = "" + session.LiarsDiceData.StartingDice + " starting dice";
                string tokens = session.LiarsDiceData.StartingTokens > 0?( "" + session.LiarsDiceData.StartingTokens + " starting penalty tokens"):"no penalty tokens available";
                string betRule = "Betting Rule: " + GetBetsRuleString(session);

                string rules = gameMode + ", " + betRule + ", " + diceSidesMessage + ", " + wilds + ", " + startingDice + ", " + tokens;

                if(session.LiarsDiceData.LiarsDicePlayers != null && session.LiarsDiceData.LiarsDicePlayers.Count > 0)
                {
                    string playersString = "";
                    foreach (LiarsDicePlayer player in session.LiarsDiceData.LiarsDicePlayers)
                    {
                        if (!string.IsNullOrEmpty(playersString))
                        {
                            playersString += ", ";
                        }

                        playersString += player.Print(session);
                    }
                    fullStatus = "Active Players: " + playersString;
                }
                if(session.State == GameState.GameInProgress)
                {
                    if (session.LiarsDiceData.CurrentBet != null)
                    {
                        if (!string.IsNullOrEmpty(fullStatus))
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

                fullStatus += string.IsNullOrEmpty(rules)? "": ("\n" + rules);
            }

            return fullStatus;
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            //dice sides #, dice amount #, tokens amount #
            //set sameside higher die vs higher side or die
            //set penalty mode
            //set ante

            messageString = "";

            if(!session.LiarsDiceData.RulesSet)
            {
                session.LiarsDiceData.RulesSet = true;
                LiarsDiceGameMode setGameMode = LiarsDiceGameMode.Elimination;

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


                string diceSidesMessage = "";
                int startingSides = GetNumberFromTerms(terms, "sides", 6);
                //foreach (string s in terms)
                //{
                //    if (s.StartsWith("sides"))
                //    {
                //        string num = s.Replace(":","").Replace("sides", "").Trim();
                //        int parsed = -1;
                //        int.TryParse(num, out parsed);
                //        if (parsed > 1 && parsed <= MAX_DICE)
                //            startingSides = parsed;
                //    }
                //}

                session.LiarsDiceData.DiceSides = startingSides;
                diceSidesMessage = "(Die Type: d" + startingSides + ")";

                string wildMessage = "(no wilds)";
                if(terms.Contains("wild1s") || terms.Contains("wild") || terms.Contains("1swild"))
                {
                    session.LiarsDiceData.WildOnes = true;
                    wildMessage = "(1s are wild)";
                }



                string startingDiceMessage = "";
                int startingDice = GetNumberFromTerms(terms, "startdice", 5);
                //foreach (string s in terms)
                //{
                //    if (s.StartsWith("startdice"))
                //    {
                //        string num = s.Replace(":", "").Replace("startdice", "").Trim();
                //        int parsed = -1;
                //        int.TryParse(num, out parsed);
                //        if (parsed > 1 && parsed <= MAX_DICE)
                //            startingDice = parsed;
                //    }
                //}

                session.LiarsDiceData.StartingDice = startingDice;
                startingDiceMessage = "(" + session.LiarsDiceData.StartingDice + " starting dice)";

                string tokensMessage = "(no penalty tokens)";
                int startingTokens = GetNumberFromTerms(terms, "tokens", 0);// 0;
                //foreach(string s in terms)
                //{
                //    if(s.StartsWith("tokens"))
                //    {
                //        string num = s.Replace(":", "").Replace("tokens", "").Trim();
                //        int parsed = -1;
                //        int.TryParse(num, out parsed);
                //        if (parsed > 0 && parsed <= MAX_DICE)
                //        {
                //            startingTokens = parsed;
                //            setGameMode = LiarsDiceGameMode.TokenOnly;
                //        }
                //    }
                //}
                if (startingTokens > 0)
                {
                    setGameMode = LiarsDiceGameMode.TokenOnly;
                    session.LiarsDiceData.StartingTokens = startingTokens;
                    tokensMessage = "(" + startingTokens + " starting penalty tokens)";
                }

                if (terms.Contains("elimination"))
                {
                    setGameMode = LiarsDiceGameMode.Elimination;
                }
                else if (terms.Contains("penalty") || terms.Contains("penalties"))
                {
                    setGameMode = LiarsDiceGameMode.Penalties;
                }
                else if (terms.Contains("diceremoval") || terms.Contains("dicepenalty"))
                {
                    setGameMode = LiarsDiceGameMode.DiceRemoval;
                }
                else if (terms.Contains("tokenremoval") || terms.Contains("tokenpenalty"))
                {
                    setGameMode = LiarsDiceGameMode.TokenOnly;
                }

                session.LiarsDiceData.CurrentGameMode = setGameMode;

                string settingsMessage = betTypeMessage + "";
                if (!string.IsNullOrEmpty(settingsMessage))
                    settingsMessage += " ";
                settingsMessage += "(Penalty Mode: " + GetGameModeString(session) + ") " + diceSidesMessage + " " + wildMessage + " " + startingDiceMessage + (tokensMessage.Length > 0 ? " " : "") + tokensMessage + (ante > 0? " (ante set to " + ante + ")" : "");

                messageString = settingsMessage;
            }

            return true;
        }

        private int GetNumberFromTerms(string[] terms, string startingText, int defaultNumber)
        {
            int returnVal = defaultNumber;
            foreach (string s in terms)
            {
                if (s.StartsWith(startingText))
                {
                    string num = s.Replace(":", "").Replace(startingText, "").Trim();
                    int parsed = -1;
                    int.TryParse(num, out parsed);
                    if (parsed > 0 && parsed <= MAX_DICE)
                    {
                        returnVal = parsed;
                    }
                }
            }
            return returnVal;
        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            string messageString = "";
            if (session.LiarsDiceData != null && session.LiarsDiceData.LiarsDicePlayers != null)
            {
                int thisPlayerIndex = GetIndexOfPlayerName(characterName, session.LiarsDiceData.LiarsDicePlayers);

                if (session.LiarsDiceData.CurrentBet != null && session.LiarsDiceData.CurrentBet.BettingPlayerIndex == thisPlayerIndex)
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

                if (session.LiarsDiceData.currentPlayerIndex >= 0 && session.LiarsDiceData.LiarsDicePlayers.Count > session.LiarsDiceData.currentPlayerIndex)
                {
                    LiarsDicePlayer currentActivePlayerNow = session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.currentPlayerIndex];
                    if (!currentActivePlayerNow.Active || currentActivePlayerNow.Eliminated)
                    {
                        MoveActiveTurnToNextPlayer(new System.Random(), session);
                    }
                }
                if (session.LiarsDiceData.currentPlayerPenaltyIndex >= 0 && session.LiarsDiceData.LiarsDicePlayers.Count > session.LiarsDiceData.currentPlayerIndex)
                {
                    LiarsDicePlayer currentActivePlayerNow = session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.currentPlayerPenaltyIndex];
                    if (!currentActivePlayerNow.Active || currentActivePlayerNow.Eliminated)
                    {
                        session.LiarsDiceData.currentPlayerPenaltyIndex = -1;
                    }
                }
            }

            messageString += "\n" + TextFormat.GetCharacterUserTags(characterName) + " removed. " + CurrentPlayerTurn(session);

            if (session.State == GameState.GameInProgress && GameIsComplete(session))
            {
                messageString += FinishGame(session, botMain.DiceBot);

            }

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
                    PlayerTokens = session.LiarsDiceData.StartingTokens,
                    Active = true });
            }

            session.LiarsDiceData.AnteCollected = false;

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
            if (session.Ante > 0 && !session.LiarsDiceData.AnteCollected)
            {
                anteString = "\n";
                session.LiarsDiceData.AnteCollected = true;
                for (int i = 0; i < session.Players.Count; i++)
                {
                    if (!string.IsNullOrEmpty(anteString))
                    {
                        anteString += "\n";
                    }

                    string betstring = "";

                    betstring = botMain.DiceBot.BetChips(new MessageAddress(session.GetMessageAddress(), session.Players[i]), session.Ante, false);

                    anteString += betstring;
                }
            }

            //declare turn order
            string turnOrder = GetPlayerList(session, false);

            string currentPlayerTurn = CurrentPlayerTurn(session);
            string botMessage = "[b]" + GetGameName() + "[/b]: A new round has started.\nTurn Order: " + turnOrder + anteString + "\n[color=yellow]Rolling player dice...[/color]\n\n" + currentPlayerTurn;
            return botMessage;
        }

        private string CurrentPlayerTurn(GameSession session)
        {
            if(session.LiarsDiceData.LiarsDicePlayers.Count == 0)
            {
                return "(Game not started)";
            }
            if(session.LiarsDiceData.currentPlayerIndex >= session.LiarsDiceData.LiarsDicePlayers.Count)
            {
                return "(Error: Player count mismatch)";
            }
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
                if(!p.Eliminated && p.PlayerDice > 0)
                {
                    MessageAddress address = new MessageAddress(session.GetMessageAddress(), p.PlayerName);
                    DiceRoll d = new DiceRoll(address, botMain) { DiceRolled = p.PlayerDice, DiceSides = session.LiarsDiceData.DiceSides };
                    d.Roll(botMain.DiceBot.random);
                    string rollResult = d.ResultString(DiceRollFormat.OjEicon6, false);
                    p.ThisRoundDice = d;

                    botMain.SendPrivateMessage("Your [b]Liar's Dice[/b] rolls for this round: " + rollResult, address);
                }
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

                allDiceString += (playerNames ? ( TextFormat.GetCharacterUserTags(p.PlayerName) + ": ") : "") + rollResult;
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
            if (session.LiarsDiceData.CurrentGameMode == LiarsDiceGameMode.DiceRemoval)
                return "(die, forfeit)";
            else if (session.LiarsDiceData.CurrentGameMode == LiarsDiceGameMode.TokenOnly)
                return "(token, forfeit)";
            else if (session.LiarsDiceData.CurrentGameMode == LiarsDiceGameMode.Elimination)
                return "(forfeit)";
            else if(session.LiarsDiceData.StartingTokens <= 0)
                return "(die, dare, strip, forfeit)";
            else
                return "(die, dare, strip, token, forfeit)";
        }

        private string GetGameModeString(GameSession session)
        {
            string rtn = "NONE";
            switch(session.LiarsDiceData.CurrentGameMode)
            {
                case LiarsDiceGameMode.NONE: rtn = "NONE"; break;
                case LiarsDiceGameMode.DiceRemoval: rtn = "Dice Removal"; break;
                case LiarsDiceGameMode.Penalties: rtn = "Penalties"; break;
                case LiarsDiceGameMode.Elimination: rtn = "Elimination"; break;
                case LiarsDiceGameMode.TokenOnly: rtn = "Token Removal"; break;
            }
            return rtn;
        }

        private string GetBetsRuleString(GameSession session)
        {
            string rtn = "NONE";
            switch (session.LiarsDiceData.BettingRule)
            {
                case LiarsDiceBettingRule.NONE: rtn = "NONE"; break;
                case LiarsDiceBettingRule.SameSidesHigherNumber: rtn = "Same face higher amount"; break;
                case LiarsDiceBettingRule.HigherSidesOrHigherNumber_PriorityFace: rtn = "Higher face or amount (cannot reduce face)"; break;
                case LiarsDiceBettingRule.HigherSidesOrHigherNumber_PriorityNumber: rtn = "Higher face or amount (cannot reduce amount)"; break;
                case LiarsDiceBettingRule.HigherSidesOrHigherNumber_BothFixed: rtn = "Higher face or amount (cannot reduce either)"; break;
            }
            return rtn;
        }

        private string GetPenaltyTypeString(GameSession session)
        {
            string rtn = "NONE";
            switch (session.LiarsDiceData.CurrentGameMode)
            {
                case LiarsDiceGameMode.NONE: rtn = "NONE"; break;
                case LiarsDiceGameMode.DiceRemoval: rtn = "will lose a die"; break;
                case LiarsDiceGameMode.Penalties: rtn = "must choose a penalty. " + GetPenaltiesAvailable(session); break;
                case LiarsDiceGameMode.Elimination: rtn = "will be eliminated"; break;
                case LiarsDiceGameMode.TokenOnly: rtn = "will lose a token"; break;
            }
            return rtn;
        }

        public LiarsDicePlayer GetLiarsDicePlayerByName(GameSession session, string name, out int foundIndex)
        {
            foundIndex = -1;
            for (int i = 0; i < session.LiarsDiceData.LiarsDicePlayers.Count; i++ )
            {
                LiarsDicePlayer thisPlayer = session.LiarsDiceData.LiarsDicePlayers[i];
                if(thisPlayer.PlayerName.ToLower() == name.ToLower())
                {
                    foundIndex = i;
                    return thisPlayer;
                }
            }
            return null;
                //return session.LiarsDiceData.LiarsDicePlayers.FirstOrDefault(a => a.PlayerName.ToLower() == name.ToLower());
        }

        private string ChallengeCurrentBet(DiceBot diceBot, GameSession session, string bettingPlayer, string character)
        {
            string challengeResult = "";
            //string penaltiesAvailable = GetPenaltiesAvailable(session);

            //bool pickPenalty = session.LiarsDiceData.CurrentGameMode == LiarsDiceGameMode.Penalties;
            if(DiceBetIsTrue(session))
            {
                session.LiarsDiceData.winningPlayerForRound = GetIndexOfPlayerName(bettingPlayer, session.LiarsDiceData.LiarsDicePlayers);
                session.LiarsDiceData.currentPlayerPenaltyIndex = GetIndexOfPlayerName(character, session.LiarsDiceData.LiarsDicePlayers);

                string penaltyPlayerString = TextFormat.GetCharacterUserTags(character) + " " + GetPenaltyTypeString(session) + ".";
                //string penaltyPlayerString = TextFormat.GetCharacterUserTags(character) + (session.Ante > 0 ? " will lose one die." : " must now pick their penalty! " + penaltiesAvailable);
                challengeResult = "The current bet was [b]TRUE[/b]. " + TextFormat.GetCharacterUserTags(character) + "'s challenge failed.\n" + penaltyPlayerString;
            }
            else
            {
                session.LiarsDiceData.winningPlayerForRound = GetIndexOfPlayerName(character, session.LiarsDiceData.LiarsDicePlayers);
                session.LiarsDiceData.currentPlayerPenaltyIndex = GetIndexOfPlayerName(bettingPlayer, session.LiarsDiceData.LiarsDicePlayers);

                string penaltyPlayerString = TextFormat.GetCharacterUserTags(bettingPlayer) + " " + GetPenaltyTypeString(session) + ".";
                challengeResult = "The current bet was [b]FALSE[/b]. " + TextFormat.GetCharacterUserTags(character) + "'s challenge succeeded.\n" + penaltyPlayerString;
            }

            switch(session.LiarsDiceData.CurrentGameMode)
            {
                case LiarsDiceGameMode.DiceRemoval:
                    challengeResult += "\n" + ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Die, false);
                    break;
                case LiarsDiceGameMode.TokenOnly:
                    challengeResult += "\n" + ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Token, false);
                    break;
                case LiarsDiceGameMode.Elimination:
                    challengeResult += "\n" + ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Elimination, false);
                    break;
                case LiarsDiceGameMode.NONE:
                case LiarsDiceGameMode.Penalties:
                    break;
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
                if (terms.Contains("eleven") || terms.Contains("elevens"))
                    dieFace = 11;
                if (terms.Contains("twelve") || terms.Contains("twelves"))
                    dieFace = 12;
                if (terms.Contains("thirteen") || terms.Contains("thirteens"))
                    dieFace = 13;
                if (terms.Contains("fourteen") || terms.Contains("fourteens"))
                    dieFace = 14;
                if (terms.Contains("fifteen") || terms.Contains("fifteens"))
                    dieFace = 15;
                if (terms.Contains("sixteen") || terms.Contains("sixteens"))
                    dieFace = 16;
                if (terms.Contains("seventeen") || terms.Contains("seventeens"))
                    dieFace = 17;
                if (terms.Contains("eightteen") || terms.Contains("eightteens"))
                    dieFace = 18;
                if (terms.Contains("nineteen") || terms.Contains("nineteens"))
                    dieFace = 19;
                if (terms.Contains("twenty") || terms.Contains("twentys") || terms.Contains("twenties"))
                    dieFace = 20;
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
            string outputString = TextFormat.GetCharacterUserTags(player.PlayerName);
            switch(penalty)
            {
                case PenaltyType.Elimination:
                    {
                        if (listChoice)
                            outputString += " has chosen [b]ELIMINATION[/b] for their penalty for this round.\n" + TextFormat.GetCharacterUserTags(player.PlayerName);

                        outputString += " has lost the game!";
                        player.Eliminated = true;
                    }
                    break;
                case PenaltyType.Die:
                    {
                        player.PlayerDice -= 1;
                        if(listChoice)
                            outputString += " has chosen to lose a [b]DIE[/b] for their penalty for this round.\n" + TextFormat.GetCharacterUserTags(player.PlayerName);

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
                            outputString += " has chosen to lose a [b]TOKEN[/b] for their penalty for this round.\n" + TextFormat.GetCharacterUserTags(player.PlayerName);

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
                returnString += "\n\n[b]Liar's Dice[/b]: The game has finished. " + TextFormat.GetCharacterUserTags(winner.PlayerName) + " wins!";

            MessageAddress address = new MessageAddress(session.GetMessageAddress(), winner.PlayerName);
            if (session.Ante > 0)
            {
                returnString += "\n" + diceBot.ClaimPot(address, 1);
            }

            diceBot.RemoveGameSession(address, session.CurrentGame);

            return returnString;
        }

        private string GetPlayerList(GameSession session, bool includeElminated)
        {
            string rtn = "";
            foreach(LiarsDicePlayer p in session.LiarsDiceData.LiarsDicePlayers)
            {
                if (includeElminated || !p.Eliminated)
                {
                    if (!string.IsNullOrEmpty(rtn))
                        rtn += ", ";

                    rtn += p.Print(session);
                }
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
            returnString = TextFormat.GetCharacterUserTags(character) + " has bet that there are " + bet.ToString();
            returnString += "\n" + CurrentPlayerTurn(session);

            return returnString;
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, MessageAddress address, GameSession session, string[] terms, string[] rawTerms)
        {
            if (session.Players.Count(a => a == address.character) < 1)
            //if(session.LiarsDiceData.LiarsDicePlayers.Count(a => a.PlayerName == character) < 1)
            {
                return "Game commands for " + GetGameName() + " can only be used by characters who are playing the game.";
            }

            bool characterIsCurrentActivePlayer = address.character == GetCurrentActivePlayerName(session);
            bool characterIsCurrentActivePenaltyPlayer = address.character == GetCurrentActivePenaltyPlayerName(session);

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
                string playerList = GetPlayerList(session, true);

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
                    returnString = "It is currently " + TextFormat.GetCharacterUserTags(GetCurrentActivePlayerName(session)) + "'s turn.";
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
                                    returnString = AcceptBet(botMain, address.character, session, bet);
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
                                        returnString = AcceptBet(botMain, address.character, session, bet);
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
                                        returnString = AcceptBet(botMain, address.character, session, bet);
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
                                        returnString = AcceptBet(botMain, address.character, session, bet);
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

                            string challengeResultString = ChallengeCurrentBet(diceBot, session, session.LiarsDiceData.LiarsDicePlayers[session.LiarsDiceData.CurrentBet.BettingPlayerIndex].PlayerName , address.character);

                            returnString = TextFormat.GetCharacterUserTags(address.character) + " has chosen to [b]Challenge[/b] the current bet ([b]" + session.LiarsDiceData.CurrentBet.ToString() + "[/b])!\n[color=yellow]Revealing Dice Rolls...[/color]\n\n" + allDiceString + "\n" + challengeResultString;
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
                    else if (terms.Contains("token") && session.LiarsDiceData.StartingTokens > 0)
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Token, true);
                    }
                    else if ((terms.Contains("elimination") || terms.Contains("forfeit")) && session.LiarsDiceData.StartingTokens > 0)
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Elimination, true);
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
                    else if (terms.Contains("token") && session.LiarsDiceData.StartingTokens > 0)
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Token, true);
                    }
                    else if ((terms.Contains("elimination") || terms.Contains("forfeit")) && session.LiarsDiceData.StartingTokens > 0)
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Elimination, true);
                    }
                    else
                    {
                        returnString = ApplyPenalty(diceBot, session, session.LiarsDiceData.currentPlayerPenaltyIndex, PenaltyType.Die, true);
                    }
                }
            }
            else if (terms.Contains("adddie") || terms.Contains("addtoken") || terms.Contains("removedie") || terms.Contains("removetoken"))
            {
                if (session.State != GameState.GameInProgress)
                {
                    returnString = "Failed: this command requires the game to be in progress to function.";//for " + GetGameName() + " only work while the game is running.";
                }
                else
                {
                    string playerName = Utils.GetUserNameFromFullInputs(rawTerms);
                    playerName = playerName.Replace("adddie", "").Replace("addtoken", "").Replace("removedie", "").Replace("removetoken", "").Trim();
                    int foundIndex = -1;
                    LiarsDicePlayer targetPlayer = GetLiarsDicePlayerByName(session, playerName, out foundIndex);
                    if (targetPlayer == null)
                    {
                        returnString = "Player name invalid (" + playerName + "). The player must have already joined the game.";
                    }
                    else
                    {
                        if (terms.Contains("adddie"))
                        {
                            targetPlayer.PlayerDice += 1;
                            returnString = "Added 1 die for " + TextFormat.GetCharacterUserTags(targetPlayer.PlayerName) + "." +
                                "(" + targetPlayer.PlayerDice + " dice remaining)";
                        }
                        else if(terms.Contains("addtoken"))
                        {
                            targetPlayer.PlayerTokens += 1;
                            returnString = "Added 1 token for " + TextFormat.GetCharacterUserTags(targetPlayer.PlayerName) + "." +
                                "(" + targetPlayer.PlayerTokens + " tokens remaining)";
                        }
                        else if(terms.Contains("removedie"))
                        {
                            returnString = ApplyPenalty(diceBot, session, foundIndex, PenaltyType.Die, false);
                        }
                        else if(terms.Contains("removetoken"))
                        {
                            returnString = ApplyPenalty(diceBot, session, foundIndex, PenaltyType.Token, false);
                        }
                    }
                }
            }
            else if (terms.Contains("setturn"))
            {
                if (session.State != GameState.GameInProgress)
                {
                    returnString = "Failed: this command requires the game to be in progress to function.";//for " + GetGameName() + " only work while the game is running.";
                }
                else
                {
                    //int playerNumber = Utils.GetNumberFromInputs(terms);
                    //playerNumber -= 1;
                    //if (playerNumber >= 0 && playerNumber <= session.Players.Count)
                    //{
                    //    session.LiarsDiceData.currentPlayerIndex = playerNumber;

                    //    returnString = "Current active player set to " + GetCurrentActivePlayerName(session);
                    //}
                    //else
                    //{
                        string playerName = Utils.GetUserNameFromFullInputs(rawTerms);
                        playerName = playerName.Replace("setturn", "").Trim();
                        int foundIndex = -1;
                        LiarsDicePlayer targetPlayer = GetLiarsDicePlayerByName(session, playerName, out foundIndex);
                        if(targetPlayer != null)
                        {
                            session.LiarsDiceData.currentPlayerIndex = foundIndex;
                            returnString = "Current active player set to " + GetCurrentActivePlayerName(session);
                        }
                        else
                        {
                            returnString = "Player name invalid. The player must have already joined the game.";
                        }
                    //}
                }
            }
            //else if (terms.Contains("removedie"))
            //{
            //    if (session.State != GameState.GameInProgress)
            //    {
            //        returnString = "Failed: this command requires the game to be in progress to function.";//for " + GetGameName() + " only work while the game is running.";
            //    }
            //    else
            //    {
            //        string playerName = Utils.GetUserNameFromFullInputs(rawTerms);
            //        playerName = playerName.Replace("setturn", "").Trim();
            //        int foundIndex = -1;
            //        LiarsDicePlayer targetPlayer = GetLiarsDicePlayerByName(session, playerName, out foundIndex);
            //        if (targetPlayer != null)
            //        {
            //            returnString = ApplyPenalty(diceBot, session, foundIndex, PenaltyType.Die, false);
            //        }
            //        else
            //        {
            //            returnString = "Player name invalid. The player must have already joined the game.";
            //        }
            //        //int playerNumber = Utils.GetNumberFromInputs(terms);
            //        //playerNumber -= 1;
            //        //if (playerNumber >= 0 && playerNumber <= session.Players.Count)
            //        //{
            //        //    returnString = ApplyPenalty(diceBot, session, playerNumber, PenaltyType.Die, false);
            //        //}
            //        //else
            //        //{
            //        //    returnString = "Player number invalid. Input a player number between 1 and the number playing.";
            //        //}
            //    }
            //}
            else if (terms.Contains("wild1s"))
            {
                if (session.LiarsDiceData == null)
                {
                    returnString = "Error: Game Data not found.";
                }
                else
                {
                    session.LiarsDiceData.WildOnes = !session.LiarsDiceData.WildOnes;

                    returnString = "Set wild ones to " + (session.LiarsDiceData.WildOnes ? "ON" : "OFF");
                }
            }
            else if (terms.Contains("setmode") || terms.Contains("penaltymode") || terms.Contains("setpenalty"))
            {
                if (session.LiarsDiceData == null)
                {
                    returnString = "Error: Game Data not found.";
                }
                else
                {
                    LiarsDiceGameMode newMode = LiarsDiceGameMode.NONE;
                    if (terms.Contains("elimination"))
                    {
                        newMode = LiarsDiceGameMode.Elimination;
                    }
                    else if (terms.Contains("penalty") || terms.Contains("penalties"))
                    {
                        newMode = LiarsDiceGameMode.Penalties;
                    }
                    else if (terms.Contains("diceremoval") || terms.Contains("dicepenalty"))
                    {
                        newMode = LiarsDiceGameMode.DiceRemoval;
                    }
                    else if (terms.Contains("tokenremoval") || terms.Contains("tokenpenalty"))
                    {
                        newMode = LiarsDiceGameMode.TokenOnly;
                    }
                    if(newMode != LiarsDiceGameMode.NONE)
                    {
                        session.LiarsDiceData.CurrentGameMode = newMode;
                        returnString = "Set game mode: " + GetGameModeString(session);
                    }
                    else
                    {
                        returnString = "Failed: please specify a penalty mode to set: elimination, penalty, diceremoval, tokenremoval";
                    }
                }
            }
            else if (terms.Contains("setstartdice") || terms.Contains("startdice"))
            {
                int amount = Utils.GetNumberFromInputs(terms);
                if (amount >= MAX_DICE)
                {
                    returnString = "Failed: start dice cannot exceed " + MAX_DICE + ".";
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
            else if (terms.Contains("setsides") || terms.Contains("dicesides"))
            {
                int amount = Utils.GetNumberFromInputs(terms);
                if (amount >= MAX_DICE)
                {
                    returnString = "Failed: dice sides cannot exceed " + MAX_DICE + ".";
                }
                else if (amount >= 2)
                {
                    session.LiarsDiceData.DiceSides = amount;
                    returnString = "Set the dice sides used for this game to [b]" + amount + "[/b].";
                }
                else
                {
                    returnString = "Failed: dice sides need to be at least 2.";
                }
            }
            else if (terms.Contains("settokens") || terms.Contains("startingtokens") || terms.Contains("setstartingtokens"))
            {
                int amount = Utils.GetNumberFromInputs(terms);
                if (amount >= MAX_DICE)
                {
                    returnString = "Failed: starting tokens cannot exceed " + MAX_DICE + ".";
                }
                else if (amount >= 0)
                {
                    session.LiarsDiceData.StartingTokens = amount;
                    returnString = "Set the starting penalty tokens used for this game to [b]" + amount + "[/b].";
                }
                else
                {
                    returnString = "Failed: starting tokens need to be at least 0.";
                }
            }
            else { returnString += "Failed: No such command exists for " + GetGameName(); }

            return returnString;
        }
    }

    public class LiarsDiceData
    {
        public bool RulesSet;
        public LiarsDiceBet CurrentBet;
        public int DiceSides = 6;
        public int StartingDice = 5;
        public LiarsDiceGameMode CurrentGameMode = LiarsDiceGameMode.Elimination;
        public int StartingTokens = 0;
        public bool WildOnes = false;
        public bool AnteCollected = false;
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

        public string Print(GameSession session)
        {
            bool showTokens = (session.LiarsDiceData.CurrentGameMode == LiarsDiceGameMode.TokenOnly || session.LiarsDiceData.CurrentGameMode == LiarsDiceGameMode.Penalties);
            return PlayerName + " (" + PlayerDice + " dice)" + (Active ? "" : " (inactive)") + (Active ? "" : " (inactive)") + (Eliminated ? " (eliminated)" : "") + (showTokens ? "(" + PlayerTokens + " token" + (PlayerTokens != 1? "s":"") +")" : "");
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
        Token,
        Elimination
    }

    public enum LiarsDiceGameMode
    {
        NONE,
        Elimination,
        Penalties,
        DiceRemoval,
        TokenOnly
    }

    public enum LiarsDiceBettingRule
    {
        NONE,
        SameSidesHigherNumber,
        HigherSidesOrHigherNumber_PriorityFace,
        HigherSidesOrHigherNumber_PriorityNumber,
        HigherSidesOrHigherNumber_BothFixed
    }
}
