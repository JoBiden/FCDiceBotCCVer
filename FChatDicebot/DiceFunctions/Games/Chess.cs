using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FChatDicebot.Model;

namespace FChatDicebot.DiceFunctions
{
    public class Chess : IGame
    {
        public string CommandsList = "(!gc move [UCI], !gc status, !gc level [1-5], !gc forfeit, !gc fen, !gc setposition [FEN], !gc eicons [on/off], !gc descriptionforboard [on/off], !gc setwhite [name/off])";

        public string GetGameName()
        {
            return "Chess";
        }

        public int GetMaxPlayers()
        {
            return 2;
        }

        public int GetMinPlayers()
        {
            return 1;
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
            string thisGameCommands = "move [UCI] (e.g. e2e4), status (show board), level [1-5] (set bot difficulty), forfeit, fen, setposition [FEN], eicons [on/off], descriptionforboard [on/off], setwhite [name/off]";
            string thisGameStartupOptions = "level:[1-5] (sets starting bot level), descriptionforboard:[true/false] (use channel description for board), eicons:[true/false] (use eicons for board display)"; //, setwhite:[name] (set who plays white)

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, false, false);
        }

        public string GetStartingDisplay()
        {
            return TextFormat.Emoji("dbchess1") + TextFormat.Emoji("dbchess2");
        }

        public string GetEndingDisplay()
        {
            return "";
        }

        public string GameStatus(GameSession session)
        {
            if (session.State == GameState.GameInProgress)
            {
                var whitePlayer = session.ChessData.WhitePlayer;
                var blackPlayer = session.ChessData.BlackPlayer;
                return $"Chess game is in progress.\n" +
                       $"White: {GetPlayerNameDisplay(whitePlayer)}\n" +
                       $"Black: {GetPlayerNameDisplay(blackPlayer)}\n" +
                       $"{GetBoardStateInfo(session)}\n" +
                       GetBoardDisplay(session);
            }
            else
            {
                return "Chess game has not started yet.";
            }
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            messageString = "";
            if (session.ChessData == null)
            {
                session.ChessData = new ChessData();
            }

            List<string> settingMessages = new List<string>();

            int level = ParseBotLevel(terms, session.ChessData.BotLevel);
            session.ChessData.BotLevel = level;
            settingMessages.Add($"bot level set to {level}");

            // Startup setting: setwhite:PlayerName
            //string setWhiteValue = ParseColonSetting(terms, "setwhite");
            //if (setWhiteValue != null)
            //{
            //    if (setWhiteValue.ToLower() == "off" || setWhiteValue.ToLower() == "random" || setWhiteValue == "")
            //    {
            //        session.ChessData.StartWhitePlayerName = null;
            //        settingMessages.Add("white assignment set to random");
            //    }
            //    else
            //    {
            //        session.ChessData.StartWhitePlayerName = setWhiteValue;
            //        settingMessages.Add($"white assigned to {setWhiteValue}");
            //    }
            //}

            // Startup setting: descriptionforboard:true/false
            string descValue = ParseColonSetting(terms, "descriptionforboard");
            if (descValue != null)
            {
                bool val = descValue.ToLower() == "true" || descValue.ToLower() == "on" || descValue.ToLower() == "yes";

                session.ChessData.UseDescriptionForBoard = val;
                settingMessages.Add($"descriptionforboard set to {val}");
            }

            // Startup setting: eicons:true/false
            string eiconsValue = ParseColonSetting(terms, "eicons");
            if (eiconsValue != null)
            {
                bool val = eiconsValue.ToLower() == "true" || eiconsValue.ToLower() == "on" || eiconsValue.ToLower() == "yes";
                session.ChessData.UseEicons = val;
                settingMessages.Add($"eicons set to {val}");
            }

            messageString = $"({string.Join(", ", settingMessages)})";
            return true;
        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            if (session.State == GameState.GameInProgress)
            {
                var whitePlayer = session.ChessData.WhitePlayer;
                var blackPlayer = session.ChessData.BlackPlayer;
                if (whitePlayer != null && blackPlayer != null)
                {
                    bool leavingIsWhite = (characterName == whitePlayer.Name);
                    bool whiteWon = !leavingIsWhite;

                    string potClaim = "";
                    var winner = whiteWon ? whitePlayer : blackPlayer;
                    if (session.HasWagerStakes && winner != null && !winner.IsBot)
                    {
                        string award = Wager.WagerGameSupport.AwardPotToWinner(botMain.DiceBot, session, winner.Name);
                        if (!string.IsNullOrEmpty(award)) potClaim = "\n" + award;
                    }

                    session.State = GameState.Finished;
                    botMain.DiceBot.RemoveGameSession(session.GetMessageAddress(), session.CurrentGame);

                    return $"{TextFormat.GetCharacterUserTags(characterName)} has left the game. {(whiteWon ? "White" : "Black")} wins!{potClaim}";
                }
            }
            return "";
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            if (session.ChessData == null)
            {
                session.ChessData = new ChessData();
            }

            if (playerNames.Count < 1)
            {
                session.State = GameState.Unstarted;
                return "Failed to start Chess: No players joined.";
            }
            else if (playerNames.Count == 1 && session.Ante > 0)
            {
                return "Failed to start Chess: cannot use ante with a computer player. Change it with '!gc setante 0' or invite another player.";
            }

            // Channel settings checks
            SavedData.ChannelSettings thisChannel = botMain.GetChannelSettings(session.GetMessageAddress());
            if (thisChannel != null)
            {
                if (session.ChessData.UseEicons && !thisChannel.AllowChessEicons)
                {
                    session.State = GameState.Unstarted;
                    return "Failed to start Chess: UseEicons is enabled in game settings, but AllowChessEicons is disabled in channel settings.";
                }
                if (session.ChessData.UseDescriptionForBoard && !thisChannel.AllowSettingChannelDescription)
                {
                    session.State = GameState.Unstarted;
                    return "Failed to start Chess: UseDescriptionForBoard is enabled in game settings, but AllowSettingChannelDescription is disabled in channel settings.";
                }
            }

            string anteString = "";
            if (playerNames.Count == 1)
            {
                // vs the computer: no wager — there's no human to stake against, so void any
                // stake the opener declared while waiting for an opponent.
                if (session.HasWagerStakes)
                {
                    Wager.WagerGameSupport.RefundAll(diceBot, session);
                    anteString = "[sub]No stakes when playing the automaton[/sub]";
                }
            }
            else if (session.HasWagerStakes)
            {
                string commitError;
                if (!Wager.WagerGameSupport.CommitAllStakes(diceBot, session, out commitError))
                {
                    session.State = GameState.Unstarted;
                    return "Session for Chess failed to start: " + commitError;
                }
            }

            session.ChessData.Board = new CustomChessEngine.Board(CustomChessEngine.Ruleset.ClassicChess);
            session.ChessData.Board.LoadFromFen(session.ChessData.StartPositionFen);

            // Determine who plays white
            string startWhite = session.ChessData.StartWhitePlayerName;
            if (playerNames.Count == 1)
            {
                bool humanIsWhite;
                if (startWhite != null)
                {
                    // If setwhite matches the human player, they are white; otherwise the bot is white
                    humanIsWhite = string.Equals(startWhite, playerNames[0], StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    humanIsWhite = r.Next(2) == 0;
                }

                if (humanIsWhite)
                {
                    session.ChessData.WhitePlayer = new ChessPlayer { Name = playerNames[0], IsBot = false, IsWhite = true, BotLevel = 0 };
                    session.ChessData.BlackPlayer = new ChessPlayer { Name = "Bot", IsBot = true, IsWhite = false, BotLevel = session.ChessData.BotLevel };
                }
                else
                {
                    session.ChessData.WhitePlayer = new ChessPlayer { Name = "Bot", IsBot = true, IsWhite = true, BotLevel = session.ChessData.BotLevel };
                    session.ChessData.BlackPlayer = new ChessPlayer { Name = playerNames[0], IsBot = false, IsWhite = false, BotLevel = 0 };
                }
            }
            else
            {
                bool firstIsWhite;
                if (startWhite != null)
                {
                    // If setwhite matches player 0, they are white; if it matches player 1, player 1 is white
                    if (string.Equals(startWhite, playerNames[0], StringComparison.OrdinalIgnoreCase))
                        firstIsWhite = true;
                    else if (string.Equals(startWhite, playerNames[1], StringComparison.OrdinalIgnoreCase))
                        firstIsWhite = false;
                    else
                        firstIsWhite = r.Next(2) == 0; // name didn't match either player, fall back to random
                }
                else
                {
                    firstIsWhite = r.Next(2) == 0;
                }

                if (firstIsWhite)
                {
                    session.ChessData.WhitePlayer = new ChessPlayer { Name = playerNames[0], IsBot = false, IsWhite = true, BotLevel = 0 };
                    session.ChessData.BlackPlayer = new ChessPlayer { Name = playerNames[1], IsBot = false, IsWhite = false, BotLevel = 0 };
                }
                else
                {
                    session.ChessData.WhitePlayer = new ChessPlayer { Name = playerNames[1], IsBot = false, IsWhite = true, BotLevel = 0 };
                    session.ChessData.BlackPlayer = new ChessPlayer { Name = playerNames[0], IsBot = false, IsWhite = false, BotLevel = 0 };
                }
            }

            session.State = GameState.GameInProgress;

            string botMoveMessage = "";
            if ((session.ChessData.Board.WhiteToMove && session.ChessData.WhitePlayer.IsBot) || (!session.ChessData.Board.WhiteToMove && session.ChessData.BlackPlayer.IsBot))
            {
                string uci = "";
                if (session.ChessData.Board.WhiteToMove)
                    uci = MakeBotMove(session.ChessData.Board, session.ChessData.WhitePlayer.BotLevel);
                else
                    uci = MakeBotMove(session.ChessData.Board, session.ChessData.BlackPlayer.BotLevel);

                if (uci != null)
                {
                    botMoveMessage = $"\n{GetPlayerNameDisplay(session.ChessData.WhitePlayer)} plays: [b]{uci}[/b]\n";
                }
            }

            string introMsg = $"[color=yellow]A new [b]Chess[/b] game is starting...[/color]\n";
            if (!string.IsNullOrEmpty(anteString))
                introMsg += anteString + "\n";

            introMsg += $"White: {GetPlayerNameDisplay(session.ChessData.WhitePlayer)}\n";
            introMsg += $"Black: {GetPlayerNameDisplay(session.ChessData.BlackPlayer)}\n";

            if (!string.IsNullOrEmpty(botMoveMessage))
                introMsg += botMoveMessage;

            if (session.ChessData.UseDescriptionForBoard)
            {
                botMain.SetChannelDescription(session.GetChannelKey(), GetBoardDisplay(session));
            }
            else
            {
                introMsg += GetBoardDisplay(session);
            }

            return introMsg;
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, MessageAddress address, GameSession session, string[] terms, string[] rawTerms)
        {
            if (session.ChessData == null)
            {
                session.ChessData = new ChessData();
            }

            bool isAllowedUnstarted = false;
            if (terms.Contains("status") || terms.Contains("board") || terms.Contains("show") || terms.Contains("display") ||
                terms.Contains("level") || terms.Contains("botlevel") || terms.Contains("setlevel") ||
                terms.Contains("fen") || terms.Contains("getfen") ||
                terms.Contains("setposition") || terms.Contains("loadfen") || terms.Contains("fenload") ||
                terms.Contains("reset") || terms.Contains("restart") ||
                terms.Contains("eicons") || terms.Contains("useeicons") || terms.Contains("setwhite") || terms.Contains("white") ||
                terms.Contains("descriptionforboard") || terms.Contains("usedescriptionforboard"))
            {
                isAllowedUnstarted = true;
            }

            if (session.State != GameState.GameInProgress && !isAllowedUnstarted)
            {
                return "Game commands for Chess only work while the game is running.";
            }

            var whitePlayer = session.ChessData.WhitePlayer;
            var blackPlayer = session.ChessData.BlackPlayer;

            if (session.State == GameState.GameInProgress && (whitePlayer == null || blackPlayer == null))
            {
                return "Error: Chess game players are not initialized.";
            }

            if (session.ChessData.Board == null)
            {
                session.ChessData.Board = new CustomChessEngine.Board(CustomChessEngine.Ruleset.ClassicChess);
                session.ChessData.Board.LoadFromFen(session.ChessData.StartPositionFen);
            }
            var board = session.ChessData.Board;

            if (terms.Contains("status") || terms.Contains("board") || terms.Contains("show") || terms.Contains("display"))
            {

                return GameStatus(session); //GetBoardDisplay(session);
            }

            if (terms.Contains("level") || terms.Contains("botlevel") || terms.Contains("setlevel"))
            {
                int newLevel = ParseBotLevel(terms, -1);
                if (newLevel == -1)
                {
                    int num = Utils.GetNumberFromInputs(terms);
                    if (num >= 1 && num <= 5)
                        newLevel = num;
                }

                if (newLevel >= 1 && newLevel <= 5)
                {
                    session.ChessData.BotLevel = newLevel;
                    if (whitePlayer != null && whitePlayer.IsBot) whitePlayer.BotLevel = newLevel;
                    if (blackPlayer != null && blackPlayer.IsBot) blackPlayer.BotLevel = newLevel;
                    return $"Bot level has been set to {newLevel}.";
                }
                else
                {
                    return "Invalid bot level. Please choose a level between 1 and 5.";
                }
            }

            if (terms.Contains("eicons") || terms.Contains("useeicons"))
            {
                bool val = true;
                if (terms.Contains("off") || terms.Contains("false") || terms.Contains("no"))
                {
                    val = false;
                }
                else if (terms.Contains("on") || terms.Contains("true") || terms.Contains("yes"))
                {
                    val = true;
                }
                else
                {
                    val = !session.ChessData.UseEicons;
                }
                SavedData.ChannelSettings channelSettings = botMain.GetChannelSettings(address);// session.GetMessageAddress());
                if (!channelSettings.AllowChessEicons)
                {
                    return "Failed: chess eicons not allowed under this channel's settings.";
                }
                else
                {
                    session.ChessData.UseEicons = val;
                    return $"UseEicons setting has been set to: {val}";
                }
            }

            if (terms.Contains("setwhite") || terms.Contains("white"))
            {
                // Find the player name argument from rawTerms (everything after the setwhite keyword)
                //string targetName = null;
                //int idx = -1;
                //for (int i = 0; i < rawTerms.Length; i++)
                //{
                //    string termLower = rawTerms[i].ToLower();
                //    if (termLower == "setwhite" || termLower == "white")
                //    {
                //        idx = i;
                //        break;
                //    }
                //}
                //if (idx != -1 && idx + 1 < rawTerms.Length)
                //{
                //    targetName = string.Join(" ", rawTerms.Skip(idx + 1));
                //}
                string[] relevantTerms = Utils.RemoveStringFromAllTerms(rawTerms, ":");
                relevantTerms = Utils.GetRemainingTermsAfterRemovingOneTerm(relevantTerms, "setwhite");
                relevantTerms = Utils.GetRemainingTermsAfterRemovingOneTerm(relevantTerms, "white");

                if (relevantTerms.Count() == 0)
                    return "Failed: This command requires a player name";
                //ensure the first digit is a number, ensure the player has more items than 0
                string targetName = Utils.GetUserNameFromFullInputs(relevantTerms);

                // Handle clearing the setting
                if (targetName != null && (targetName.ToLower() == "off" || targetName.ToLower() == "random" || targetName.ToLower() == "none"))
                {
                    session.ChessData.StartWhitePlayerName = null;
                    return "White player assignment has been cleared (will be random).";
                }

                if (string.IsNullOrWhiteSpace(targetName))
                {
                    string current = session.ChessData.StartWhitePlayerName;
                    if (current != null)
                        return $"White is currently set to: {current}";
                    else
                        return "White is currently set to: random. Use !gc setwhite [name] to assign a player as white, or !gc setwhite off to clear.";
                }

                if (session.State == GameState.GameInProgress)
                {
                    // During a game, swap the players if the target is currently black
                    if (whitePlayer != null && string.Equals(targetName, whitePlayer.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return $"{GetPlayerNameDisplay(whitePlayer)} is already playing as White.";
                    }
                    if (blackPlayer != null && string.Equals(targetName, blackPlayer.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // Swap the players
                        var oldWhite = whitePlayer;
                        var oldBlack = blackPlayer;
                        session.ChessData.WhitePlayer = new ChessPlayer { Name = oldBlack.Name, IsBot = oldBlack.IsBot, IsWhite = true, BotLevel = oldBlack.BotLevel };
                        session.ChessData.BlackPlayer = new ChessPlayer { Name = oldWhite.Name, IsBot = oldWhite.IsBot, IsWhite = false, BotLevel = oldWhite.BotLevel };
                        session.ChessData.StartWhitePlayerName = targetName;
                        return $"Players swapped! White: {GetPlayerNameDisplay(session.ChessData.WhitePlayer)}, Black: {GetPlayerNameDisplay(session.ChessData.BlackPlayer)}";
                    }
                    return $"Player '{targetName}' is not in the current game.";
                }
                else
                {
                    // Pre-game: store the desired white player name for use in RunGame
                    session.ChessData.StartWhitePlayerName = targetName;
                    return $"White player has been set to: {targetName} (will take effect when the game starts).";
                }
            }

            if (terms.Contains("descriptionforboard") || terms.Contains("usedescriptionforboard"))
            {
                bool val = true;
                if (terms.Contains("off") || terms.Contains("false") || terms.Contains("no"))
                {
                    val = false;
                }
                else if (terms.Contains("on") || terms.Contains("true") || terms.Contains("yes"))
                {
                    val = true;
                }
                else
                {
                    val = !session.ChessData.UseDescriptionForBoard;
                }

                SavedData.ChannelSettings channelSettings = botMain.GetChannelSettings(address);// session.GetMessageAddress());
                if (!channelSettings.AllowSettingChannelDescription)
                {
                    return "Failed: setting channel description is not allowed under this channel's settings.";
                }
                else
                {
                    session.ChessData.UseDescriptionForBoard = val;
                    return $"UseDescriptionForBoard setting has been set to: {val}";
                }
            }

            if (terms.Contains("forfeit") || terms.Contains("resign"))
            {
                bool resigningPlayerIsWhite = (address.character == whitePlayer.Name);
                if (whitePlayer.Name != address.character && blackPlayer.Name != address.character)
                {
                    return "You cannot forfeit a game you are not playing.";
                }
                bool whiteWon = !resigningPlayerIsWhite;
                FinishGame(session, diceBot, whiteWon);
                return $"{TextFormat.GetCharacterUserTags(address.character)} resigned. {(whiteWon ? "White" : "Black")} wins!";
            }

            if (terms.Contains("fen") || terms.Contains("getfen"))
            {
                return $"Current FEN: {board.GetFen()}";
            }

            if (terms.Contains("setposition") || terms.Contains("loadfen") || terms.Contains("fenload"))
            {
                int idx = -1;
                for (int i = 0; i < rawTerms.Length; i++)
                {
                    string termLower = rawTerms[i].ToLower();
                    if (termLower == "setposition" || termLower == "loadfen" || termLower == "fenload")
                    {
                        idx = i;
                        break;
                    }
                }
                if (idx != -1 && idx + 1 < rawTerms.Length)
                {
                    string fen = string.Join(" ", rawTerms.Skip(idx + 1));
                    try
                    {
                        board.LoadFromFen(fen);
                        session.ChessData.StartPositionFen = fen;

                        string botMoveMessage = "";
                        if (session.State == GameState.GameInProgress)
                        {
                            var curPlayer = board.WhiteToMove ? whitePlayer : blackPlayer;
                            if (curPlayer != null && curPlayer.IsBot)
                            {
                                string uci = MakeBotMove(board, curPlayer.BotLevel);
                                if (uci != null)
                                {
                                    botMoveMessage = $"\n{GetPlayerNameDisplay(curPlayer)} plays: [b]{uci}[/b]\n";
                                }
                            }
                        }
                        return $"Position loaded successfully!{botMoveMessage}\n" + GetBoardDisplay(session);
                    }
                    catch (Exception ex)
                    {
                        return $"Failed to load FEN: {ex.Message}";
                    }
                }
                else
                {
                    return "Usage: !gc setposition <FEN>";
                }
            }

            if (terms.Contains("reset") || terms.Contains("restart"))
            {
                board.LoadFromFen(session.ChessData.StartPositionFen);

                string botMoveMessage = "";
                if (session.State == GameState.GameInProgress)
                {
                    var curPlayer = board.WhiteToMove ? whitePlayer : blackPlayer;
                    if (curPlayer != null && curPlayer.IsBot)
                    {
                        string uci = MakeBotMove(board, curPlayer.BotLevel);
                        if (uci != null)
                        {
                            botMoveMessage = $"\n{GetPlayerNameDisplay(curPlayer)} plays: [b]{uci}[/b]\n";
                        }
                    }
                }
                return $"Chess game has been reset!{botMoveMessage}\n" + GetBoardDisplay(session);
            }

            // Move Command
            string moveInput = "";
            if (terms.Length > 0)
            {
                int startIdx = 0;
                if (terms[0] == "move")
                    startIdx = 1;
                moveInput = string.Concat(terms.Skip(startIdx));
            }

            if (!IsUciMoveFormat(moveInput))
            {
                return "A command for Chess was not found. To make a move, use UCI format, e.g. '!gc e2e4' or '!gc move e2e4'.";
            }

            var currentPlayer = board.WhiteToMove ? whitePlayer : blackPlayer;
            if (currentPlayer.Name != address.character)
            {
                return $"It is not your turn. It is {GetPlayerNameDisplay(currentPlayer)}'s turn.";
            }

            var mg = new CustomChessEngine.MoveGenerator();
            var legalMoves = mg.GenerateLegalMoves(board);
            CustomChessEngine.Move matchedMove = new CustomChessEngine.Move();
            bool found = false;

            foreach (var move in legalMoves)
            {
                string uci = MoveToUci(move, board);
                if (uci == moveInput || (moveInput.Length == 4 && uci == moveInput + "q"))
                {
                    matchedMove = move;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                string legalMovesList = string.Join(", ", legalMoves.Select(m => MoveToUci(m, board)));
                return $"Illegal move. Legal moves are: {legalMovesList}";
            }

            // Play the player's move
            board.MakeMove(matchedMove);

            string playerMoveMsg = $"{TextFormat.GetCharacterUserTags(address.character)} played [b]{moveInput}[/b].\n";
            string playerEndMsg = "";
            bool isGameOverAfterPlayer = CheckGameEndState(session, diceBot, out playerEndMsg);

            if (isGameOverAfterPlayer)
            {
                if (session.ChessData.UseDescriptionForBoard)
                {
                    botMain.SetChannelDescription(session.GetChannelKey(), GetBoardDisplay(session));
                    return playerMoveMsg + "\n" + playerEndMsg;
                }
                else
                {
                    return playerMoveMsg + GetBoardDisplay(session) + "\n" + playerEndMsg;
                }
            }

            string boardAfterPlayer = GetBoardDisplay(session);

            // Handle bot's turn if next player is a bot
            var nextPlayer = board.WhiteToMove ? whitePlayer : blackPlayer;
            if (nextPlayer.IsBot)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                string botUci = MakeBotMove(board, nextPlayer.BotLevel);
                stopwatch.Stop();
                long elapsed = stopwatch.ElapsedMilliseconds;

                if (botUci != null)
                {
                    string botMoveMsg = $"{GetPlayerNameDisplay(nextPlayer)} plays [b]{botUci}[/b].\n";
                    string botEndMsg = "";
                    bool isGameOverAfterBot = CheckGameEndState(session, diceBot, out botEndMsg);

                    if (elapsed <= 2000)
                    {
                        string botMoveMsgOnly = $"{GetPlayerNameDisplay(nextPlayer)} plays [b]{botUci}[/b].";
                        if (isGameOverAfterBot)
                        {
                            botMoveMsgOnly += "\n" + botEndMsg;
                        }

                        if (session.ChessData.UseDescriptionForBoard)
                        {
                            botMain.SetChannelDescription(session.GetChannelKey(), GetBoardDisplay(session));
                            return playerMoveMsg + botMoveMsgOnly;
                        }
                        else
                        {
                            return playerMoveMsg + botMoveMsgOnly + "\n" + GetBoardDisplay(session);
                        }
                    }
                    else
                    {
                        if (session.ChessData.UseDescriptionForBoard)
                        {
                            botMain.SetChannelDescription(session.GetChannelKey(), boardAfterPlayer);
                        }

                        string botMoveMsgOnly = $"{GetPlayerNameDisplay(nextPlayer)} plays [b]{botUci}[/b].";
                        if (isGameOverAfterBot)
                        {
                            botMoveMsgOnly += "\n" + botEndMsg;
                        }

                        string finalBoard = GetBoardDisplay(session);
                        bool useDescription = session.ChessData.UseDescriptionForBoard;
                        string finalMsg = botMoveMsgOnly + (useDescription ? "" : "\n" + finalBoard);
                        var addressCopy = session.GetMessageAddress();
                        string channelKeyCopy = session.GetChannelKey();

                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(2000);

                            botMain.SendMessageInChannel(finalMsg, addressCopy);

                            if (useDescription)
                            {
                                botMain.SetChannelDescription(channelKeyCopy, finalBoard);
                            }
                        });

                        string responseMsg = playerMoveMsg;
                        if (!session.ChessData.UseDescriptionForBoard)
                        {
                            responseMsg += boardAfterPlayer;
                        }
                        return responseMsg;
                    }
                }
            }

            if (session.ChessData.UseDescriptionForBoard)
            {
                botMain.SetChannelDescription(session.GetChannelKey(), boardAfterPlayer);
                return playerMoveMsg;
            }
            else
            {
                return playerMoveMsg + boardAfterPlayer;
            }
        }

        private bool IsUciMoveFormat(string s)
        {
            if (s.Length < 4 || s.Length > 5) return false;
            if (s[0] < 'a' || s[0] > 'h') return false;
            if (s[1] < '1' || s[1] > '8') return false;
            if (s[2] < 'a' || s[2] > 'h') return false;
            if (s[3] < '1' || s[3] > '8') return false;
            if (s.Length == 5)
            {
                char promo = s[4];
                if (promo != 'q' && promo != 'n' && promo != 'b' && promo != 'r') return false;
            }
            return true;
        }

        public string GetBoardDisplay(GameSession session)
        {
            if (session.ChessData == null || session.ChessData.Board == null)
                return "(No board loaded)";

            bool discord = Utils.IsDiscordMessage(session.GetMessageAddress());
            var fenPos = new FChatDicebot.BotCommands.ChessFEN.ChessFENPosition(discord);
            try
            {
                fenPos.loadFromFEN(session.ChessData.Board.GetFen());
                return fenPos.ToBBCode(session.ChessData.UseEicons);
            }
            catch (Exception ex)
            {
                return $"Error rendering board: {ex.Message}\nFEN: {session.ChessData.Board.GetFen()}";
            }
        }

        public string GetBoardStateInfo(GameSession session)
        {
            if (session.ChessData == null || session.ChessData.Board == null)
                return "(No board loaded)";

            bool discord = Utils.IsDiscordMessage(session.GetMessageAddress());
            var board = session.ChessData.Board;
            string returnString = "Turn: " + board.FullmoveNumber + ", " + (board.WhiteToMove ? "white to move" : "black to move") + ", castle? " + GetCastleRights(board);
            return returnString;
        }

        public string GetCastleRights(CustomChessEngine.Board board)
        {
            List<CustomChessEngine.Move> moves = new List<CustomChessEngine.Move>();
            //60 black king, might need to change if game uses different board size, for now this is ok
            //4 white king
            CustomChessEngine.MoveGenerator moveGenerator = new CustomChessEngine.MoveGenerator();
            //moveGenerator.GenerateCastlingMoves(60, board, true, moves);
            //moveGenerator.GenerateCastlingMoves(4, board, false, moves);
            bool whiteKingEligible = CustomChessEngine.Piece.Type(board.Squares[60]) == CustomChessEngine.Piece.King && CustomChessEngine.Piece.HasNotMoved(board.Squares[60]);
            bool blackKingEligible = CustomChessEngine.Piece.Type(board.Squares[4]) == CustomChessEngine.Piece.King && CustomChessEngine.Piece.HasNotMoved(board.Squares[4]);
            bool kingsideWhite = whiteKingEligible && CustomChessEngine.Piece.Type(board.Squares[63]) == CustomChessEngine.Piece.Rook && CustomChessEngine.Piece.HasNotMoved(board.Squares[63]);
            bool queensideWhite = whiteKingEligible && CustomChessEngine.Piece.Type(board.Squares[56]) == CustomChessEngine.Piece.Rook && CustomChessEngine.Piece.HasNotMoved(board.Squares[56]);
            bool queensideBlack = blackKingEligible && CustomChessEngine.Piece.Type(board.Squares[0]) == CustomChessEngine.Piece.Rook && CustomChessEngine.Piece.HasNotMoved(board.Squares[0]);
            bool kingsideBlack = blackKingEligible && CustomChessEngine.Piece.Type(board.Squares[7]) == CustomChessEngine.Piece.Rook && CustomChessEngine.Piece.HasNotMoved(board.Squares[7]);
            //bool queensideBlack = moves.Count(a => a.ToSquare == ) > 0;
            //bool kingsideBlack = moves.Count(a => a.ToSquare == 6) > 0;
            string rtn = ""; //KQkq
            if (kingsideWhite) rtn += "K";
            if (queensideWhite) rtn += "Q";
            if (kingsideBlack) rtn += "k";
            if (queensideBlack) rtn += "q";
            if (string.IsNullOrEmpty(rtn)) rtn = "(none)";
            return rtn;
        }

        private string GetPlayerNameDisplay(ChessPlayer player)
        {
            if (player.IsBot)
                return $"Bot (Level {player.BotLevel})";
            return TextFormat.GetCharacterUserTags(player.Name);
        }

        public string MakeBotMove(CustomChessEngine.Board board, int depth)
        {
            var search = new CustomChessEngine.Search();
            search.StartSearch(board, depth);
            var botMove = search.BestMoveFound;
            if (botMove.Value != 0)
            {
                string uci = MoveToUci(botMove, board);
                board.MakeMove(botMove);
                return uci;
            }
            return null;
        }

        private string MoveToUci(CustomChessEngine.Move move, CustomChessEngine.Board board)
        {
            string from = SqToUci(move.FromSquare, board);
            string to = SqToUci(move.ToSquare, board);
            string promo = "";
            if (move.Flags >= CustomChessEngine.Move.PromoteToQueen)
            {
                if (move.Flags == CustomChessEngine.Move.PromoteToQueen) promo = "q";
                else if (move.Flags == CustomChessEngine.Move.PromoteToKnight) promo = "n";
                else if (move.Flags == CustomChessEngine.Move.PromoteToBishop) promo = "b";
                else if (move.Flags == CustomChessEngine.Move.PromoteToRook) promo = "r";
            }
            return from + to + promo;
        }

        private string SqToUci(int sq, CustomChessEngine.Board board)
        {
            int file = sq % board.BoardWidth;
            int rank = board.BoardHeight - (sq / board.BoardWidth);
            return $"{(char)('a' + file)}{rank}";
        }

        public bool CheckGameEndState(GameSession session, DiceBot diceBot, out string endMessage)
        {
            endMessage = "";
            var board = session.ChessData.Board;
            var moveGenerator = new CustomChessEngine.MoveGenerator();

            bool whiteKingExists = false;
            bool blackKingExists = false;
            for (int i = 0; i < board.Squares.Length; i++)
            {
                int piece = board.Squares[i];
                if (CustomChessEngine.Piece.Type(piece) == CustomChessEngine.Piece.King)
                {
                    if (CustomChessEngine.Piece.IsWhite(piece)) whiteKingExists = true;
                    else blackKingExists = true;
                }
            }

            if (!whiteKingExists)
            {
                endMessage = "GAME OVER! Black Wins!";
                FinishGame(session, diceBot, false);
                return true;
            }
            if (!blackKingExists)
            {
                endMessage = "GAME OVER! White Wins!";
                FinishGame(session, diceBot, true);
                return true;
            }

            var legalMoves = moveGenerator.GenerateLegalMoves(board);
            if (legalMoves.Count == 0)
            {
                bool isCheck = IsCheck(board, moveGenerator);
                if (isCheck)
                {
                    if (board.RulesetUsed.UseCheckmate)
                    {
                        bool whiteWon = !board.WhiteToMove;
                        endMessage = $"[b]CHECKMATE![/b] {(whiteWon ? "White" : "Black")} Wins!";
                        FinishGame(session, diceBot, whiteWon);
                        return true;
                    }
                }
                else
                {
                    if (board.RulesetUsed.StalemateDraw)
                    {
                        endMessage = "[b]STALEMATE![/b] Game is a Draw.";
                        FinishGameDraw(session, diceBot);
                        return true;
                    }
                    else
                    {
                        bool whiteWon = !board.WhiteToMove;
                        endMessage = $"[b]STALEMATE![/b] {(whiteWon ? "White" : "Black")} Wins!";
                        FinishGame(session, diceBot, whiteWon);
                        return true;
                    }
                }
            }

            if (board.RulesetUsed.FiftyMoveRuleDraw && board.HalfmoveClock >= 100)
            {
                endMessage = "DRAW BY 50-MOVE RULE!";
                FinishGameDraw(session, diceBot);
                return true;
            }

            return false;
        }

        private bool IsCheck(CustomChessEngine.Board board, CustomChessEngine.MoveGenerator moveGenerator)
        {
            bool isWhite = board.WhiteToMove;
            int kingSq = -1;
            for (int i = 0; i < board.Squares.Length; i++)
            {
                int piece = board.Squares[i];
                if (CustomChessEngine.Piece.Type(piece) == CustomChessEngine.Piece.King && CustomChessEngine.Piece.IsWhite(piece) == isWhite)
                {
                    kingSq = i;
                    break;
                }
            }

            if (kingSq == -1) return false;
            return moveGenerator.IsSquareAttacked(board, kingSq, !isWhite);
        }

        private void FinishGame(GameSession session, DiceBot diceBot, bool whiteWon)
        {
            session.State = GameState.Finished;
            var winner = whiteWon ? session.ChessData.WhitePlayer : session.ChessData.BlackPlayer;
            if (session.HasWagerStakes && winner != null && !winner.IsBot)
            {
                Wager.WagerGameSupport.AwardPotToWinner(diceBot, session, winner.Name);
            }
            diceBot.RemoveGameSession(session.GetMessageAddress(), session.CurrentGame);
        }

        private void FinishGameDraw(GameSession session, DiceBot diceBot)
        {
            session.State = GameState.Finished;
            // A draw returns every staked currency to whoever staked it (no exchange, no contest).
            if (session.HasWagerStakes)
            {
                Wager.WagerGameSupport.RefundAll(diceBot, session);
            }
            diceBot.RemoveGameSession(session.GetMessageAddress(), session.CurrentGame);
        }

        private int ParseBotLevel(string[] terms, int defaultLevel)
        {
            for (int i = 0; i < terms.Length; i++)
            {
                if (terms[i] == "level" || terms[i] == "botlevel")
                {
                    if (i + 1 < terms.Length && int.TryParse(terms[i + 1], out int val))
                    {
                        if (val >= 1 && val <= 5)
                            return val;
                    }
                }
                else if (terms[i].StartsWith("level:") || terms[i].StartsWith("botlevel:"))
                {
                    string[] parts = terms[i].Split(':');
                    if (parts.Length > 1 && int.TryParse(parts[1], out int val))
                    {
                        if (val >= 1 && val <= 5)
                            return val;
                    }
                }
            }
            return defaultLevel;
        }

        private string ParseColonSetting(string[] terms, string settingName)
        {
            string prefix = settingName.ToLower() + ":";
            for (int i = 0; i < terms.Length; i++)
            {
                if (terms[i].ToLower().StartsWith(prefix))
                {
                    return terms[i].Substring(prefix.Length);
                }
            }
            return null;
        }
    }

    public class ChessData
    {
        public bool RulesAssigned = false;
        public CustomChessEngine.Board Board;
        public string StartWhitePlayerName;
        public ChessPlayer WhitePlayer;
        public ChessPlayer BlackPlayer;
        public int BotLevel = 3; // default depth/level is 3
        public string StartPositionFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        public bool UseEicons = true;
        public bool UseDescriptionForBoard = false;
    }

    public class ChessPlayer
    {
        public string Name;
        public bool IsBot;
        public bool IsWhite;
        public int BotLevel; // if bot, the depth/level
    }
}