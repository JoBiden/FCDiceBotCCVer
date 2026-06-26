using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonsterGenerator2_Net47.Models.Data
{
    public class Alignments
    {
        public const int LawfulGood = 11, NeutralGood = 21, ChaoticGood = 31,
            LawfulNeutral = 12, Neutral = 22, ChaoticNeutral = 32,
            LawfulEvil = 13, NeutralEvil= 23, ChaoticEvil = 33;

        public List<Alignment> AllAlignments;

        private RandomGenerationTable Table;

        private static Alignments Instance;
        public static Alignments GetInstance()
        {
            if (Instance == null)
                Instance = new Alignments();

            return Instance;
        }

        public Alignments()
        {
            PopulateAbilities();
            Table = new RandomGenerationTable();
            Table.FillRandomGenerationTable<Alignment>(AllAlignments);
        }

        private void PopulateAbilities()
        {
            AllAlignments = new List<Alignment>();
            AllAlignments.Add(new Alignment()
            {
                Id = LawfulGood,
                Name = "Lawful Good",
                Explanation = ".",
                Abbreviation = "LG",
                Rarity = FeatureElement.CommonFeature
            });
            AllAlignments.Add(new Alignment()
            {
                Id = LawfulNeutral,
                Name = "Lawful Neutral",
                Explanation = ".",
                Abbreviation = "LN",
                Rarity = FeatureElement.CommonFeature
            });
            AllAlignments.Add(new Alignment()
            {
                Id = LawfulEvil,
                Name = "Lawful Evil",
                Explanation = ".",
                Abbreviation = "LE",
                Rarity = FeatureElement.CommonFeature
            });
            AllAlignments.Add(new Alignment()
            {
                Id = NeutralGood,
                Name = "Neutral Good",
                Explanation = ".",
                Abbreviation = "NG",
                Rarity = FeatureElement.CommonFeature
            });
            AllAlignments.Add(new Alignment()
            {
                Id = Neutral,
                Name = "Neutral",
                Explanation = ".",
                Abbreviation = "N",
                Rarity = FeatureElement.CommonFeature
            });
            AllAlignments.Add(new Alignment()
            {
                Id = NeutralEvil,
                Name = "Neutral Evil",
                Explanation = ".",
                Abbreviation = "NE",
                Rarity = FeatureElement.CommonFeature
            });
            AllAlignments.Add(new Alignment()
            {
                Id = ChaoticGood,
                Name = "Chaotic Good",
                Explanation = ".",
                Abbreviation = "CG",
                Rarity = FeatureElement.CommonFeature
            });
            AllAlignments.Add(new Alignment()
            {
                Id = ChaoticNeutral,
                Name = "Chaotic Neutral",
                Explanation = ".",
                Abbreviation = "CN",
                Rarity = FeatureElement.CommonFeature
            });
            AllAlignments.Add(new Alignment()
            {
                Id = ChaoticEvil,
                Name = "Chaotic Evil",
                Explanation = ".",
                Abbreviation = "CE",
                Rarity = FeatureElement.CommonFeature
            });

        }
        public Alignment GetAlignment(int id)
        {
            return AllAlignments.FirstOrDefault(a => a.Id == id);
        }
        public Alignment GetRandomAlignment(System.Random random)
        {
            int id = Table.GetRandom(random);
            return GetAlignment(id);
        }
    }
}
