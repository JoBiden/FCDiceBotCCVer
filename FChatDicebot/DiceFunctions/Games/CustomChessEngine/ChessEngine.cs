using System;

namespace CustomChessEngine
{
    class ChessEngine
    {
        public ChessEngine()
        {
            Console.WriteLine("Custom Chess Engine v1.0");
            UciLoop();
        }

        static void UciLoop()
        {
            var board = new Board(Ruleset.ClassicChess);
            board.SetStartingPosition();
            MatchRecording lastRecording = null;
            Search search = new Search();

            while (true)
            {
                string input = Console.ReadLine();
                if (string.IsNullOrEmpty(input)) continue;

                string[] tokens = input.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string command = tokens[0].ToLower();

                switch (command)
                {
                    case "uci":
                        Console.WriteLine("id name CustomChessEngine");
                        Console.WriteLine("id author Antigravity");
                        Console.WriteLine("uciok");
                        break;
                    case "isready":
                        Console.WriteLine("readyok");
                        break;
                    case "setoption":
                        // setoption name Hash value 32
                        if (tokens.Length >= 5 && tokens[1].ToLower() == "name" && tokens[2].ToLower() == "hash" && tokens[3].ToLower() == "value")
                        {
                            if (int.TryParse(tokens[4], out int hashSize))
                            {
                                search.SetHashSize(hashSize);
                                Console.WriteLine($"Hash size set to {hashSize}MB");
                            }
                        }
                        break;
                    case "position":
                        // position startpos
                        // position startpos moves e2e4 e7e5
                        if (tokens.Length >= 2 && tokens[1].ToLower() == "startpos")
                        {
                            board.SetStartingPosition();

                            // Check if there are moves provided
                            if (tokens.Length >= 3 && tokens[2].ToLower() == "moves")
                            {
                                for (int i = 3; i < tokens.Length; i++)
                                {
                                    board.MakeUciMove(tokens[i]);
                                }
                            }
                        }
                        else if (tokens.Length >= 2 && tokens[1].ToLower() == "fen")
                        {
                            string fen = "";
                            int movesIndex = -1;

                            // Assemble FEN until "moves" or end of command
                            for (int i = 2; i < tokens.Length; i++)
                            {
                                if (tokens[i].ToLower() == "moves")
                                {
                                    movesIndex = i;
                                    break;
                                }
                                fen += tokens[i] + " ";
                            }

                            board.LoadFromFen(fen.Trim());

                            if (movesIndex != -1)
                            {
                                for (int i = movesIndex + 1; i < tokens.Length; i++)
                                {
                                    board.MakeUciMove(tokens[i]);
                                }
                            }
                        }
                        break;
                    case "d":
                    case "display":
                        board.PrintBoard();
                        break;
                    case "displayraw":
                        board.PrintBoardRaw();
                        break;
                    case "eval":
                    case "evaluate":
                        var eval = new Evaluation();
                        Console.WriteLine("Evaluation: " + eval.Evaluate(board));
                        break;
                    case "go":
                        int depth = 5;
                        for (int i = 1; i < tokens.Length; i++)
                        {
                            if (tokens[i] == "depth" && i + 1 < tokens.Length)
                            {
                                int.TryParse(tokens[i + 1], out depth);
                                break;
                            }
                        }
                        search.StartSearch(board, depth);
                        break;
                    case "move":
                        if (tokens.Length >= 2)
                        {
                            string moveStr = tokens[1];
                            MoveGenerator mg = new MoveGenerator();
                            var legalMoves = mg.GenerateLegalMoves(board);
                            bool found = false;
                            foreach (var move in legalMoves)
                            {
                                string uci = MoveToUci(move, board);
                                if (uci == moveStr.ToLower() || (moveStr.Length == 4 && uci == moveStr.ToLower() + "q"))
                                {
                                    board.MakeUciMove(moveStr);
                                    found = true;
                                    break;
                                }
                            }
                            if (!found) Console.WriteLine("Illegal move.");
                            board.PrintBoard();
                        }
                        break;
                    case "testmoves":
                        var generator = new MoveGenerator();
                        var moves = generator.GenerateLegalMoves(board);
                        Console.WriteLine($"Generated {moves.Count} pseudo-legal moves.");
                        break;
                    case "play":
                        GameController controller = new GameController(board);
                        int depthGame = 5;
                        Ruleset.RulesetData ruleset = Ruleset.GetRuleset(Ruleset.ClassicChess);
                        bool whiteIsAi = false;
                        bool blackIsAi = true;

                        for (int i = 1; i < tokens.Length; i++)
                        {
                            string token = tokens[i].ToLower();
                            if (token == "depth" && i + 1 < tokens.Length)
                            {
                                int.TryParse(tokens[i + 1], out depthGame);
                                i++;
                            }
                            else if (token.StartsWith("w=") || token.StartsWith("white="))
                            {
                                string val = token.Split('=')[1];
                                whiteIsAi = (val == "ai" || val == "a");
                            }
                            else if (token.StartsWith("b=") || token.StartsWith("black="))
                            {
                                string val = token.Split('=')[1];
                                blackIsAi = (val == "ai" || val == "a");
                            }
                            else if (token == "ai" || token == "human")
                            {
                                // Positional: play human ai
                                if (i == 1) whiteIsAi = (token == "ai");
                                else if (i == 2) blackIsAi = (token == "ai");
                            }
                        }
                        lastRecording = controller.Play(depthGame, ruleset, whiteIsAi, blackIsAi);
                        break;
                    case "showhistory":
                        if (lastRecording != null) lastRecording.PrintHistory();
                        else Console.WriteLine("No match recording available.");
                        break;
                    case "replay":
                        if (lastRecording != null && tokens.Length >= 2 && int.TryParse(tokens[1], out int moveNum))
                        {
                            var replayBoard = lastRecording.GetBoardAtMove(moveNum);
                            replayBoard.PrintBoard();
                        }
                        else if (lastRecording == null) Console.WriteLine("No match recording available.");
                        else Console.WriteLine("Usage: replay <moveNumber>");
                        break;
                    case "exportfen":
                        Console.WriteLine(board.GetFen());
                        break;

                    case "quit":
                        return;
                    default:
                        Console.WriteLine("Unknown command: " + command);
                        break;
                }
            }
        }
        static string MoveToUci(Move move, Board board)
        {
            string from = SqToUci(move.FromSquare, board);
            string to = SqToUci(move.ToSquare, board);
            string promo = "";
            if (move.Flags >= Move.PromoteToQueen)
            {
                if (move.Flags == Move.PromoteToQueen) promo = "q";
                else if (move.Flags == Move.PromoteToKnight) promo = "n";
                else if (move.Flags == Move.PromoteToBishop) promo = "b";
                else if (move.Flags == Move.PromoteToRook) promo = "r";
            }
            return from + to + promo;
        }

        static string SqToUci(int sq, Board board)
        {
            int file = sq % board.BoardWidth;
            int rank = board.BoardHeight - (sq / board.BoardWidth);
            return $"{(char)('a' + file)}{rank}";
        }
    }
}
