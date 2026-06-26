using System;
using System.Collections.Generic;

namespace CustomChessEngine
{
    public class GameController
    {
        private Board board;
        private MoveGenerator moveGenerator;
        private Search search;
        private MatchRecording recording;
        private List<string> positionHistory;

        public GameController(Board board)
        {
            this.board = board;
            this.moveGenerator = new MoveGenerator();
            this.search = new Search();
            this.recording = new MatchRecording(board.GetFen());
            this.positionHistory = new List<string>();
        }

        public MatchRecording Play(int depth, Ruleset.RulesetData ruleset, bool whiteIsAi, bool blackIsAi)
        {
            Console.WriteLine($"\n--- Entering Play Mode (W: {(whiteIsAi ? "AI" : "Human")}, B: {(blackIsAi ? "AI" : "Human")}) ---");
            Console.WriteLine("Commands: UCI move (e.g., e2e4), 'exit' to quit play mode.");
            Console.WriteLine("Depth: " + depth + ", Ruleset: " + ruleset.Name + " WhiteIsAi: " + whiteIsAi + " BlackIsAi: " + blackIsAi);

            positionHistory.Clear();
            positionHistory.Add(GetRepetitionKey());

            while (true)
            {
                board.PrintBoard();

                if (IsGameOver())
                {
                    break;
                }

                var legalMoves = moveGenerator.GenerateLegalMoves(board);
                if (legalMoves.Count == 0)
                {
                    if (IsCheck())
                    {
                        if (board.RulesetUsed.UseCheckmate)
                        {
                            Console.WriteLine("CHECKMATE!");
                            Console.WriteLine((board.WhiteToMove ? "Black" : "White") + " Wins!");
                            break;
                        }
                        else
                        {
                            //Not a checkmate, king must be captured in this ruleset. Game continues.
                        }
                    }
                    else
                    {
                        if (board.RulesetUsed.StalemateDraw)
                        {
                            Console.WriteLine("STALEMATE!");
                            Console.WriteLine("Game is a Draw.");
                            break;
                        }
                        else
                        {
                            //Stalemate when it is not a draw results in the other side winning
                            Console.WriteLine("STALEMATE!");
                            Console.WriteLine((board.WhiteToMove ? "Black" : "White") + " Wins!");
                            break;
                        }
                    }
                }

                if (IsThreefoldRepetition())
                {
                    Console.WriteLine("DRAW BY THREEFOLD REPETITION!");
                    break;
                }

                if (board.RulesetUsed.FiftyMoveRuleDraw && board.HalfmoveClock >= 100)
                {
                    Console.WriteLine("DRAW BY 50-MOVE RULE!");
                    break;
                }

                bool isCurrentAi = (board.WhiteToMove && whiteIsAi) || (!board.WhiteToMove && blackIsAi);

                if (isCurrentAi)
                {
                    // Engine Turn
                    Console.WriteLine(board.WhiteToMove ? "White (AI) is thinking..." : "Black (AI) is thinking...");
                    search.StartSearch(board, depth);

                    Move bestMove = search.BestMoveFound;
                    if (bestMove.Value != 0)
                    {
                        string uci = MoveToUci(bestMove);
                        Console.WriteLine($"{(board.WhiteToMove ? "White" : "Black")} moves: {uci}");
                        recording.AddMove(uci);
                        board.MakeMove(bestMove);
                        positionHistory.Add(GetRepetitionKey());

                        // Add a small delay so we can see the board updates in AI vs AI
                        if (whiteIsAi && blackIsAi) System.Threading.Thread.Sleep(500);
                    }
                    else
                    {
                        Console.WriteLine("Engine resigned or has no moves.");
                        break;
                    }
                }
                else
                {
                    // Player Turn
                    Console.Write(board.WhiteToMove ? "White move: " : "Black move: ");
                    string input = Console.ReadLine();

                    if (string.IsNullOrEmpty(input)) continue;
                    if (input.ToLower() == "exit" || input.ToLower() == "quit")
                    {
                        Console.WriteLine("Exiting play mode...");
                        break;
                    }

                    if (TryMakePlayerMove(input, legalMoves))
                    {
                        recording.AddMove(input.ToLower());
                        positionHistory.Add(GetRepetitionKey());
                    }
                    else
                    {
                        Console.WriteLine("Invalid or illegal move. Try again.");
                    }
                }
            }

            recording.PrintHistory();
            return recording;
        }

        private bool TryMakePlayerMove(string input, List<Move> legalMoves)
        {
            if (input.Length < 4) return false;

            foreach (var move in legalMoves)
            {
                string uci = MoveToUci(move);
                if (uci == input.ToLower() || (input.Length == 4 && uci == input.ToLower() + "q"))
                {
                    board.MakeMove(move);
                    return true;
                }
            }

            return false;
        }

        private bool IsThreefoldRepetition()
        {
            string currentKey = GetRepetitionKey();
            int count = 0;
            foreach (var key in positionHistory)
            {
                if (key == currentKey) count++;
            }
            return count >= 3;
        }

        private string GetRepetitionKey()
        {
            // Repetition is based on piece placement, turn, castling rights, and en passant
            string fen = board.GetFen();
            string[] parts = fen.Split(' ');
            if (parts.Length < 4) return fen;
            return $"{parts[0]} {parts[1]} {parts[2]} {parts[3]}";
        }

        private bool IsGameOver()
        {
            bool whiteKingExists = false;
            bool blackKingExists = false;

            for (int i = 0; i < board.Squares.Length; i++)
            {
                int piece = board.Squares[i];
                if (Piece.Type(piece) == Piece.King)
                {
                    if (Piece.IsWhite(piece)) whiteKingExists = true;
                    else blackKingExists = true;
                }
            }

            if (!whiteKingExists)
            {
                Console.WriteLine("GAME OVER! Black Wins!");
                return true;
            }
            if (!blackKingExists)
            {
                Console.WriteLine("GAME OVER! White Wins!");
                return true;
            }

            return false;
        }



        private bool IsCheck()
        {
            bool isWhite = board.WhiteToMove;
            int kingSq = -1;
            for (int i = 0; i < board.Squares.Length; i++)
            {
                int piece = board.Squares[i];
                if (Piece.Type(piece) == Piece.King && Piece.IsWhite(piece) == isWhite)
                {
                    kingSq = i;
                    break;
                }
            }

            if (kingSq == -1) return false;
            return moveGenerator.IsSquareAttacked(board, kingSq, !isWhite);
        }

        private string MoveToUci(Move move)
        {
            int from = move.FromSquare;
            int to = move.ToSquare;

            string fromStr = SqToUci(from);
            string toStr = SqToUci(to);
            string promo = "";
            if (move.Flags >= Move.PromoteToQueen)
            {
                if (move.Flags == Move.PromoteToQueen) promo = "q";
                else if (move.Flags == Move.PromoteToKnight) promo = "n";
                else if (move.Flags == Move.PromoteToBishop) promo = "b";
                else if (move.Flags == Move.PromoteToRook) promo = "r";
            }

            return fromStr + toStr + promo;
        }

        private string SqToUci(int sq)
        {
            int file = sq % board.BoardWidth;
            int rank = board.BoardHeight - (sq / board.BoardWidth);
            return $"{(char)('a' + file)}{rank}";
        }
    }
}
