using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonsterGenerator2_Net47.Models.Data
{
    public class MonsterOrganizations
    {
        public const int Solitary = 4001, Pair = 4002, Group = 4003, Troop = 4004, Troop2 = 4005,
            Hive = 4006, Swarm = 4007, Colony = 4008, Colony2 = 4009, Tangle = 4010, Gang = 4011,
            Squad = 4012, Clan = 4013, Clan2 = 4014, Family = 4015, Brood = 4016, Pile = 4017;

        public const int Flock = 4101, School = 4102;

        public List<Organization> Organizations;

        private RandomGenerationTable Table;

        private static MonsterOrganizations Instance;
        public static MonsterOrganizations GetInstance()
        {
            if (Instance == null)
                Instance = new MonsterOrganizations();

            return Instance;
        }

        public MonsterOrganizations()
        {
            PopulateTypes();
            Table = new RandomGenerationTable();
            Table.FillRandomGenerationTable<Organization>(Organizations);
        }

        private void PopulateTypes()
        {
            Organizations = new List<Organization>();
            Organizations.Add(new Organization() {
                Id = Solitary,
                Name = "Solitary",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = MonsterTypes.AllMonsterTypes,
                MinimumIntelligence = 0
            });
            Organizations.Add(new Organization()
            {
                Id = Pair,
                Name = "Pair",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = MonsterTypes.AllMonsterTypes,
                MinimumIntelligence = 1
            });
            Organizations.Add(new Organization()
            {
                Id = Group,
                Name = "Group (3-6)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = MonsterTypes.AllMonsterTypes,
                MinimumIntelligence = 2
            });
            Organizations.Add(new Organization()
            {
                Id = Troop,
                Name = "Troop (8-16)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = MonsterTypes.AllMonsterTypes,
                MinimumIntelligence = 2
            });
            Organizations.Add(new Organization()
            {
                Id = Troop2,
                Name = "Troop (20-80)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = MonsterTypes.AllMonsterTypes,
                MinimumIntelligence = 2
            });
            Organizations.Add(new Organization()
            {
                Id = Tangle,
                Name = "Tangle (3-6)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Plant, MonsterTypes.Vermin },
                MinimumIntelligence = 0
            });
            Organizations.Add(new Organization()
            {
                Id = Hive,
                Name = "Hive (30-400)",
                Rarity = FeatureElement.RareFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Construct, MonsterTypes.Vermin },
                MinimumIntelligence = 0
            });
            Organizations.Add(new Organization()
            {
                Id = Colony,
                Name = "Colony (3-20)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Animal, MonsterTypes.Aberration, MonsterTypes.Dragon,
                    MonsterTypes.Fey, MonsterTypes.MonstrousHumanoid, MonsterTypes.MagicalBeast, MonsterTypes.Outsider, MonsterTypes.Ooze,
                    MonsterTypes.Plant, MonsterTypes.Vermin },
                MinimumIntelligence = 0
            });
            Organizations.Add(new Organization()
            {
                Id = Colony2,
                Name = "Colony (10-400)",
                Rarity = FeatureElement.RareFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Animal, MonsterTypes.Aberration, MonsterTypes.Dragon,
                    MonsterTypes.Fey, MonsterTypes.MonstrousHumanoid, MonsterTypes.MagicalBeast, MonsterTypes.Outsider, MonsterTypes.Ooze,
                    MonsterTypes.Plant, MonsterTypes.Vermin },
                MinimumIntelligence = 0
            });
            Organizations.Add(new Organization()
            {
                Id = Swarm,
                Name = "Swarm (10-20)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Construct,
                    MonsterTypes.Plant, MonsterTypes.Vermin },
                MinimumIntelligence = 0
            });
            Organizations.Add(new Organization()
            {
                Id = Gang,
                Name = "Gang (2-4)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Construct, MonsterTypes.Outsider, MonsterTypes.MonstrousHumanoid,
                    MonsterTypes.Humanoid, MonsterTypes.Fey, MonsterTypes.Undead},
                MinimumIntelligence = 0
            });

            Organizations.Add(new Organization()
            {
                Id = Squad,
                Name = "Squad (5-10)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Construct, MonsterTypes.Outsider, MonsterTypes.MonstrousHumanoid,
                    MonsterTypes.Humanoid },
                MinimumIntelligence = 2
            });

            Organizations.Add(new Organization()
            {
                Id = Clan,
                Name = "Clan (3-6)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Animal, MonsterTypes.Aberration, MonsterTypes.Fey,
                    MonsterTypes.Outsider, MonsterTypes.MonstrousHumanoid, MonsterTypes.Dragon,
                    MonsterTypes.Humanoid },
                MinimumIntelligence = 2
            });

            Organizations.Add(new Organization()
            {
                Id = Clan2,
                Name = "Clan (10-100)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Animal, MonsterTypes.Aberration, MonsterTypes.Fey,
                    MonsterTypes.Outsider, MonsterTypes.MonstrousHumanoid, MonsterTypes.Dragon,
                    MonsterTypes.Humanoid },
                MinimumIntelligence = 2
            });

            Organizations.Add(new Organization()
            {
                Id = Family,
                Name = "Family (3-10)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Animal, MonsterTypes.Aberration, MonsterTypes.Fey,
                    MonsterTypes.Outsider, MonsterTypes.MonstrousHumanoid, MonsterTypes.Dragon,
                    MonsterTypes.Humanoid, MonsterTypes.Plant, MonsterTypes.MagicalBeast, MonsterTypes.Ooze },
                MinimumIntelligence = 0
            });

            Organizations.Add(new Organization()
            {
                Id = Brood,
                Name = "Brood (3-6)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Animal, MonsterTypes.Aberration,
                    MonsterTypes.Dragon, MonsterTypes.Plant, MonsterTypes.MagicalBeast, MonsterTypes.Ooze, MonsterTypes.Vermin },
                MinimumIntelligence = 0
            });

            Organizations.Add(new Organization()
            {
                Id = Pile,
                Name = "Pile (3-10)",
                Rarity = FeatureElement.RareFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Construct, MonsterTypes.Plant, MonsterTypes.Ooze, MonsterTypes.Vermin },
                MinimumIntelligence = 0
            });

            Organizations.Add(new Organization()
            {
                Id = Flock,
                Name = "Flock (3-12)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Animal, MonsterTypes.Outsider, MonsterTypes.MagicalBeast },
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Winged },
                MinimumIntelligence = 0
            });
            Organizations.Add(new Organization()
            {
                Id = School,
                Name = "School (3-36)",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Animal, MonsterTypes.Outsider, MonsterTypes.MagicalBeast },
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Finned },
                MinimumIntelligence = 0
            });
        }

        public Organization GetOrganization(int id)
        {
            return Organizations.FirstOrDefault(a => a.Id == id);
        }

        public Organization GetRandomMonsterType(System.Random random)
        {
            int id = Table.GetRandom(random);
            return GetOrganization(id);
        }
    }
}
