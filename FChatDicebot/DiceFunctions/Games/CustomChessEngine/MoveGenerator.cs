using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomChessEngine
{
    public class MoveGenerator
    {
        public List<Move> GenerateLegalMoves(Board board)
        {
            var pseudoMoves = GeneratePseudoLegalMoves(board);
            var legalMoves = new List<Move>();
            bool isWhite = board.WhiteToMove;

            foreach (var move in pseudoMoves)
            {
                board.MakeMove(move);

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

                if (kingSq != -1 && !IsSquareAttacked(board, kingSq, !isWhite))
                {
                    legalMoves.Add(move);
                }

                board.UnmakeMove();
            }

            return legalMoves;
        }

        public List<Move> GeneratePseudoLegalMoves(Board board)
        {
            var moves = new List<Move>();
            bool isWhite = board.WhiteToMove;

            for (int sq = 0; sq < board.Squares.Length; sq++)
            {
                int piece = board.Squares[sq];
                if (piece == Piece.Empty) continue;

                if (Piece.IsWhite(piece) == isWhite)
                {
                    int type = Piece.Type(piece);
                    GenerateMovesFromPatterns(sq, board, isWhite, Piece.Table[type].Patterns, moves);

                    if (type == Piece.King)
                    {
                        GenerateCastlingMoves(sq, board, isWhite, moves);
                    }
                }
            }

            return moves;
        }

        private void GenerateMovesFromPatterns(int sq, Board board, bool isWhite, Piece.MovementPattern[] patterns, List<Move> moves)
        {
            int piece = board.Squares[sq];
            int width = board.BoardWidth;
            foreach (var pattern in patterns)
            {
                if (pattern.FromStartOnly && !Piece.HasNotMoved(piece)) continue;

                foreach (int offset in pattern.Offsets)
                {
                    int actualOffset = isWhite ? offset : -offset;
                    int currentSq = sq;

                    for (int r = 1; r <= pattern.Range; r++)
                    {
                        int nextSq = currentSq + actualOffset;
                        if (nextSq < 0 || nextSq >= board.Squares.Length) break;

                        // Wrapping check
                        int fileFrom = currentSq % width;
                        int fileTo = nextSq % width;
                        if (Math.Abs(fileTo - fileFrom) > pattern.MaxHorizontalJump) break;

                        int destPiece = board.Squares[nextSq];
                        if (destPiece == Piece.Empty)
                        {
                            if (!pattern.CaptureOnly)
                            {
                                if (Piece.Type(piece) == Piece.Pawn) AddPawnMove(sq, nextSq, board, marksAsLegal: true, isWhite, moves);
                                else moves.Add(new Move(sq, nextSq));
                            }
                            else if (Piece.Type(piece) == Piece.Pawn && nextSq == board.EnPassantSquare && board.RulesetUsed.EnPassant && fileFrom != fileTo)
                            {
                                moves.Add(new Move(sq, nextSq, Move.EnPassantCapture));
                            }
                        }
                        else
                        {
                            if (!pattern.QuietOnly && Piece.IsWhite(destPiece) != isWhite)
                            {
                                if (Piece.Type(piece) == Piece.Pawn) AddPawnMove(sq, nextSq, board, marksAsLegal: true, isWhite, moves);
                                else moves.Add(new Move(sq, nextSq));
                            }
                            break; // Blocked, capture square if possible
                        }

                        if (!pattern.Sliding) break;
                        currentSq = nextSq;
                    }
                }
            }
        }

        public void GenerateCastlingMoves(int sq, Board board, bool isWhite, List<Move> moves)
        {
            int king = board.Squares[sq];
            if (!Piece.HasNotMoved(king)) return;
            if (IsSquareAttacked(board, sq, !isWhite)) return;

            if (isWhite)
            {
                if (Piece.Type(board.Squares[63]) == Piece.Rook && Piece.HasNotMoved(board.Squares[63]) && board.Squares[61] == 0 && board.Squares[62] == 0)
                {
                    if (!IsSquareAttacked(board, 61, false) && !IsSquareAttacked(board, 62, false))
                        moves.Add(new Move(60, 62, Move.CastleKingside));
                }
                if (Piece.Type(board.Squares[56]) == Piece.Rook && Piece.HasNotMoved(board.Squares[56]) && board.Squares[59] == 0 && board.Squares[58] == 0 && board.Squares[57] == 0)
                {
                    if (!IsSquareAttacked(board, 59, false) && !IsSquareAttacked(board, 58, false))
                        moves.Add(new Move(60, 58, Move.CastleQueenside));
                }
            }
            else
            {
                //NOTE: when champions are implemented, replace Piece.Rook with champion flag
                if (Piece.Type(board.Squares[7]) == Piece.Rook && Piece.HasNotMoved(board.Squares[7]) && board.Squares[5] == 0 && board.Squares[6] == 0)
                {
                    if (!IsSquareAttacked(board, 5, true) && !IsSquareAttacked(board, 6, true))
                        moves.Add(new Move(4, 6, Move.CastleKingside));
                }
                if (Piece.Type(board.Squares[0]) == Piece.Rook && Piece.HasNotMoved(board.Squares[0]) && board.Squares[3] == 0 && board.Squares[2] == 0 && board.Squares[1] == 0)
                {
                    if (!IsSquareAttacked(board, 3, true) && !IsSquareAttacked(board, 2, true))
                        moves.Add(new Move(4, 2, Move.CastleQueenside));
                }
            }
        }

        public bool IsSquareAttacked(Board board, int sq, bool byWhite)
        {
            for (int attackerSq = 0; attackerSq < board.Squares.Length; attackerSq++)
            {
                int piece = board.Squares[attackerSq];
                if (piece == Piece.Empty || Piece.IsWhite(piece) != byWhite) continue;

                int type = Piece.Type(piece);
                var patterns = Piece.Table[type].Patterns;

                foreach (var pattern in patterns)
                {
                    if (pattern.QuietOnly) continue; // Pawns can't attack forward
                    if (pattern.FromStartOnly && !Piece.HasNotMoved(piece)) continue;

                    foreach (int offset in pattern.Offsets)
                    {
                        bool pieceIsWhite = Piece.IsWhite(piece);
                        int actualOffset = pieceIsWhite ? offset : -offset;

                        int currentSq = attackerSq;
                        for (int r = 1; r <= pattern.Range; r++)
                        {
                            int nextSq = currentSq + actualOffset;
                            if (nextSq < 0 || nextSq >= board.Squares.Length) break;

                            int fileFrom = currentSq % board.BoardWidth;
                            int fileTo = nextSq % board.BoardWidth;
                            if (Math.Abs(fileTo - fileFrom) > pattern.MaxHorizontalJump) break;

                            if (nextSq == sq) return true;
                            if (board.Squares[nextSq] != Piece.Empty) break;
                            if (!pattern.Sliding) break;
                            currentSq = nextSq;
                        }
                    }
                }
            }
            return false;
        }

        private void AddPawnMove(int from, int to, Board board, bool marksAsLegal, bool isWhite, List<Move> moves)
        {
            int promoRank = isWhite ? 0 : board.BoardHeight - 1;
            if (to / board.BoardWidth == promoRank)
            {
                moves.Add(new Move(from, to, Move.PromoteToQueen));
                moves.Add(new Move(from, to, Move.PromoteToKnight));
                moves.Add(new Move(from, to, Move.PromoteToBishop));
                moves.Add(new Move(from, to, Move.PromoteToRook));
            }
            else moves.Add(new Move(from, to, Move.NoFlag));
        }
    }
}
