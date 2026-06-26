using System;
using System.Collections.Generic;

namespace CustomChessEngine
{
    public class MatchRecording
    {
        public string InitialFen { get; private set; }
        public List<string> Moves { get; private set; }

        public MatchRecording(string initialFen)
        {
            InitialFen = initialFen;
            Moves = new List<string>();
        }

        public void AddMove(string uciMove)
        {
            Moves.Add(uciMove);
        }

        public Board GetBoardAtMove(int moveNumber)
        {
            Board board = new Board(Ruleset.ClassicChess);
            board.LoadFromFen(InitialFen);

            int count = Math.Min(moveNumber, Moves.Count);
            for (int i = 0; i < count; i++)
            {
                board.MakeUciMove(Moves[i]);
            }

            return board;
        }

        public void PrintHistory()
        {
            Console.WriteLine("\nMatch History:");
            Console.WriteLine($"Starting FEN: {InitialFen}");
            for (int i = 0; i < Moves.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {Moves[i]}");
            }
        }
    }
}
