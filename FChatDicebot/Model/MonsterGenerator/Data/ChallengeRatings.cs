using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonsterGenerator2_Net47.Models.Data
{
    public class ChallengeRatings
    {
        public const int MaximumChallengeRating = 50;

        public const int OneHalf = 101, OneThird = 102, OneFourth = 103, OneSixth = 104, OneEighth = 105;//, OneTenth = 106;

        public List<ChallengeRating> AllChallengeRatings;

        private RandomGenerationTable Table;

        private static ChallengeRatings Instance;
        public static ChallengeRatings GetInstance()
        {
            if (Instance == null)
                Instance = new ChallengeRatings();

            return Instance;
        }


        public ChallengeRatings()
        {
            PopulateData();
            Table = new RandomGenerationTable();
            Table.FillRandomGenerationTable<ChallengeRating>(AllChallengeRatings);
        }

        private void PopulateData()
        {
            AllChallengeRatings = new List<ChallengeRating>();

            AllChallengeRatings.Add(new ChallengeRating()
            {
                Id = OneEighth,
                Name = "1/8",
                Rarity = FeatureElement.SuperRareFeature,
                Amount = .125,
                XpAward = 50
            });
            AllChallengeRatings.Add(new ChallengeRating()
            {
                Id = OneSixth,
                Name = "1/6",
                Rarity = FeatureElement.RareFeature,
                Amount = .166,
                XpAward = 65
            });
            AllChallengeRatings.Add(new ChallengeRating()
            {
                Id = OneFourth,
                Name = "1/4",
                Rarity = FeatureElement.UncommonFeature,
                Amount = .25,
                XpAward = 100
            });
            AllChallengeRatings.Add(new ChallengeRating()
            {

                Id = OneThird,
                Name = "1/3",
                Rarity = FeatureElement.CommonFeature,
                Amount = .33,
                XpAward = 135
            });
            AllChallengeRatings.Add(new ChallengeRating()
            {

                Id = OneHalf,
                Name = "1/2",
                Rarity = FeatureElement.CommonFeature,
                Amount = .5,
                XpAward = 200
            });

            for(int i = 1; i <= MaximumChallengeRating; i++)
            {
                double rarity = FeatureElement.CommonFeature;
                int xpAward = 400;
                if (i > 10)
                    rarity = FeatureElement.UncommonFeature;
                if (i > 19)
                    rarity = FeatureElement.RareFeature;
                if (i > 26)
                    rarity = FeatureElement.SuperRareFeature;
                if (i > 30)
                    rarity = FeatureElement.DoNotGenerateFeature;

                switch (i)
                {
                    case 1: xpAward = 400; break;
                    case 2: xpAward = 600; break;
                    case 3: xpAward = 800; break;
                    case 4: xpAward = 1200; break;
                    case 5: xpAward = 1600; break;
                    case 6: xpAward = 2400; break;
                    case 7: xpAward = 3200; break;
                    case 8: xpAward = 4800; break;
                    case 9: xpAward = 6400; break;
                    case 10: xpAward = 9600; break;
                    case 11: xpAward = 12800; break;
                    case 12: xpAward = 19200; break;
                    case 13: xpAward = 25600; break;
                    case 14: xpAward = 38400; break;
                    case 15: xpAward = 51200; break;
                    case 16: xpAward = 76800; break;
                    case 17: xpAward = 102400; break;
                    case 18: xpAward = 153600; break;
                    case 19: xpAward = 204800; break;
                    case 20: xpAward = 307200; break;
                    case 21: xpAward = 409600; break;
                    case 22: xpAward = 614400; break;
                    case 23: xpAward = 819200; break;
                    case 24: xpAward = 1228800; break;
                    case 25: xpAward = 1638400; break;
                    case 26: xpAward = 2457600; break;
                    case 27: xpAward = 3276800; break;
                    case 28: xpAward = 4915200; break;
                    case 29: xpAward = 6553600; break;
                    case 30: xpAward = 9830400; break;
                }

                int currentCR = i;
                double currentHighXpAward = 9830400;
                while (currentCR > 30)
                {
                    if(currentCR % 2 == 0)
                    {
                        currentHighXpAward *= 1.5;
                    }
                    else
                    {
                        currentHighXpAward *= 1.33;
                    }
                    currentCR--;
                    xpAward = (int) Math.Round(currentHighXpAward / 100) * 100;
                }

                AllChallengeRatings.Add(new ChallengeRating()
                {
                    Id = i,
                    Name = i.ToString(),
                    Rarity = rarity,
                    Amount = i,
                    XpAward = xpAward
                });
            }
        }
        public ChallengeRating GetChallengeRating(int id)
        {
            return AllChallengeRatings.FirstOrDefault(a => a.Id == id);
        }
        public ChallengeRating GetRandomChallengeRating(System.Random random)
        {
            int id = Table.GetRandom(random);
            return GetChallengeRating(id);
        }
    }

}
