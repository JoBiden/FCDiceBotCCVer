using System;
using System.Runtime.InteropServices;

namespace CustomChessEngine
{
    public static class Piece
    {
        public const int Empty = 0;

        // Piece types (1-6 standard, others fairy)
        public const int Pawn = 1;
        public const int Knight = 2;
        public const int Bishop = 3;
        public const int Rook = 4;
        public const int Queen = 5;
        public const int King = 6;

        public const int Camel = 7;
        public const int Chancellor = 8;
        public const int Archbishop = 9;
        public const int Amazon = 10;
        public const int Centaur = 11;
        public const int Wazir = 12;
        public const int Ferz = 13;
        public const int Dabbaba = 14;
        public const int Alfil = 15;
        public const int Zebra = 16;

        // Flags
        public const int BlackFlag = 256; //2^8
        public const int StartingPositionFlag = 512;
        public const int KingFlag = 1024;
        public const int MinionFlag = 2048;
        public const int ChampionFlag = 4096; //2^12
        public const int NeutralFlag = 8192; //2^13
        public const int TokenFlag = 16384;
        public const int ArmorFlag = 32768; //2^15
        public const int StealthFlag = 65536; //2^16
        public const int InvulnerableFlag = 131072; //2^17
        public const int BerserkFlag = 262144; //2^18
        public const int WithdrawFlag = 524288; //2^19
        public const int EthrealFlag = 1048576; //2^20
        public const int ChargesMask = 31457280; //21-25
        public const int PlayDelayMask = 2113929216; //26-31
        public const int PieceBitsMask = 255;
        public const int PieceAndColorBitsMask = PieceBitsMask + BlackFlag;
        public const int AllNonColorFlags = ~(PieceAndColorBitsMask);

        // Colored piece constants
        public const int WhitePawn = Pawn;
        public const int WhiteKnight = Knight;
        public const int WhiteBishop = Bishop;
        public const int WhiteRook = Rook;
        public const int WhiteQueen = Queen;
        public const int WhiteKing = King;

        public const int BlackPawn = Pawn | BlackFlag;
        public const int BlackKnight = Knight | BlackFlag;
        public const int BlackBishop = Bishop | BlackFlag;
        public const int BlackRook = Rook | BlackFlag;
        public const int BlackQueen = Queen | BlackFlag;
        public const int BlackKing = King | BlackFlag;

        public static bool IsWhite(int piece) => piece != 0 && (piece & BlackFlag) == 0;
        public static bool IsBlack(int piece) => (piece & BlackFlag) != 0;
        public static int Type(int piece) => piece & PieceBitsMask;
        public static bool HasNotMoved(int piece) => (piece & StartingPositionFlag) != 0;
        public static bool HasFlag(int piece, int flag) => (piece & flag) != 0;
        public static int GetCharges(int piece) => (piece & ChargesMask) >> 21;
        public static int GetPlayDelay(int piece) => (piece & PlayDelayMask) >> 26;
        public static int SetCharges(int piece, int charges) => (piece & ~ChargesMask) | (charges << 21);
        public static int SetPlayDelay(int piece, int playDelay) => (piece & ~PlayDelayMask) | (playDelay << 26);

        public struct MovementPattern
        {
            public int[] Offsets;
            public bool Sliding;
            public int Range;
            public bool CaptureOnly;
            public bool QuietOnly;
            public bool FromStartOnly;
            public int MaxHorizontalJump; //used for determining out of bounds moves with single dimensional board array
            public MovementPattern(int[] offsets, bool sliding, int range = 8, int maxHorizontalJump = 1, bool captureOnly = false, bool quietOnly = false, bool fromStartOnly = false)
            {
                Offsets = offsets;
                Sliding = sliding;
                Range = range;
                CaptureOnly = captureOnly;
                QuietOnly = quietOnly;
                FromStartOnly = fromStartOnly;
                MaxHorizontalJump = maxHorizontalJump;
            }
        }

        public struct PieceData
        {
            public int Type;
            public int Value;
            public char Symbol;
            public int[] PST;
            public MovementPattern[] Patterns;
            public int StartingFlags;

            public PieceData(int type, int value, char symbol, int[] pst, MovementPattern[] patterns, int startingFlags, int startingTurnDelay = 0, int startingCharges = 0)
            {
                Type = type;
                Value = value;
                Symbol = symbol;
                PST = pst;
                Patterns = patterns;
                StartingFlags = startingFlags;
                StartingFlags += (startingTurnDelay << 26);
                StartingFlags += (startingCharges << 21);
            }
        }

        public static readonly PieceData[] Table;

        static Piece()
        {
            Table = new PieceData[32];

            // Standard Directions
            int[] diag = { -9, -7, 7, 9 };
            int[] orth = { -8, -1, 1, 8 };
            int[] diagAndOrth = { -9, -8, -7, -1, 1, 7, 8, 9 };
            int[] knight = { -17, -15, -10, -6, 6, 10, 15, 17 };
            int[] camel = { -25, -23, -11, -5, 5, 11, 23, 25 };
            int[] zebra = { -26, -25, -19, -13, 13, 19, 25, 26 };
            //Note: 4-length horizontal jumps would break the engine, if they are not on the same row because of single dimensional arrays with the current checks
            Table[Empty] = new PieceData(Empty, 0, '.', new int[64], new MovementPattern[0], 0);

            Table[Pawn] = new PieceData(Pawn, 100, 'P', new int[] {
                 0,  0,  0,  0,  0,  0,  0,  0,
                50, 50, 50, 50, 50, 50, 50, 50,
                10, 10, 20, 30, 30, 20, 10, 10,
                 5,  5, 10, 25, 25, 10,  5,  5,
                 0,  0,  0, 20, 20,  0,  0,  0,
                 5, -5,-10,  0,  0,-10, -5,  5,
                 5, 10, 10,-20,-20, 10, 10,  5,
                 0,  0,  0,  0,  0,  0,  0,  0
            }, new MovementPattern[] {
                new MovementPattern(new[] { -8 }, false, 1, 1, false, true),
                new MovementPattern(new[] { -16 }, false, 1, 1, false, true, true),
                new MovementPattern(new[] { -9, -7 }, false, 1, 1, true, false)
            }, (MinionFlag));

            Table[Knight] = new PieceData(Knight, 300, 'N', new int[] {
                -50,-40,-30,-30,-30,-30,-40,-50,
                -40,-20,  0,  0,  0,  0,-20,-40,
                -30,  0, 10, 15, 15, 10,  0,-30,
                -30,  5, 15, 20, 20, 15,  5,-30,
                -30,  0, 15, 20, 20, 15,  0,-30,
                -30,  5, 10, 15, 15, 10,  5,-30,
                -40,-20,  0,  5,  5,  0,-20,-40,
                -50,-40,-30,-30,-30,-30,-40,-50
            }, new MovementPattern[] { new MovementPattern(knight, false, 1, 2) }, ChampionFlag);

            Table[Bishop] = new PieceData(Bishop, 300, 'B', new int[] {
                -20,-10,-10,-10,-10,-10,-10,-20,
                -10,  0,  0,  0,  0,  0,  0,-10,
                -10,  0,  5, 10, 10,  5,  0,-10,
                -10,  5,  5, 10, 10,  5,  5,-10,
                -10,  0, 10, 10, 10, 10,  0,-10,
                -10, 10, 10, 10, 10, 10, 10,-10,
                -10,  5,  0,  0,  0,  0,  5,-10,
                -20,-10,-10,-10,-10,-10,-10,-20
            }, new MovementPattern[] { new MovementPattern(diag, true) }, ChampionFlag);

            int[] rookPST = new int[64];
            for (int i = 8; i < 16; i++) rookPST[i] = 10;
            Table[Rook] = new PieceData(Rook, 500, 'R', rookPST, new MovementPattern[] { new MovementPattern(orth, true) }, ChampionFlag);

            int[] queenPST = new int[64];
            queenPST[27] = 5; queenPST[28] = 5; queenPST[35] = 5; queenPST[36] = 5;
            Table[Queen] = new PieceData(Queen, 900, 'Q', queenPST, new MovementPattern[] { new MovementPattern(diagAndOrth, true) }, ChampionFlag);

            Table[King] = new PieceData(King, 10000, 'K', new int[] {
                 -30,-40,-40,-50,-50,-40,-40,-30,
                 -30,-40,-40,-50,-50,-40,-40,-30,
                 -30,-40,-40,-50,-50,-40,-40,-30,
                 -30,-40,-40,-50,-50,-40,-40,-30,
                 -20,-30,-30,-40,-40,-30,-30,-20,
                 -10,-20,-20,-20,-20,-20,-20,-10,
                  20, 20,  0,  0,  0,  0, 20, 20,
                  20, 30, 10,  0,  0, 10, 30, 20
            }, new MovementPattern[] { new MovementPattern(diagAndOrth, false, 1) }, KingFlag);

            var CamelPST = new int[] {
                -50,-40,-30,-30,-30,-30,-40,-50,
                -40,-20,  0,  0,  0,  0,-20,-40,
                -30,  0, 10, 15, 15, 10,  0,-30,
                -30,  5, 20, 30, 30, 20,  5,-30,
                -30,  0, 20, 30, 30, 20,  0,-30,
                -30,  5, 10, 15, 15, 10,  5,-30,
                -40,-20,  0,  5,  5,  0,-20,-40,
                -50,-40,-30,-30,-30,-30,-40,-50
            };
            // Fairy Pieces
            Table[Camel] = new PieceData(Camel, 290, 'L', CamelPST, new MovementPattern[] { new MovementPattern(camel, false, 1, 3) }, ChampionFlag, 3, 0);
            Table[Chancellor] = new PieceData(Chancellor, 850, 'C', new int[64], new MovementPattern[] {
                new MovementPattern(orth, true),
                new MovementPattern(knight, false, 1, 2)
            }, ChampionFlag);
            Table[Archbishop] = new PieceData(Archbishop, 850, 'A', new int[64], new MovementPattern[] {
                new MovementPattern(diag, true),
                new MovementPattern(knight, false, 1, 2)
            }, ChampionFlag);
            Table[Amazon] = new PieceData(Amazon, 1200, 'M', new int[64], new MovementPattern[] {
                new MovementPattern(diagAndOrth, true),
                new MovementPattern(knight, false, 1, 2)
            }, ChampionFlag);
            Table[Centaur] = new PieceData(Centaur, 600, 'S', Table[Knight].PST, new MovementPattern[] {
                new MovementPattern(diagAndOrth, false, 1),
                new MovementPattern(knight, false, 1, 2)
            }, ChampionFlag);
            Table[Zebra] = new PieceData(Zebra, 280, 'Z', CamelPST, new MovementPattern[] { new MovementPattern(zebra, false, 1, 3) }, ChampionFlag, 3, 0);

            // Ferz, Dabbaba, Alfil
            int[] ferz = { -9, -7, 7, 9 };
            int[] dabbaba = { -16, -2, 2, 16 };
            int[] alfil = { -18, -14, 14, 18 };
            int[] wazir = { -8, -1, 1, 8 };

            Table[Ferz] = new PieceData(Ferz, 150, 'F', new int[64], new MovementPattern[] { new MovementPattern(ferz, false, 1) }, ChampionFlag);
            Table[Dabbaba] = new PieceData(Dabbaba, 200, 'D', new int[64], new MovementPattern[] { new MovementPattern(dabbaba, false, 1) }, ChampionFlag);
            Table[Alfil] = new PieceData(Alfil, 150, 'G', new int[64], new MovementPattern[] { new MovementPattern(alfil, false, 1) }, ChampionFlag);
            Table[Wazir] = new PieceData(Wazir, 150, 'W', new int[64], new MovementPattern[] { new MovementPattern(wazir, false, 1) }, ChampionFlag);
        }
    }
}
