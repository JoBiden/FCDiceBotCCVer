using System;

namespace CustomChessEngine
{
    public class Evaluation
    {
        public int Evaluate(Board board)
        {
            int score = 0;

            int whiteKingSq = -1;
            int blackKingSq = -1;

            // Find kings first for safety evaluation
            for (int sq = 0; sq < board.Squares.Length; sq++)
            {
                int piece = board.Squares[sq];
                if (piece == Piece.WhiteKing) whiteKingSq = sq;
                else if (piece == Piece.BlackKing) blackKingSq = sq;
            }

            for (int sq = 0; sq < board.Squares.Length; sq++)
            {
                int piece = board.Squares[sq];
                if (piece != Piece.Empty)
                {
                    bool isWhite = Piece.IsWhite(piece);
                    int type = Piece.Type(piece);

                    var data = Piece.Table[type];
                    int pstVal = 0;
                    if (data.PST != null && data.PST.Length == 64 && board.BoardWidth == 8 && board.BoardHeight == 8)
                    {
                        int pstIndex = isWhite ? sq : (56 - (sq / 8 * 8) + (sq % 8)); // Flip index for black
                        pstVal = data.PST[pstIndex];
                    }

                    int val = data.Value;

                    int pieceScore = val + pstVal;

                    // Add mobility and King safety
                    int mobility = CalculateMobilityAndKingSafety(board, sq, type, isWhite, whiteKingSq, blackKingSq);
                    pieceScore += mobility;

                    score += isWhite ? pieceScore : -pieceScore;
                }
            }
            // Return perspective of side to move
            return board.WhiteToMove ? score : -score;
        }

        private int CalculateMobilityAndKingSafety(Board board, int sq, int type, bool isWhite, int whiteKingSq, int blackKingSq)
        {
            int bonus = 0;
            if (type == 1 || type == 6) return 0; // Skip pawns and kings for simplified mobility tracking

            int friendlyKingSq = isWhite ? whiteKingSq : blackKingSq;
            int enemyKingSq = isWhite ? blackKingSq : whiteKingSq;

            // Simple mobility approximation based on distance from center/edges, as actual move gen per node is expensive.
            // But we can approximate by calculating distance to king.
            // Distance is Chebyshev distance

            // King defence bonus: positive points for being close to our own king
            if (friendlyKingSq != -1)
            {
                int distToOurKing = Math.Max(Math.Abs((sq % board.BoardWidth) - (friendlyKingSq % board.BoardWidth)), Math.Abs((sq / board.BoardWidth) - (friendlyKingSq / board.BoardWidth)));
                if (distToOurKing <= 2) bonus += 5; // Small bonus for sheltering king
            }

            // King attack bonus: positive points for being close to enemy king
            if (enemyKingSq != -1)
            {
                int distToEnemyKing = Math.Max(Math.Abs((sq % board.BoardWidth) - (enemyKingSq % board.BoardWidth)), Math.Abs((sq / board.BoardWidth) - (enemyKingSq / board.BoardWidth)));
                if (distToEnemyKing <= 3) bonus += 10; // Bonus for attacking enemy king vicinity
            }

            // Central control (simple mobility proxy for Knights/Bishops)
            int centerX = board.BoardWidth / 2;
            int centerY = board.BoardHeight / 2;
            int centralDist = Math.Max(Math.Abs((sq % board.BoardWidth) - centerX), Math.Abs((sq / board.BoardWidth) - centerY));
            if (type == 2 || type == 3) // Knight, Bishop
            {
                bonus += (centerX - centralDist) * 3; // roughly center bonus
            }

            return bonus;
        }
    }
}
