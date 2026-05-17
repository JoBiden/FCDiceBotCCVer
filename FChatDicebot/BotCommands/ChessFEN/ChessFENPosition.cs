using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Thank you to Eliza Wisniewska
namespace FChatDicebot.BotCommands.ChessFEN
{
    public class ChessFENPosition
    {
        public Tuple<Color, Piece>[] Board;
        public Color ActiveColor;
        public bool WhiteCanCastleKingside;
        public bool WhiteCanCastleQueenside;
        public bool BlackCanCastleKingside;
        public bool BlackCanCastleQueenside;
        public ChessFENSquare EnPassantTargetSquare;
        public int HalfmoveClock;
        public int FullmoveNumber;

        public ChessFENPosition()
        {
            Board = new Tuple<Color, Piece>[64];
        }

        void loadFenPiecePlacement(string placement)
        {
            int rank = 0;
            int file = 0;

            foreach (char c in placement)
            {
                if (c >= '1' && c <= '8')
                {
                    file += c - '0';
                }
                else if (c == '/')
                {
                    rank += 1;
                    file = 0;
                }
                else
                {
                    int i = 8 * rank + file;
                    char clower = Char.ToLower(c);
                    Color color = (c == clower) ? Color.Black : Color.White;
                    Piece piece;
                    switch (clower)
                    {
                        case 'r':
                            piece = Piece.Rook;
                            break;
                        case 'n':
                            piece = Piece.Knight;
                            break;
                        case 'b':
                            piece = Piece.Bishop;
                            break;
                        case 'q':
                            piece = Piece.Queen;
                            break;
                        case 'k':
                            piece = Piece.King;
                            break;
                        case 'p':
                            piece = Piece.Pawn;
                            break;
                        default:
                            throw new Exception("Invalid FEN");
                    }
                    Board[i] = new Tuple<Color, Piece>(color, piece);
                    ++file;
                }
            }
        }

        public void loadFromFEN(string fen)
        {
            string[] parts = fen.Trim().Split();
            if (parts.Length != 6)
            {
                throw new Exception("Invalid FEN");
            }

            string fenPiecePlacement = parts[0];
            string fenActiveColor = parts[1];
            string fenCastlingAvailability = parts[2];
            string fenEnPassantTargetSquare = parts[3];
            string fenHalfmoveClock = parts[4];
            string fenFullmoveNumber = parts[5];

            loadFenPiecePlacement(fenPiecePlacement);

            switch (fenActiveColor)
            {
                case "w":
                    ActiveColor = Color.White;
                    break;
                case "b":
                    ActiveColor = Color.Black;
                    break;
                default:
                    throw new Exception("Invalid FEN");
            }

            foreach (char c in fenCastlingAvailability)
            {
                switch (c)
                {
                    case 'K':
                        WhiteCanCastleKingside = true;
                        break;

                    case 'Q':
                        WhiteCanCastleQueenside = true;
                        break;

                    case 'k':
                        BlackCanCastleKingside = true;
                        break;

                    case 'q':
                        BlackCanCastleQueenside = true;
                        break;
                }
            }

            if (fenEnPassantTargetSquare == "-")
                EnPassantTargetSquare = null;
            else
                EnPassantTargetSquare = new ChessFENSquare(fenEnPassantTargetSquare);

            HalfmoveClock = int.Parse(fenHalfmoveClock);
            FullmoveNumber = int.Parse(fenFullmoveNumber);
        }


        private string squareCharacters(Tuple<Color, Piece> square, bool darkSquare)
        {
            bool includeColor = true;
            string bbcode = "";
            if (square == null)
            {
                bbcode += "╳";
            }
            else
            {
                Color color = square.Item1;
                Piece piece = square.Item2;
                if (color == Color.Black)
                {
                    if (includeColor)
                        bbcode += "[color=black]";

                    switch (piece)
                    {
                        case Piece.Rook:
                            bbcode += "♜";
                            break;

                        case Piece.Knight:
                            bbcode += "♞";
                            break;

                        case Piece.Bishop:
                            bbcode += "♝";
                            break;

                        case Piece.Queen:
                            bbcode += "♛";
                            break;

                        case Piece.King:
                            bbcode += "♚";
                            break;

                        case Piece.Pawn:
                            bbcode += "♟";
                            break;
                    }
                    if (includeColor)
                        bbcode += "[/color]";
                }
                else
                {
                    if (includeColor)
                        bbcode += "[color=white]";

                    switch (piece)
                    {
                        case Piece.Rook:
                            bbcode += "♖";
                            break;

                        case Piece.Knight:
                            bbcode += "♘";
                            break;

                        case Piece.Bishop:
                            bbcode += "♗";
                            break;

                        case Piece.Queen:
                            bbcode += "♕";
                            break;

                        case Piece.King:
                            bbcode += "♔";
                            break;

                        case Piece.Pawn:
                            bbcode += "♙";
                            break;
                    }
                    if (includeColor)
                        bbcode += "[/color]";
                }

                
            }

            return bbcode;
        }

        private string squareBBCode(Tuple<Color, Piece> square, bool darkSquare)
        {
            string bbcode = "[eicon]";

            if (square == null)
            {
                bbcode += "square";
            }
            else
            {
                Color color = square.Item1;
                Piece piece = square.Item2;
                if (color == Color.Black)
                {
                    bbcode += "b";
                }
                else
                {
                    bbcode += "w";
                }

                switch (piece)
                {
                    case Piece.Rook:
                        bbcode += "rook";
                        break;

                    case Piece.Knight:
                        bbcode += "knight";
                        break;

                    case Piece.Bishop:
                        bbcode += "bishop";
                        break;

                    case Piece.Queen:
                        bbcode += "queen";
                        break;

                    case Piece.King:
                        bbcode += "king";
                        break;

                    case Piece.Pawn:
                        bbcode += "pawn";
                        break;
                }
            }

            if (darkSquare)
            {
                bbcode += "d";
            }
            else
            {
                bbcode += "l";
            }
            bbcode += "[/eicon]";
            return bbcode;
        }

        public string ToBBCode(bool eicons)
        {
            string bbcode = "";
            bool darkSquare = false;

            IEnumerable<Tuple<Color, Piece>> board;
            if (ActiveColor == Color.Black)
            {
                board = Board.Reverse();
            }
            else
            {
                board = Board;
            }

            int i = 0;
            foreach (var sq in board)
            {
                if (eicons)
                    bbcode += squareBBCode(sq, darkSquare);
                else
                    bbcode += squareCharacters(sq, darkSquare);

                if (++i == 8)
                {
                    bbcode += "\n";
                    i = 0;
                }
                else
                {
                    darkSquare = !darkSquare;
                }
            }

            return bbcode;
        }

    }

    public enum Color { White, Black };

    public enum Piece /* (or pawn) */ { Rook, Knight, Bishop, Queen, King, Pawn };

    public class ChessFENSquare
    {
        private int n;
        public override string ToString()
        {
            char rank = (char) ('a' + (n / 8));
            char file = (char) ('1' + (n % 8));
            return string.Format("{1}{2}",rank, file);
        }
        public ChessFENSquare(string s)
        {
            if (s.Length != 2)
            {
                throw new Exception(s + " is not a valid square");
            }
            int rankNum = s[0] - 'a';
            int fileNum = s[1] - '1';
            if (rankNum < 0 || rankNum > 7 || fileNum < 0 || fileNum > 7)
            {
                throw new Exception(s + " is not a valid square");
            }
            n = 8 * rankNum + fileNum;
        }
    }

}


