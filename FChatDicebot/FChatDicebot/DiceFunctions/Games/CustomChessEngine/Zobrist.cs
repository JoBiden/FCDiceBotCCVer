using System;

namespace CustomChessEngine
{
    public static class Zobrist
    {
        const int PIECE_STATES = 32; //piece states include in start position, charges number, armored status

        const int TOTAL_COLORS = 2; const int white = 0, black = 1;
        const int ARMORED_STATE = 2; const int NOT_ARMORED = 0, ARMORED = 1;
        const int MAX_CHARGES = 8;
        const int TOTAL_PIECES = 32;
        const int MAX_BOARD_SIZE = 121;
        public const int ReferenceHashKeys = TOTAL_COLORS * MAX_CHARGES * ARMORED_STATE * PIECE_STATES * TOTAL_PIECES * MAX_BOARD_SIZE;
        // 32 piece types (including colors) x 121 squares (max board size 11x11 to be safe)
        public static readonly ulong[,,,,,] PieceKeys = new ulong[TOTAL_COLORS, MAX_CHARGES, ARMORED_STATE, PIECE_STATES, TOTAL_PIECES, MAX_BOARD_SIZE];
        public static readonly ulong SideToMoveKey;
        public static readonly ulong[] CastlingKeys = new ulong[16];
        public static readonly ulong[] EnPassantKeys = new ulong[MAX_BOARD_SIZE];

        static Zobrist()
        {
            // Seeded random for consistency
            Random rnd = new Random(42);

            //public static readonly ulong[,,,,,] PieceKeys = new ulong[TOTAL_COLORS, MAX_CHARGES, ARMORED_STATE, PIECE_STATES, TOTAL_PIECES, MAX_BOARD_SIZE];
            for (int colorInt = 0; colorInt < TOTAL_COLORS; colorInt++)
                for (int chargesInt = 0; chargesInt < MAX_CHARGES; chargesInt++)
                    for (int armoredInt = 0; armoredInt < ARMORED_STATE; armoredInt++)
                        for (int pieceStates = 0; pieceStates < PIECE_STATES; pieceStates++)
                        {
                            for (int p = 0; p < TOTAL_PIECES; p++)
                            {
                                for (int s = 0; s < MAX_BOARD_SIZE; s++)
                                {
                                    PieceKeys[colorInt, chargesInt, armoredInt, pieceStates, p, s] = NextUlong(rnd);
                                }
                            }
                        }

            SideToMoveKey = NextUlong(rnd);

            for (int i = 0; i < 16; i++)
            {
                CastlingKeys[i] = NextUlong(rnd);
            }

            for (int i = 0; i < 121; i++)
            {
                EnPassantKeys[i] = NextUlong(rnd);
            }
        }

        private static ulong NextUlong(Random rnd)
        {
            byte[] buf = new byte[8];
            rnd.NextBytes(buf);
            return BitConverter.ToUInt64(buf, 0);
        }

        public static ulong CalculateKey(Board board)
        {
            ulong key = 0;

            // Pieces
            for (int i = 0; i < board.Squares.Length; i++)
            {
                int piece = board.Squares[i];
                if (piece != Piece.Empty)
                {
                    key ^= GetPieceKey(piece, i);
                }
            }

            // Side to move
            if (!board.WhiteToMove)
            {
                key ^= SideToMoveKey;
            }

            // Castling rights (simple bitmask)
            int rights = GetCastlingRightsMask(board);
            key ^= CastlingKeys[rights];

            // En Passant
            if (board.EnPassantSquare != -1)
            {
                key ^= EnPassantKeys[board.EnPassantSquare];
            }

            return key;
        }

        public static ulong GetPieceKey(int piece, int square)
        {
            int type = Piece.Type(piece);
            int pieceColor = Piece.IsBlack(piece) ? black : white;
            int charges = Piece.GetCharges(piece);
            int armored = Piece.HasFlag(piece, Piece.ArmorFlag) ? 1 : 0;
            int pieceStates = 0;
            return PieceKeys[pieceColor, charges, armored, pieceStates, type, square];
        }

        public static int GetCastlingRightsMask(Board board)
        {
            int mask = 0;
            if (board.BoardWidth == 8)
            {
                if (Piece.Type(board.Squares[60]) == Piece.King && (board.Squares[60] & Piece.StartingPositionFlag) != 0)
                {
                    if (Piece.Type(board.Squares[63]) == Piece.Rook && (board.Squares[63] & Piece.StartingPositionFlag) != 0) mask |= 1;
                    if (Piece.Type(board.Squares[56]) == Piece.Rook && (board.Squares[56] & Piece.StartingPositionFlag) != 0) mask |= 2;
                }
                if (Piece.Type(board.Squares[4]) == Piece.King && (board.Squares[4] & Piece.StartingPositionFlag) != 0)
                {
                    if (Piece.Type(board.Squares[7]) == Piece.Rook && (board.Squares[7] & Piece.StartingPositionFlag) != 0) mask |= 4;
                    if (Piece.Type(board.Squares[0]) == Piece.Rook && (board.Squares[0] & Piece.StartingPositionFlag) != 0) mask |= 8;
                }
            }
            else if (board.BoardWidth == 10)
            {
                if (Piece.Type(board.Squares[75]) == Piece.King && (board.Squares[75] & Piece.StartingPositionFlag) != 0)
                {
                    if (Piece.Type(board.Squares[79]) == Piece.Rook && (board.Squares[79] & Piece.StartingPositionFlag) != 0) mask |= 1;
                    if (Piece.Type(board.Squares[70]) == Piece.Rook && (board.Squares[70] & Piece.StartingPositionFlag) != 0) mask |= 2;
                }
                if (Piece.Type(board.Squares[5]) == Piece.King && (board.Squares[5] & Piece.StartingPositionFlag) != 0)
                {
                    if (Piece.Type(board.Squares[9]) == Piece.Rook && (board.Squares[9] & Piece.StartingPositionFlag) != 0) mask |= 4;
                    if (Piece.Type(board.Squares[0]) == Piece.Rook && (board.Squares[0] & Piece.StartingPositionFlag) != 0) mask |= 8;
                }
            }
            return mask;
        }
    }
}
