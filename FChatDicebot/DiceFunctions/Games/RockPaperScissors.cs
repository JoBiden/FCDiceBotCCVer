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
            return 6;
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
            string thisGameCommands = "setante #, showplayers, newround, setlives # (player name)" +
                "(as current player only - send to " + DiceBot.DiceBotCharacter + " in private message): !rock !paper !scissors";
            string thisGameStartupOptions = "# (sets ante amount), lives# (sets lives amount, 2-4)" +
                "\nThe default rules are: 1 starting life";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, false, false);
        }

        public string GetStartingDisplay()
        {
            return "[eicon]dbrps1[/eicon][eicon]dbrps2[/eicon]";
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

                if(terms.Contains("lives2"))
                {
                    session.RockPaperScissorsData.StartingLives = 2;
                    messageString += "(starting lives set to 2)";
                }
                else if (terms.Contains("lives3"))
                {
                    session.RockPaperScissorsData.StartingLives = 3;
                    messageString += "(starting lives set to 3)";
                }
                else if (terms.Contains("lives4"))
                {
                    session.RockPaperScissorsData.StartingLives = 4;
                    messageString += "(starting lives set to 4)";
                }
                
                if(ante > 0)
                {
                    messageString += "(ante set to " + ante + ")";
                }
            }

            return true;
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

            //start the new round , prompt all players for throws
            string newRoundString = StartNewRound(botMain, session);

            session.RockPaperScissorsData.CollectedAnte = false;
            session.State = DiceFunctions.GameState.GameInProgress;

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
                    MessagePlayerToSendCommand(botMain, session.ChannelId, player.PlayerName, PlayerWhisperWaitMs);
                }
            }
            
            string anteString = "";
            if(session.Ante > 0 && !session.RockPaperScissorsData.CollectedAnte)
            {
                anteString = "\n";
                for (int i = 0; i < session.Players.Count; i++)
                {
                    if (!string.IsNullOrEmpty(anteString))
                    {
                        anteString += "\n";
                    }

                    string betstring = "";

                    betstring = botMain.DiceBot.BetChips(session.Players[i], session.ChannelId, session.Ante, false);

                    anteString += betstring;
                }
            }
            session.RockPaperScissorsData.CollectedAnte = true;

            string botMessage = "[b]" + GetGameName() + "[/b]: A new round has started.\n" + GetPlayerList(session, true) +
                (string.IsNullOrEmpty(anteString)? "" : "\n" + anteString) + "\n[color=yellow]Ready your throws![/color]\n";
            return botMessage;
        }

        public void MessagePlayerToSendCommand(BotMain botMain, string channel, string character, int msDelay)
        {
            string message = "Send your throw here in private for Rock Paper Scissors in " + channel + " by using !rock !paper or !scissors .";
            if(msDelay > 0)
            {
                botMain.SendFutureMessage(message, channel, character, false, msDelay);
            }
            else
            {
                botMain.SendPrivateMessage(message, character);
            }
        }

        public void ApplySymbolToPlayer(BotMain botMain, GameSession session, string character, GameSymbol symbol)
        {
            var player = GetRockPaperScissorsPlayerByName(session, character);
            player.GameSymbol = symbol;
            FinishRoundIfAllPlayersHaveEntered(botMain, session);
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
                if(!player.EliminatedForRound)
                {
                    if(!string.IsNullOrEmpty(resultString))
                    {
                        resultString += "\n";
                    }

                    resultString += Utils.GetCharacterUserTags(player.PlayerName) + " " + GetFlavorTextForAttack(botMain.DiceBot.random) + " " + GetThrowString(player.GameSymbol) + "!";
                }
            }

            resultString = "[b]Everyone is ready![/b] They [color=yellow]reveal their hands[/color] and...\n" + resultString;
            List<RockPaperScissorsPlayer> playersOut = EliminatePlayers(session);

            resultString += "\n";
            //print players out
            foreach (RockPaperScissorsPlayer player in playersOut)
            {
                if (!string.IsNullOrEmpty(resultString))
                {
                    resultString += "\n";
                }

                resultString += Utils.GetCharacterUserTags(player.PlayerName) + " was [b]eliminated![/b]";
            }
            if (playersOut.Count == 0)
                resultString += "[b]no one[/b] was eliminated...";


            if (RoundIsComplete(session))
            {
                if(GameIsComplete(session))
                {
                    resultString += "\n" + FinishGame(session, botMain.DiceBot);
                }
                else
                {
                    RockPaperScissorsPlayer play = GetRoundWinner(session);
                    resultString += "\nThis round is over! " + (play == null? "(not found)" : Utils.GetCharacterUserTags(play.PlayerName)) + " wins the round!";
                    foreach(RockPaperScissorsPlayer playz in session.RockPaperScissorsData.RockPaperScissorsPlayers)
                    {
                        playz.EliminatedForRound = false;
                    }
                    
                }
            }

            if(!GameIsComplete(session))
            {
                ResetGameRound(session);
                resultString += "\n" + StartNewRound(botMain, session);
            }


            botMain.SendFutureMessage(resultString, session.ChannelId, null, true, RoundResultWaitMs);
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

        private bool RoundIsComplete(GameSession session)
        {
            return session.RockPaperScissorsData.RockPaperScissorsPlayers.Count(a => !a.EliminatedForRound) == 1;
        }

        private RockPaperScissorsPlayer GetRoundWinner(GameSession session)
        {
            if (RoundIsComplete(session))
            {
                return session.RockPaperScissorsData.RockPaperScissorsPlayers.FirstOrDefault(a => !a.EliminatedForRound);
            }
            else
                return null;
        }

        private List<RockPaperScissorsPlayer> EliminatePlayers(GameSession session)
        {
            List<RockPaperScissorsPlayer> eliminatedPlayers = new List<RockPaperScissorsPlayer>();
            var viablePlayers = session.RockPaperScissorsData.RockPaperScissorsPlayers.Where(a => a.Active && !a.EliminatedForRound);

            int rockCount = viablePlayers.Count(a => a.GameSymbol == GameSymbol.Rock);
            int scissorsCount = viablePlayers.Count(a => a.GameSymbol == GameSymbol.Scissors);
            int paperCount = viablePlayers.Count(a => a.GameSymbol == GameSymbol.Paper);
            int lizardCount = viablePlayers.Count(a => a.GameSymbol == GameSymbol.Lizard);
            int spockCount = viablePlayers.Count(a => a.GameSymbol == GameSymbol.Spock);

            if (rockCount > 0 && paperCount > 0 && scissorsCount == 0) //TODO: add in lizardspock mode
                //if (rockCount > 0 && paperCount >= 0 && scissorsCount == 0 && lizardCount >= 0 && spockCount == 0)
            {
                //all the rock players lose
                eliminatedPlayers = session.RockPaperScissorsData.RockPaperScissorsPlayers.Where(a => a.GameSymbol == GameSymbol.Rock).ToList();
                //eliminatedPlayers.AddRange(session.RockPaperScissorsData.RockPaperScissorsPlayers.Where(a => a.GameSymbol == GameSymbol.Rock).ToList());
            }
            if (rockCount > 0 && scissorsCount > 0 && paperCount == 0)
            {
                //all the scissors players lose
                eliminatedPlayers = session.RockPaperScissorsData.RockPaperScissorsPlayers.Where(a => a.GameSymbol == GameSymbol.Scissors).ToList();
            }
            if (rockCount == 0 && scissorsCount > 0 && paperCount > 0)
            {
                //all the paperCount players lose
                eliminatedPlayers = session.RockPaperScissorsData.RockPaperScissorsPlayers.Where(a => a.GameSymbol == GameSymbol.Paper).ToList();
            }

            foreach (RockPaperScissorsPlayer player in eliminatedPlayers)
            {
                player.EliminatedForRound = true;
                player.PlayerLives -= 1;
            }

            return eliminatedPlayers;
        }

        private string FinishGame(GameSession session, DiceBot diceBot)
        {
            string returnString = "";
            RockPaperScissorsPlayer winner = GetRoundWinner(session);
            if (winner != null)
                returnString += "\n[b]Rock Paper Scissors[/b]: The game has finished. " + Utils.GetCharacterUserTags(winner.PlayerName) + " wins!";

            if (session.Ante > 0)
            {
                returnString += "\n" + diceBot.ClaimPot(winner.PlayerName, session.ChannelId, 1);
            }

            session.State = DiceFunctions.GameState.Finished;

            diceBot.RemoveGameSession(session.ChannelId, session.CurrentGame); //auto remove game session here might not be good :: keepsession is ignored

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

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, GameSession session, string[] terms, string[] rawTerms)
        {
            if(session.State != GameState.GameInProgress)
            {
                return "Game commands for " + GetGameName() + " only work while the game is running.";
            }
            else if(session.RockPaperScissorsData.RockPaperScissorsPlayers.Count(a => a.PlayerName == character) < 1)
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
                        playerName = character;
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
                        returnString = Utils.GetCharacterUserTags(relevantPlayer.PlayerName) + " was set to " + inputNumber + " lives.";
                    }
                    else
                    {
                        returnString = "Player name (" + playerName + ") was invalid.";
                    }
                }
            }
            else
            {
                returnString = "A command for " + GetGameName() + " was not found.";
            }

            return returnString;
        }
    }

    public class RockPaperScissorsData
    {
        public bool RulesSet;
        public int StartingLives = 1;
        public RockPaperScissorsGamePhase CurrentGamePhase;
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
}
