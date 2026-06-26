using System;

namespace CustomChessEngine
{
    public class TranspositionTable
    {
        public enum EntryType : byte
        {
            Exact,
            LowerBound,
            UpperBound
        }

        public struct Entry
        {
            public ulong Key;
            public short Score;
            public ushort BestMoveValue;
            public byte Depth;
            public EntryType Type;
        }

        private Entry[] table;
        private int count;

        public TranspositionTable(int sizeInMb)
        {
            // Each entry is: 8 + 2 + 2 + 1 + 1 = 14 bytes
            // Let's round up to 16 for alignment/simplicity
            int entrySize = 16;
            int entries = (sizeInMb * 1024 * 1024) / entrySize;
            table = new Entry[entries];
            count = entries;
        }

        public void Store(ulong key, byte depth, short score, EntryType type, ushort bestMoveValue)
        {
            int index = (int)(key % (ulong)count);

            // Depth-preferred replacement
            if (table[index].Key == 0 || table[index].Depth <= depth)
            {
                table[index].Key = key;
                table[index].Depth = depth;
                table[index].Score = score;
                table[index].Type = type;
                table[index].BestMoveValue = bestMoveValue;
            }
        }

        public bool Lookup(ulong key, out Entry entry)
        {
            int index = (int)(key % (ulong)count);
            if (table[index].Key == key)
            {
                entry = table[index];
                return true;
            }
            entry = default;
            return false;
        }

        public void Clear()
        {
            for (int i = 0; i < count; i++)
            {
                table[i] = default;
            }
        }
    }
}
