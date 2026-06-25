using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonsterGenerator2_Net47.Models.Data
{
    public class MonsterSizeCategories
    {
        //number legs, number arms, number wings
        public const int Fine = 1, Diminutive = 2, Tiny = 3, Small = 4, Medium = 5, Large = 6, Huge = 7, Gargantuan = 8, Colossal = 9, ColossalPlus = 10;

        public List<SizeCategory> SizeCategories;

        public Dictionary<int, double> RandomGenerationTable;

        private RandomGenerationTable Table;

        private static MonsterSizeCategories Instance;
        public static MonsterSizeCategories GetInstance()
        {
            if (Instance == null)
                Instance = new MonsterSizeCategories();

            return Instance;
        }

        public MonsterSizeCategories()
        {
            PopulateMonsterSizeCategories();
            Table = new RandomGenerationTable();
            Table.FillRandomGenerationTable<SizeCategory>(SizeCategories);
        }

        private void PopulateMonsterSizeCategories()
        {
            SizeCategories = new List<SizeCategory>();
            SizeCategories.Add(new SizeCategory()
            {
                Id = Fine,
                SizeNumber = Fine,
                Name = "Fine",
                ImageGenerationName = "Incredibly tiny",
                Explanation = ".",
                Rarity = FeatureElement.RareFeature,
                BaseMovementSlow = 5,
                BaseMovementMedium = 5,
                BaseMovementFast = 10,
                AcBonus = 8,
                ConstructHpBonus = 0,
                UseDexterityForCmb = true,
                CMBBonus = -8,
                CMDBonus = -8,
                Reach = 0,
                SpecialGrappleBonus = 0,
                SquareSize = 0,

                BonusStrength = -8,
                BonusDexterity = 8,
                BonusConstitution = -6,
                BonusMaxAP = -4,
                PowerCost = 0
            });
            SizeCategories.Add(new SizeCategory()
            {
                Id = Diminutive,
                SizeNumber = Diminutive,
                Name = "Diminutive",
                ImageGenerationName = "Very tiny",
                Explanation = ".",
                Rarity = FeatureElement.UncommonFeature,
                BaseMovementSlow = 5,
                BaseMovementMedium = 10,
                BaseMovementFast = 15,
                AcBonus = 4,
                ConstructHpBonus = 0,
                UseDexterityForCmb = true,
                CMBBonus = -4,
                CMDBonus = -4,
                Reach = 2.5,
                SpecialGrappleBonus = 0,
                SquareSize = 2.5,

                BonusStrength = -6,
                BonusDexterity = 6,
                BonusConstitution = -4,
                BonusMaxAP = -2,
                PowerCost = 0
            });
            SizeCategories.Add(new SizeCategory()
            {
                Id = Tiny,
                SizeNumber = Tiny,
                Name = "Tiny",
                ImageGenerationName = "Tiny",
                Explanation = ".",
                Rarity = FeatureElement.UncommonFeature,
                BaseMovementSlow = 10,
                BaseMovementMedium = 15,
                BaseMovementFast = 20,
                AcBonus = 2,
                ConstructHpBonus = 0,
                UseDexterityForCmb = true,
                CMBBonus = -2,
                CMDBonus = -2,
                Reach = 5,
                SpecialGrappleBonus = 0,
                SquareSize = 5,

                BonusStrength = -4,
                BonusDexterity = 4,
                BonusConstitution = -2,
                PowerCost = 0
            });
            SizeCategories.Add(new SizeCategory()
            {
                Id = Small,
                SizeNumber = Small,
                Name = "Small",
                ImageGenerationName = "Small",
                Explanation = ".",
                Rarity = FeatureElement.CommonFeature,
                BaseMovementSlow = 15,
                BaseMovementMedium = 20,
                BaseMovementFast = 30,
                AcBonus = 1,
                ConstructHpBonus = 10,
                UseDexterityForCmb = false,
                CMBBonus = -1,
                CMDBonus = -1,
                Reach = 5,
                SpecialGrappleBonus = 0,
                SquareSize = 5,

                BonusStrength = -2,
                BonusDexterity = 2,
                BonusConstitution = 0,
                PowerCost = 0
            });
            SizeCategories.Add(new SizeCategory()
            {
                Id = Medium,
                SizeNumber = Medium,
                Name = "Medium",
                ImageGenerationName = "",
                Explanation = ".",
                Rarity = FeatureElement.CommonFeature,
                BaseMovementSlow = 20,
                BaseMovementMedium = 30,
                BaseMovementFast = 40,
                AcBonus = 0,
                ConstructHpBonus = 20,
                UseDexterityForCmb = false,
                CMBBonus = 0,
                CMDBonus = 0,
                Reach = 5,
                SpecialGrappleBonus = 0,
                SquareSize = 5,

                BonusStrength = 0,
                BonusDexterity = 0,
                BonusConstitution = 0,
                PowerCost = 0
            });
            SizeCategories.Add(new SizeCategory()
            {
                Id = Large,
                SizeNumber = Large,
                Name = "Large",
                ImageGenerationName = "Large",
                Explanation = ".",
                Rarity = FeatureElement.CommonFeature,
                BaseMovementSlow = 30,
                BaseMovementMedium = 40,
                BaseMovementFast = 50,
                AcBonus = -1,
                ConstructHpBonus = 30,
                UseDexterityForCmb = false,
                CMBBonus = 1,
                CMDBonus = 1,
                Reach = 10,
                SpecialGrappleBonus = 0,
                SquareSize = 10,

                BonusStrength = 6,
                BonusDexterity = -2,
                BonusConstitution = 2,
                BonusMaxAP = -6,
                PowerCost = 3
            });
            SizeCategories.Add(new SizeCategory()
            {
                Id = Huge,
                SizeNumber = Huge,
                Name = "Huge",
                ImageGenerationName = "Huge",
                Explanation = ".",
                Rarity = FeatureElement.UncommonFeature,
                BaseMovementSlow = 30,
                BaseMovementMedium = 50,
                BaseMovementFast = 60,
                AcBonus = -2,
                ConstructHpBonus = 40,
                UseDexterityForCmb = false,
                CMBBonus = 2,
                CMDBonus = 2,
                Reach = 15,
                SpecialGrappleBonus = 0,
                SquareSize = 15,

                BonusStrength = 12,
                BonusDexterity = -4,
                BonusConstitution = 4,
                PowerCost = 6,
                BonusMaxAP = -12,
                MinimumCR = 3
            });
            SizeCategories.Add(new SizeCategory()
            {
                Id = Gargantuan,
                SizeNumber = Gargantuan,
                Name = "Gargantuan",
                ImageGenerationName = "Massive",
                Explanation = ".",
                Rarity = FeatureElement.RareFeature,
                BaseMovementSlow = 40,
                BaseMovementMedium = 60,
                BaseMovementFast = 80,
                AcBonus = -4,
                ConstructHpBonus = 60,
                UseDexterityForCmb = false,
                CMBBonus = 4,
                CMDBonus = 4,
                Reach = 20,
                SpecialGrappleBonus = 0,
                SquareSize = 20,

                BonusStrength = 18,
                BonusDexterity = -6,
                BonusConstitution = 6,
                PowerCost = 9,
                BonusMaxAP = -18,
                MinimumCR = 5
            });
            SizeCategories.Add(new SizeCategory()
            {
                Id = Colossal,
                SizeNumber = Colossal,
                Name = "Colossal",
                ImageGenerationName = "Colossal",
                Explanation = ".",
                Rarity = FeatureElement.SuperRareFeature,
                BaseMovementSlow = 50,
                BaseMovementMedium = 80,
                BaseMovementFast = 100,
                AcBonus = -8,
                ConstructHpBonus = 80,
                UseDexterityForCmb = false,
                CMBBonus = 8,
                CMDBonus = 8,
                Reach = 30,
                SpecialGrappleBonus = 0,
                SquareSize = 30,

                BonusStrength = 24,
                BonusDexterity = -8,
                BonusConstitution = 10,
                PowerCost = 12,
                BonusMaxAP = -26,
                MinimumCR = 8
            });
        }

        public SizeCategory GetSizeCategory(int id)
        {
            return SizeCategories.FirstOrDefault(a => a.Id == id);
        }

        public SizeCategory GetRandomSizeCategory(System.Random random)
        {
            int id = Table.GetRandom(random);
            return GetSizeCategory(id);
        }
    }
}
