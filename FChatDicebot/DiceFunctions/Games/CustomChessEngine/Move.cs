namespace CustomChessEngine
{
    public struct Move
    {
        public ushort Value;

        // E.g., bits 0-5 for fromSq, 6-11 for toSq, 12-15 for flags (promotion/en passant/castling)
        public Move(int from, int to, int flags = 0)
        {
            Value = (ushort)(from | (to << 6) | (flags << 12));
        }

        public int FromSquare => Value & 0x3F;
        public int ToSquare => (Value >> 6) & 0x3F;
        public int Flags => (Value >> 12) & 0xF;

        public const int NoFlag = 0;
        public const int EnPassantCapture = 5;
        public const int CastleKingside = 2;
        public const int CastleQueenside = 3;

        // Promotion flags
        public const int PromoteToQueen = 8;
        public const int PromoteToKnight = 9;
        public const int PromoteToBishop = 10;
        public const int PromoteToRook = 11;
    }
}
