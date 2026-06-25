using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonsterGenerator2_Net47.Models.Data
{
    public class MonsterBodyTypes
    {
        //number legs, number arms, number wings
        public const int Biped = 101, Quadruped = 102, Serpentine = 103, Winged = 104, Finned = 105, Arachnid = 106, Other = 107;

        public List<BodyType> BodyTypes;

        public Dictionary<int, double> RandomGenerationTable;

        private RandomGenerationTable Table;

        private static MonsterBodyTypes Instance;
        public static MonsterBodyTypes GetInstance()
        {
            if (Instance == null)
                Instance = new MonsterBodyTypes();

            return Instance;
        }

        public MonsterBodyTypes()
        {
            PopulateBodyTypes();
            Table = new RandomGenerationTable();
            Table.FillRandomGenerationTable<BodyType>(BodyTypes);
        }

        private void PopulateBodyTypes()
        {
            BodyTypes = new List<BodyType>();
            BodyTypes.Add(new BodyType()
            {
                Id = Biped,
                Name = "Biped",
                Explanation = "Walks on two legs on land. Bipeds don't get any abnormal bonuses or movement types and have average speed.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Aberration, MonsterTypes.Construct, MonsterTypes.Fey,
                    MonsterTypes.Humanoid, MonsterTypes.MonstrousHumanoid, MonsterTypes.Outsider, MonsterTypes.Undead },
                PointsCost = 0,
                SpeedCategory = SpeedCategory.Medium,
                BaseLandSpeed = 1,
                BaseBurrowSpeed = 0, BaseClimbSpeed = 0, BaseSwimSpeed = 0, BaseFlySpeed = 0, BaseFlyManeuverability = 0
            });

            BodyTypes.Add(new BodyType()
            {
                Id = Quadruped,
                Name = "Quadruped",
                Explanation = "Walks on four legs on land. Quadropeds get a bonus to CMD vs trip attacks, and carry more weight than bipedal creatures. Creatures with 4 or more legs typically have less reach and more movement speed than other creatures.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Aberration, MonsterTypes.Construct, MonsterTypes.Fey,
                    MonsterTypes.Animal, MonsterTypes.MonstrousHumanoid, MonsterTypes.Outsider, MonsterTypes.Undead, MonsterTypes.MagicalBeast,
                    MonsterTypes.Vermin, MonsterTypes.Dragon},
                PointsCost = 0,
                SpeedCategory = SpeedCategory.Fast,
                BaseLandSpeed = 1,
                BaseBurrowSpeed = 0,
                BaseClimbSpeed = 0,
                BaseSwimSpeed = 0,
                BaseFlySpeed = 0,
                BaseFlyManeuverability = 0,
                BonusAbilities = new List<Ability>() { MonsterAbilities.GetInstance().GetAbility(MonsterAbilities.LegStability) }
            });

            BodyTypes.Add(new BodyType()
            {
                Id = Serpentine,
                Name = "Serpentine",
                Explanation = "Crawls or slithers on its belly. Serpentine creatures get a get a climb speed equal to their land speed, and typically have less reach and less movement speed than other creatures.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Aberration, MonsterTypes.Construct,
                    MonsterTypes.Animal, MonsterTypes.MonstrousHumanoid, MonsterTypes.Outsider, MonsterTypes.Undead, MonsterTypes.MagicalBeast,
                    MonsterTypes.Vermin, MonsterTypes.Dragon, MonsterTypes.Plant},
                PointsCost = 0,
                SpeedCategory = SpeedCategory.Medium,
                BaseLandSpeed = 1,
                BaseBurrowSpeed = 0,
                BaseClimbSpeed = 1,
                BaseSwimSpeed = 0,
                BaseFlySpeed = 0,
                BaseFlyManeuverability = 0,
                BonusAbilities = new List<Ability>() { MonsterAbilities.GetInstance().GetAbility(MonsterAbilities.ClimbSpeed) }
            });

            BodyTypes.Add(new BodyType()
            {
                Id = Winged,
                Name = "Winged",
                Explanation = "Flies in the air for its main form of movement. Winged creatures get a get a fly speed equal to 2 times their land speed, and typically have less movement speed than other creatures.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Aberration, MonsterTypes.Construct,
                    MonsterTypes.Animal, MonsterTypes.MonstrousHumanoid, MonsterTypes.Outsider, MonsterTypes.Undead, MonsterTypes.MagicalBeast,
                    MonsterTypes.Vermin, MonsterTypes.Dragon},
                PointsCost = 0,
                SpeedCategory = SpeedCategory.Medium,
                BaseLandSpeed = 1,
                BaseBurrowSpeed = 0,
                BaseClimbSpeed = 0,
                BaseSwimSpeed = 0,
                BaseFlySpeed = 2,
                BaseFlyManeuverability = 0,
                BonusAbilities = new List<Ability>() { MonsterAbilities.GetInstance().GetAbility(MonsterAbilities.FlySpeed) }
            });

            BodyTypes.Add(new BodyType()
            {
                Id = Finned,
                Name = "Finned",
                Explanation = "Swims through water for its main form of movement. Finned creatures get a get a swim speed instead of a land speed, and typically have less or no normal move speed.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Aberration,
                    MonsterTypes.Animal, MonsterTypes.Outsider, MonsterTypes.MagicalBeast,
                    MonsterTypes.Vermin, MonsterTypes.Plant},
                PointsCost = 0,
                SpeedCategory = SpeedCategory.Slow,
                BaseLandSpeed = 0,
                BaseBurrowSpeed = 0,
                BaseClimbSpeed = 0,
                BaseSwimSpeed = 1,
                BaseFlySpeed = 0,
                BaseFlyManeuverability = 0,
                BonusAbilities = new List<Ability>() { MonsterAbilities.GetInstance().GetAbility(MonsterAbilities.SwimSpeed) }
            });

            BodyTypes.Add(new BodyType()
            {
                Id = Other,
                Name = "Other",
                Explanation = "Uses various means of transportation like tentacles, levitation, teleportation, or rolling. They have a normal move speed, but often take other movement abilities.",
                Rarity = FeatureElement.RareFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Aberration, MonsterTypes.Construct, MonsterTypes.Ooze, MonsterTypes.Fey,
                    MonsterTypes.Animal, MonsterTypes.MonstrousHumanoid, MonsterTypes.Outsider, MonsterTypes.Undead, MonsterTypes.MagicalBeast,
                    MonsterTypes.Vermin, MonsterTypes.Dragon, MonsterTypes.Plant},
                PointsCost = 0,
                SpeedCategory = SpeedCategory.Slow,
                BaseLandSpeed = 1,
                BaseBurrowSpeed = 0,
                BaseClimbSpeed = 0,
                BaseSwimSpeed = 0,
                BaseFlySpeed = 0,
                BaseFlyManeuverability = 0
            });

            BodyTypes.Add(new BodyType()
            {
                Id = Arachnid,
                Name = "Arachnid",
                Explanation = "Walks on 8 or more limbs. Arachnids get a bonus to CMD vs trip attacks, and carry more weight than bipedal creatures. Arachnid creatures get a get a climb speed equal to their land speed, and typically have less reach and less movement speed than other creatures.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Aberration, MonsterTypes.Construct, MonsterTypes.Fey,
                    MonsterTypes.MonstrousHumanoid, MonsterTypes.Outsider, MonsterTypes.Undead, MonsterTypes.MagicalBeast,
                    MonsterTypes.Vermin},
                PointsCost = 0,
                SpeedCategory = SpeedCategory.Medium,
                BaseLandSpeed = 1,
                BaseBurrowSpeed = 0,
                BaseClimbSpeed = 1,
                BaseSwimSpeed = 0,
                BaseFlySpeed = 0,
                BaseFlyManeuverability = 0,
                BonusAbilities = new List<Ability>() { MonsterAbilities.GetInstance().GetAbility(MonsterAbilities.LegStability), MonsterAbilities.GetInstance().GetAbility(MonsterAbilities.ClimbSpeed) }
            });

        }

        public BodyType GetBodyType(int id)
        {
            return BodyTypes.FirstOrDefault(a => a.Id == id);
        }

        public BodyType GetRandomBodyType(System.Random random)
        {
            int id = Table.GetRandom(random);
            return GetBodyType(id);
        }
    }

    public enum SpeedCategory
    {
        NONE,
        Slow,
        Medium,
        Fast
    }
}
