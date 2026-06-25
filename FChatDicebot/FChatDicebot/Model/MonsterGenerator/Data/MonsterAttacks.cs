using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonsterGenerator2_Net47.Models.Data
{
    public class MonsterAttacks
    {
        public const int Claws2 = 3001, Slam1 = 3002, Slam2 = 3003, Tentacle2 = 3004, Bite = 3005,
            TailSlap = 3006, Wings2 = 3007, Hoof2 = 3008, Sting = 3009, Talon2 = 3010, Gore = 3011,
            ColdTouch = 3021, FireTouch = 3012, ElectricityTouch = 3013, AcidTouch = 3014,
            NegativeEnergyTouch = 3015, PositiveEnergyTouch = 3016, Spine2 = 3017, Spike1 = 3018,
            Pincher2 = 3019, SonicTouch = 3020; //21 used;

        public const int Unarmed = 3101, Dagger = 3102, Club = 3103, MaceLight = 3104, MaceHeavy = 3105,
            GreatClub = 3106, Scimitar = 3107, Shortsword = 3108, Longsword = 3109, BastardSword = 3110,
            Greatsword = 3111, Battleaxe = 3112, Greataxe = 3113, Shortspear = 3114, Spear = 3115,
            Lance = 3116, Scythe = 3117, Javelin = 3118, Sling = 3119, Shortbow = 3120,
            Longbow = 3121, LightCrossbow= 3122, HeavyCrossbow = 3123, Dart = 3124, LightPick = 3125,
            HeavyPick = 3126, Trident = 3127, Warhammer = 3128, Glaive = 3129, Falchion = 3130,
            Kama = 3131, Sai = 132, Whip = 3133, TwoBladedSword = 3134, Quarterstaff = 3135,
            Longspear = 3136, Rapier = 3137;

        public List<Attack> Attacks;

        private RandomGenerationTable Table;

        private static MonsterAttacks Instance;
        public static MonsterAttacks GetInstance()
        {
            if (Instance == null)
                Instance = new MonsterAttacks();

            return Instance;
        }


        public MonsterAttacks()
        {
            PopulateAbilities();
            Table = new RandomGenerationTable();
            Table.FillRandomGenerationTable<Attack>(Attacks);
        }

        private void PopulateAbilities()
        {
            Attacks = new List<Attack>();
            #region natural weapons
            Attacks.Add(new Attack()
            {
                Id = Claws2,
                Name = "2x Claw",
                Explanation = "2 melee attacks with claws on limbs (1d4 slashing)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Slashing,
                FullAttackCount = 2,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped, MonsterBodyTypes.Winged,
                    MonsterBodyTypes.Quadruped, MonsterBodyTypes.Other }
            });
            //Claws2 = 1, Slam1 = 2, Slam2 = 3, Tentacle2 = 4, Bite = 5,
            //TailSlap = 6, Wings2 = 7, Hoof2 = 8, Sting = 9, Talon2 = 10, Gore = 11,
            //ColdTouch = 11, FireTouch = 12, ElectricityTouch = 13, AcidTouch = 14,
            //NegativeEnergyTouch = 15, PositiveEnergyTouch = 16, Spine2 = 17, Spike1 = 18,
            //Pincher2 = 19;
            Attacks.Add(new Attack()
            {
                Id = Slam2,
                Name = "2x Slam",
                Explanation = "2 melee attacks with slam from limbs (1d4 bludgeoning)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Bludgeoning,
                FullAttackCount = 2,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = Bite,
                Name = "Bite",
                Explanation = "1 melee attack with bite from jaws (1d6 bludgeon, piercing, and slashing)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.PiercingBludgeoningAndSlashing,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped, MonsterBodyTypes.Winged,
                    MonsterBodyTypes.Quadruped, MonsterBodyTypes.Other, MonsterBodyTypes.Serpentine,
                    MonsterBodyTypes.Finned, MonsterBodyTypes.Arachnid }
            });

            Attacks.Add(new Attack()
            {
                Id = Gore,
                Name = "Gore",
                Explanation = "1 melee attack with gore from horn (1d6 piercing)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Piercing,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Quadruped, MonsterBodyTypes.Other,
                    MonsterBodyTypes.Finned }
            });

            Attacks.Add(new Attack()
            {
                Id = Hoof2,
                Name = "2x Hoof",
                Explanation = "2 melee attacks with hooves from limbs (1d4 bludgeoning)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Bludgeoning,
                FullAttackCount = 2,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Quadruped, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = Tentacle2,
                Name = "2x Tentacle",
                Explanation = "2 melee attacks with tentacles from tendrils (1d4 bludgeoning)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Bludgeoning,
                FullAttackCount = 2,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Finned, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = Wings2,
                Name = "2x Wing",
                Explanation = "2 melee wing attacks with wings from flight limbs (1d4 bludgeoning)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Bludgeoning,
                FullAttackCount = 2,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Winged, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = Pincher2,
                Name = "2x Pincer",
                Explanation = "2 melee pincer attacks with pincers from limbs (1d4 bludgeoning)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Bludgeoning,
                FullAttackCount = 2,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Arachnid, MonsterBodyTypes.Biped, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = TailSlap,
                Name = "Tail Slap",
                Explanation = "1 melee tail slap attack from rear tail (1d6 bludgeoning)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Bludgeoning,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Quadruped, MonsterBodyTypes.Biped, MonsterBodyTypes.Finned,
                    MonsterBodyTypes.Winged, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = Sting,
                Name = "Sting",
                Explanation = "1 melee sting attack from rear tail (1d4 piercing)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Piercing,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Quadruped, MonsterBodyTypes.Biped, MonsterBodyTypes.Finned,
                    MonsterBodyTypes.Winged, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = Talon2,
                Name = "2x Talon",
                Explanation = "2 melee talon attacks from limbs (1d4 slashing)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Slashing,
                FullAttackCount = 2,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Winged, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = Spike1,
                Name = "Spike",
                Explanation = "1 ranged spike projectile attack (1d6 piercing)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Piercing,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 60,
                UsesDexterityHit = true,
                WeaponAttack = false,
                Rarity = FeatureElement.RareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Arachnid, MonsterBodyTypes.Biped, MonsterBodyTypes.Serpentine,
                    MonsterBodyTypes.Winged, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = Spine2,
                Name = "2x Spine",
                Explanation = "2 ranged spine projectile attacks (1d4 piercing)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Piercing,
                FullAttackCount = 2,
                DamageAttribute = BaseAttribute.Strength,
                Range = 60,
                UsesDexterityHit = true,
                WeaponAttack = false,
                Rarity = FeatureElement.RareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Arachnid, MonsterBodyTypes.Biped, MonsterBodyTypes.Serpentine,
                    MonsterBodyTypes.Winged, MonsterBodyTypes.Other }
            });
            #endregion
            #region touch attacks

            Attacks.Add(new Attack()
            {
                Id = ElectricityTouch,
                Name = "Touch (Electricity)",
                Explanation = "1 touch attack that deals energy damage (1d6 electricity)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                TouchAttack = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Electricity,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.NONE,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.RareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Finned, MonsterBodyTypes.Biped,
                    MonsterBodyTypes.Winged, MonsterBodyTypes.Quadruped, MonsterBodyTypes.Arachnid,
                    MonsterBodyTypes.Serpentine, MonsterBodyTypes.Other }
            });
            Attacks.Add(new Attack()
            {
                Id = FireTouch,
                Name = "Touch (Fire)",
                Explanation = "1 touch attack that deals energy damage (1d6 fire)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                TouchAttack = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Fire,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.NONE,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.RareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Finned, MonsterBodyTypes.Biped,
                    MonsterBodyTypes.Winged, MonsterBodyTypes.Quadruped, MonsterBodyTypes.Arachnid,
                    MonsterBodyTypes.Serpentine, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = ColdTouch,
                Name = "Touch (Cold)",
                Explanation = "1 touch attack that deals energy damage (1d6 cold)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                TouchAttack = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Cold,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.NONE,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.RareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Finned, MonsterBodyTypes.Biped,
                    MonsterBodyTypes.Winged, MonsterBodyTypes.Quadruped, MonsterBodyTypes.Arachnid,
                    MonsterBodyTypes.Serpentine, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = AcidTouch,
                Name = "Touch (Acid)",
                Explanation = "1 touch attack that deals energy damage (1d6 acid)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                TouchAttack = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Acid,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.NONE,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.RareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Finned, MonsterBodyTypes.Biped,
                    MonsterBodyTypes.Winged, MonsterBodyTypes.Quadruped, MonsterBodyTypes.Arachnid,
                    MonsterBodyTypes.Serpentine, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = SonicTouch,
                Name = "Touch (Sonic)",
                Explanation = "1 touch attack that deals energy damage (1d6 sonic)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                TouchAttack = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Sonic,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.NONE,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.SuperRareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Finned, MonsterBodyTypes.Biped,
                    MonsterBodyTypes.Winged, MonsterBodyTypes.Quadruped, MonsterBodyTypes.Arachnid,
                    MonsterBodyTypes.Serpentine, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = NegativeEnergyTouch,
                Name = "Touch (Negative Energy)",
                Explanation = "1 touch attack that deals energy damage (1d6 negative energy)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                TouchAttack = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.NegativeEnergy,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.NONE,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.SuperRareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Finned, MonsterBodyTypes.Biped,
                    MonsterBodyTypes.Winged, MonsterBodyTypes.Quadruped, MonsterBodyTypes.Arachnid,
                    MonsterBodyTypes.Serpentine, MonsterBodyTypes.Other }
            });

            Attacks.Add(new Attack()
            {
                Id = PositiveEnergyTouch,
                Name = "Touch (Positive Energy)",
                Explanation = "1 touch attack that deals energy damage (1d6 positive energy)",
                PointsCost = 3,
                NaturalAttack = true,
                LightWeapon = true,
                CapableIterativeAttacks = false,
                TouchAttack = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.PositiveEnergy,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.NONE,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.SuperRareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Finned, MonsterBodyTypes.Biped,
                    MonsterBodyTypes.Winged, MonsterBodyTypes.Quadruped, MonsterBodyTypes.Arachnid,
                    MonsterBodyTypes.Serpentine, MonsterBodyTypes.Other }
            });

            #endregion
            #region weapon attacks

            Attacks.Add(new Attack()
            {
                Id = Unarmed,
                Name = "Unarmed",
                Explanation = "Using unsuitable limbs for natural attacks with no training. (1d3 bludgeoning)",
                PointsCost = 3,
                NaturalAttack = false,
                LightWeapon = true,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Bludgeoning,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = false,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Dagger,
                Name = "Dagger",
                Explanation = "A small bladed handheld weapon. (1d4 piercing and slashing)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = true,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.PiercingAndSlashing,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Simple,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = MaceLight,
                Name = "Light Mace",
                Explanation = "A metal ball atop a shaft. (1d6 bludgeoning)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = true,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Bludgeoning,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Simple,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = MaceHeavy,
                Name = "Heavy Mace",
                Explanation = "A metal ball atop a shaft. (1d8 bludgeoning)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 8,
                DamageType = DamageType.Bludgeoning,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Simple,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = GreatClub,
                Name = "Great Mace",
                Explanation = "A large two handed club. (1d10 bludgeoning)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = true,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 10,
                DamageType = DamageType.Bludgeoning,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Simple,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Scimitar,
                Name = "Scimitar",
                Explanation = "A curved metal sword. (1d6 slashing, 18-20 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Slashing,
                CriticalRange = 3,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Shortsword,
                Name = "Shortsword",
                Explanation = "A short metal sword. (1d6 piercing, 19-20 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = true,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Piercing,
                CriticalRange = 2,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Longsword,
                Name = "Longsword",
                Explanation = "A long metal sword. (1d8 slashing, 19-20 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 8,
                DamageType = DamageType.Slashing,
                CriticalRange = 2,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = BastardSword,
                Name = "Bastard Sword",
                Explanation = "A hand-and-a-half long metal sword. (1d10 slashing, 19-20 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 10,
                DamageType = DamageType.Slashing,
                CriticalRange = 2,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Exotic,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Greatsword,
                Name = "Greatsword",
                Explanation = "A two handed long metal sword. (2d6 slashing, 19-20 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 2,
                DamageDieSides = 6,
                DamageType = DamageType.Slashing,
                CriticalRange = 2,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Battleaxe,
                Name = "Battleaxe",
                Explanation = "A one handed metal axe. (1d8 slashing, x3 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 8,
                DamageType = DamageType.Slashing,
                CriticalRange = 1,
                CriticalMultiplier = 3,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Greataxe,
                Name = "Greataxe",
                Explanation = "A two handed metal axe. (1d12 slashing, x3 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 12,
                DamageType = DamageType.Slashing,
                CriticalRange = 1,
                CriticalMultiplier = 3,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Shortspear,
                Name = "Shortspear",
                Explanation = "A one handed spear. (1d6 piercing, x2 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Piercing,
                CriticalRange = 1,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Simple,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Spear,
                Name = "Spear",
                Explanation = "A two handed spear. (1d8 piercing, x3 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = true,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 8,
                DamageType = DamageType.Piercing,
                CriticalRange = 1,
                CriticalMultiplier = 3,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Simple,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Longspear,
                Name = "Longspear",
                Explanation = "A two handed spear with reach. (1d8 piercing, x3 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = true,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 8,
                DamageType = DamageType.Piercing,
                CriticalRange = 1,
                CriticalMultiplier = 3,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Long,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Simple,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Lance,
                Name = "Lance",
                Explanation = "A two handed spear with reach. (1d8 piercing, x3 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = true,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 8,
                DamageType = DamageType.Piercing,
                CriticalRange = 1,
                CriticalMultiplier = 3,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Long,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = LightPick,
                Name = "LightPick",
                Explanation = "A spiked metal weapon. (1d4 piercing, x4 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = true,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Piercing,
                CriticalRange = 1,
                CriticalMultiplier = 4,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.UncommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = HeavyPick,
                Name = "Heavy Pick",
                Explanation = "A spiked metal weapon. (1d6 piercing, x4 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Piercing,
                CriticalRange = 1,
                CriticalMultiplier = 4,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.UncommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Trident,
                Name = "Trident",
                Explanation = "A spear with three tips. (1d8 piercing)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 8,
                DamageType = DamageType.Piercing,
                CriticalRange = 1,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.UncommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Warhammer,
                Name = "Warhammer",
                Explanation = "A metal club weapon. (1d8 bludgeoning, x3 critical)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 8,
                DamageType = DamageType.Bludgeoning,
                CriticalRange = 1,
                CriticalMultiplier = 3,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.UncommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Glaive,
                Name = "Glaive",
                Explanation = "A metal bladed polearm with reach. (1d10 slashing, x3 critical)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 10,
                DamageType = DamageType.Slashing,
                CriticalRange = 1,
                CriticalMultiplier = 3,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Long,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.UncommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Falchion,
                Name = "Falchion",
                Explanation = "A curved two handed sword. (1d10 slashing, 18-20 critical)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 2,
                DamageDieSides = 4,
                DamageType = DamageType.Slashing,
                CriticalRange = 3,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.UncommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Kama,
                Name = "Kama",
                Explanation = "A one handed hooked metal weapon. (1d6 slashing)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = true,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Slashing,
                CriticalRange = 1,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Exotic,
                Rarity = FeatureElement.SuperRareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Sai,
                Name = "Sai",
                Explanation = "A one handed pronged metal weapon. (1d4 slashing)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = true,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Bludgeoning,
                CriticalRange = 1,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Exotic,
                Rarity = FeatureElement.SuperRareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Whip,
                Name = "Whip",
                Explanation = "A one handed pronged metal weapon. (1d3 slashing)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                SpecialFinesseable = true,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 3,
                DamageType = DamageType.Slashing,
                CriticalRange = 1,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.SuperLong,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Exotic,
                Rarity = FeatureElement.SuperRareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Rapier,
                Name = "Rapier",
                Explanation = "A one handed thin sword. (1d6 piercing, 18-20 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                SpecialFinesseable = true,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Piercing,
                CriticalRange = 3,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = TwoBladedSword,
                Name = "Two-Bladed Sword",
                Explanation = "A sword with a handle in the middle and 2 bladed sides. (1d8 slashing, 19-20 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                SpecialDualWeapon = true,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 8,
                DamageType = DamageType.Slashing,
                CriticalRange = 2,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Exotic,
                Rarity = FeatureElement.RareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Quarterstaff,
                Name = "Quarterstaff",
                Explanation = "A long wooden staff used for fighting with 1 or 2 sides. (1d6 bludgeoning, 19-20 crit)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                SpecialDualWeapon = true,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Bludgeoning,
                CriticalRange = 1,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 0,
                UsesDexterityHit = false,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Simple,
                Rarity = FeatureElement.RareFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Javelin,
                Name = "Javelin",
                Explanation = "A wooden shaft with a metal spearhead made for throwing. (1d6 piercing)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Piercing,
                CriticalRange = 1,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 30,
                UsesDexterityHit = true,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Simple,
                Rarity = FeatureElement.UncommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Sling,
                Name = "Sling",
                Explanation = "A leather strap used to throw rocks. (1d4 bludgeoning)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Bludgeoning,
                CriticalRange = 1,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 50,
                UsesDexterityHit = true,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Simple,
                Rarity = FeatureElement.UncommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Shortbow,
                Name = "Composite Shortbow",
                Explanation = "A curved wooden weapon with a string that shoots arrows. (1d6 piercing, x3 critical)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 6,
                DamageType = DamageType.Piercing,
                CriticalRange = 1,
                CriticalMultiplier = 3,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 70,
                UsesDexterityHit = true,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.UncommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Longbow,
                Name = "Composite Longbow",
                Explanation = "A curved wooden weapon with a string that shoots arrows. (1d8 piercing, x3 critical)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 8,
                DamageType = DamageType.Piercing,
                CriticalRange = 1,
                CriticalMultiplier = 3,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 110,
                UsesDexterityHit = true,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Martial,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = LightCrossbow,
                Name = "Light Crossbow",
                Explanation = "A handheld metal spring that fires bolts. (1d8 piercing, 19-20 critical)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 8,
                DamageType = DamageType.Piercing,
                CriticalRange = 1,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.NONE,
                Reach = AttackReach.Normal,
                Range = 80,
                UsesDexterityHit = true,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Simple,
                Rarity = FeatureElement.UncommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = HeavyCrossbow,
                Name = "Heavy Crossbow",
                Explanation = "A handheld metal spring that fires bolts. (1d10 piercing, 19-20 critical)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 10,
                DamageType = DamageType.Piercing,
                CriticalRange = 1,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.NONE,
                Reach = AttackReach.Normal,
                Range = 120,
                UsesDexterityHit = true,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Simple,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });

            Attacks.Add(new Attack()
            {
                Id = Dart,
                Name = "Dart",
                Explanation = "A small sharp object thrown at a target. (1d4 piercing)",
                PointsCost = 4,
                NaturalAttack = false,
                LightWeapon = false,
                TwoHandedWeapon = false,
                CapableIterativeAttacks = true,
                DamageDieAmount = 1,
                DamageDieSides = 4,
                DamageType = DamageType.Piercing,
                CriticalRange = 1,
                CriticalMultiplier = 2,
                FullAttackCount = 1,
                DamageAttribute = BaseAttribute.Strength,
                Reach = AttackReach.Normal,
                Range = 20,
                UsesDexterityHit = true,
                WeaponAttack = true,
                Proficiency = WeaponProficiency.Simple,
                Rarity = FeatureElement.UncommonFeature,
                RelevantBodyTypes = new List<int> { MonsterBodyTypes.Biped }
            });
            #endregion
        }
        public Attack GetAttack(int id)
        {
            return Attacks.FirstOrDefault(a => a.Id == id);
        }
        public Attack GetRandomAttack(System.Random random)
        {
            int id = Table.GetRandom(random);
            return GetAttack(id);
        }
        public Attack GetRandomAttackForType(System.Random random, BodyType bType)
        {
            RandomGenerationTable genT = new RandomGenerationTable();
            genT.FillRandomGenerationTable<Attack>(Attacks.Where(a => a.RelevantBodyTypes.Contains(bType.Id)).ToList());
            int id = genT.GetRandom(random);
            return GetAttack(id);
        }
    }
}
