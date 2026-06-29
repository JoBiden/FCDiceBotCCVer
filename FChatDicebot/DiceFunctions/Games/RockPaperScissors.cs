using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class RockPaperScissors : IGame
    {
        public const int RoundResultWaitMs = 3500;
        public const int PlayerWhisperWaitMs = 4500;

        public string GetGameName()
        {
            return "RockPaperScissors";
        }

        public int GetMaxPlayers()
        {
            return 12;
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
            string thisGameCommands = "setante #, showplayers, newround, setlives # (player name), setmode (wheel OR group)" +
                "(as current player only - send to " + DiceBot.DiceBotCharacter + " in private message): !rock !paper !scissors !lizard !spock";
            string thisGameStartupOptions = "# (sets ante amount), lives:# (sets lives amount to #), lizardspock (activates LizardSpock mode), wheel, group (sets the elimination mode to wheel or group)" +
                "\nThe default rules are: wheel elimination mode, 1 starting life";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, false, false);
        }

        public string GetStartingDisplay()
        {
            return FChatDicebot.TextFormat.Emoji("dbrps1") + FChatDicebot.TextFormat.Emoji("dbrps2");
        }

        public string GetEndingDisplay()
        {
            return "";
        }

        public string GameStatus(GameSession session)
        {
            string fullStatus = "";
            if (session.RockPaperScissorsData != null)
            {
                if(session.RockPaperScissorsData.RockPaperScissorsPlayers != null && session.RockPaperScissorsData.RockPaperScissorsPlayers.Count() > 0)
                {
                    fullStatus = "Current Players: " + GetPlayerList(session);
                }
                if(session.State == GameState.GameInProgress)
                {
                    fullStatus += "\nCurrent Phase: " + session.RockPaperScissorsData.CurrentGamePhase;
                }
                if (session.RockPaperScissorsData.CurrentGamePhase == RockPaperScissorsGamePhase.WaitingForThrows)
                {
                    if (!string.IsNullOrEmpty(fullStatus))
                        fullStatus += "\n";

                    //get who has not yet made a symbol for the round
                    List<RockPaperScissorsPlayer> unfinishedPlayers = session.RockPaperScissorsData.RockPaperScissorsPlayers.Where(a => a.GameSymbol == GameSymbol.NONE).ToList();
                    if(unfinishedPlayers.Count() >= 1)
                    {
                        fullStatus += string.Join(", ", unfinishedPlayers.Select(a => a.PlayerName)) + " still need to send their plays for this round.";
                    }
                }
                string spockMode = session.RockPaperScissorsData.EnableLizardSpock ? ", LizardSpock mode" : "";
                string eliminationMode = session.RockPaperScissorsData.EliminationMode == RockPaperScissorsEliminationMode.Wheel ? "Wheel elimination mode" : "Group elimination mode";
                string startingLives = "Starting Lives: " + session.RockPaperScissorsData.StartingLives;

                fullStatus += "\nRules: " + eliminationMode + ", " + startingLives + spockMode;
            }

            return fullStatus;
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            //starting lives 2-4
            //set ante

            messageString = "";

            if(!session.RockPaperScissorsData.RulesSet)
            {
                session.RockPaperScissorsData.RulesSet = true;

                int startingLives = GetNumberFromTerms(terms, "lives", 1);
                string livesString = "(starting lives set to " + startingLives + ")";
                session.RockPaperScissorsData.StartingLives = startingLives;

                RockPaperScissorsEliminationMode eliminationMode = RockPaperScissorsEliminationMode.Wheel;
                string eliminationString = "(wheel elimination mode)";

                if(terms.Contains("wheel"))
                {
                    eliminationMode = RockPaperScissorsEliminationMode.Wheel;
                    eliminationString = "(wheel elimination mode)";
                }
                else if (terms.Contains("group"))
                {
                    eliminationMode = RockPaperScissorsEliminationMode.Group;
                    eliminationString = "(group elimination mode)";
                }
                session.RockPaperScissorsData.EliminationMode = eliminationMode;

                string lizardSpockString = "";
                if(terms.Contains("lizardspock"))
                {
                    session.RockPaperScissorsData.EnableLizardSpock = true;
                    lizardSpockString = " (Lizard and Spock enabled!)";
                }

                string anteString = "";
                if(ante > 0)
                {
                    anteString = " (ante set to " + ante + ")";
                }
                messageString += livesString + " " + eliminationString + lizardSpockString + anteString;
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
                    if (parsed > 0 && parsed <= 1000)
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
            if (session.RockPaperScissorsData != null && session.RockPaperScissorsData.RockPaperScissorsPlayers != null)
            {

                session.RockPaperScissorsData.RockPaperScissorsPlayers = session.RockPaperScissorsData.RockPaperScissorsPlayers.Where(a => a.PlayerName != characterName).ToList();

                if(session.RockPaperScissorsData.CurrentGamePhase == RockPaperScissorsGamePhase.WaitingForThrows)
                {
                    FinishRoundIfAllPlayersHaveEntered(botMain, session);
                }
            }

            return messageString;
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            string outputString = "";
            
            //set up starting data 
            foreach (string player in playerNames)
            {
                session.RockPaperScissorsData.RockPaperScissorsPlayers.Add(new RockPaperScissorsPlayer()
                {
                    PlayerLives = session.RockPaperScissorsData.StartingLives, 
                    PlayerName = player, 
                    GameSymbol = DiceFunctions.GameSymbol.NONE,
                    Active = true });
            }
            session.RockPaperScissorsData.CollectedAnte = false;
            session.State = DiceFunctions.GameState.GameInProgress;

            //start the new round , prompt all players for throws
            string newRoundString = StartNewRound(botMain, session);

            return outputString + newRoundString;
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {

        }

        private string StartNewRound(BotMain botMain, GameSession session)
        {
            session.RockPaperScissorsData.CurrentGamePhase = RockPaperScissorsGamePhase.WaitingForThrows;
            //reset all players' throws
            foreach(RockPaperScissorsPlayer player in session.RockPaperScissorsData.RockPaperScissorsPlayers)
            {
                player.GameSymbol = GameSymbol.NONE;
                if (player.PlayerLives <= 0) //set players with no lives to inactive
                {
                    player.Active = false;
                }
                if(player.Active && !player.EliminatedForRound)
                {
                    MessagePlayerToSendCommand(botMain, session, new MessageAddress(session.GetMessageAddress(), player.PlayerName), PlayerWhisperWaitMs);
                }
            }

            string anteString = "";
            if(session.HasWagerStakes && !session.RockPaperScissorsData.CollectedAnte)
            {
                session.RockPaperScissorsData.CollectedAnte = true;
                string commitError;
                if (!Wager.WagerGameSupport.CommitAllStakes(botMain.DiceBot, session, out commitError))
                {
                    anteString = commitError;
                }
            }

            string botMessage = "[b]" + GetGameName() + "[/b]: A new round has started.\n" + GetPlayerList(session, true) +
                (string.IsNullOrEmpty(anteString)? "" : "\n" + anteString) + "\n[color=yellow]Ready your throws![/color]\n";
            return botMessage;
        }

        public void MessagePlayerToSendCommand(BotMain botMain, GameSession session, MessageAddress address, int msDelay)
        {
            string lizardSpock = session.RockPaperScissorsData.EnableLizardSpock? " !scissors !lizard or !spock" : " or !scissors";
            string message = "Send your throw here in private for Rock Paper Scissors in " + address.GetChannelKey() + " by using !rock !paper " + lizardSpock + " .";
            if(msDelay > 0)
            {
                botMain.SendFutureMessage(message, address, false, msDelay);
            }
            else
            {
                botMain.SendPrivateMessage(message, address);
            }
        }

        public void ApplySymbolToPlayer(BotMain botMain, GameSession session, string character, GameSymbol symbol)
        {
            if(session.RockPaperScissorsData != null && session.RockPaperScissorsData.RockPaperScissorsPlayers != null)
            {
                var player = GetRockPaperScissorsPlayerByName(session, character);
                if(player != null)
                {
                    player.GameSymbol = symbol;
                    FinishRoundIfAllPlayersHaveEntered(botMain, session);
                }
            }
        }

        public static string GetFlavorTextForAttack(System.Random random)
        {
            string flavorText = "";

            int flavorRoll = random.Next(10);
            switch (flavorRoll)
            {
                case 0:
                    flavorText = "shows their hand is";
                    break;
                case 1:
                    flavorText = "reveals their hand to be a";
                    break;
                case 2:
                    flavorText = "slides in with";
                    break;
                case 3:
                    flavorText = "changes at the last minute to";
                    break;
                case 4:
                    flavorText = "confidently throws a";
                    break;
                case 5:
                    flavorText = "slips in";
                    break;
                case 6:
                    flavorText = "thrusts forward their";
                    break;
                case 7:
                    flavorText = "has thrown a";
                    break;
                case 8:
                    flavorText = "puts out their";
                    break;
                case 9:
                    flavorText = "quickly shows";
                    break;
            }

            return flavorText;
        }

        private void FinishRoundIfAllPlayersHaveEntered(BotMain botMain, GameSession session)
        {

            if(session.RockPaperScissorsData.RockPaperScissorsPlayers.Count(a => a.GameSymbol == GameSymbol.NONE && a.Active && !a.EliminatedForRound) > 0)
            {
                return;
            }

            session.RockPaperScissorsData.CurrentGamePhase = RockPaperScissorsGamePhase.ShowingResult;
            //show all throws
            string resultString = "";

            foreach(RockPaperScissorsPlayer player in session.RockPaperScissorsData.RockPaperScissorsPlayers)
            {
                if(player.PlayerLives > 0)// !player.EliminatedForRound)
                {
                    if(!string.IsNullOrEmpty(resultString))
                    {
                        resultString += "\n";
                    }

                    resultString += TextFormat.GetCharacterUserTags(player.PlayerName) + " " + GetFlavorTextForAttack(botMain.DiceBot.random) + " " + GetThrowString(player.GameSymbol) + "!";
                }
            }

            resultString = "[b]Everyone is ready![/b] They [color=yellow]reveal their hands[/color] and...\n" + resultString;
            List<RockPaperScissorsPlayer> playersOut = EliminatePlayers(session);

            //print eliminated players out
            foreach (RockPaperScissorsPlayer player in playersOut)
            {
                resultString += "\n";

                resultString += TextFormat.GetCharacterUserTags(player.PlayerName) + " was [b]eliminated![/b]";
            }
            if (playersOut.Count == 0)
                resultString += "\n[b]no one[/b] was eliminated...";


            //if (RoundIsComplete(session))
            //{
            //    if(GameIsComplete(session))
            //    {
            //        resultString += "\n" + FinishGame(session, botMain.DiceBot);
            //    }
            //    else
            //    {
            //        RockPaperScissorsPlayer play = GetRoundWinner(session);
            //        resultString += "\nThis round is over! " + (play == null? "(not found)" : TextFormat.GetCharacterUserTags(play.PlayerName)) + " wins the round!";
            //        foreach(RockPaperScissorsPlayer playz in session.RockPaperScissorsData.RockPaperScissorsPlayers)
            //        {
            //            playz.EliminatedForRound = false;
            //        }

            //    }
            //}
            //if (!GameIsComplete(session))
            //{
            //    ResetGameRound(session);
            //    resultString += "\n" + StartNewRound(botMain, session);
            //}

            if (GameIsComplete(session))
            {
                resultString += "\n" + FinishGame(session, botMain.DiceBot);
            }
            else
            {
                //RockPaperScissorsPlayer play = GetRoundWinner(session);
                //resultString += "\nThis round is over! " + (play == null ? "(not found)" : TextFormat.GetCharacterUserTags(play.PlayerName)) + " wins the round!";
                foreach (RockPaperScissorsPlayer playz in session.RockPaperScissorsData.RockPaperScissorsPlayers)
                {
                    playz.EliminatedForRound = false;
                }

                ResetGameRound(session);
                resultString += "\n" + StartNewRound(botMain, session);
            }

            botMain.SendFutureMessage(resultString, session.GetMessageAddress(), true, RoundResultWaitMs);
        }

        public string GetThrowString(GameSymbol symbol)
        {
            string result = "";
            switch(symbol)
            {
                case GameSymbol.Rock:
                    result = "[b]ROCK[/b]";
                    break;
                case GameSymbol.Paper:
                    result = "[b]PAPER[/b]";
                    break;
                case GameSymbol.Scissors:
                    result = "[b]SCISSORS[/b]";
                    break;
                case GameSymbol.Lizard:
                    result = "[b]LIZARD[/b]";
                    break;
                case GameSymbol.Spock:
                    result = "[b]SPOCK[/b]";
                    break;
                case GameSymbol.NONE:
                    result = "(none)";
                    break;
            }

            return result;
        }

        private bool GameIsComplete(GameSession session)
        {
            return session.RockPaperScissorsData.RockPaperScissorsPlayers.Count(a => a.PlayerLives > 0) <= 1;
        }

        //private bool RoundIsComplete(GameSession session)
        //{
        //    return session.RockPaperScissorsData.RockPaperScissorsPlayers.Count(a => !a.EliminatedForRound) == 1;
        //}

        //private RockPaperScissorsPlayer GetRoundWinner(GameSession session)
        //{
        //    if (RoundIsComplete(session))
        //    {
        //        return session.RockPaperScissorsData.RockPaperScissorsPlayers.FirstOrDefault(a => !a.EliminatedForRound);
        //    }
        //    else
        //        return null;
        //}

        private RockPaperScissorsPlayer GetGameWinner(GameSession session)
        {
            if (GameIsComplete(session))
            {
                return session.RockPaperScissorsData.RockPaperScissorsPlayers.FirstOrDefault(a => a.PlayerLives > 0);
            }
            else
                return null;
        }

        private List<RockPaperScissorsPlayer> EliminatePlayers(GameSession session)
        {
            List<RockPaperScissorsPlayer> eliminatedPlayers = new List<RockPaperScissorsPlayer>();

            var viablePlayers = session.RockPaperScissorsData.RockPaperScissorsPlayers.Where(a => a.Active && !a.EliminatedForRound).ToList();

            if(session.RockPaperScissorsData.EliminationMode == RockPaperScissorsEliminationMode.Group)
            {
                List<GameSymbol> allAttackingSymbols = viablePlayers.Select(a => a.GameSymbol).ToList();

                //bool rockEliminated = SymbolEliminated(GameSymbol.Rock, allAttackingSymbols);
                //bool paperEliminated = SymbolEliminated(GameSymbol.Paper, allAttackingSymbols);
                //bool scissorsEliminated = SymbolEliminated(GameSymbol.Scissors, allAttackingSymbols);
                //bool lizardEliminated = SymbolEliminated(GameSymbol.Lizard, allAttackingSymbols);
                //bool spockEliminated = SymbolEliminated(GameSymbol.Spock, allAttackingSymbols);

                //if ((!rockEliminated || !scissorsEliminated || !paperEliminated) ||
                //    (session.RockPaperScissorsData.EnableLizardSpock && (!lizardEliminated || !spockEliminated) ))
                //{
                //    //someone survived: eliminate all the eliminated people

                foreach(RockPaperScissorsPlayer play in viablePlayers)
                {
                    bool eliminated = SymbolEliminated(play.GameSymbol, allAttackingSymbols);
                    if(eliminated)
                    {
                        eliminatedPlayers.Add(play);
                    }
                }
                //}
            }
            else if(session.RockPaperScissorsData.EliminationMode == RockPaperScissorsEliminationMode.Wheel)
            {
                //check players one at a time to see what their attack is and then check the next player to see if that player is eliminated

                for(int i = 0; i < viablePlayers.Count; i++)
                {
                    RockPaperScissorsPlayer currentPlayer = viablePlayers[i];
                    int threatenedIndex = i -1;
                    if (threatenedIndex < 0)
                        threatenedIndex = viablePlayers.Count - 1;

                    RockPaperScissorsPlayer threateningPlayer = viablePlayers[threatenedIndex];

                    if(SymbolEliminated(currentPlayer.GameSymbol, threateningPlayer.GameSymbol))
                    {
                        eliminatedPlayers.Add(currentPlayer);
                    }
                }
                //eliminatedPlayers = viablePlayers.Where(a => a.EliminatedForRound).ToList();
            }

            if (eliminatedPlayers.Count == viablePlayers.Count) //if everyone is eliminated, it's a tie and no one is eliminated
            {
                eliminatedPlayers = new List<RockPaperScissorsPlayer>();
            }

            foreach(RockPaperScissorsPlayer currentPlayer in eliminatedPlayers)
            {
                currentPlayer.EliminatedForRound = true;
                currentPlayer.PlayerLives -= 1;
            }

            return eliminatedPlayers;
        }

        private bool SymbolEliminated(GameSymbol vulnerableSymbol, List<GameSymbol> attackingSymbols)
        {
            bool eliminated = false;
            foreach(GameSymbol symbol in attackingSymbols)
            {
                if (SymbolEliminated(vulnerableSymbol, symbol))
                {
                    eliminated = true;
                    break;
                }
            }
            return eliminated;
        }

        private bool SymbolEliminated(GameSymbol vulnerableSymbol, GameSymbol attackingSymbol)
        {
            bool eliminated = false;
            if(vulnerableSymbol == GameSymbol.Paper)
            {
                if (attackingSymbol == GameSymbol.Lizard || attackingSymbol == GameSymbol.Scissors)
                    eliminated = true;
            }
            else if (vulnerableSymbol == GameSymbol.Scissors)
            {
                if (attackingSymbol == GameSymbol.Spock || attackingSymbol == GameSymbol.Rock)
                    eliminated = true;
            }
            else if (vulnerableSymbol == GameSymbol.Rock)
            {
                if (attackingSymbol == GameSymbol.Spock || attackingSymbol == GameSymbol.Paper)
                    eliminated = true;
            }
            else if (vulnerableSymbol == GameSymbol.Lizard)
            {
                if (attackingSymbol == GameSymbol.Scissors || attackingSymbol == GameSymbol.Rock)
                    eliminated = true;
            }
            else if (vulnerableSymbol == GameSymbol.Spock)
            {
                if (attackingSymbol == GameSymbol.Lizard || attackingSymbol == GameSymbol.Paper)
                    eliminated = true;
            }

            return eliminated;
        }

        private string FinishGame(GameSession session, DiceBot diceBot)
        {
            string returnString = "";
            RockPaperScissorsPlayer winner = GetGameWinner(session);//was GetRoundWinner
            if (winner != null)
                returnString += "\n[b]Rock Paper Scissors[/b]: The game has finished. " + TextFormat.GetCharacterUserTags(winner.PlayerName) + " wins!";

            if (session.HasWagerStakes && winner != null)
            {
                string award = Wager.WagerGameSupport.AwardPotToWinner(diceBot, session, winner.PlayerName);
                if (!string.IsNullOrEmpty(award)) returnString += "\n" + award;
            }

            session.State = DiceFunctions.GameState.Finished;

            diceBot.RemoveGameSession(session.GetMessageAddress(), session.CurrentGame); //auto remove game session here might not be good :: keepsession is ignored

            return returnString;
        }

        private string GetPlayerList(GameSession session, bool newLines = false)
        {
            string rtn = "";
            foreach(RockPaperScissorsPlayer p in session.RockPaperScissorsData.RockPaperScissorsPlayers)
            {
                if (!string.IsNullOrEmpty(rtn))
                {
                    if (newLines)
                        rtn += "\n";
                    else
                        rtn += ", ";
                }

                rtn += p.ToString();
            }
            return rtn;
        }

        public void ResetGameRound(GameSession session)
        {
            session.State = GameState.Unstarted;
            session.RockPaperScissorsData.CurrentGamePhase = RockPaperScissorsGamePhase.Starting;
        }

        public RockPaperScissorsPlayer GetRockPaperScissorsPlayerByName(GameSession session, string name)
        {
            return session.RockPaperScissorsData.RockPaperScissorsPlayers.FirstOrDefault(a => a.PlayerName == name);
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, MessageAddress address, GameSession session, string[] terms, string[] rawTerms)
        {
            if(session.State != GameState.GameInProgress)
            {
                return "Game commands for " + GetGameName() + " only work while the game is running.";
            }
            else if(session.RockPaperScissorsData.RockPaperScissorsPlayers.Count(a => a.PlayerName == address.character) < 1)
            {
                return "Game commands for " + GetGameName() + " can only be used by characters who are playing the game.";
            }

            string returnString = "";
            if (terms.Contains("newround") || terms.Contains("startround"))
            {
                string newRoundString = StartNewRound(botMain, session);
                returnString = newRoundString;
            }
            else if (terms.Contains("showplayers"))
            {
                string playerList = GetPlayerList(session);

                returnString = playerList;
            }
            else if (terms.Contains("setmode"))
            {
                if(terms.Contains("wheel"))
                {
                    session.RockPaperScissorsData.EliminationMode = RockPaperScissorsEliminationMode.Wheel;
                    returnString = "Set game mode to wheel.";
                }
                else if(terms.Contains("group"))
                {
                    session.RockPaperScissorsData.EliminationMode = RockPaperScissorsEliminationMode.Group;
                    returnString = "Set game mode to group.";
                }
                else
                {
                    returnString = "Failed: You must specify 'wheel' or 'group' game mode.";
                }
            }
            else if (terms.Contains("setlives"))
            {
                if (terms.Length < 2)
                {
                    returnString = "Error: improper command format. Use 'setlives # (playername)' with # as the number of lives and (playername) as the user's full display name.";
                }
                else
                {
                    string allInputs = Utils.GetUserNameFromFullInputs(rawTerms);
                    string playerName = allInputs.Substring(allInputs.IndexOf(' ')).Trim();
                    if (terms.Length == 2)
                    {
                        playerName = address.character;
                    }
                    else
                        playerName = playerName.Substring(playerName.IndexOf(' ')).Trim();

                    int inputNumber = Utils.GetNumberFromInputs(terms);

                    RockPaperScissorsPlayer relevantPlayer = GetRockPaperScissorsPlayerByName(session, playerName);

                    if (inputNumber <= 0)
                        returnString = "Error: Cannot set lives to 0 or less.";
                    if (relevantPlayer != null)
                    {
                        relevantPlayer.PlayerLives = inputNumber;
                        returnString = TextFormat.GetCharacterUserTags(relevantPlayer.PlayerName) + " was set to " + inputNumber + " lives.";
                    }
                    else
                    {
                        returnString = "Player name (" + playerName + ") was invalid.";
                    }
                }
            }
            else { returnString += "Failed: No such command exists for " + GetGameName(); }

            return returnString;
        }
    }

    public class RockPaperScissorsData
    {
        public bool RulesSet;
        public int StartingLives = 1;
        public RockPaperScissorsGamePhase CurrentGamePhase;
        public RockPaperScissorsEliminationMode EliminationMode = RockPaperScissorsEliminationMode.Wheel;
        public bool EnableLizardSpock;
        public bool CollectedAnte;
        public List<RockPaperScissorsPlayer> RockPaperScissorsPlayers = new List<RockPaperScissorsPlayer>();

        public int currentPlayerIndex = -1;

        public int winningPlayerForRound = -1;
    }

    public class RockPaperScissorsPlayer 
    {
        public string PlayerName;
        public int PlayerLives;
        public bool Active;
        public bool EliminatedForRound;

        public GameSymbol GameSymbol;

        public override string ToString()
        {
            string lives = PlayerLives == 1 ? "life" : "lives";
            return PlayerName + " (" + PlayerLives + " " + lives + ")" + (EliminatedForRound ? "(eliminated)" : "") + (Active ? "" : " (inactive)");
        }
    }

    public enum GameSymbol
    {
        NONE,
        Rock,
        Paper,
        Scissors,
        Lizard,
        Spock
    }

    public enum RockPaperScissorsGamePhase
    {
        Starting,
        WaitingForThrows,
        ShowingResult
    }

    public enum RockPaperScissorsEliminationMode
    {
        NONE,
        Group,
        Wheel
    }
}
