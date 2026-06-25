using System;
using static CustomChessEngine.Ruleset;

namespace CustomChessEngine
{
    public class Board
    {
        public RulesetData RulesetUsed;
        // 0 = empty
        public int BoardWidth = 8;
        public int BoardHeight = 8;
        public int PromotionRankWhite;
        public int PromotionRankBlack;
        public int BoardSize;

        public int[] Squares;
        public bool WhiteToMove = true;
        public ulong ZobristKey;
        string StartingPositionFen;

        public int EnPassantSquare = -1;
        private System.Collections.Generic.Stack<UndoInfo> undoStack = new System.Collections.Generic.Stack<UndoInfo>();

        public int HalfmoveClock; //halfmove clock measures the half-moves since the last capture or pawn move
        public int FullmoveNumber = 1;

        public struct UndoInfo
        {
            public int moveValue;
            public int capturedPiece;
            public int fromPieceWithFlags;
            public int prevHalfmoveClock;
            public int prevEnPassantSquare;
            public ulong prevZobristKey;
        }

        public Board(int rulesetType)
        {
            RulesetUsed = Ruleset.Table[rulesetType];
            BoardWidth = RulesetUsed.BoardWidth;
            BoardHeight = RulesetUsed.BoardHeight;
            if (RulesetUsed.UseReserveRow)
            {
                BoardHeight += 2;
                PromotionRankWhite = 1;
                PromotionRankBlack = BoardHeight - 2;
            }
            else
            {
                PromotionRankWhite = 0;
                PromotionRankBlack = BoardHeight - 1;
            }
            BoardSize = BoardWidth * BoardHeight;
            Squares = new int[BoardSize];
            StartingPositionFen = RulesetUsed.StartingPositionFen;
        }

        public void SetStartingPosition()
        {
            LoadFromFen(StartingPositionFen);
        }

        public void SetGothicChessPosition()
        {
            LoadFromFen("rnbqckabnr/pppppppppp/10/10/10/10/PPPPPPPPPP/RNBQCKABNR w KQkq - 0 1");
        }

        public void LoadFromFen(string fen)
        {
            // Clear board
            for (int i = 0; i < BoardSize; i++) Squares[i] = 0;

            string[] fenParts = fen.Split(' ');
            string ranks = fenParts[0];

            int file = 0, rank = 0; // 0 is 8th rank

            foreach (char symbol in ranks)
            {
                if (symbol == '/')
                {
                    file = 0;
                    rank++;
                }
                else if (char.IsDigit(symbol))
                {
                    file += (int)char.GetNumericValue(symbol);
                }
                else
                {
                    int pieceValue = char.IsUpper(symbol) ? GetPieceFromChar(char.ToLower(symbol), true) : GetPieceFromChar(symbol, false);

                    if (Piece.Type(pieceValue) == Piece.Pawn)
                    {
                        if (Piece.IsWhite(pieceValue) && rank == BoardHeight - 2) pieceValue |= Piece.StartingPositionFlag;
                        else if (Piece.IsBlack(pieceValue) && rank == 1) pieceValue |= Piece.StartingPositionFlag;
                    }

                    if (file < BoardWidth && rank < BoardHeight)
                        Squares[rank * BoardWidth + file] = pieceValue;
                    file++;
                }
            }

            if (fenParts.Length > 2)
            {
                WhiteToMove = fenParts[1] == "w";
                string rights = fenParts[2];

                if (BoardWidth == 8)
                {
                    // Scan board for Kings and Rooks and apply flags based on UCI position string parsing
                    // To keep it simple, if 'K' is in rights, White King at e1 and Rook at h1 get the flag.
                    if (rights.Contains("K")) { SetFlagIfPieceAt(60, Piece.WhiteKing); SetFlagIfPieceAt(63, Piece.WhiteRook); }
                    if (rights.Contains("Q")) { SetFlagIfPieceAt(60, Piece.WhiteKing); SetFlagIfPieceAt(56, Piece.WhiteRook); }
                    if (rights.Contains("k")) { SetFlagIfPieceAt(4, Piece.BlackKing); SetFlagIfPieceAt(7, Piece.BlackRook); }
                    if (rights.Contains("q")) { SetFlagIfPieceAt(4, Piece.BlackKing); SetFlagIfPieceAt(0, Piece.BlackRook); }
                }
                else if (BoardWidth == 10)
                {
                    // Scan board for Kings and Rooks and apply flags based on UCI position string parsing
                    // To keep it simple, if 'K' is in rights, White King at e1 and Rook at h1 get the flag.
                    if (rights.Contains("K")) { SetFlagIfPieceAt(75, Piece.WhiteKing); SetFlagIfPieceAt(79, Piece.WhiteRook); }
                    if (rights.Contains("Q")) { SetFlagIfPieceAt(75, Piece.WhiteKing); SetFlagIfPieceAt(70, Piece.WhiteRook); }
                    if (rights.Contains("k")) { SetFlagIfPieceAt(5, Piece.BlackKing); SetFlagIfPieceAt(9, Piece.BlackRook); }
                    if (rights.Contains("q")) { SetFlagIfPieceAt(5, Piece.BlackKing); SetFlagIfPieceAt(0, Piece.BlackRook); }
                }
            }

            if (fenParts.Length > 3)
            {
                string ep = fenParts[3];
                if (ep != "-")
                {
                    int epFile = ep[0] - 'a';
                    int epRank = 0;
                    int idx = 1;
                    while (idx < ep.Length && char.IsDigit(ep[idx])) { epRank = epRank * 10 + (ep[idx] - '0'); idx++; }
                    epRank = BoardHeight - epRank;
                    EnPassantSquare = epRank * BoardWidth + epFile;
                }
                else EnPassantSquare = -1;
            }

            if (fenParts.Length > 4)
            {
                int.TryParse(fenParts[4], out HalfmoveClock);
            }
            if (fenParts.Length > 5)
            {
                int.TryParse(fenParts[5], out FullmoveNumber);
            }

            ZobristKey = Zobrist.CalculateKey(this);
        }

        private void SetFlagIfPieceAt(int sq, int pieceType)
        {
            if (Piece.Type(Squares[sq]) == Piece.Type(pieceType))
            {
                Squares[sq] |= Piece.StartingPositionFlag;
            }
        }

        private int GetPieceFromChar(char symbol, bool isWhite)
        {
            switch (symbol)
            {
                case 'p': return isWhite ? Piece.WhitePawn : Piece.BlackPawn;
                case 'n': return isWhite ? Piece.WhiteKnight : Piece.BlackKnight;
                case 'b': return isWhite ? Piece.WhiteBishop : Piece.BlackBishop;
                case 'r': return isWhite ? Piece.WhiteRook : Piece.BlackRook;
                case 'q': return isWhite ? Piece.WhiteQueen : Piece.BlackQueen;
                case 'k': return isWhite ? Piece.WhiteKing : Piece.BlackKing;
                case 'l': return isWhite ? Piece.Camel : Piece.Camel | Piece.BlackFlag;
                case 'c': return isWhite ? Piece.Chancellor : Piece.Chancellor | Piece.BlackFlag;
                case 'a': return isWhite ? Piece.Archbishop : Piece.Archbishop | Piece.BlackFlag;
                case 'm': return isWhite ? Piece.Amazon : Piece.Amazon | Piece.BlackFlag;
                case 's': return isWhite ? Piece.Centaur : Piece.Centaur | Piece.BlackFlag;
                case 'z': return isWhite ? Piece.Zebra : Piece.Zebra | Piece.BlackFlag;
                case 'f': return isWhite ? Piece.Ferz : Piece.Ferz | Piece.BlackFlag;
                case 'd': return isWhite ? Piece.Dabbaba : Piece.Dabbaba | Piece.BlackFlag;
                case 'g': return isWhite ? Piece.Alfil : Piece.Alfil | Piece.BlackFlag;
                case 'w': return isWhite ? Piece.Wazir : Piece.Wazir | Piece.BlackFlag;
                default: return 0;
            }
        }

        public void PrintBoard()
        {
            Console.WriteLine();
            for (int rank = 0; rank < BoardHeight; rank++)
            {
                Console.Write((BoardHeight - rank) + "  ");
                for (int file = 0; file < BoardWidth; file++)
                {
                    int sq = rank * BoardWidth + file;
                    int piece = Squares[sq];
                    Console.Write(GetPieceChar(piece) + " ");
                }
                Console.WriteLine();
            }
            if (BoardWidth == 10)
                Console.WriteLine("\n   a b c d e f g h i j\n");
            else
                Console.WriteLine("\n   a b c d e f g h\n");
            Console.WriteLine("To Move: " + (WhiteToMove ? "White" : "Black") + "\n");
        }

        public void PrintBoardRaw()
        {
            Console.WriteLine();
            for (int rank = 0; rank < BoardHeight; rank++)
            {
                Console.Write((BoardHeight - rank) + "  ");
                for (int file = 0; file < BoardWidth; file++)
                {
                    int sq = rank * BoardWidth + file;
                    int piece = Squares[sq];
                    Console.Write(piece + " ");
                }
                Console.WriteLine();
            }
            if (BoardWidth == 10)
                Console.WriteLine("\n   a b c d e f g h i j\n");
            else
                Console.WriteLine("\n   a b c d e f g h\n");
            Console.WriteLine("To Move: " + (WhiteToMove ? "White" : "Black") + "\n");
        }

        private char GetPieceChar(int piece)
        {
            int type = Piece.Type(piece);
            char c = Piece.Table[type].Symbol;
            return Piece.IsBlack(piece) ? char.ToLower(c) : c;
        }

        public void MakeUciMove(string moveStr)
        {
            // Convert UCI string to Move object
            int fromFile = moveStr[0] - 'a';
            int fromRank = 0;
            int nextIdx = 1;
            while (nextIdx < moveStr.Length && char.IsDigit(moveStr[nextIdx])) { fromRank = fromRank * 10 + (moveStr[nextIdx] - '0'); nextIdx++; }
            fromRank = BoardHeight - fromRank;
            int fromSq = fromRank * BoardWidth + fromFile;

            int toFile = moveStr[nextIdx] - 'a';
            nextIdx++;
            int toRank = 0;
            while (nextIdx < moveStr.Length && char.IsDigit(moveStr[nextIdx])) { toRank = toRank * 10 + (moveStr[nextIdx] - '0'); nextIdx++; }
            toRank = BoardHeight - toRank;
            int toSq = toRank * BoardWidth + toFile;

            int flags = Move.NoFlag;
            if (nextIdx < moveStr.Length)
            {
                char promo = moveStr[nextIdx];
                if (promo == 'q') flags = Move.PromoteToQueen;
                else if (promo == 'n') flags = Move.PromoteToKnight;
                else if (promo == 'b') flags = Move.PromoteToBishop;
                else if (promo == 'r') flags = Move.PromoteToRook;
                else flags = Move.PromoteToQueen; // Default
            }

            // Check if it's a promotion that was NOT specified?
            int piece = Squares[fromSq];
            if (Piece.Type(piece) == Piece.Pawn && (toRank == PromotionRankWhite || toRank == PromotionRankBlack) && flags == Move.NoFlag)
            {
                flags = Move.PromoteToQueen; // Default for UCI strings like e7e8
            }

            // Check for castling
            if (Piece.Type(piece) == Piece.King && Math.Abs(toFile - fromFile) == 2)
            {
                flags = toFile > fromFile ? Move.CastleKingside : Move.CastleQueenside;
            }

            MakeMove(new Move(fromSq, toSq, flags));
        }

        public void MakeMove(Move move, string uciStr = "")
        {
            int from = move.FromSquare;
            int to = move.ToSquare;
            int flags = move.Flags;

            int piece = Squares[from];
            int captured = Squares[to];
            bool isWhite = Piece.IsWhite(piece);

            // Store previous state for undo
            int actualCaptured = captured;
            if (flags == Move.EnPassantCapture)
            {
                int capturedPawnSq = isWhite ? to + BoardWidth : to - BoardWidth;
                actualCaptured = Squares[capturedPawnSq];
            }

            undoStack.Push(new UndoInfo
            {
                moveValue = move.Value,
                capturedPiece = actualCaptured,
                fromPieceWithFlags = piece,
                prevHalfmoveClock = HalfmoveClock,
                prevEnPassantSquare = EnPassantSquare,
                prevZobristKey = ZobristKey
            });

            // Update move counters
            HalfmoveClock++;
            if (Piece.Type(piece) == Piece.Pawn || captured != Piece.Empty)
            {
                HalfmoveClock = 0;
            }

            // XOR out old EP if any
            if (EnPassantSquare != -1) ZobristKey ^= Zobrist.EnPassantKeys[EnPassantSquare];
            int oldRightsMask = Zobrist.GetCastlingRightsMask(this);

            // En Passant target square management
            int nextEnPassantSquare = -1;
            if (Piece.Type(piece) == Piece.Pawn && Math.Abs(to - from) == BoardWidth * 2)
            {
                nextEnPassantSquare = (from + to) / 2;
            }
            EnPassantSquare = nextEnPassantSquare;

            // XOR in new EP if any
            if (EnPassantSquare != -1) ZobristKey ^= Zobrist.EnPassantKeys[EnPassantSquare];

            if (!WhiteToMove)
            {
                FullmoveNumber++;
            }

            // Move piece on board and XOR in hash
            ZobristKey ^= Zobrist.GetPieceKey(piece, from); //remove piece from start square

            if (captured != Piece.Empty)
            {
                ZobristKey ^= Zobrist.GetPieceKey(captured, to); //remove captured piece from end square
            }

            Squares[to] = piece & ~Piece.StartingPositionFlag; //set piece at end square and remove starting flag
            Squares[from] = Piece.Empty; //set piece at start square to empty

            ZobristKey ^= Zobrist.GetPieceKey(Squares[to], to); //add piece to end square

            // Handle Special Moves
            if (flags == Move.EnPassantCapture)
            {
                int capturedPawnSq = isWhite ? to + BoardWidth : to - BoardWidth;
                int epCaptured = Squares[capturedPawnSq];
                ZobristKey ^= Zobrist.GetPieceKey(epCaptured, capturedPawnSq);

                Squares[capturedPawnSq] = Piece.Empty;
                HalfmoveClock = 0; // Capture resets clock
            }
            else if (flags == Move.CastleKingside)
            {
                if (BoardWidth == 8)
                {
                    if (isWhite)
                    {
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[63], 63);
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[61], 61);
                        Squares[61] = Squares[63] & ~Piece.StartingPositionFlag;
                        Squares[63] = Piece.Empty;
                    }
                    else
                    {
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[7], 7);
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[5], 5);
                        Squares[5] = Squares[7] & ~Piece.StartingPositionFlag;
                        Squares[7] = Piece.Empty;
                    }
                }
                else if (BoardWidth == 10)
                {
                    if (isWhite)
                    {
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[79], 79);
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[76], 76);
                        Squares[76] = Squares[79] & ~Piece.StartingPositionFlag;
                        Squares[79] = Piece.Empty;
                    }
                    else
                    {
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[9], 9);
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[6], 6);
                        Squares[6] = Squares[9] & ~Piece.StartingPositionFlag;
                        Squares[9] = Piece.Empty;
                    }
                }
            }
            else if (flags == Move.CastleQueenside)
            {
                if (BoardWidth == 8)
                {
                    if (isWhite)
                    {
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[56], 56);
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[59], 59);
                        Squares[59] = Squares[56] & ~Piece.StartingPositionFlag;
                        Squares[56] = Piece.Empty;
                    }
                    else
                    {
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[0], 0);
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[3], 3);
                        Squares[3] = Squares[0] & ~Piece.StartingPositionFlag;
                        Squares[0] = Piece.Empty;
                    }
                }
                else if (BoardWidth == 10)
                {
                    if (isWhite)
                    {
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[70], 70);
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[74], 74);
                        Squares[74] = Squares[70] & ~Piece.StartingPositionFlag;
                        Squares[70] = Piece.Empty;
                    }
                    else
                    {
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[0], 0);
                        ZobristKey ^= Zobrist.GetPieceKey(Squares[4], 4);
                        Squares[4] = Squares[0] & ~Piece.StartingPositionFlag;
                        Squares[0] = Piece.Empty;
                    }
                }
            }
            else if (flags >= Move.PromoteToQueen)
            {
                // Remove pawn already there (was XORed in move piece section)
                ZobristKey ^= Zobrist.GetPieceKey(Squares[to], to);

                if (flags == Move.PromoteToQueen) Squares[to] = isWhite ? Piece.WhiteQueen : Piece.BlackQueen;
                else if (flags == Move.PromoteToKnight) Squares[to] = isWhite ? Piece.WhiteKnight : Piece.BlackKnight;
                else if (flags == Move.PromoteToBishop) Squares[to] = isWhite ? Piece.WhiteBishop : Piece.BlackBishop;
                else if (flags == Move.PromoteToRook) Squares[to] = isWhite ? Piece.WhiteRook : Piece.BlackRook;

                ZobristKey ^= Zobrist.GetPieceKey(Squares[to], to);
            }

            // XOR out old rights, XOR in new rights
            int newRightsMask = Zobrist.GetCastlingRightsMask(this);
            ZobristKey ^= Zobrist.CastlingKeys[oldRightsMask];
            ZobristKey ^= Zobrist.CastlingKeys[newRightsMask];

            ZobristKey ^= Zobrist.SideToMoveKey;
            WhiteToMove = !WhiteToMove;
        }

        public void UnmakeMove()
        {
            if (undoStack.Count == 0) return;
            var undo = undoStack.Pop();
            Move move = new Move();
            move.Value = (ushort)undo.moveValue;

            int from = move.FromSquare;
            int to = move.ToSquare;
            int flags = move.Flags;

            int piece = undo.fromPieceWithFlags;
            int captured = undo.capturedPiece;
            bool isWhite = Piece.IsWhite(piece);

            // Restore pieces
            Squares[from] = piece;
            Squares[to] = (flags == Move.EnPassantCapture) ? Piece.Empty : captured;

            // Undo Special Moves (Rooks)
            if (flags == Move.EnPassantCapture)
            {
                int capturedPawnSq = isWhite ? to + BoardWidth : to - BoardWidth;
                Squares[capturedPawnSq] = captured;
            }
            else if (flags == Move.CastleKingside)
            {
                if (BoardWidth == 8)
                {
                    if (isWhite) { Squares[63] = Squares[61] | Piece.StartingPositionFlag; Squares[61] = Piece.Empty; }
                    else { Squares[7] = Squares[5] | Piece.StartingPositionFlag; Squares[5] = Piece.Empty; }
                }
                else if (BoardWidth == 10)
                {
                    if (isWhite) { Squares[79] = Squares[76] | Piece.StartingPositionFlag; Squares[76] = Piece.Empty; }
                    else { Squares[9] = Squares[6] | Piece.StartingPositionFlag; Squares[6] = Piece.Empty; }
                }
            }
            else if (flags == Move.CastleQueenside)
            {
                if (BoardWidth == 8)
                {
                    if (isWhite) { Squares[56] = Squares[59] | Piece.StartingPositionFlag; Squares[59] = Piece.Empty; }
                    else { Squares[0] = Squares[3] | Piece.StartingPositionFlag; Squares[3] = Piece.Empty; }
                }
                else if (BoardWidth == 10)
                {
                    if (isWhite) { Squares[70] = Squares[74] | Piece.StartingPositionFlag; Squares[74] = Piece.Empty; }
                    else { Squares[0] = Squares[4] | Piece.StartingPositionFlag; Squares[4] = Piece.Empty; }
                }
            }

            HalfmoveClock = undo.prevHalfmoveClock;
            EnPassantSquare = undo.prevEnPassantSquare;
            ZobristKey = undo.prevZobristKey;
            if (WhiteToMove) // If it was White's turn, then it was just Black who moved
            {
                FullmoveNumber--;
            }

            WhiteToMove = !WhiteToMove;
        }

        public string SqToUci(int sq)
        {
            int file = sq % BoardWidth;
            int rank = BoardHeight - (sq / BoardWidth);
            return $"{(char)('a' + file)}{rank}";
        }

        public string GetFen()
        {
            System.Text.StringBuilder fen = new System.Text.StringBuilder();

            // 1. Piece placement
            for (int rank = 0; rank < BoardHeight; rank++)
            {
                int emptyCount = 0;
                for (int file = 0; file < BoardWidth; file++)
                {
                    int piece = Squares[rank * BoardWidth + file];
                    if (piece == Piece.Empty)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            fen.Append(emptyCount);
                            emptyCount = 0;
                        }
                        fen.Append(GetPieceChar(piece));
                    }
                }
                if (emptyCount > 0) fen.Append(emptyCount);
                if (rank < BoardHeight - 1) fen.Append('/');
            }

            // 2. Side to move
            fen.Append(" " + (WhiteToMove ? "w" : "b"));

            // 3. Castling rights
            fen.Append(" ");
            string rights = "";
            if (BoardWidth == 8)
            {
                if (Piece.Type(Squares[60]) == Piece.King && (Squares[60] & Piece.StartingPositionFlag) != 0)
                {
                    if (Piece.Type(Squares[63]) == Piece.Rook && (Squares[63] & Piece.StartingPositionFlag) != 0) rights += "K";
                    if (Piece.Type(Squares[56]) == Piece.Rook && (Squares[56] & Piece.StartingPositionFlag) != 0) rights += "Q";
                }
                if (Piece.Type(Squares[4]) == Piece.King && (Squares[4] & Piece.StartingPositionFlag) != 0)
                {
                    if (Piece.Type(Squares[7]) == Piece.Rook && (Squares[7] & Piece.StartingPositionFlag) != 0) rights += "k";
                    if (Piece.Type(Squares[0]) == Piece.Rook && (Squares[0] & Piece.StartingPositionFlag) != 0) rights += "q";
                }
            }
            else if (BoardWidth == 10)
            {
                if (Piece.Type(Squares[75]) == Piece.King && (Squares[75] & Piece.StartingPositionFlag) != 0)
                {
                    if (Piece.Type(Squares[79]) == Piece.Rook && (Squares[79] & Piece.StartingPositionFlag) != 0) rights += "K";
                    if (Piece.Type(Squares[70]) == Piece.Rook && (Squares[70] & Piece.StartingPositionFlag) != 0) rights += "Q";
                }
                if (Piece.Type(Squares[5]) == Piece.King && (Squares[5] & Piece.StartingPositionFlag) != 0)
                {
                    if (Piece.Type(Squares[9]) == Piece.Rook && (Squares[9] & Piece.StartingPositionFlag) != 0) rights += "k";
                    if (Piece.Type(Squares[0]) == Piece.Rook && (Squares[0] & Piece.StartingPositionFlag) != 0) rights += "q";
                }
            }
            fen.Append(string.IsNullOrEmpty(rights) ? "-" : rights);

            // 4. En passant
            if (EnPassantSquare != -1)
            {
                fen.Append(" " + SqToUci(EnPassantSquare));
            }
            else
            {
                fen.Append(" -");
            }

            // 5. Halfmove clock
            fen.Append(" " + HalfmoveClock);

            // 6. Fullmove number
            fen.Append(" " + FullmoveNumber);

            return fen.ToString();
        }
    }
}
