using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonsterGenerator2_Net47.Models.Data
{
    public class MonsterTypes
    {
        public const double BaFast = 1, BaAverage = .75, BaSlow = .5;

        public const int Aberration = 1, Animal = 2, Construct = 3, Dragon = 4;
        public const int Fey = 5, Humanoid = 6, MagicalBeast = 7, MonstrousHumanoid = 8;
        public const int Ooze = 9, Outsider = 10, Plant = 11, Undead = 12;
        public const int Vermin = 13;
        //used to be lazy on other class initizalizations
        public static List<int> AllMonsterTypes = new List<int>() { MonsterTypes.Aberration, MonsterTypes.Animal, MonsterTypes.Construct, MonsterTypes.Dragon,
            MonsterTypes.Fey, MonsterTypes.Humanoid, MonsterTypes.MagicalBeast, MonsterTypes.MonstrousHumanoid, MonsterTypes.Ooze,
            MonsterTypes.Outsider, MonsterTypes.Plant, MonsterTypes.Undead, MonsterTypes.Vermin };

        public List<MonsterType> Types;

        private RandomGenerationTable Table;

        private static MonsterTypes Instance;
        public static MonsterTypes GetInstance()
        {
            if (Instance == null)
                Instance = new MonsterTypes();

            return Instance;
        }


        public MonsterTypes()
        {
            PopulateTypes();
            Table = new RandomGenerationTable();
            Table.FillRandomGenerationTable<MonsterType>(Types);
        }

        private void PopulateTypes()
        {
            Types = new List<MonsterType>();
            Types.Add(new MonsterType() {
                Id = Aberration,
                Name = "Aberration",
                ImageGenerationName = "Spirit",
                Rarity = FeatureElement.RareFeature,
                BaseSKillAmount = 4,
                GoodFortitudeSave = false, GoodReflexSave = false, GoodWillSave = true,
                HitDieSides = 8,
                BaseAttackAmount = BaAverage,
                ClassSkillIds = new List<int>() { Skills.Acrobatics, Skills.Climb, Skills.EscapeArtist, Skills.Fly, Skills.Intimidate,
                    Skills.Perception, Skills.Spellcraft, Skills.Stealth,
                    Skills.Survival, Skills.Swim },
                SelectableKnowledgeSkills = 1,
                BaseCharisma = 10,
                BaseIntelligence = 10,
                BaseWisdom = 10,
                BaseNaturalAcRatio = 1.1,
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision60, MonsterAbilities.NoConstitution },
                FeatIds = new List<int>() { Feats.SimpleWeaponProficiency }
            });

            Types.Add(new MonsterType()
            {
                Id = Animal,
                Name = "Animal",
                ImageGenerationName = "Creature",
                Rarity = FeatureElement.CommonFeature,
                BaseSKillAmount = 2,
                GoodFortitudeSave = true,
                GoodReflexSave = true,
                GoodWillSave = false,
                HitDieSides = 8,
                BaseAttackAmount = BaAverage,
                ClassSkillIds = new List<int>() { Skills.Acrobatics, Skills.Climb, Skills.Fly, Skills.Perception, Skills.Stealth, Skills.Swim },
                BaseCharisma = 6,
                BaseIntelligence = 2,
                BaseWisdom = 10,
                BaseNaturalAcRatio = 1.1
            });

            Types.Add(new MonsterType()
            {
                Id = Construct,
                Name = "Construct",
                ImageGenerationName = "Golem",
                Rarity = FeatureElement.UncommonFeature,
                BaseSKillAmount = 2,
                GoodFortitudeSave = false,
                GoodReflexSave = false,
                GoodWillSave = false,
                HitDieSides = 10,
                BaseAttackAmount = BaFast,
                ClassSkillIds = new List<int>(),
                BaseCharisma = 1,
                BaseIntelligence = 0,
                BaseWisdom = 10,
                BaseNaturalAcRatio = 1.3,
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision60, MonsterAbilities.MindlessInt, MonsterAbilities.LowLightVision,
                    MonsterAbilities.NoConstitution, MonsterAbilities.ImmunityNecromancyEffects, MonsterAbilities.ConstructTraits,
                    MonsterAbilities.ImmunityMindAffecting, MonsterAbilities.ImmunityBleed, MonsterAbilities.ImmunityDisease, MonsterAbilities.ImmunityDeathEffects,
                    MonsterAbilities.ImmunityStun, MonsterAbilities.ImmunityParalysis, MonsterAbilities.ImmunitySleep, MonsterAbilities.ImmunityPoison }
            });

            Types.Add(new MonsterType()
            {
                Id = Dragon,
                Name = "Dragon",
                ImageGenerationName = "",
                Rarity = FeatureElement.UncommonFeature,
                BaseSKillAmount = 6,
                GoodFortitudeSave = true,
                GoodReflexSave = true,
                GoodWillSave = true,
                HitDieSides = 12,
                BaseAttackAmount = BaFast,
                ClassSkillIds = new List<int>() { Skills.Appraise, Skills.Bluff, Skills.Climb, Skills.Craft, Skills.Diplomacy, Skills.Fly,
                    Skills.Heal, Skills.Intimidate,
                    Skills.KnowledgeArcana, Skills.KnowledgeDungeoneering, Skills.KnowledgeEngineering, Skills.KnowledgeGeography,
                    Skills.KnowledgeHistory, Skills.KnowledgeLocal, Skills.KnowledgeNature, Skills.KnowledgeNobility,
                    Skills.KnowledgePlanes, Skills.KnowledgeReligion, Skills.Linguistics, Skills.Perception, Skills.SenseMotive, Skills.Spellcraft,
                    Skills.Stealth, Skills.Survival, Skills.Swim, Skills.UseMagicDevice },
                BaseCharisma = 10,
                BaseIntelligence = 10,
                BaseWisdom = 10,
                BaseNaturalAcRatio = 1.3,
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision60, MonsterAbilities.LowLightVision,
                    MonsterAbilities.ImmunitySleep, MonsterAbilities.ImmunityParalysis },
                FeatIds = new List<int>() { Feats.SimpleWeaponProficiency }
            });

            Types.Add(new MonsterType()
            {
                Id = Fey,
                Name = "Fey",
                ImageGenerationName = "Fairy",
                Rarity = FeatureElement.UncommonFeature,
                BaseSKillAmount = 6,
                GoodFortitudeSave = false,
                GoodReflexSave = true,
                GoodWillSave = true,
                HitDieSides = 6,
                BaseAttackAmount = BaSlow,
                ClassSkillIds = new List<int>() { Skills.Acrobatics, Skills.Bluff, Skills.Climb, Skills.Craft, Skills.Diplomacy, Skills.Disguise,
                    Skills.EscapeArtist, Skills.Fly, Skills.KnowledgeGeography,
                    Skills.KnowledgeLocal, Skills.KnowledgeNature, Skills.Perception, Skills.Perform, Skills.SenseMotive, Skills.SleightOfHand,
                    Skills.Stealth, Skills.Swim, Skills.UseMagicDevice },
                BaseCharisma = 10,
                BaseIntelligence = 10,
                BaseWisdom = 10,
                BaseNaturalAcRatio = 1.1,
                AbilityIds = new List<int>() { MonsterAbilities.LowLightVision,
                    MonsterAbilities.ImmunitySleep, MonsterAbilities.ImmunityParalysis },
                FeatIds = new List<int>() { Feats.SimpleWeaponProficiency }
            });

            Types.Add(new MonsterType()
            {
                Id = Humanoid,
                Name = "Humanoid",
                ImageGenerationName = "Adventurer",
                Rarity = FeatureElement.CommonFeature,
                BaseSKillAmount = 2,
                GoodFortitudeSave = false,
                GoodReflexSave = false,
                GoodWillSave = false,
                SelectableGoodSaves = 1,
                HitDieSides = 8,
                BaseAttackAmount = BaAverage,
                BaseNaturalAcRatio = 0.9,
                ClassSkillIds = new List<int>() { Skills.Climb, Skills.Craft, Skills.HandleAnimal, Skills.Heal,
                    Skills.Profession, Skills.Ride, Skills.Survival },
                BaseCharisma = 10,
                BaseIntelligence = 10,
                BaseWisdom = 10,
                FeatIds = new List<int>() { Feats.SimpleWeaponProficiency }
            });

            Types.Add(new MonsterType()
            {
                Id = MagicalBeast,
                Name = "Magical Beast",
                ImageGenerationName = "Monster",
                Rarity = FeatureElement.CommonFeature,
                BaseSKillAmount = 2,
                GoodFortitudeSave = true,
                GoodReflexSave = true,
                GoodWillSave = false,
                HitDieSides = 10,
                BaseAttackAmount = BaFast,
                ClassSkillIds = new List<int>() { Skills.Acrobatics, Skills.Climb, Skills.Fly, Skills.Perception, Skills.Stealth, Skills.Swim },
                BaseCharisma = 10,
                BaseIntelligence = 2,
                BaseWisdom = 10,
                BaseNaturalAcRatio = 1.1,
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision60, MonsterAbilities.LowLightVision}
            });

            Types.Add(new MonsterType()
            {
                Id = MonstrousHumanoid,
                Name = "Monstrous Humanoid",
                ImageGenerationName = "Monster",
                Rarity = FeatureElement.CommonFeature,
                BaseSKillAmount = 4,
                GoodFortitudeSave = false,
                GoodReflexSave = true,
                GoodWillSave = true,
                HitDieSides = 10,
                BaseAttackAmount = BaFast,
                ClassSkillIds = new List<int>() { Skills.Climb, Skills.Craft, Skills.Fly, Skills.Intimidate, Skills.Perception,
                    Skills.Ride, Skills.Stealth, Skills.Survival, Skills.Swim },
                BaseCharisma = 10,
                BaseIntelligence = 10,
                BaseWisdom = 10,
                BaseNaturalAcRatio = 1.1,
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision60 },
                FeatIds = new List<int>() { Feats.SimpleWeaponProficiency }
            });

            Types.Add(new MonsterType()
            {
                Id = Ooze,
                Name = "Ooze",
                ImageGenerationName = "Creature",
                Rarity = FeatureElement.UncommonFeature,
                BaseSKillAmount = 2,
                GoodFortitudeSave = false,
                GoodReflexSave = false,
                GoodWillSave = false,
                HitDieSides = 8,
                BaseAttackAmount = BaAverage,
                ClassSkillIds = new List<int>(),
                BaseCharisma = 1,
                BaseIntelligence = 0,
                BaseWisdom = 1,
                BaseNaturalAcRatio = 0.9,
                AbilityIds = new List<int>() { MonsterAbilities.MindlessInt, MonsterAbilities.ImmunityMindAffecting, MonsterAbilities.Blind,
                    MonsterAbilities.ImmunityPoison, MonsterAbilities.ImmunityStun, MonsterAbilities.ImmunityParalysis, MonsterAbilities.ImmunitySleep,
                    MonsterAbilities.ImmunityPolymorph, MonsterAbilities.ImmunityFlanking, MonsterAbilities.ImmunityPrecisionDamage, MonsterAbilities.ImmunityCriticalHits}
            });

            Types.Add(new MonsterType()
            {
                Id = Outsider,
                Name = "Outsider",
                ImageGenerationName = "Spirit",
                Rarity = FeatureElement.CommonFeature,
                BaseSKillAmount = 6,
                GoodFortitudeSave = false,
                GoodReflexSave = false,
                GoodWillSave = false,
                SelectableGoodSaves = 2,
                HitDieSides = 10,
                BaseAttackAmount = BaFast,
                ClassSkillIds = new List<int>() { Skills.Bluff, Skills.Craft, Skills.KnowledgePlanes, Skills.Perception,
                    Skills.SenseMotive, Skills.Stealth },
                SelectableAnySkills = 4,
                BaseCharisma = 10,
                BaseIntelligence = 10,
                BaseWisdom = 10,
                BaseNaturalAcRatio = 1.1,
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision60, MonsterAbilities.CannotBeRaised },
                FeatIds = new List<int>() { Feats.SimpleWeaponProficiency, Feats.MartialWeaponProficiency }
            });

            Types.Add(new MonsterType()
            {
                Id = Plant,
                Name = "Plant",
                ImageGenerationName = "",
                Rarity = FeatureElement.UncommonFeature,
                BaseSKillAmount = 2,
                GoodFortitudeSave = true,
                GoodReflexSave = false,
                GoodWillSave = false,
                HitDieSides = 8,
                BaseAttackAmount = BaAverage,
                ClassSkillIds = new List<int>() { Skills.Perception, Skills.Stealth },
                BaseCharisma = 6,
                BaseIntelligence = 1,
                BaseWisdom = 10,
                BaseNaturalAcRatio = 1.1,
                AbilityIds = new List<int>() { MonsterAbilities.LowLightVision, MonsterAbilities.ImmunityMindAffecting,
                    MonsterAbilities.ImmunityPoison, MonsterAbilities.ImmunityStun, MonsterAbilities.ImmunityParalysis, MonsterAbilities.ImmunitySleep,
                    MonsterAbilities.ImmunityPolymorph }
            });

            Types.Add(new MonsterType()
            {
                Id = Undead,
                Name = "Undead",
                ImageGenerationName = "Monster",
                Rarity = FeatureElement.CommonFeature,
                BaseSKillAmount = 4,
                GoodFortitudeSave = false,
                GoodReflexSave = false,
                GoodWillSave = true,
                HitDieSides = 8,
                BaseAttackAmount = BaAverage,
                ClassSkillIds = new List<int>() { Skills.Climb, Skills.Disguise, Skills.Fly, Skills.KnowledgeArcana, Skills.KnowledgeReligion,
                    Skills.Perception, Skills.Spellcraft, Skills.Stealth },
                BaseCharisma = 10,
                BaseIntelligence = 10,
                BaseWisdom = 10,
                BaseNaturalAcRatio = 1.1,
                AbilityIds = new List<int>() { MonsterAbilities.NoConstitution, MonsterAbilities.CharismaReplacesCon, MonsterAbilities.UndeadTraits,
                    MonsterAbilities.ImmunityMindAffecting, MonsterAbilities.ImmunityBleed, MonsterAbilities.ImmunityDisease, MonsterAbilities.ImmunityDeathEffects,
                    MonsterAbilities.ImmunityStun, MonsterAbilities.ImmunityParalysis, MonsterAbilities.ImmunitySleep, MonsterAbilities.ImmunityPoison }
            });

            Types.Add(new MonsterType()
            {
                Id = Vermin,
                Name = "Vermin",
                ImageGenerationName = "Bug",
                Rarity = FeatureElement.CommonFeature,
                BaseSKillAmount = 2,
                GoodFortitudeSave = true,
                GoodReflexSave = false,
                GoodWillSave = false,
                HitDieSides = 8,
                BaseAttackAmount = BaAverage,
                ClassSkillIds = new List<int>(),
                BaseCharisma = 6,
                BaseIntelligence = 0,
                BaseWisdom = 10,
                BaseNaturalAcRatio = 1.1,
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision60, MonsterAbilities.MindlessInt, MonsterAbilities.ImmunityMindAffecting }
            });

            foreach (MonsterType monsterType in Types)
            {
                int powersCost = 0;
                int immuneResistCost = 0;
                if(monsterType.AbilityIds != null)
                {
                    foreach (int abilityId in monsterType.AbilityIds)
                    {
                        Ability ability = MonsterAbilities.GetInstance().GetAbility(abilityId);
                        if (ability != null)
                        {
                            if (ability.DisplayCategory == DisplayCategory.DefenseImmunity || ability.DisplayCategory == DisplayCategory.DefenseResistance)
                                immuneResistCost += ability.PointsCost;
                            else
                                powersCost += ability.PointsCost;
                        }
                    }
                    monsterType.BonusPowerPoints = (int)Math.Floor((double)powersCost * .5) + (int)Math.Floor((double)immuneResistCost * .8);
                }
            }
        }
        public MonsterType GetMonsterType(int id)
        {
            return Types.FirstOrDefault(a => a.Id == id);
        }
        public MonsterType GetRandomMonsterType(System.Random random)
        {
            int id = Table.GetRandom(random);
            return GetMonsterType(id);
        }
    }
}
