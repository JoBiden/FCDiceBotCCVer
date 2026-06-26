using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonsterGenerator2_Net47.Models.Data
{
    public class Races
    {
        public const int Human = 1, Halfling = 2, Orc = 3, Dwarf = 4, Elf = 5,
            Beastman = 6, Daemon = 7, Draenei = 8, Druichi = 9, Harpy = 10,
            Goblin = 11;

        public List<Race> AllRaces;

        private RandomGenerationTable Table;

        private static Races Instance;
        public static Races GetInstance()
        {
            if (Instance == null)
                Instance = new Races();

            return Instance;
        }


        public Races()
        {
            PopulateAbilities();
            Table = new RandomGenerationTable();
            Table.FillRandomGenerationTable<Race>(AllRaces);
        }

        public void PopulateAbilities()
        {
            AllRaces = new List<Race>();

            AllRaces.Add(new Race()
            {
                Id = Human,
                Name = "Human",
                NamingRegionName = "canada",
                RaceConsonants = "bcdhjlmnrstw",
                RaceVowels = "a",
                NameLengthModifier = 1,
                Rarity = FeatureElement.CommonFeature,
                ClassWeightBonuses = new List<int>()
            });
            AllRaces.Add(new Race()
            {
                Id = Dwarf,
                Name = "Dwarf",
                NamingRegionName = "belgium",
                RaceConsonants = "bcdfgnprstv",
                RaceVowels = "aou",
                NameLengthModifier = .8,
                Rarity = FeatureElement.CommonFeature,
                ClassWeightBonuses = new List<int>(),
                //BonusWeapons = new List<WeaponType>() { WeaponType.Axe, WeaponType.BattleAxe, WeaponType.Crossbow, WeaponType.Musket, WeaponType.Warhammer }
            });
            AllRaces.Add(new Race()
            {
                Id = Goblin,
                Name = "Goblin",
                NamingRegionName = "bangladesh",
                RaceConsonants = "cdfgklnprstw",
                RaceVowels = "ao",
                NameLengthModifier = .6,
                Rarity = FeatureElement.CommonFeature,
                ClassWeightBonuses = new List<int>(),
                //BonusWeapons = new List<WeaponType>() { WeaponType.Maul, WeaponType.Spear, WeaponType.Flail, WeaponType.Dagger }
            });
            AllRaces.Add(new Race()
            {
                Id = Elf,
                Name = "Elf",
                NamingRegionName = "belgium",
                RaceConsonants = "cdflmnrsy",
                RaceVowels = "aei",
                NameLengthModifier = 1.2,
                Rarity = FeatureElement.CommonFeature,
                ClassWeightBonuses = new List<int>(),
                //BonusWeapons = new List<WeaponType>() { WeaponType.Longbow, WeaponType.Javelin, WeaponType.Sword, WeaponType.Dagger }
            });
            AllRaces.Add(new Race()
            {
                Id = Druichi,
                Name = "Druichi",
                NamingRegionName = "finland",
                RaceConsonants = "cdfghklmrt",
                RaceVowels = "eiu",
                NameLengthModifier = 1,
                Rarity = FeatureElement.UncommonFeature,
                ClassWeightBonuses = new List<int>(),
                //BonusWeapons = new List<WeaponType>() { WeaponType.Longbow, WeaponType.Javelin, WeaponType.Sword, WeaponType.Halberd }
            });
            AllRaces.Add(new Race()
            {
                Id = Harpy,
                Name = "Harpy",
                NamingRegionName = "estonia",
                RaceConsonants = "dlmpswy",
                RaceVowels = "eiu",
                NameLengthModifier = .8,
                Rarity = FeatureElement.UncommonFeature,
                ClassWeightBonuses = new List<int>(),
                //BonusWeapons = new List<WeaponType>() { WeaponType.Dagger }
            });
            AllRaces.Add(new Race()
            {
                Id = Draenei,
                Name = "Draenei",
                NamingRegionName = "sweden",
                RaceConsonants = "bcdfghlmnrsty",
                RaceVowels = "aeou",
                NameLengthModifier = 1,
                Rarity = FeatureElement.UncommonFeature,
                ClassWeightBonuses = new List<int>(),
                //BonusWeapons = new List<WeaponType>() { WeaponType.Mace, WeaponType.GreatSword, WeaponType.Warhammer }
            });
            AllRaces.Add(new Race()
            {
                Id = Daemon,
                Name = "Daemon",
                NamingRegionName = "england",
                RaceConsonants = "bcdfjlmnrstwy",
                RaceVowels = "au",
                NameLengthModifier = 1,
                Rarity = FeatureElement.RareFeature,
                //BonusWeapons = new List<WeaponType>() { WeaponType.Flail, WeaponType.GreatSword, WeaponType.Dagger }
            });
            AllRaces.Add(new Race()
            {
                Id = Beastman,
                Name = "Beastman",
                NamingRegionName = "netherlands",
                RaceConsonants = "bcdmnrst",
                RaceVowels = "ao",
                NameLengthModifier = .6,
                Rarity = FeatureElement.CommonFeature,
                //BonusWeapons = new List<WeaponType>() { WeaponType.Mace, WeaponType.Halberd, WeaponType.Spear, WeaponType.Longbow, WeaponType.Maul }
            });
            AllRaces.Add(new Race()
            {
                Id = Orc,
                Name = "Orc",
                NamingRegionName = "hungary",
                RaceConsonants = "bcdfghjkmnrtw",
                RaceVowels = "ou",
                NameLengthModifier = .6,
                Rarity = FeatureElement.CommonFeature,
                ClassWeightBonuses = new List<int>(),
                //BonusWeapons = new List<WeaponType>() { WeaponType.BattleAxe, WeaponType.GreatSword, WeaponType.Axe, WeaponType.Longbow }
            });

            AllRaces.Add(new Race()
            {
                Id = Halfling,
                Name = "Halfling",
                NamingRegionName = "united states",
                RaceConsonants = "bcdfhjlmnrstw",
                RaceVowels = "au",
                NameLengthModifier = .9,
                Rarity = FeatureElement.UncommonFeature,
                ClassWeightBonuses = new List<int>(),
                //BonusWeapons = new List<WeaponType>() { WeaponType.Musket, WeaponType.Pistol, WeaponType.Dagger }
            });
            //Races.Add(new Race()
            //{
            //    Name = "Troll",
            //    NamingRegionName = "sweden",
            //    Rarity = Rarity.Uncommon
            //});
            //Races.Add(new Race()
            //{
            //    Name = "Lamia",
            //    NamingRegionName = "belgium",
            //    Rarity = Rarity.Rare
            //});
        }

        public Race GetRace(int id)
        {
            return AllRaces.FirstOrDefault(a => a.Id == id);
        }

        public Race GetRandomRace(System.Random random)
        {
            int id = Table.GetRandom(random);
            return GetRace(id);
        }

        public List<Race> GetRacesOfRarity(double searchRarity)
        {
            return AllRaces.Where(a => a.Rarity == searchRarity).ToList<Race>();
        }

        public Race GetRaceByName(string raceName)
        {
            return AllRaces.FirstOrDefault(b => b.Name == raceName);
        }
    }
}
