using System;
using System.Runtime.InteropServices;
using System.Security.Policy;

namespace CustomChessEngine
{
    public static class Ruleset
    {
        public const int ClassicChess = 1;
        public const int GothicChess = 2;
        public const int FavorChess = 3;
        public const int ChessEvolvedOnline = 4;
        public const int HordeChess = 5;
        public const int TamerlaneChess = 6;

        // Flags
        // public const int BlackFlag = 256; //2^8
        // public const int StartingPositionFlag = 512;
        // public const int KingFlag = 1024;
        // public const int PieceBitsMask = 255;

        // public static bool IsBlack(int piece) => (piece & BlackFlag) != 0;
        // public static int Type(int piece) => piece & PieceBitsMask;

        public struct RulesetData
        {
            public int Type;
            public string Name;
            public int BoardWidth;
            public int BoardHeight;
            public bool EnPassant;
            public bool CastleAnyPiece;
            public bool UseCheckmate;
            public bool UseReserveRow;
            public bool AllowTokens;
            public bool AllowPromotionToAnyPiece;
            public bool ThreefoldRepetitionDraw;
            public bool StalemateDraw;
            public bool FiftyMoveRuleDraw;
            public string StartingPositionFen;
            public RulesetData(int type, string name, int boardWidth, int boardHeight, bool enPassant, bool castleAnyPiece, bool useCheckmate, bool useReserveRow, bool allowTokens, bool allowPromotionToAnyPiece, bool threefoldRepetitionDraw, bool stalemateDraw, bool fiftyMoveRuleDraw, string startingPositionFen)
            {
                Type = type;
                Name = name;
                BoardWidth = boardWidth;
                BoardHeight = boardHeight;
                EnPassant = enPassant;
                CastleAnyPiece = castleAnyPiece;
                UseCheckmate = useCheckmate;
                UseReserveRow = useReserveRow;
                AllowTokens = allowTokens;
                AllowPromotionToAnyPiece = allowPromotionToAnyPiece;
                ThreefoldRepetitionDraw = threefoldRepetitionDraw;
                StalemateDraw = stalemateDraw;
                FiftyMoveRuleDraw = fiftyMoveRuleDraw;
                StartingPositionFen = startingPositionFen;
            }
        }

        public static readonly RulesetData[] Table;

        public static RulesetData GetRuleset(int type)
        {
            return Table[type];
        }

        static Ruleset()
        {
            Table = new RulesetData[32];
            Table[ClassicChess] = new RulesetData(ClassicChess, "Classic Chess", 8, 8, true, false, true, false, false, true, true, true, true, "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            Table[GothicChess] = new RulesetData(GothicChess, "Gothic Chess", 8, 8, true, false, true, false, false, true, true, true, true, "rnbqckabnr/pppppppppp/10/10/10/10/PPPPPPPPPPP/RNBQCKABNR w KQkq - 0 1");
            Table[FavorChess] = new RulesetData(FavorChess, "Favor Chess", 8, 8, false, false, false, true, true, false, true, false, true, "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            Table[ChessEvolvedOnline] = new RulesetData(ChessEvolvedOnline, "ChessEvolvedOnline", 8, 8, false, false, false, false, false, false, false, false, false, "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            Table[HordeChess] = new RulesetData(HordeChess, "Horde Chess", 8, 8, true, false, false, false, false, true, true, true, true, "rnbqkbnr/pppppppp/8/1PP2PP1/PPPPPPPP/PPPPPPPP/PPPPPPPP/PPPPPPPP w KQkq - 0 1");
            Table[TamerlaneChess] = new RulesetData(TamerlaneChess, "Tamerlane Chess", 11, 10, false, false, true, false, false, false, true, true, true, "g1l1d1d1l1g/rnbrwkarbnr/ppppppppppp/11/11/11/11/PPPPPPPPPPP/RNBRWKARBNR/G1L1D1G1L1G w KQkq - 0 1");
        }
    }
}
