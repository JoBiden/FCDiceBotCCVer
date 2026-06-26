using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonsterGenerator2_Net47.Models.Data
{
    public class Skills
    {
        public const int Acrobatics = 1, Appraise = 2, Bluff = 3, Climb = 4, Craft = 5, Diplomacy = 6;
        public const int DisableDevice = 7, Disguise = 8, EscapeArtist = 9, Fly = 10, HandleAnimal = 11, Heal = 12;
        public const int Intimidate = 13, KnowledgeArcana = 14, KnowledgeDungeoneering = 15, KnowledgeGeography = 16;
        public const int KnowledgeHistory = 17, KnowledgeLocal = 18, KnowledgeNature = 19, KnowledgeNobility = 20;
        public const int KnowledgePlanes = 21, KnowledgeReligion = 22, Linguistics = 23, Perception = 24;
        public const int Perform = 25, Profession = 26, Ride = 27, SenseMotive = 28, SleightOfHand = 29;
        public const int Spellcraft = 30, Stealth = 31, Survival = 32, Swim = 33, UseMagicDevice = 34;
        public const int KnowledgeEngineering = 35;

        public List<Skill> AllSkills;

        private RandomGenerationTable Table;

        private static Skills Instance;
        public static Skills GetInstance()
        {
            if (Instance == null)
                Instance = new Skills();

            return Instance;
        }

        public Skills()
        {
            PopulateSKills();
            Table = new RandomGenerationTable();
            Table.FillRandomGenerationTable<Skill>(AllSkills);
        }

        private void PopulateSKills()
        {
            AllSkills = new List<Skill>();
            AllSkills.Add(new Skill()
            {
                Id = Acrobatics,
                Name = "Acrobatics",
                Rarity = FeatureElement.CommonFeature,
                Untrained = true,
                ArmorCheckPenalty = true,
                KeyAbility = BaseAttribute.Dexterity,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Appraise,
                Name = "Appraise",
                Rarity = FeatureElement.RareFeature,
                Untrained = true,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Bluff,
                Name = "Bluff",
                Rarity = FeatureElement.CommonFeature,
                Untrained = true,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Charisma,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Climb,
                Name = "Climb",
                Rarity = FeatureElement.CommonFeature,
                Untrained = true,
                ArmorCheckPenalty = true,
                KeyAbility = BaseAttribute.Strength,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Craft,
                Name = "Craft",
                Rarity = FeatureElement.UncommonFeature,
                Untrained = true,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Diplomacy,
                Name = "Diplomacy",
                Rarity = FeatureElement.UncommonFeature,
                Untrained = true,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Charisma,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = DisableDevice,
                Name = "Disable Device",
                Rarity = FeatureElement.SuperRareFeature,
                Untrained = false,
                ArmorCheckPenalty = true,
                KeyAbility = BaseAttribute.Dexterity,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Disguise,
                Name = "Disguise",
                Rarity = FeatureElement.RareFeature,
                Untrained = true,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Charisma,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = EscapeArtist,
                Name = "Escape Artist",
                Rarity = FeatureElement.CommonFeature,
                Untrained = true,
                ArmorCheckPenalty = true,
                KeyAbility = BaseAttribute.Dexterity,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Fly,
                Name = "Fly",
                Rarity = FeatureElement.CommonFeature,
                Untrained = true,
                ArmorCheckPenalty = true,
                KeyAbility = BaseAttribute.Dexterity,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = HandleAnimal,
                Name = "Handle Animal",
                Rarity = FeatureElement.CommonFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Charisma,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Heal,
                Name = "Heal",
                Rarity = FeatureElement.UncommonFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Wisdom,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Intimidate,
                Name = "Intimidate",
                Rarity = FeatureElement.CommonFeature,
                Untrained = true,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Charisma,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = KnowledgeArcana,
                Name = "Knowledge Arcana",
                Rarity = FeatureElement.RareFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = KnowledgeDungeoneering,
                Name = "Knowledge Dungeoneering",
                Rarity = FeatureElement.RareFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = KnowledgeEngineering,
                Name = "Knowledge Engineering",
                Rarity = FeatureElement.RareFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = KnowledgeGeography,
                Name = "Knowledge Geography",
                Rarity = FeatureElement.RareFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = KnowledgeHistory,
                Name = "Knowledge History",
                Rarity = FeatureElement.RareFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = KnowledgeLocal,
                Name = "Knowledge Local",
                Rarity = FeatureElement.RareFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = KnowledgeNature,
                Name = "Knowledge Nature",
                Rarity = FeatureElement.RareFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = KnowledgeNobility,
                Name = "Knowledge Nobility",
                Rarity = FeatureElement.RareFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = KnowledgePlanes,
                Name = "Knowledge Planes",
                Rarity = FeatureElement.RareFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = KnowledgeReligion,
                Name = "Knowledge Religion",
                Rarity = FeatureElement.RareFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Linguistics,
                Name = "Linguistics",
                Rarity = FeatureElement.RareFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Perception,
                Name = "Perception",
                Rarity = FeatureElement.CommonFeature,
                Untrained = true,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Wisdom,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Perform,
                Name = "Perform",
                Rarity = FeatureElement.UncommonFeature,
                Untrained = true,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Charisma,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Profession,
                Name = "Profession",
                Rarity = FeatureElement.UncommonFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Wisdom,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Ride,
                Name = "Ride",
                Rarity = FeatureElement.UncommonFeature,
                Untrained = true,
                ArmorCheckPenalty = true,
                KeyAbility = BaseAttribute.Dexterity,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = SenseMotive,
                Name = "Sense Motive",
                Rarity = FeatureElement.CommonFeature,
                Untrained = true,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Wisdom,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = SleightOfHand,
                Name = "Sleight Of Hand",
                Rarity = FeatureElement.UncommonFeature,
                Untrained = false,
                ArmorCheckPenalty = true,
                KeyAbility = BaseAttribute.Dexterity,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Spellcraft,
                Name = "Spellcraft",
                Rarity = FeatureElement.UncommonFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Intelligence,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Stealth,
                Name = "Stealth",
                Rarity = FeatureElement.CommonFeature,
                Untrained = true,
                ArmorCheckPenalty = true,
                KeyAbility = BaseAttribute.Dexterity,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Survival,
                Name = "Survival",
                Rarity = FeatureElement.CommonFeature,
                Untrained = true,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Wisdom,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = Swim,
                Name = "Swim",
                Rarity = FeatureElement.CommonFeature,
                Untrained = true,
                ArmorCheckPenalty = true,
                KeyAbility = BaseAttribute.Strength,
                Explanation = ""
            });
            AllSkills.Add(new Skill()
            {
                Id = UseMagicDevice,
                Name = "Use Magic Device",
                Rarity = FeatureElement.RareFeature,
                Untrained = false,
                ArmorCheckPenalty = false,
                KeyAbility = BaseAttribute.Charisma,
                Explanation = ""
            });

        }
        public Skill GetSkill(int id)
        {
            return AllSkills.FirstOrDefault(a => a.Id == id);
        }
        public Skill GetRandomSkill(System.Random random)
        {
            int id = Table.GetRandom(random);
            return GetSkill(id);
        }
    }
}
