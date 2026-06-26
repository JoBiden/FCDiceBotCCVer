using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomChessEngine
{
    public class Search
    {
        private MoveGenerator moveGenerator = new MoveGenerator();
        private Evaluation evaluation = new Evaluation();
        private TranspositionTable tt = new TranspositionTable(16); // Default 16MB

        public Move BestMoveFound { get; private set; }

        public void SetHashSize(int sizeMb)
        {
            tt = new TranspositionTable(sizeMb);
        }

        public void StartSearch(Board board, int depth)
        {
            DateTime startTime = DateTime.Now;

            List<Move> rootMoves = moveGenerator.GenerateLegalMoves(board);
            SortMoves(board, rootMoves);

            if (rootMoves.Count == 0)
            {
                Console.WriteLine("bestmove (none)");
                return;
            }

            int alpha = -999999;
            int beta = 999999;
            Move bestMoveAtRoot = rootMoves[0];

            foreach (Move move in rootMoves)
            {
                board.MakeMove(move);
                int score = -AlphaBeta(board, -beta, -alpha, depth - 1);
                board.UnmakeMove();

                if (score > alpha)
                {
                    alpha = score;
                    bestMoveAtRoot = move;
                }
            }

            BestMoveFound = bestMoveAtRoot;

            TimeSpan duration = DateTime.Now - startTime;
            string bestMoveStr = MoveToUci(BestMoveFound);

            Console.WriteLine($"info depth {depth} score cp {alpha} time {duration.TotalMilliseconds:0} pv {bestMoveStr}");
            Console.WriteLine($"bestmove {bestMoveStr}");
        }

        private int AlphaBeta(Board board, int alpha, int beta, int depth)
        {
            int originalAlpha = alpha;

            // TT Lookup
            if (tt.Lookup(board.ZobristKey, out var ttEntry) && ttEntry.Depth >= depth)
            {
                if (ttEntry.Type == TranspositionTable.EntryType.Exact) return ttEntry.Score;
                if (ttEntry.Type == TranspositionTable.EntryType.LowerBound) alpha = Math.Max(alpha, ttEntry.Score);
                else if (ttEntry.Type == TranspositionTable.EntryType.UpperBound) beta = Math.Min(beta, ttEntry.Score);

                if (alpha >= beta) return ttEntry.Score;
            }

            if (depth <= 0)
            {
                return evaluation.Evaluate(board);
            }

            List<Move> moves = moveGenerator.GenerateLegalMoves(board);

            // Move ordering: put TT move first
            ushort ttMoveValue = ttEntry.Key == board.ZobristKey ? ttEntry.BestMoveValue : (ushort)0;
            SortMoves(board, moves, ttMoveValue);

            if (moves.Count == 0)
            {
                if (moveGenerator.IsSquareAttacked(board, GetKingSquare(board, board.WhiteToMove), !board.WhiteToMove))
                {
                    return -100000 - depth;
                }
                return 0;
            }

            Move bestMoveFoundInNode = new Move();
            int bestScoreInNode = -9999999;

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int score = -AlphaBeta(board, -beta, -alpha, depth - 1);
                board.UnmakeMove();

                if (score > bestScoreInNode)
                {
                    bestScoreInNode = score;
                    bestMoveFoundInNode = move;
                }

                if (score >= beta)
                {
                    tt.Store(board.ZobristKey, (byte)depth, (short)beta, TranspositionTable.EntryType.LowerBound, move.Value);
                    return beta;
                }

                if (score > alpha)
                {
                    alpha = score;
                }
            }

            // TT Store
            TranspositionTable.EntryType type = TranspositionTable.EntryType.Exact;
            if (bestScoreInNode <= originalAlpha) type = TranspositionTable.EntryType.UpperBound;
            else if (bestScoreInNode >= beta) type = TranspositionTable.EntryType.LowerBound;

            tt.Store(board.ZobristKey, (byte)depth, (short)bestScoreInNode, type, bestMoveFoundInNode.Value);

            return alpha;
        }

        private void SortMoves(Board board, List<Move> moves, ushort ttMoveValue = 0)
        {
            // Simple sorting: TT move first, then Captures
            moves.Sort((a, b) =>
            {
                if (a.Value == ttMoveValue) return -1;
                if (b.Value == ttMoveValue) return 1;

                int scoreA = GetMoveScore(board, a);
                int scoreB = GetMoveScore(board, b);
                return scoreB.CompareTo(scoreA); // High score first
            });
        }

        private int GetMoveScore(Board board, Move move)
        {
            int score = 0;
            int piece = board.Squares[move.FromSquare];
            int captured = board.Squares[move.ToSquare];
            int fromType = Piece.Type(piece);
            int capturedType = Piece.Type(captured);

            // Simple piece value (bonus for piece activity/value)
            score += Piece.Table[fromType].Value / 10;

            // Capture bonus (MVV-LVA)
            if (capturedType != Piece.Empty)
            {
                score = 10 * Piece.Table[capturedType].Value - Piece.Table[fromType].Value;
                score += 10000; // Big bonus for captures
            }

            // Promotion bonus
            if (move.Flags >= Move.PromoteToQueen)
            {
                score += 500;
                if (move.Flags == Move.PromoteToQueen) score += 400; // Prefer Queen
            }

            // Castling bonus
            if (move.Flags == Move.CastleKingside || move.Flags == Move.CastleQueenside)
                score += 500;

            return score;
        }

        private int GetKingSquare(Board board, bool white)
        {
            int targetKing = white ? Piece.WhiteKing : Piece.BlackKing;
            for (int i = 0; i < 64; i++)
            {
                if (board.Squares[i] == targetKing) return i;
            }
            return -1;
        }

        private string MoveToUci(Move move)
        {
            string from = SqToUci(move.FromSquare);
            string to = SqToUci(move.ToSquare);
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

        private string SqToUci(int sq)
        {
            int file = sq % 8;
            int rank = 8 - (sq / 8);
            return $"{(char)('a' + file)}{rank}";
        }
    }
}
