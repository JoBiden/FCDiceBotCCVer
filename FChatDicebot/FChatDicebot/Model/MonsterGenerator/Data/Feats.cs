using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonsterGenerator2_Net47.Models.Data
{
    public class Feats
    {
        //PFRPG Core
        public const int Acrobatic = 1, AcrobaticSteps = 2, AgileManeuvers = 3, Alertness = 4, AlignmentChannel = 5,
            AnimalAffinity = 6, ArcaneArmorMastery = 7, ArcaneArmorTraining = 8, ArcaneStrike = 9, HeavyArmorProficiency = 10,
            LightArmorProficiency = 11, MediumArmorProficiency = 12, Athletic = 13, AugmentSummoning = 14, BleedingCritical = 15,
            BlindFight = 16, BlindingCritical = 17, BrewPotion = 18, CatchOffGuard = 19, ChannelSmite = 20,
            Cleave = 21, CombatCasting = 22, CombatExpertise = 23, CombatReflexes = 24, CommandUndead = 25,
            CraftMagicArmsAndArmor = 26, CraftRod = 27, CraftWand = 28, CraftStaff = 29, CraftWondrousItem = 30,
            CriticalFocus = 31, CriticalMastery = 32, DazzlingDisplay = 33, DeadlyAim = 34, DeadlyStroke = 35,
            DeafeningCritical = 36, Deceitful = 37, DefensiveCombatTraining = 38, DeflectArrows = 39, DeftHands = 40,
            Diehard = 41, Disruptive = 42, Dodge = 43, DoubleSlice = 44, ElementalChannel = 45,
            EmpowerSpell = 46, Endurance = 47, EnlargeSpell = 48, EschewMaterials = 49, ExhaustingCritical = 50,
            ExoticWeaponProficiency = 51, ExtendSpell = 52, ExtraChannel = 53, ExtraKi = 54, ExtraLayOnHands = 55,
            ExtraMercy = 56, ExtraPerformance = 57, ExtraRage = 58, FarShot = 59, Fleet = 60,
            ForgeRing = 61, GorgonsFist = 62, GreatCleave = 63, GreatFortitude = 64, GreaterBullRush = 65,
            GreaterDisarm = 66, GreaterFeint = 67, GreaterGrapple = 68, GreaterOverrun = 69, GreaterPenetratingStrike = 70,
            GreaterShieldFocus = 71, GreaterSpellFocus = 72, GreaterSpellPenetration = 73, GreaterSunder = 74, GreaterTrip = 75,
            GreaterTwoWeaponFighting = 76, GreaterVitalStrike = 77, GreaterWeaponFocus = 78, GreaterWeaponSpecialization = 79, HeightenSpell = 80,
            ImprovedBullRush = 81, ImprovedChannel = 82, ImprovedCounterspell = 83, ImprovedCritical = 84, ImprovedDisarm = 85,
            ImprovedFamiliar = 86, ImprovedFeint = 87, ImprovedGrapple = 88, ImprovedGreatFortitude = 89, ImprovedInitiative = 90,
            ImprovedIronWill = 91, ImprovedLightningReflexes = 92, ImprovedOverrun = 93, ImprovedPreciseShot = 94, ImprovedShieldBash = 95,
            ImprovedSunder = 96, ImprovedTrip = 97, ImprovedTwoWeaponFighting = 98, ImprovedUnarmedStrike = 99, ImprovedVitalStrike = 100,
            ImprovisedWeaponMastery = 101, IntimidatingProwess = 102, IronWill = 103, Leadership = 104, LightningReflexes = 105,
            LightningStance = 106, Lunge = 107, MagicalAptitude = 108, Manyshot = 109, MartialWeaponProficiency = 110,
            MasterCraftsman = 111, MaximizeSpell = 112, MedusasWrath = 113, Mobility = 114, MountedArchery = 115,
            MountedCombat = 116, NaturalSpell = 117, NimbleMoves = 118, PenetratingStrike = 119, Persuasive = 120,
            PinpointTargeting = 121, PointBlankShot = 122, PowerAttack = 123, PreciseShot = 124, QuickDraw = 125,
            QuickenSpell = 126, RapidShot = 127, RideByAttack = 128, Run = 129, ScorpionStyle = 130,
            ScribeScroll = 131, SelectiveChanneling = 132, SelfSufficient = 133, ShatterDefenses = 134, ShieldFocus = 135,
            ShieldProficiency = 136, ShieldSlam = 137, ShotontheRun = 138, SickeningCritical = 139, SilentSpell = 140,
            SimpleWeaponProficiency = 141, SkillFocus = 142, SnatchArrows = 143, SpellFocus = 144, SpellMastery = 145,
            SpellPenetration = 146, Spellbreaker = 147, SpiritedCharge = 148, SpringAttack = 149, StaggeringCritical = 150,
            StandStill = 151, Stealthy = 152, StepUp = 153, StillSpell = 154, StrikeBack = 155,
            StunningCritical = 156, StunningFist = 157, ThrowAnything = 158, TiringCritical = 159, Toughness = 160,
            TowerShieldProficiency = 161, Trample = 162, TurnUndead = 163, TwoWeaponDefense = 164, TwoWeaponFighting = 165,
            TwoWeaponRend = 166, Unseat = 167, VitalStrike = 168, WeaponFinesse = 169, WeaponFocus = 170,
            WeaponSpecialization = 171, WhirlwindAttack = 172, WidenSpell = 173, WindStance = 174;

        //Bestiary
        public const int AbilityFocus = 501, AwesomeBlow = 502, CraftConstruct = 503, EmpowerSpellLikeAbility = 504,
            FlybyAttack = 505, Hover = 506, ImprovedNaturalArmor = 507, ImprovedNaturalAttack = 508,
            Multiattack = 509, QuickenSpellLikeAbility = 510, Snatch = 511, Wingover = 512;

        //Advanced Player Handbook
        public List<Feat> AllFeats;

        private RandomGenerationTable Table;

        private static Feats Instance;
        public static Feats GetInstance()
        {
            if (Instance == null)
                Instance = new Feats();

            return Instance;
        }

        public Feats()
        {
            PopulateFeats();
            Table = new RandomGenerationTable();
            Table.FillRandomGenerationTable<Feat>(AllFeats);
        }

        private void PopulateFeats()
        {
            #region PFMAIN
            AllFeats = new List<Feat>();
            AllFeats.Add(new Feat()
            {
                Id = Acrobatic,
                Name = "Acrobatic",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                AppliedSkillId = Skills.Acrobatics,
                AppliedSkillId2 = Skills.Fly,
                Explanation = "You get a +2 bonus on all Acrobatics and Fly skill checks. If you have 10 or more ranks in one of these skills, the bonus increases to +4 for that skill.",
                AppliedBonus = 2
            });
            AllFeats.Add(new Feat()
            {
                Id = AcrobaticSteps,
                Name = "Acrobatic Steps",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.UncommonFeature,
                RequiredDex = 15,
                Explanation = "Whenever you move, you may move through up to 15 feet of difficult terrain each round as if it were normal terrain. The effects of this feat stack with those provided by Nimble Moves (allowing you to move normally through a total of 20 feet of difficult terrain each round).",
                RequiredFeatIds = new List<int>() { NimbleMoves }
            });
            AllFeats.Add(new Feat()
            {
                Id = AgileManeuvers,
                Name = "Agile Maneuvers",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "You add your Dexterity bonus to your base attack bonus and size bonus when determining your Combat Maneuver Bonus (see Chapter 8) instead of your Strength bonus.",
            });
            AllFeats.Add(new Feat()
            {
                Id = Alertness,
                Name = "Alertness",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                AppliedSkillId = Skills.Perception,
                AppliedSkillId2 = Skills.SenseMotive,
                Explanation = "You get a +2 bonus on Perception and Sense Motive skill checks. If you have 10 or more ranks in one of these skills, the bonus increases to +4 for that skill.",
                AppliedBonus = 2
            });
            AllFeats.Add(new Feat()
            {
                Id = AlignmentChannel,
                Name = "Alignment Channel",
                RequiredClassFeature = RequiredClassFeature.ChannelEnergy,
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.RareFeature,
                Explanation = "Instead of its normal effect, you can choose to have your ability to channel energy heal or harm outsiders of the chosen alignment subtype. You must make this choice each time you channel energy. If you choose to heal or harm creatures of the chosen alignment subtype, your channel energy has no effect on other creatures. The amount of damage healed or dealt and the DC to halve the damage is otherwise unchanged.",
            });
            AllFeats.Add(new Feat()
            {
                Id = AnimalAffinity,
                Name = "Animal Affinity",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.RareFeature,
                AppliedSkillId = Skills.HandleAnimal,
                AppliedSkillId2 = Skills.Ride,
                Explanation = "You get a +2 bonus on all Handle Animal and Ride skill checks. If you have 10 or more ranks in one of these skills, the bonus increases to +4 for that skill.",
                AppliedBonus = 2
            });
            AllFeats.Add(new Feat()
            {
                Id = ArcaneArmorMastery,
                Name = "Arcane Armor Mastery",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.RareFeature,
                Explanation = "As a swift action, reduce the arcane spell failure chance due to the armor you are wearing by 20% for any spells you cast this round. This bonus replaces, and does not stack with, the bonus granted by Arcane Armor Training.",
                RequiredFeatIds = new List<int>() { ArcaneArmorTraining, MediumArmorProficiency }
            });
            AllFeats.Add(new Feat()
            {
                Id = ArcaneArmorTraining,
                Name = "Arcane Armor Training",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.RareFeature,
                Explanation = "As a swift action, reduce the arcane spell failure chance due to the armor you are wearing by 10% for any spells you cast this round.",
                RequiredFeatIds = new List<int>() { LightArmorProficiency }
            });
            AllFeats.Add(new Feat()
            {
                Id = ArcaneStrike,
                Name = "Arcane Strike",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.RareFeature,
                RequiredSpellcasterLevel = 1,
                Explanation = "As a swift action, you can imbue your weapons with a fraction of your power. For 1 round, your weapons deal +1 damage and are treated as magic for the purpose of overcoming damage reduction. For every five caster levels you possess, this bonus increases by +1, to a maximum of +5 at 20th level."
            });
            AllFeats.Add(new Feat()
            {
                Id = HeavyArmorProficiency,
                Name = "Heavy Armor Proficiency",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "When you wear a type of armor with which you are proficient, the armor check penalty for that armor applies only to Dexterity- and Strength-based skill checks.",
                RequiredFeatIds = new List<int>() { LightArmorProficiency, MediumArmorProficiency }
            });
            AllFeats.Add(new Feat()
            {
                Id = MediumArmorProficiency,
                Name = "Medium Armor Proficiency",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "When you wear a type of armor with which you are proficient, the armor check penalty for that armor applies only to Dexterity- and Strength-based skill checks.",
                RequiredFeatIds = new List<int>() { LightArmorProficiency }
            });
            AllFeats.Add(new Feat()
            {
                Id = LightArmorProficiency,
                Name = "Light Armor Proficiency",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "When you wear a type of armor with which you are proficient, the armor check penalty for that armor applies only to Dexterity- and Strength-based skill checks."
            });
            AllFeats.Add(new Feat()
            {
                Id = Athletic,
                Name = "Athletic",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                AppliedSkillId = Skills.Climb,
                AppliedSkillId2 = Skills.Swim,
                Explanation = "You get a +2 bonus on Climb and Swim skill checks. If you have 10 or more ranks in one of these skills, the bonus increases to +4 for that skill.",
                AppliedBonus = 2
            });
            AllFeats.Add(new Feat()
            {
                Id = AugmentSummoning,
                Name = "Augment Summoning",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.SuperRareFeature,
                Explanation = "Each creature you conjure with any summon spell gains a +4 enhancement bonus to Strength and Constitution for the duration of the spell that summoned it.",
                RequiredFeatIds = new List<int>() { SpellFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = BleedingCritical,
                Name = "Bleeding Critical",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                RequiredBaseAttackBonus = 11,
                Explanation = "Whenever you score a critical hit with a slashing or piercing weapon, your opponent takes 2d6 points of bleed damage (see Appendix 2) each round on his turn, in addition to the damage dealt by the critical hit. Bleed damage can be stopped by a DC 15 Heal skill check or through any magical healing. The effects of this feat stack.",
                RequiredFeatIds = new List<int>() { CriticalFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = BlindFight,
                Name = "Blind-Fight",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "In melee, every time you miss because of concealment (see Chapter 8), you can reroll your miss chance percentile roll one time to see if you actually hit. An invisible attacker gets no advantages related to hitting you in melee. That is, you don't lose your Dexterity bonus to Armor Class, and the attacker doesn't get the usual +2 bonus for being invisible. The invisible attacker's bonuses do still apply for ranged attacks, however. You do not need to make Acrobatics skill checks to move at full speed while blinded."
            });
            AllFeats.Add(new Feat()
            {
                Id = BlindingCritical,
                Name = "Blinding Critical",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                RequiredBaseAttackBonus = 15,
                Explanation = "In melee, every time you miss because of concealment (see Chapter 8), you can reroll your miss chance percentile roll one time to see if you actually hit. An invisible attacker gets no advantages related to hitting you in melee. That is, you don't lose your Dexterity bonus to Armor Class, and the attacker doesn't get the usual +2 bonus for being invisible. The invisible attacker's bonuses do still apply for ranged attacks, however. You do not need to make Acrobatics skill checks to move at full speed while blinded.",
                RequiredFeatIds = new List<int>() { CriticalFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = BrewPotion,
                Name = "Brew Potion",
                FeatCategory = FeatCategory.ItemCreation,
                Rarity = FeatureElement.RareFeature,
                RequiredSpellcasterLevel = 3,
                Explanation = "You can create a potion of any 3rd-level or lower spell that you know and that targets one or more creatures or objects. Brewing a potion takes 2 hours if its base price is 250 gp or less, otherwise brewing a potion takes 1 day for each 1,000 gp in its base price. When you create a potion, you set the caster level, which must be sufficient to cast the spell in question and no higher than your own level. To brew a potion, you must use up raw materials costing one half this base price. See the magic item creation rules in Chapter 15 for more information. When you create a potion, you make any choices that you would normally make when casting the spell. Whoever drinks the potion is the target of the spell."
            });
            AllFeats.Add(new Feat()
            {
                Id = CatchOffGuard,
                Name = "Catch Off-Guard",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.RareFeature,
                Explanation = "You do not suffer any penalties for using an improvised melee weapon. Unarmed opponents are flat-footed against any attacks you make with an improvised melee weapon."
            });
            AllFeats.Add(new Feat()
            {
                Id = ChannelSmite,
                Name = "Channel Smite",
                RequiredClassFeature = RequiredClassFeature.ChannelEnergy,
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.SuperRareFeature,
                Explanation = "Before you make a melee attack roll, you can choose to spend one use of your channel energy ability as a swift action. If you channel positive energy and you hit an undead creature, that creature takes an amount of additional damage equal to the damage dealt by your channel positive energy ability. If you channel negative energy and you hit a living creature, that creature takes an amount of additional damage equal to the damage dealt by your channel negative energy ability. Your target can make a Will save, as normal, to halve this additional damage. If your attack misses, the channel energy ability is still expended with no effect."
            });
            AllFeats.Add(new Feat()
            {
                Id = Cleave,
                Name = "Cleave",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                RequiredStr = 13,
                RequiredBaseAttackBonus = 1,
                Explanation = "As a standard action, you can make a single attack at your full base attack bonus against a foe within reach. If you hit, you deal damage normally and can make an additional attack (using your full base attack bonus) against a foe that is adjacent to the first and also within reach. You can only make one additional attack per round with this feat. When you use this feat, you take a -2 penalty to your Armor Class until your next turn.",
                RequiredFeatIds = new List<int>() { PowerAttack }
            });
            AllFeats.Add(new Feat()
            {
                Id = CombatCasting,
                Name = "Combat Casting",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +4 bonus on concentration checks made to cast a spell or use a spell-like ability when casting on the defensive or while grappled."
            });
            AllFeats.Add(new Feat()
            {
                Id = CombatExpertise,
                Name = "Combat Expertise",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                RequiredInt = 13,
                Explanation = "You can choose to take a -1 penalty on melee attack rolls and combat maneuver checks to gain a +1 dodge bonus to your Armor Class. When your base attack bonus reaches +4, and every +4 thereafter, the penalty increases by -1 and the dodge bonus increases by +1. You can only choose to use this feat when you declare that you are making an attack or a full-attack action with a melee weapon. The effects of this feat last until your next turn."
            });
            AllFeats.Add(new Feat()
            {
                Id = CombatReflexes,
                Name = "Combat Reflexes",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You may make a number of additional attacks of opportunity per round equal to your Dexterity bonus. With this feat, you may also make attacks of opportunity while flat-footed."
            });
            AllFeats.Add(new Feat()
            {
                Id = CommandUndead,
                Name = "Command Undead",
                RequiredClassFeature = RequiredClassFeature.ChannelEnergy,
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.SuperRareFeature,
                RequiredSpellcasterLevel = 1,
                Explanation = "As a standard action, you can use one of your uses of channel negative energy to enslave undead within 30 feet. Undead receive a Will save to negate the effect. The DC for this Will save is equal to 10 + 1/2 your cleric level + your Charisma modifier. Undead that fail their saves fall under your control, obeying your commands to the best of their ability, as if under the effects of control undead. Intelligent undead receive a new saving throw each day to resist your command. You can control any number of undead, so long as their total Hit Dice do not exceed your cleric level. If you use channel energy in this way, it has no other effect (it does not heal or harm nearby creatures). If an undead creature is under the control of another creature, you must make an opposed Charisma check whenever your orders conflict."
            });
            AllFeats.Add(new Feat()
            {
                Id = CraftMagicArmsAndArmor,
                Name = "Craft Magic Arms And Armor",
                FeatCategory = FeatCategory.ItemCreation,
                Rarity = FeatureElement.SuperRareFeature,
                RequiredSpellcasterLevel = 5,
                Explanation = "You can create magic weapons, armor, or shields. Enhancing a weapon, suit of armor, or shield takes 1 day for each 1,000 gp in the price of its magical features. To enhance a weapon, suit of armor, or shield, you must use up raw materials costing half of this total price. See the magic item creation rules in Chapter 15 for more information. The weapon, armor, or shield to be enhanced must be a masterwork item that you provide. Its cost is not included in the above cost. You can also mend a broken or destroyed magic weapon, suit of armor, or shield if it is one that you could make. Doing so costs half the raw materials and half the time it would take to craft that item in the first place."
            });
            AllFeats.Add(new Feat()
            {
                Id = CraftRod,
                Name = "Craft Rod",
                FeatCategory = FeatCategory.ItemCreation,
                Rarity = FeatureElement.SuperRareFeature,
                RequiredSpellcasterLevel = 9,
                Explanation = "You can create magic rods. Crafting a rod takes 1 day for each 1,000 gp in its base price. To craft a rod, you must use up raw materials costing half of its base price. See the magic item creation rules in Chapter 15 for more information."
            });
            AllFeats.Add(new Feat()
            {
                Id = CraftWand,
                Name = "Craft Wand",
                FeatCategory = FeatCategory.ItemCreation,
                Rarity = FeatureElement.SuperRareFeature,
                RequiredSpellcasterLevel = 5,
                Explanation = "You can create a wand of any 4th-level or lower spell that you know. Crafting a wand takes 1 day for each 1,000 gp in its base price. To craft a wand, you must use up raw materials costing half of this base price. A newly created wand has 50 charges. See the magic item creation rules in Chapter 15 for more information."
            });
            AllFeats.Add(new Feat()
            {
                Id = CraftStaff,
                Name = "Craft Staff",
                FeatCategory = FeatCategory.ItemCreation,
                Rarity = FeatureElement.SuperRareFeature,
                RequiredSpellcasterLevel = 11,
                Explanation = "You can create any staff whose prerequisites you meet. Crafting a staff takes 1 day for each 1,000 gp in its base price. To craft a staff, you must use up raw materials costing half of its base price. A newly created staff has 10 charges. See the magic item creation rules in Chapter 15 for more information."
            });
            AllFeats.Add(new Feat()
            {
                Id = CraftWondrousItem,
                Name = "Craft Wondrous Item",
                FeatCategory = FeatCategory.ItemCreation,
                Rarity = FeatureElement.SuperRareFeature,
                RequiredSpellcasterLevel = 3,
                Explanation = "You can create a wide variety of magic wondrous items. Crafting a wondrous item takes 1 day for each 1,000 gp in its price. To create a wondrous item, you must use up raw materials costing half of its base price. See the magic item creation rules in Chapter 15 for more information. You can also mend a broken or destroyed wondrous item if it is one that you could make. Doing so costs half the raw materials and half the time it would take to craft that item."
            });
            AllFeats.Add(new Feat()
            {
                Id = CriticalFocus,
                Name = "CriticalFocus",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                RequiredBaseAttackBonus = 9,
                Explanation = "You receive a +4 circumstance bonus on attack rolls made to confirm critical hits."
            });
            AllFeats.Add(new Feat()
            {
                Id = CriticalMastery,
                Name = "CriticalMastery",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                RequiredBaseAttackBonus = 14,
                Explanation = "When you score a critical hit, you can apply the effects of two critical feats in addition to the damage dealt.",
                RequiredFeatIds = new List<int>() { CriticalFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = DazzlingDisplay,
                Name = "Dazzling Display",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "While wielding the weapon in which you have Weapon Focus, you can perform a bewildering show of prowess as a full-round action. Make an Intimidate check to demoralize all foes within 30 feet who can see your display.",
                RequiredFeatIds = new List<int>() { WeaponFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = DeadlyAim,
                Name = "Deadly Aim",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                RequiredBaseAttackBonus = 1,
                RequiredDex = 13,
                Explanation = "You can choose to take a -1 penalty on all ranged attack rolls to gain a +2 bonus on all ranged damage rolls. When your base attack bonus reaches +4, and every +4 thereafter, the penalty increases by -1 and the bonus to damage increases by +2. You must choose to use this feat before making an attack roll and its effects last until your next turn. The bonus damage does not apply to touch attacks or effects that do not deal hit point damage."
            });
            AllFeats.Add(new Feat()
            {
                Id = DeadlyStroke,
                Name = "Deadly Stroke",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                RequiredBaseAttackBonus = 11,
                Explanation = "As a standard action, make a single attack with the weapon for which you have Greater Weapon Focus against a stunned or flat-footed opponent. If you hit, you deal double the normal damage and the target takes 1 point of Constitution bleed (see Appendix 2). The additional damage and bleed is not multiplied on a critical hit.",
                RequiredFeatIds = new List<int>() { DazzlingDisplay, GreaterWeaponFocus, ShatterDefenses, WeaponFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = DeafeningCritical,
                Name = "Deafening Critical",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                RequiredBaseAttackBonus = 13,
                Explanation = "Whenever you score a critical hit against an opponent, the victim is permanently deafened. A successful Fortitude save reduces the deafness to 1 round. The DC of this Fortitude save is equal to 10 + your base attack bonus. This feat has no effect on deaf creatures. This deafness can be cured by heal, regeneration, remove deafness, or a similar ability.",
                RequiredFeatIds = new List<int>() { CriticalFocus }
            });
            //TODO: +140ish more feats
            AllFeats.Add(new Feat()
            {
                Id = Deceitful,
                Name = "Deceitful",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                AppliedSkillId = Skills.Bluff,
                AppliedSkillId2 = Skills.Disguise,
                Explanation = "You get a +2 bonus on all Bluff and Disguise skill checks. If you have 10 or more ranks in one of these skills, the bonus increases to +4 for that skill.",
                AppliedBonus = 2
            });
            AllFeats.Add(new Feat()
            {
                Id = DefensiveCombatTraining,
                Name = "Defensive Combat Training",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You treat your total Hit Dice as your base attack bonus when calculating your Combat Maneuver Defense (see Chapter 8)."
            });
            AllFeats.Add(new Feat()
            {
                Id = DeflectArrows,
                Name = "Deflect Arrows",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                RequiredDex = 13,
                Explanation = "You must have at least one hand free (holding nothing) to use this feat. Once per round when you would normally be hit with an attack from a ranged weapon, you may def lect it so that you take no damage from it. You must be aware of the attack and not flat-footed. Attempting to def lect a ranged attack doesn't count as an action. Unusually massive ranged weapons (such as boulders or ballista bolts) and ranged attacks generated by natural attacks or spell effects can't be def lected.",
                RequiredFeatIds = new List<int>() { ImprovedUnarmedStrike }
            });
            AllFeats.Add(new Feat()
            {
                Id = DeftHands,
                Name = "Deft Hands",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                AppliedSkillId = Skills.DisableDevice,
                AppliedSkillId2 = Skills.SleightOfHand,
                Explanation = "You get a +2 bonus on Disable Device and Sleight of Hand skill checks. If you have 10 or more ranks in one of these skills, the bonus increases to +4 for that skill.",
                AppliedBonus = 2
            });
            AllFeats.Add(new Feat()
            {
                Id = Diehard,
                Name = "Diehard",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "When your hit point total is below 0, but you are not dead, you automatically stabilize. You do not need to make a Constitution check each round to avoid losing additional hit points. You may choose to act as if you were disabled, rather than dying. You must make this decision as soon as you are reduced to negative hit points (even if it isn't your turn). If you do not choose to act as if you were disabled, you immediately fall unconscious. When using this feat, you are staggered. You can take a move action without further injuring yourself, but if you perform any standard action (or any other action deemed as strenuous, including some swift actions, such as casting a quickened spell) you take 1 point of damage after completing the act. If your negative hit points are equal to or greater than your Constitution score, you immediately die.",
                RequiredFeatIds = new List<int>() { Endurance }
            });
            AllFeats.Add(new Feat()
            {
                Id = Disruptive,
                Name = "Disruptive",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                RequiredBaseAttackBonus = 6,
                Explanation = "The DC to cast spells defensively increases by +4 for all enemies that are within your threatened area. This increase to casting spells defensively only applies if you are aware of the enemy's location and are capable of taking an attack of opportunity. If you can only take one attack of opportunity per round and have already used that attack, this increase does not apply."
            });
            AllFeats.Add(new Feat()
            {
                Id = Dodge,
                Name = "Dodge",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                RequiredDex = 13,
                Explanation = "You gain a +1 dodge bonus to your AC. A condition that makes you lose your Dex bonus to AC also makes you lose the benefits of this feat."
            });
            AllFeats.Add(new Feat()
            {
                Id = DoubleSlice,
                Name = "Double Slice",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                RequiredDex = 15,
                Explanation = "Add your Strength bonus to damage rolls made with your off-hand weapon.",
                RequiredFeatIds = new List<int>() { TwoWeaponFighting }
            });
            /////////////////////////////
            ///
            AllFeats.Add(new Feat()
            {
                Id = ElementalChannel,
                Name = "Elemental Channel",
                RequiredClassFeature = RequiredClassFeature.ChannelEnergy,
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.SuperRareFeature,
                Explanation = "Instead of its normal effect, you can choose to have your ability to channel energy heal or harm outsiders of your chosen elemental subtype. You must make this choice each time you channel energy. If you choose to heal or harm creatures of your elemental subtype, your channel energy has no affect on other creatures. The amount of damage healed or dealt and the DC to halve the damage is otherwise unchanged."
            });//
            AllFeats.Add(new Feat()
            {
                Id = EmpowerSpell,
                Name = "Empower Spell",
                FeatCategory = FeatCategory.Metamagic,
                RequiredSpellcasterLevel = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "All variable, numeric effects of an empowered spell are increased by half, including bonuses to those dice rolls. Saving throws and opposed rolls are not affected, nor are spells without random variables. An empowered spell uses up a spell slot two levels higher than the spell's actual level.",
                RequiredFeatIds = new List<int>() { Endurance }
            });
            AllFeats.Add(new Feat()
            {
                Id = Endurance,
                Name = "Endurance",
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You gain a +4 bonus on the following checks and saves: Swim checks made to resist nonlethal damage from exhaustion; Constitution checks made to continue running; Constitution checks made to avoid nonlethal damage from a forced march; Constitution checks made to hold your breath; Constitution checks made to avoid nonlethal damage from starvation or thirst; Fortitude saves made to avoid nonlethal damage from hot or cold environments; and Fortitude saves made to resist damage from suffocation. You may sleep in light or medium armor without becoming fatigued."
            });
            AllFeats.Add(new Feat()
            {
                Id = EnlargeSpell,
                Name = "Enlarge Spell",
                FeatCategory = FeatCategory.Metamagic,
                RequiredSpellcasterLevel = 1,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "You can alter a spell with a range of close, medium, or long to increase its range by 100%. An enlarged spell with a range of close now has a range of 50 ft. + 5 ft./ level, while medium-range spells have a range of 200 ft. + 20 ft./level and long-range spells have a range of 800 ft. + 80 ft./level. An enlarged spell uses up a spell slot one level higher than the spell's actual level. Spells whose ranges are not defined by distance, as well as spells whose ranges are not close, medium, or long, do not benefit from this feat."
            });
            AllFeats.Add(new Feat()
            {
                Id = EschewMaterials,
                Name = "Eschew Materials",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can cast any spell with a material component costing 1 gp or less without needing that component. The casting of the spell still provokes attacks of opportunity as normal. If the spell requires a material component that costs more than 1 gp, you must have the material component on hand to cast the spell, as normal."
            });
            AllFeats.Add(new Feat()
            {
                Id = ExhaustingCritical,
                Name = "Exhausting Critical",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                RequiredBaseAttackBonus = 15,
                Explanation = "When you score a critical hit on a foe, your target immediately becomes exhausted. This feat has no effect on exhausted creatures.",
                RequiredFeatIds = new List<int>() { CriticalFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = ExoticWeaponProficiency,
                Name = "Exotic Weapon Proficiency",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                RequiredBaseAttackBonus = 1,
                Explanation = "You make attack rolls with the chosen exotic weapon normally."
            });
            AllFeats.Add(new Feat()
            {
                Id = ExtendSpell,
                Name = "Extend Spell",
                FeatCategory = FeatCategory.Metamagic,
                RequiredSpellcasterLevel = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "An extended spell lasts twice as long as normal. A spell with a duration of concentration, instantaneous, or permanent is not affected by this feat. An extended spell uses up a spell slot one level higher than the spell's actual level."
            });
            AllFeats.Add(new Feat()
            {
                Id = ExtraChannel,
                Name = "Extra Channel",
                FeatCategory = FeatCategory.General,
                RequiredClassFeature = RequiredClassFeature.ChannelEnergy,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can channel energy two additional times per day."
            });
            AllFeats.Add(new Feat()
            {
                Id = ExtraKi,
                Name = "Extra Ki",
                RequiredClassFeature = RequiredClassFeature.KiPool,
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Your ki pool increases by 2."
            });
            AllFeats.Add(new Feat()
            {
                Id = ExtraLayOnHands,
                Name = "Extra Lay On Hands",
                RequiredClassFeature = RequiredClassFeature.LayOnHands,
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can use your lay on hands ability two additional times per day."
            });
            AllFeats.Add(new Feat()
            {
                Id = ExtraMercy,
                Name = "Extra Mercy",
                RequiredClassFeature = RequiredClassFeature.LayOnHands,
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Select one additional mercy for which you qualify. When you use lay on hands to heal damage to one target, it also receives the additional effects of this mercy."
            });
            AllFeats.Add(new Feat()
            {
                Id = ExtraPerformance,
                Name = "Extra Performance",
                RequiredClassFeature = RequiredClassFeature.BardicPerformance,
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can use bardic performance for 6 additional rounds per day."
            });
            AllFeats.Add(new Feat()
            {
                Id = ExtraRage,
                Name = "Extra Rage",
                RequiredClassFeature = RequiredClassFeature.Rage,
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can rage for 6 additional rounds per day."
            });
            AllFeats.Add(new Feat()
            {
                Id = FarShot,
                Name = "Far Shot",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You only suffer a -1 penalty per full range increment between you and your target when using a ranged weapon.",
                RequiredFeatIds = new List<int>() { PointBlankShot }
            });
            AllFeats.Add(new Feat()
            {
                Id = Fleet,
                Name = "Fleet",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "While you are wearing light or no armor, your base speed increases by 5 feet. You lose the benefits of this feat if you carry a medium or heavy load."
            });
            AllFeats.Add(new Feat()
            {
                Id = ForgeRing,
                Name = "Forge Ring",
                FeatCategory = FeatCategory.ItemCreation,
                RequiredSpellcasterLevel = 7,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can create magic rings. Crafting a ring takes 1 day for each 1,000 gp in its base price. To craft a ring, you must use up raw materials costing half of the base price. See the magic item creation rules in Chapter 15 for more information. You can also mend a broken ring if it is one that you could make. Doing so costs half the raw materials and half the time it would take to forge that ring in the first place."
            });
            AllFeats.Add(new Feat()
            {
                Id = GorgonsFist,
                Name = "Gorgon's Fist",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 6,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "As a standard action, make a single unarmed melee attack against a foe whose speed is reduced (such as from Scorpion Style). If the attack hits, you deal damage normally and the target is staggered until the end of your next turn unless it makes a Fortitude saving throw (DC 10 + 1/2 your character level + your Wis modifier). This feat has no effect on targets that are staggered.",
                RequiredFeatIds = new List<int>() { ImprovedUnarmedStrike, ScorpionStyle }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreatCleave,
                Name = "Great Cleave",
                FeatCategory = FeatCategory.Combat,
                RequiredStr = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "As a standard action, you can make a single attack at your full base attack bonus against a foe within reach. If you hit, you deal damage normally and can make an additional attack (using your full base attack bonus) against a foe that is adjacent to the previous foe and also within reach. If you hit, you can continue to make attacks against foes adjacent to the previous foe, so long as they are within your reach. You cannot attack an individual foe more than once during this attack action. When you use this feat, you take a -2 penalty to your Armor Class until your next turn.",
                RequiredFeatIds = new List<int>() { Cleave, PowerAttack }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreatFortitude,
                Name = "Great Fortitude",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +2 bonus on all Fortitude saving throws."
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterBullRush,
                Name = "Greater Bull Rush",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 6,
                RequiredStr = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You receive a +2 bonus on checks made to bull rush a foe. This bonus stacks with the bonus granted by Improved Bull Rush. Whenever you bull rush an opponent, his movement provokes attacks of opportunity from all of your allies (but not you).",
                RequiredFeatIds = new List<int>() { ImprovedBullRush, PowerAttack }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterDisarm,
                Name = "Greater Disarm",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 6,
                RequiredInt = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You receive a +2 bonus on checks made to disarm a foe. This bonus stacks with the bonus granted by Improved Disarm. Whenever you successfully disarm an opponent, the weapon lands 15 feet away from its previous wielder, in a random direction.",
                RequiredFeatIds = new List<int>() { CombatExpertise, ImprovedDisarm }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterFeint,
                Name = "Greater Feint",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 6,
                RequiredInt = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Whenever you use feint to cause an opponent to lose his Dexterity bonus, he loses that bonus until the beginning of your next turn, in addition to losing his Dexterity bonus against your next attack.",
                RequiredFeatIds = new List<int>() { CombatExpertise, ImprovedFeint }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterGrapple,
                Name = "Greater Grapple",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 6,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You receive a +2 bonus on checks made to grapple a foe. This bonus stacks with the bonus granted by Improved Grapple. Once you have grappled a creature, maintaining the grapple is a move action. This feat allows you to make two grapple checks each round (to move, harm, or pin your opponent), but you are not required to make two checks. You only need to succeed at one of these checks to maintain the grapple.",
                RequiredFeatIds = new List<int>() { ImprovedUnarmedStrike, ImprovedGrapple }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterOverrun,
                Name = "Greater Overrun",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 6,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You receive a +2 bonus on checks made to overrun a foe. This bonus stacks with the bonus granted by Improved Overrun. Whenever you overrun opponents, they provoke attacks of opportunity if they are knocked prone by your overrun.",
                RequiredFeatIds = new List<int>() { PowerAttack, ImprovedOverrun }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterPenetratingStrike,
                Name = "Greater Penetrating Strike",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 16,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Your attacks made with weapons selected with Weapon Focus ignore up to 10 points of damage reduction. This amount is reduced to 5 points for damage reduction without a type (such as DR 10/-).",
                RequiredFeatIds = new List<int>() { PenetratingStrike, WeaponFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterShieldFocus,
                Name = "Greater Shield Focus",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 8,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Increase the AC bonus granted by any shield you are using by 1. This bonus stacks with the bonus granted by Shield Focus.",
                RequiredFeatIds = new List<int>() { ShieldFocus, ShieldProficiency }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterSpellFocus,
                Name = "Greater Spell Focus",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Add +1 to the Difficulty Class for all saving throws against spells from the school of magic you select. This bonus stacks with the bonus from Spell Focus.",
                RequiredFeatIds = new List<int>() { SpellFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterSpellPenetration,
                Name = "Greater Spell Penetration",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +2 bonus on caster level checks (1d20 + caster level) made to overcome a creature's spell resistance. This bonus stacks with the one from Spell Penetration.",
                RequiredFeatIds = new List<int>() { SpellPenetration }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterSunder,
                Name = "Greater Sunder",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 6,
                RequiredStr = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You receive a +2 bonus on checks made to sunder an item. This bonus stacks with the bonus granted by Improved Sunder. Whenever you sunder to destroy a weapon, shield, or suit of armor, any excess damage is applied to the item's wielder. No damage is transferred if you decide to leave the item with 1 hit point.",
                RequiredFeatIds = new List<int>() { ImprovedSunder, PowerAttack }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterTrip,
                Name = "Greater Trip",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 6,
                RequiredInt = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You receive a +2 bonus on checks made to trip a foe. This bonus stacks with the bonus granted by Improved Trip. Whenever you successfully trip an opponent, that opponent provokes attacks of opportunity.",
                RequiredFeatIds = new List<int>() { CombatExpertise, ImprovedTrip }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterTwoWeaponFighting,
                Name = "Greater Two-Weapon Fighting",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 11,
                RequiredDex = 19,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a third attack with your off-hand weapon, albeit at a -10 penalty.",
                RequiredFeatIds = new List<int>() { ImprovedTwoWeaponFighting, TwoWeaponFighting }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterVitalStrike,
                Name = "Greater Vital Strike",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 16,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When you use the attack action, you can make one attack at your highest base attack bonus that deals additional damage. Roll the weapon's damage dice for the attack four times and add the results together before adding bonuses from Strength, weapon abilities (such as flaming), precision-based damage (such as sneak attack), and other damage bonuses. These extra weapon damage dice are not multiplied on a critical hit, but are added to the total.",
                RequiredFeatIds = new List<int>() { ImprovedVitalStrike, VitalStrike }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterWeaponFocus,
                Name = "Greater Weapon Focus",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 8,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You gain a +1 bonus on attack rolls you make using the selected weapon. This bonus stacks with other bonuses on attack rolls, including those from Weapon Focus.",
                RequiredFeatIds = new List<int>() { WeaponFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = GreaterWeaponSpecialization,
                Name = "Greater Weapon Specialization",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 12,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You gain a +2 bonus on all damage rolls you make using the selected weapon. This bonus to damage stacks with other damage roll bonuses, including any you gain from Weapon Specialization.",
                RequiredFeatIds = new List<int>() { WeaponFocus, WeaponSpecialization }
            });
            AllFeats.Add(new Feat()
            {
                Id = HeightenSpell,
                Name = "Heighten Spell",
                FeatCategory = FeatCategory.Metamagic,
                RequiredSpellcasterLevel = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "A heightened spell has a higher spell level than normal (up to a maximum of 9th level). Unlike other metamagic feats, Heighten Spell actually increases the effective level of the spell that it modifies. All effects dependent on spell level (such as saving throw DCs and ability to penetrate a lesser globe of invulnerability) are calculated according to the heightened level. The heightened spell is as difficult to prepare and cast as a spell of its effective level."
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedBullRush,
                Name = "Improved Bull Rush",
                FeatCategory = FeatCategory.Combat,
                RequiredStr = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You do not provoke an attack of opportunity when performing a bull rush combat maneuver. In addition, you receive a +2 bonus on checks made to bull rush a foe. You also receive a +2 bonus to your Combat Maneuver Defense whenever an opponent tries to bull rush you.",
                RequiredFeatIds = new List<int>() { PowerAttack }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedChannel,
                Name = "Improved Channel",
                RequiredClassFeature = RequiredClassFeature.ChannelEnergy,
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Add 2 to the DC of saving throws made to resist the effects of your channel energy ability."
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedCounterspell,
                Name = "Improved Counterspell",
                FeatCategory = FeatCategory.General,
                RequiredSpellcasterLevel = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When counterspelling, you may use a spell of the same school that is one or more spell levels higher than the target spell."
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedCritical,
                Name = "Improved Critical",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 8,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When using the weapon you selected, your threat range is doubled."
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedDisarm,
                Name = "Improved Disarm",
                FeatCategory = FeatCategory.Combat,
                RequiredInt = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You do not provoke an attack of opportunity when performing a disarm combat maneuver. In addition, you receive a +2 bonus on checks made to disarm a foe. You also receive a +2 bonus to your Combat Maneuver Defense whenever an opponent tries to disarm you.",
                RequiredFeatIds = new List<int>() { CombatExpertise }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedFamiliar,
                Name = "Improved Familiar",
                RequiredClassFeature = RequiredClassFeature.Familiar,
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.SuperRareFeature,
                Explanation = "When choosing a familiar, the creatures listed below are also available to you (see the Pathfinder RPG Bestiary for statistics on these creatures). You may choose a familiar with an alignment up to one step away on each alignment axis (lawful through chaotic, good through evil). Arcane Spellcaster Familiar Alignment Level Celestial hawk1 Neutral good 3rd Dire rat Neutral 3rd Fiendish viper2 Neutral evil 3rd Elemental, Small (any type) Neutral 5th Stirge Neutral 5th Homunculus3 Any 7th Imp Lawful evil 7th Mephit (any type) Neutral 7th Pseudodragon Neutral good 7th Quasit Chaotic evil 7th 1 Or other celestial animal from the standard familiar list. 2 Or other fiendish animal from the standard familiar list. 3 The master must first create the homunculus. Improved familiars otherwise use the rules for regular familiars, with two exceptions: if the creature's type is something other than animal, its type does not change; and improved familiars do not gain the ability to speak with other creatures of their kind (although many of them already have the ability to communicate)."
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedFeint,
                Name = "Improved Feint",
                FeatCategory = FeatCategory.Combat,
                RequiredInt = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can make a Bluff check to feint in combat as a move action.",
                RequiredFeatIds = new List<int>() { CombatExpertise }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedGrapple,
                Name = "Improved Grapple",
                FeatCategory = FeatCategory.Combat,
                RequiredDex = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You do not provoke an attack of opportunity when performing a grapple combat maneuver. In addition, you receive a +2 bonus on checks made to grapple a foe. You also receive a +2 bonus to your Combat Maneuver Defense whenever an opponent tries to grapple you.",
                RequiredFeatIds = new List<int>() { ImprovedUnarmedStrike }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedGreatFortitude,
                Name = "Improved Great Fortitude",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Once per day, you may reroll a Fortitude save. You must decide to use this ability before the results are revealed. You must take the second roll, even if it is worse.",
                RequiredFeatIds = new List<int>() { GreatFortitude }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedInitiative,
                Name = "Improved Initiative",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +4 bonus on initiative checks."
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedIronWill,
                Name = "Improved Iron Will",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Once per day, you may reroll a Will save. You must decide to use this ability before the results are revealed. You must take the second roll, even if it is worse.",
                RequiredFeatIds = new List<int>() { IronWill }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedLightningReflexes,
                Name = "Improved Lightning Reflexes",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Once per day, you may reroll a Reflex save. You must decide to use this ability before the results are revealed. You must take the second roll, even if it is worse.",
                RequiredFeatIds = new List<int>() { LightningReflexes }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedOverrun,
                Name = "Improved Overrun",
                FeatCategory = FeatCategory.Combat,
                RequiredStr = 13,
                RequiredBaseAttackBonus = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You do not provoke an attack of opportunity when performing an overrun combat maneuver. In addition, you receive a +2 bonus on checks made to overrrun a foe. You also receive a +2 bonus to your Combat Maneuver Defense whenever an opponent tries to overrun you. Targets of your overrun attempt may not choose to avoid you.",
                RequiredFeatIds = new List<int>() { PowerAttack }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedPreciseShot,
                Name = "Improved Precise Shot",
                FeatCategory = FeatCategory.Combat,
                RequiredDex = 19,
                RequiredBaseAttackBonus = 11,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Your ranged attacks ignore the AC bonus granted to targets by anything less than total cover, and the miss chance granted to targets by anything less than total concealment. Total cover and total concealment provide their normal benefits against your ranged attacks.",
                RequiredFeatIds = new List<int>() { PointBlankShot, PreciseShot }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedShieldBash,
                Name = "Improved Shield Bash",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When you perform a shield bash, you may still apply the shield's shield bonus to your AC.",
                RequiredFeatIds = new List<int>() { ShieldProficiency }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedSunder,
                Name = "Improved Sunder",
                FeatCategory = FeatCategory.Combat,
                RequiredStr = 13,
                RequiredBaseAttackBonus = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You do not provoke an attack of opportunity when performing a sunder combat maneuver. In addition, you receive a +2 bonus on checks made to sunder an item. You also receive a +2 bonus to your Combat Maneuver Defense whenever an opponent tries to sunder your gear.",
                RequiredFeatIds = new List<int>() { PowerAttack }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedTrip,
                Name = "Improved Trip",
                FeatCategory = FeatCategory.Combat,
                RequiredInt = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You do not provoke an attack of opportunity when performing a trip combat maneuver. In addition, you receive a +2 bonus on checks made to trip a foe. You also receive a +2 bonus to your Combat Maneuver Defense whenever an opponent tries to trip you.",
                RequiredFeatIds = new List<int>() { CombatExpertise }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedTwoWeaponFighting,
                Name = "Improved Two-Weapon Fighting",
                FeatCategory = FeatCategory.Combat,
                RequiredDex = 17,
                RequiredBaseAttackBonus = 6,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "In addition to the standard single extra attack you get with an off-hand weapon, you get a second attack with it, albeit at a -5 penalty.",
                RequiredFeatIds = new List<int>() { TwoWeaponFighting }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedUnarmedStrike,
                Name = "Improved Unarmed Strike",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You are considered to be armed even when unarmed-you do not provoke attacks of opportunity when you attack foes while unarmed. Your unarmed strikes can deal lethal or nonlethal damage, at your choice."
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovedVitalStrike,
                Name = "Improved Vital Strike",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 11,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When you use the attack action, you can make one attack at your highest base attack bonus that deals additional damage. Roll the weapon's damage dice for the attack three times and add the results together before adding bonuses from Strength, weapon abilities (such as flaming), precision-based damage, and other damage bonuses. These extra weapon damage dice are not multiplied on a critical hit, but are added to the total.",
                RequiredFeatIds = new List<int>() { VitalStrike }
            });
            AllFeats.Add(new Feat()
            {
                Id = ImprovisedWeaponMastery,
                Name = "Improvised Weapon Mastery",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "You do not suffer any penalties for using an improvised weapon. Increase the amount of damage dealt by the improvised weapon by one step (for example, 1d4 becomes 1d6) to a maximum of 1d8 (2d6 if the improvised weapon is two-handed). The improvised weapon has a critical threat range of 19-20, with a critical multiplier of ×2.",
                RequiredFeatIds = new List<int>() { CatchOffGuard }
            });
            AllFeats.Add(new Feat()
            {
                Id = IntimidatingProwess,
                Name = "Intimidating Prowess",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "Add your Strength modifier to Intimidate skill checks in addition to your Charisma modifier."
            });
            AllFeats.Add(new Feat()
            {
                Id = IronWill,
                Name = "Iron Will",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +2 bonus on all Will saving throws."
            });
            AllFeats.Add(new Feat()
            {
                Id = Leadership,
                Name = "Leadership",
                FeatCategory = FeatCategory.General,
                RequiredBaseAttackBonus = 7,
                Rarity = FeatureElement.SuperRareFeature,
                Explanation = "This feat enables you to attract a loyal cohort and a number of devoted subordinates who assist you. A cohort is generally an NPC with class levels, while followers are typically lower level NPCs. See Table 5-2 for what level of cohort and how many followers you can recruit."
            });
            AllFeats.Add(new Feat()
            {
                Id = LightningReflexes,
                Name = "Lightning Reflexes",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +2 bonus on all Ref lex saving throws."
            });
            AllFeats.Add(new Feat()
            {
                Id = LightningStance,
                Name = "Lightning Stance",
                FeatCategory = FeatCategory.Combat,
                RequiredDex = 17,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "If you take two actions to move or a withdraw action in a turn, you gain 50% concealment for 1 round.",
                RequiredFeatIds = new List<int>() { WindStance, Dodge }
            });
            AllFeats.Add(new Feat()
            {
                Id = Lunge,
                Name = "Lunge",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 6,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can increase the reach of your melee attacks by 5 feet until the end of your turn by taking a -2 penalty to your AC until your next turn. You must decide to use this ability before any attacks are made."
            });
            AllFeats.Add(new Feat()
            {
                Id = MagicalAptitude,
                Name = "Magical Aptitude",
                FeatCategory = FeatCategory.General,
                AppliedSkillId = Skills.Spellcraft,
                AppliedSkillId2 = Skills.UseMagicDevice,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +2 bonus on all Spellcraft checks and Use Magic Device checks. If you have 10 or more ranks in one of these skills, the bonus increases to +4 for that skill.",
                AppliedBonus = 2
            });
            AllFeats.Add(new Feat()
            {
                Id = Manyshot,
                Name = "Manyshot",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 6,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When making a full-attack action with a bow, your first attack fires two arrows. If the attack hits, both arrows hit. Apply precision-based damage (such as sneak attack) and critical hit damage only once for this attack. Damage bonuses from using a composite bow with a high Strength bonus apply to each arrow, as do other damage bonuses, such as a ranger's favored enemy bonus. Damage reduction and resistances apply separately to each arrow.",
                RequiredFeatIds = new List<int>() { PointBlankShot, RapidShot }
            });
            AllFeats.Add(new Feat()
            {
                Id = MartialWeaponProficiency,
                Name = "Martial Weapon Proficiency",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "You make attack rolls with the selected weapon normally (without the non-proficient penalty)."
            });
            AllFeats.Add(new Feat()
            {
                Id = MasterCraftsman,
                Name = "Master Craftsman",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.SuperRareFeature,
                Explanation = "Choose one Craft or Profession skill in which you possess at least 5 ranks. You receive a +2 bonus on your chosen Craft or Profession skill. Ranks in your chosen skill count as your caster level for the purposes of qualifying for the Craft Magic Arms and Armor and Craft Wondrous Item feats. You can create magic items using these feats, substituting your ranks in the chosen skill for your total caster level. You must use the chosen skill for the check to create the item. The DC to create the item still increases for any necessary spell requirements (see the magic item creation rules in Chapter 15). You cannot use this feat to create any spell-trigger or spell-activation item.",
                RequiredSkillRanks = new Dictionary<int, int>() { { Skills.Craft, 1 } }
            });
            AllFeats.Add(new Feat()
            {
                Id = MaximizeSpell,
                Name = "Maximize Spell",
                FeatCategory = FeatCategory.Metamagic,
                RequiredSpellcasterLevel = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "All variable, numeric effects of a spell modified by this feat are maximized. Saving throws and opposed rolls are not affected, nor are spells without random variables. A maximized spell uses up a spell slot three levels higher than the spell's actual level. An empowered, maximized spell gains the separate benefits of each feat: the maximum result plus half the normally rolled result."
            });
            AllFeats.Add(new Feat()
            {
                Id = MedusasWrath,
                Name = "Medusa's Wrath",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 11,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Whenever you use the full-attack action and make at least one unarmed strike, you can make two additional unarmed strikes at your highest base attack bonus. These bonus attacks must be made against a dazed, flat-footed, paralyzed, staggered, stunned, or unconscious foe.",
                RequiredFeatIds = new List<int>() { ImprovedUnarmedStrike, GorgonsFist, ScorpionStyle }
            });
            AllFeats.Add(new Feat()
            {
                Id = Mobility,
                Name = "Mobility",
                FeatCategory = FeatCategory.Combat,
                RequiredDex = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +4 dodge bonus to Armor Class against attacks of opportunity caused when you move out of or within a threatened area. A condition that makes you lose your Dexterity bonus to Armor Class (if any) also makes you lose dodge bonuses. Dodge bonuses stack with each other, unlike most types of bonuses.",
                RequiredFeatIds = new List<int>() { Dodge }
            });
            AllFeats.Add(new Feat()
            {
                Id = MountedArchery,
                Name = "Mounted Archery",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "The penalty you take when using a ranged weapon while mounted is halved: -2 instead of -4 if your mount is taking a double move, and -4 instead of -8 if your mount is running.",
                RequiredFeatIds = new List<int>() { MountedCombat },
                RequiredSkillRanks = new Dictionary<int, int>() { { Skills.Ride, 1 } }
            });
            AllFeats.Add(new Feat()
            {
                Id = MountedCombat,
                Name = "Mounted Combat",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "Once per round when your mount is hit in combat, you may attempt a Ride check (as an immediate action) to negate the hit. The hit is negated if your Ride check result is greater than the opponent's attack roll.",
                RequiredSkillRanks = new Dictionary<int, int>() { { Skills.Ride, 1 } }
            });
            AllFeats.Add(new Feat()
            {
                Id = NaturalSpell,
                Name = "Natural Spell",
                RequiredClassFeature = RequiredClassFeature.WildShape,
                FeatCategory = FeatCategory.General,
                RequiredWis = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can complete the verbal and somatic components of spells while using wild shape. You substitute various noises and gestures for the normal verbal and somatic components of a spell. You can also use any material components or focuses you possess, even if such items are melded within your current form. This feat does not permit the use of magic items while you are in a form that could not ordinarily use them, and you do not gain the ability to speak while using wild shape."
            });
            AllFeats.Add(new Feat()
            {
                Id = NimbleMoves,
                Name = "Nimble Moves",
                FeatCategory = FeatCategory.General,
                RequiredDex = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Whenever you move, you may move through 5 feet of difficult terrain each round as if it were normal terrain. This feat allows you to take a 5-foot step into difficult terrain."
            });
            AllFeats.Add(new Feat()
            {
                Id = PenetratingStrike,
                Name = "Penetrating Strike",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 12,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Your attacks made with weapons selected with Weapon Focus ignore up to 5 points of damage reduction. This feat does not apply to damage reduction without a type (such as DR 10/-).",
                RequiredFeatIds = new List<int>() { WeaponFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = Persuasive,
                Name = "Persuasive",
                FeatCategory = FeatCategory.General,
                AppliedSkillId = Skills.Diplomacy,
                AppliedSkillId2 = Skills.Intimidate,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +2 bonus on Diplomacy and Intimidate skill checks. If you have 10 or more ranks in one of these skills, the bonus increases to +4 for that skill.",
                AppliedBonus = 2
            });
            AllFeats.Add(new Feat()
            {
                Id = PinpointTargeting,
                Name = "Pinpoint Targeting",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 16,
                RequiredDex = 19,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "As a standard action, make a single ranged attack. The target does not gain any armor, natural armor, or shield bonuses to its Armor Class. You do not gain the benefit of this feat if you move this round.",
                RequiredFeatIds = new List<int>() { ImprovedPreciseShot, PointBlankShot, PreciseShot }
            });
            AllFeats.Add(new Feat()
            {
                Id = PointBlankShot,
                Name = "Point-Blank Shot",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +1 bonus on attack and damage rolls with ranged weapons at ranges of up to 30 feet."
            });
            AllFeats.Add(new Feat()
            {
                Id = PowerAttack,
                Name = "Power Attack",
                FeatCategory = FeatCategory.Combat,
                RequiredStr = 13,
                RequiredBaseAttackBonus = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can choose to take a -1 penalty on all melee attack rolls and combat maneuver checks to gain a +2 bonus on all melee damage rolls. This bonus to damage is increased by half (+50%) if you are making an attack with a two-handed weapon, a one handed weapon using two hands, or a primary natural weapon that adds 1-1/2 times your Strength modif ier on damage rolls. This bonus to damage is halved (-50%) if you are making an attack with an off-hand weapon or secondary natural weapon. When your base attack bonus reaches +4, and every 4 points thereafter, the penalty increases by -1 and the bonus to damage increases by +2. You must choose to use this feat before making an attack roll, and its effects last until your next turn. The bonus damage does not apply to touch attacks or effects that do not deal hit point damage."
            });
            AllFeats.Add(new Feat()
            {
                Id = PreciseShot,
                Name = "Precise Shot",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can shoot or throw ranged weapons at an opponent engaged in melee without taking the standard -4 penalty on your attack roll.",
                RequiredFeatIds = new List<int>() { PointBlankShot }
            });
            AllFeats.Add(new Feat()
            {
                Id = QuickDraw,
                Name = "Quick Draw",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can draw a weapon as a free action instead of as a move action. You can draw a hidden weapon (see the Sleight of Hand skill) as a move action. A character who has selected this feat may throw weapons at his full normal rate of attacks (much like a character with a bow). Alchemical items, potions, scrolls, and wands cannot be drawn quickly using this feat."
            });
            AllFeats.Add(new Feat()
            {
                Id = QuickenSpell,
                Name = "Quicken Spell",
                FeatCategory = FeatCategory.Metamagic,
                RequiredSpellcasterLevel = 1,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "Casting a quickened spell is a swift action. You can perform another action, even casting another spell, in the same round as you cast a quickened spell. A spell whose casting time is more than 1 round or 1 fullround action cannot be quickened. A quickened spell uses up a spell slot four levels higher than the spell's actual level. Casting a quickened spell doesn't provoke an attack of opportunity."
            });
            AllFeats.Add(new Feat()
            {
                Id = RapidShot,
                Name = "Rapid Shot",
                FeatCategory = FeatCategory.Combat,
                RequiredDex = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When making a full-attack action with a ranged weapon, you can fire one additional time this round at your highest bonus. All of your attack rolls take a -2 penalty when using Rapid Shot.",
                RequiredFeatIds = new List<int>() { PointBlankShot }
            });
            AllFeats.Add(new Feat()
            {
                Id = RideByAttack,
                Name = "Ride-By Attack",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When you are mounted and use the charge action, you may move and attack as if with a standard charge and then move again (continuing the straight line of the charge). Your total movement for the round can't exceed double your mounted speed. You and your mount do not provoke an attack of opportunity from the opponent that you attack.",
                RequiredFeatIds = new List<int>() { MountedCombat },
                RequiredSkillRanks = new Dictionary<int, int>() { { Skills.Ride, 1 } }
            });
            AllFeats.Add(new Feat()
            {
                Id = Run,
                Name = "Run",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When running, you move five times your normal speed (if wearing medium, light, or no armor and carrying no more than a medium load) or four times your speed (if wearing heavy armor or carrying a heavy load). If you make a jump after a running start (see the Acrobatics skill description), you gain a +4 bonus on your Acrobatics check. While running, you retain your Dexterity bonus to your Armor Class."
            });
            AllFeats.Add(new Feat()
            {
                Id = ScorpionStyle,
                Name = "Scorpion Style",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "To use this feat, you must make a single unarmed attack as a standard action. If this unarmed attack hits, you deal damage normally, and the target's base land speed is reduced to 5 feet for a number of rounds equal to your Wisdom modifier unless it makes a Fortitude saving throw (DC 10 + 1/2 your character level + your Wis modifier).",
                RequiredFeatIds = new List<int>() { ImprovedUnarmedStrike }
            });
            AllFeats.Add(new Feat()
            {
                Id = ScribeScroll,
                Name = "Scribe Scroll",
                FeatCategory = FeatCategory.ItemCreation,
                RequiredSpellcasterLevel = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can create a scroll of any spell that you know. Scribing a scroll takes 2 hours if its base price is 250 gp or less, otherwise scribing a scroll takes 1 day for each 1,000 gp in its base price. To scribe a scroll, you must use up raw materials costing half of this base price. See the magic item creation rules in Chapter 15 for more information."
            });
            AllFeats.Add(new Feat()
            {
                Id = SelectiveChanneling,
                Name = "Selective Channeling",
                RequiredClassFeature = RequiredClassFeature.ChannelEnergy,
                FeatCategory = FeatCategory.General,
                RequiredCha = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When you channel energy, you can choose a number of targets in the area up to your Charisma modifier. These targets are not affected by your channeled energy."
            });
            AllFeats.Add(new Feat()
            {
                Id = SelfSufficient,
                Name = "Self-Sufficient",
                FeatCategory = FeatCategory.General,
                AppliedSkillId = Skills.Heal,
                AppliedSkillId2 = Skills.Survival,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +2 bonus on all Heal checks and Survival checks. If you have 10 or more ranks in one of these skills, the bonus increases to +4 for that skill.",
                AppliedBonus = 2,
            });
            AllFeats.Add(new Feat()
            {
                Id = ShatterDefenses,
                Name = "Shatter Defenses",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 6,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Any shaken, frightened, or panicked opponent hit by you this round is flat-footed to your attacks until the end of your next turn. This includes any additional attacks you make this round.",
                RequiredFeatIds = new List<int>() { WeaponFocus, DazzlingDisplay }
            });
            AllFeats.Add(new Feat()
            {
                Id = ShieldFocus,
                Name = "Shield Focus",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Increase the AC bonus granted by any shield you are using by 1.",
                RequiredFeatIds = new List<int>() { ShieldProficiency }
            });
            AllFeats.Add(new Feat()
            {
                Id = ShieldProficiency,
                Name = "Shield Proficiency",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When you use a shield (except a tower shield), the shield's armor check penalty only applies to Strength- and Dexterity-based skills."
            });
            AllFeats.Add(new Feat()
            {
                Id = ShieldSlam,
                Name = "Shield Slam",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 6,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Any opponents hit by your shield bash are also hit with a free bull rush attack, substituting your attack roll for the combat maneuver check (see Chapter 8). This bull rush does not provoke an attack of opportunity. Opponents who cannot move back due to a wall or other surface are knocked prone after moving the maximum possible distance. You may choose to move with your target if you are able to take a 5-foot step or to spend an action to move this turn.",
                RequiredFeatIds = new List<int>() { ImprovedShieldBash, ShieldProficiency, TwoWeaponFighting }
            });
            AllFeats.Add(new Feat()
            {
                Id = ShotontheRun,
                Name = "Shot on the Run",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 4,
                RequiredDex = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "As a full-round action, you can move up to your speed and make a single ranged attack at any point during your movement.",
                RequiredFeatIds = new List<int>() { Dodge, Mobility, PointBlankShot }
            });
            AllFeats.Add(new Feat()
            {
                Id = SickeningCritical,
                Name = "Sickening Critical",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 11,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Whenever you score a critical hit, your opponent becomes sickened for 1 minute. The effects of this feat do not stack. Additional hits instead add to the effect's duration.",
                RequiredFeatIds = new List<int>() { CriticalFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = SilentSpell,
                Name = "Silent Spell",
                FeatCategory = FeatCategory.Metamagic,
                RequiredSpellcasterLevel = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "A silent spell can be cast with no verbal components. Spells without verbal components are not affected. A silent spell uses up a spell slot one level higher than the spell's actual level."
            });
            AllFeats.Add(new Feat()
            {
                Id = SimpleWeaponProficiency,
                Name = "Simple Weapon Proficiency",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You make attack rolls with simple weapons without penalty."
            });
            AllFeats.Add(new Feat()
            {
                Id = SkillFocus,
                Name = "Skill Focus",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +3 bonus on all checks involving the chosen skill. If you have 10 or more ranks in that skill, this bonus increases to +6."
            });
            AllFeats.Add(new Feat()
            {
                Id = SnatchArrows,
                Name = "Snatch Arrows",
                FeatCategory = FeatCategory.Combat,
                RequiredDex = 15,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When using the Def lect Arrows feat you may choose to catch the weapon instead of just def lecting it. Thrown weapons can immediately be thrown back as an attack against the original attacker (even though it isn't your turn) or kept for later use. You must have at least one hand free (holding nothing) to use this feat.",
                RequiredFeatIds = new List<int>() { DeflectArrows, ImprovedUnarmedStrike }
            });
            AllFeats.Add(new Feat()
            {
                Id = SpellFocus,
                Name = "Spell Focus",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Add +1 to the Difficulty Class for all saving throws against spells from the school of magic you select."
            });
            AllFeats.Add(new Feat()
            {
                Id = SpellMastery,
                Name = "Spell Mastery",
                FeatCategory = FeatCategory.General,
                RequiredSpellcasterLevel = 1,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "Each time you take this feat, choose a number of spells that you already know equal to your Intelligence modifier. From that point on, you can prepare these spells without referring to a spellbook."
            });
            AllFeats.Add(new Feat()
            {
                Id = SpellPenetration,
                Name = "Spell Penetration",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +2 bonus on caster level checks (1d20 + caster level) made to overcome a creature's spell resistance."
            });
            AllFeats.Add(new Feat()
            {
                Id = Spellbreaker,
                Name = "Spellbreaker",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 10,
                Rarity = FeatureElement.RareFeature,
                Explanation = "Enemies in your threatened area that fail their checks to cast spells defensively provoke attacks of opportunity from you.",
                RequiredFeatIds = new List<int>() { Disruptive }
            });
            AllFeats.Add(new Feat()
            {
                Id = SpiritedCharge,
                Name = "Spirited Charge",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When mounted and using the charge action, you deal double damage with a melee weapon (or triple damage with a lance).",
                RequiredFeatIds = new List<int>() { MountedCombat, RideByAttack },
                RequiredSkillRanks = new Dictionary<int, int>() { { Skills.Ride, 1 } }
            });
            AllFeats.Add(new Feat()
            {
                Id = SpringAttack,
                Name = "Spring Attack",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 4,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "As a full-round action, you can move up to your speed and make a single melee attack without provoking any attacks of opportunity from the target of your attack. You can move both before and after the attack, but you must move at least 10 feet before the attack and the total distance that you move cannot be greater than your speed. You cannot use this ability to attack a foe that is adjacent to you at the start of your turn.",
                RequiredFeatIds = new List<int>() { Dodge, Mobility }
            });
            AllFeats.Add(new Feat()
            {
                Id = StaggeringCritical,
                Name = "Staggering Critical",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 13,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "Whenever you score a critical hit, your opponent becomes staggered for 1d4+1 rounds. A successful Fortitude save reduces the duration to 1 round. The DC of this Fortitude save is equal to 10 + your base attack bonus. The effects of this feat do not stack. Additional hits instead add to the duration.",
                RequiredFeatIds = new List<int>() { CriticalFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = StandStill,
                Name = "Stand Still",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When a foe provokes an attack of opportunity due to moving through your adjacent squares, you can make a combat maneuver check as your attack of opportunity. If successful, the enemy cannot move for the rest of his turn. An enemy can still take the rest of his action, but cannot move. This feat also applies to any creature that attempts to move from a square that is adjacent to you if such movement provokes an attack of opportunity.",
                RequiredFeatIds = new List<int>() { CombatReflexes }
            });
            AllFeats.Add(new Feat()
            {
                Id = Stealthy,
                Name = "Stealthy",
                FeatCategory = FeatCategory.General,
                AppliedSkillId = Skills.EscapeArtist,
                AppliedSkillId2 = Skills.Stealth,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You get a +2 bonus on all Escape Artist and Stealth skill checks. If you have 10 or more ranks in one of these skills, the bonus increases to +4 for that skill.",
                AppliedBonus = 2
            });
            AllFeats.Add(new Feat()
            {
                Id = StepUp,
                Name = "Step Up",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Whenever an adjacent foe attempts to take a 5-foot step away from you, you may also make a 5-foot step as an immediate action so long as you end up adjacent to the foe that triggered this ability. If you take this step, you cannot take a 5-foot step during your next turn. If you take an action to move during your next turn, subtract 5 feet from your total movement."
            });
            AllFeats.Add(new Feat()
            {
                Id = StillSpell,
                Name = "Still Spell",
                FeatCategory = FeatCategory.Metamagic,
                RequiredSpellcasterLevel = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "A stilled spell can be cast with no somatic components. Spells without somatic components are not affected. A stilled spell uses up a spell slot one level higher than the spell's actual level."
            });
            AllFeats.Add(new Feat()
            {
                Id = StrikeBack,
                Name = "Strike Back",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 11,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can ready an action to make a melee attack against any foe that attacks you in melee, even if the foe is outside of your reach."
            });
            AllFeats.Add(new Feat()
            {
                Id = StunningCritical,
                Name = "Stunning Critical",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 17,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Whenever you score a critical hit, your opponent becomes stunned for 1d4 rounds. A successful Fortitude save reduces this to staggered for 1d4 rounds. The DC of this Fortitude save is equal to 10 + your base attack bonus. The effects of this feat do not stack. Additional hits instead add to the duration.",
                RequiredFeatIds = new List<int>() { CriticalFocus, StaggeringCritical }
            });
            AllFeats.Add(new Feat()
            {
                Id = StunningFist,
                Name = "Stunning Fist",
                FeatCategory = FeatCategory.Combat,
                RequiredWis = 13,
                RequiredDex = 13,
                RequiredBaseAttackBonus = 8,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You must declare that you are using this feat before you make your attack roll (thus, a failed attack roll ruins the attempt). Stunning Fist forces a foe damaged by your unarmed attack to make a Fortitude saving throw (DC 10 + 1/2 your character level + your Wis modifier), in addition to dealing damage normally. A defender who fails this saving throw is stunned for 1 round (until just before your next turn). A stunned character drops everything held, can't take actions, loses any Dexterity bonus to AC, and takes a -2 penalty to AC. You may attempt a stunning attack once per day for every four levels you have attained (but see Special), and no more than once per round. Constructs, oozes, plants, undead, incorporeal creatures, and creatures immune to critical hits cannot be stunned.",
                RequiredFeatIds = new List<int>() { ImprovedUnarmedStrike }
            });
            AllFeats.Add(new Feat()
            {
                Id = ThrowAnything,
                Name = "Throw Anything",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You do not suffer any penalties for using an improvised ranged weapon. You receive a +1 circumstance bonus on attack rolls made with thrown splash weapons."
            });
            AllFeats.Add(new Feat()
            {
                Id = TiringCritical,
                Name = "Tiring Critical",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Whenever you score a critical hit, your opponent becomes fatigued. This feat has no additional effect on a fatigued or exhausted creature.",
                RequiredFeatIds = new List<int>() { CriticalFocus }
            });
            AllFeats.Add(new Feat()
            {
                Id = Toughness,
                Name = "Toughness",
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You gain +3 hit points. For every Hit Die you possess beyond 3, you gain an additional +1 hit point. If you have more than 3 Hit Dice, you gain +1 hit points whenever you gain a Hit Die (such as when you gain a level)."
            });
            AllFeats.Add(new Feat()
            {
                Id = TowerShieldProficiency,
                Name = "Tower Shield Proficiency",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When you use a tower shield, the shield's armor check penalty only applies to Strength and Dexterity-based skills.",
                RequiredFeatIds = new List<int>() { ShieldProficiency }
            });
            AllFeats.Add(new Feat()
            {
                Id = Trample,
                Name = "Trample",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When you attempt to overrun an opponent while mounted, your target may not choose to avoid you. Your mount may make one hoof attack against any target you knock down, gaining the standard +4 bonus on attack rolls against prone targets.",
                RequiredFeatIds = new List<int>() { MountedCombat },
                RequiredSkillRanks = new Dictionary<int, int>() { { Skills.Ride, 1 } }
            });
            AllFeats.Add(new Feat()
            {
                Id = TurnUndead,
                Name = "Turn Undead",
                RequiredClassFeature = RequiredClassFeature.ChannelEnergy,
                FeatCategory = FeatCategory.General,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can, as a standard action, use one of your uses of channel positive energy to cause all undead within 30 feet of you to flee, as if panicked. Undead receive a Will save to negate the effect. The DC for this Will save is equal to 10 + 1/2 your cleric level + your Charisma modifier. Undead that fail their save flee for 1 minute. Intelligent undead receive a new saving throw each round to end the effect. If you use channel energy in this way, it has no other effect (it does not heal or harm nearby creatures)."
            });
            AllFeats.Add(new Feat()
            {
                Id = TwoWeaponDefense,
                Name = "Two-Weapon Defense",
                FeatCategory = FeatCategory.Combat,
                RequiredDex = 15,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When wielding a double weapon or two weapons (not including natural weapons or unarmed strikes), you gain a +1 shield bonus to your AC. When you are fighting defensively or using the total defense action, this shield bonus increases to +2.",
                RequiredFeatIds = new List<int>() { TwoWeaponFighting }
            });
            AllFeats.Add(new Feat()
            {
                Id = TwoWeaponFighting,
                Name = "Two-Weapon Fighting",
                FeatCategory = FeatCategory.Combat,
                RequiredDex = 15,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Your penalties on attack rolls for fighting with two weapons are reduced. The penalty for your primary hand lessens by 2 and the one for your off hand lessens by 6. See Two-Weapon Fighting in Chapter 8."
            });
            AllFeats.Add(new Feat()
            {
                Id = TwoWeaponRend,
                Name = "Two-Weapon Rend",
                FeatCategory = FeatCategory.Combat,
                RequiredDex = 17,
                RequiredBaseAttackBonus = 11,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "If you hit an opponent with both your primary hand and your off-hand weapon, you deal an additional 1d10 points of damage plus 1-1/2 times your Strength modifier. You can only deal this additional damage once each round.",
                RequiredFeatIds = new List<int>() { DoubleSlice, TwoWeaponFighting, ImprovedTwoWeaponFighting }
            });
            AllFeats.Add(new Feat()
            {
                Id = Unseat,
                Name = "Unseat",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 1,
                RequiredStr = 13,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When charging an opponent while mounted and wielding a lance, resolve the attack as normal. If it hits, you may immediately make a free bull rush attempt in addition to the normal damage. If successful, the target is knocked off his horse and lands prone in a space adjacent to his mount that is directly away from you.",
                RequiredFeatIds = new List<int>() { MountedCombat, PowerAttack, ImprovedBullRush },
                RequiredSkillRanks = new Dictionary<int, int>() { { Skills.Ride, 1 } }
            });
            AllFeats.Add(new Feat()
            {
                Id = VitalStrike,
                Name = "Vital Strike",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 6,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When you use the attack action, you can make one attack at your highest base attack bonus that deals additional damage. Roll the weapon's damage dice for the attack twice and add the results together before adding bonuses from Strength, weapon abilities (such as flaming), precision-based damage, and other damage bonuses. These extra weapon damage dice are not multiplied on a critical hit, but are added to the total."
            });
            AllFeats.Add(new Feat()
            {
                Id = WeaponFinesse,
                Name = "Weapon Finesse",
                FeatCategory = FeatCategory.Combat,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "With a light weapon, elven curve blade, rapier, whip, or spiked chain made for a creature of your size category, you may use your Dexterity modifier instead of your Strength modifier on attack rolls. If you carry a shield, its armor check penalty applies to your attack rolls."
            });
            AllFeats.Add(new Feat()
            {
                Id = WeaponFocus,
                Name = "Weapon Focus",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You gain a +1 bonus on all attack rolls you make using the selected weapon."
            });
            AllFeats.Add(new Feat()
            {
                Id = WeaponSpecialization,
                Name = "Weapon Specialization",
                FeatCategory = FeatCategory.Combat,
                RequiredBaseAttackBonus = 4,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You gain a +2 bonus on all damage rolls you make using the selected weapon."
            });
            AllFeats.Add(new Feat()
            {
                Id = WhirlwindAttack,
                Name = "Whirlwind Attack",
                FeatCategory = FeatCategory.Combat,
                RequiredDex = 13,
                RequiredInt = 13,
                RequiredBaseAttackBonus = 4,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When you use the full-attack action, you can give up your regular attacks and instead make one melee attack at your highest base attack bonus against each opponent within reach. You must make a separate attack roll against each opponent. When you use the Whirlwind Attack feat, you also forfeit any bonus or extra attacks granted by other feats, spells, or abilities.",
                RequiredFeatIds = new List<int>() { CombatExpertise, Dodge, Mobility, SpringAttack }
            });
            AllFeats.Add(new Feat()
            {
                Id = WidenSpell,
                Name = "Widen Spell",
                FeatCategory = FeatCategory.Metamagic,
                RequiredSpellcasterLevel = 1,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "You can alter a burst, emanation, or spread-shaped spell to increase its area. Any numeric measurements of the spell's area increase by 100%. A widened spell uses up a spell slot three levels higher than the spell's actual level. Spells that do not have an area of one of these four sorts are not affected by this feat."
            });
            AllFeats.Add(new Feat()
            {
                Id = WindStance,
                Name = "Wind Stance",
                FeatCategory = FeatCategory.Combat,
                RequiredDex = 15,
                RequiredBaseAttackBonus = 6,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "If you move more than 5 feet this turn, you gain 20% concealment for 1 round against ranged attacks.",
                RequiredFeatIds = new List<int>() { Dodge }
            });
            #endregion

            #region Bestiary

            //public const int AbilityFocus = 1, AwesomeBlow = 2, CraftConstruct = 3, EmpowerSpellLikeAbility = 4,
            //    FlybyAttack = 5, Hover = 6, ImprovedNaturalArmor = 7, ImprovedNaturalAttack = 8,
            //    Multiattack = 9, QuickenSpellLikeAbility = 10, Snatch = 11, Wingover = 12;

            AllFeats.Add(new Feat()
            {
                Id = AbilityFocus,
                Name = "Ability Focus",
                FeatCategory = FeatCategory.Monster,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "Choose one of the creature's special attacks. Add +2 to the DC for all saving throws against the special attack on which the creature focuses.",
                AppliedBonus = 2,
                Stackable = true //stacks for different attacks
            });

            AllFeats.Add(new Feat()
            {
                Id = AwesomeBlow,
                Name = "Awesome Blow",
                FeatCategory = FeatCategory.Monster,
                Rarity = FeatureElement.CommonFeature,
                RequiredStr = 25,
                RequiredFeatIds = new List<int> { Feats.PowerAttack, Feats.ImprovedBullRush },
                RequiredSizeNumber = MonsterSizeCategories.Large,
                Explanation = "As a standard action, the creature may perform an awesome blow combat maneuver. If the creature's maneuver succeeds against a corporeal opponent smaller than itself, its opponent takes damage (typically slam damage plus Strength bonus) and is knocked flying 10 feet in a direction of the attacking creature's choice and falls prone. The attacking creature can only push the opponent in a straight line, and the opponent can't move closer to the attacking creature than the square it started in. If an obstacle prevents the completion of the opponent's move, the opponent and the obstacle each take 1d6 points of damage, and the opponent is knocked prone in the space adjacent to the obstacle."
            });

            AllFeats.Add(new Feat()
            {
                Id = CraftConstruct,
                Name = "Craft Construct",
                FeatCategory = FeatCategory.ItemCreation,
                Rarity = FeatureElement.SuperRareFeature,
                RequiredSpellcasterLevel = 5,
                RequiredFeatIds = new List<int> { Feats.CraftMagicArmsAndArmor, Feats.CraftWondrousItem},
                Explanation = "You can create any construct whose prerequisites you meet. The act of animating a construct takes one day for each 1,000 gp in its market price. To create a construct, you must use up raw materials costing half of its base price, plus the full cost of the basic body created for the construct. Each construct has a special section that summarizes its costs and other prerequisites. A newly created construct has average hit points for its Hit Dice."
            });

            AllFeats.Add(new Feat()
            {
                Id = EmpowerSpellLikeAbility,
                Name = "Empower Spell-Like Ability",
                FeatCategory = FeatCategory.Monster,
                Rarity = FeatureElement.UncommonFeature,
                RequiredSpellcasterLevel = 6,
                Explanation = "Choose one of the creature's spell-like abilities, subject to the restrictions below. The creature can use that ability as an empowered spell-like ability three times per day (or less, if the ability is normally usable only once or twice per day). When a creature uses an empowered spell-like ability, all variable, numeric effects of the spell-like ability are increased by half (+50%). Saving throws and opposed rolls are not affected. Spell-like abilities without random variables are not affected. The creature can only select a spell-like ability duplicating a spell with a level less than or equal to 1/2 its caster level (round down) - 2. For a summary, see the table in the description of the Quicken Spell-Like Ability feat on page 316.",
                Stackable = true //stacks for different attacks
            });

            AllFeats.Add(new Feat()
            {
                Id = FlybyAttack,
                Name = "Flyby Attack",
                FeatCategory = FeatCategory.Monster,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "When flying, the creature can take a move action and another standard action at any point during the move. The creature cannot take a second move action during a round when it makes a flyby attack.",
                RequiredClassFeature = RequiredClassFeature.FlySpeed
            });

            AllFeats.Add(new Feat()
            {
                Id = Hover,
                Name = "Hover",
                FeatCategory = FeatCategory.Monster,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "A creature with this feat can halt its movement while flying, allowing it to hover without needing to make a Fly skill check. If a creature of size Large or larger with this feat hovers within 20 feet of the ground in an area with lots of loose debris, the draft from its wings creates a hemispherical cloud with a radius of 60 feet. The winds generated can snuff torches, small campfires, exposed lanterns, and other small, open flames of non-magical origin. Clear vision within the cloud is limited to 10 feet. Creatures have concealment at 15 to 20 feet (20% miss chance). At 25 feet or more, creatures have total concealment (50% miss chance, and opponents cannot use sight to locate the creature).",
                RequiredClassFeature = RequiredClassFeature.FlySpeed
            });

            AllFeats.Add(new Feat()
            {
                Id = ImprovedNaturalArmor,
                Name = "Improved Natural Armor",
                FeatCategory = FeatCategory.Monster,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "The creature's natural armor bonus increases by +1.",
                AppliedBonus = 1,
                Stackable = true //stacks wholesale
            });

            AllFeats.Add(new Feat()
            {
                Id = ImprovedNaturalAttack,
                Name = "Improved Natural Attack",
                FeatCategory = FeatCategory.Monster,
                Rarity = FeatureElement.CommonFeature,
                RequiredBaseAttackBonus = 4,
                Explanation = "Choose one of the creature's natural attack forms (not an unarmed strike). The damage for this natural attack increases by one step on the following list, as if the creature's size had increased by one category. Damage dice increase as follows: 1d2, 1d3, 1d4, 1d6, 1d8, 2d6, 3d6, 4d6, 6d6, 8d6, 12d6. A weapon or attack that deals 1d10 points of damage increases as follows: 1d10, 2d8, 3d8, 4d8, 6d8, 8d8, 12d8.",
                AppliedBonus = 1,
                Stackable = true //stacks for different attacks
            });

            AllFeats.Add(new Feat()
            {
                Id = Multiattack,
                Name = "Multiattack",
                FeatCategory = FeatCategory.Monster,
                Rarity = FeatureElement.CommonFeature,
                Explanation = "The creature's secondary attacks with natural weapons take only a -2 penalty.",
                AppliedBonus = 3
            });

            AllFeats.Add(new Feat()
            {
                Id = QuickenSpellLikeAbility,
                Name = "Quicken Spell-Like Ability",
                FeatCategory = FeatCategory.Monster,
                Rarity = FeatureElement.UncommonFeature,
                RequiredSpellcasterLevel = 10,
                Explanation = "Choose one of the creature's spell-like abilities, subject to the restrictions described in this feat. The creature can use the chosen spell-like ability as a quickened spell-like ability three times per day (or less, if the ability is normally usable only once or twice per day). Using a quickened spell-like ability is a swift action that does not provoke an attack of opportunity. The creature can perform another action-including the use of another spell-like ability (but not another swift action)-in the same round that it uses a quickened spell-like ability. The creature may use only one quickened spell-like ability per round. The creature can only select a spell-like ability duplicating a spell with a level less than or equal to 1/2 its caster level (round down) - 4. For a summary, see the table on page 316. A spell-like ability that duplicates a spell with a casting time greater than 1 full round cannot be quickened.",
                Stackable = true //stacks for different attacks
            });

            AllFeats.Add(new Feat()
            {
                Id = Snatch,
                Name = "Snatch",
                FeatCategory = FeatCategory.Monster,
                Rarity = FeatureElement.CommonFeature,
                RequiredSizeNumber = MonsterSizeCategories.Huge,
                Explanation = "The creature can start a grapple when it hits with a claw or bite attack, as though it had the grab ability. If it grapples a creature three or more sizes smaller, it squeezes each round for automatic bite or claw damage with a successful grapple check. A snatched opponent held in the creature's mouth is not allowed a Reflex save against the creature's breath weapon, if it has one. The creature can drop a creature it has snatched as a free action or use a standard action to fling it aside. A flung creature travels 1d6 X 10 feet, and takes 1d6 points of damage per 10 feet traveled. If the creature flings a snatched opponent while flying, the opponent takes this amount or falling damage, whichever is greater."
            });

            AllFeats.Add(new Feat()
            {
                Id = Wingover,
                Name = "Wingover",
                FeatCategory = FeatCategory.Monster,
                Rarity = FeatureElement.UncommonFeature,
                Explanation = "Once each round, a creature with this feat can turn up to 180 degrees as a free action without making a Fly skill check. This free turn does not consume any additional movement from the creature.",
                RequiredClassFeature = RequiredClassFeature.FlySpeed
            });
            #endregion

        }
        public Feat GetFeat(int id)
        {
            return AllFeats.FirstOrDefault(a => a.Id == id);
        }
        public Feat GetRandomFeat(System.Random random)
        {
            int id = Table.GetRandom(random);
            return GetFeat(id);
        }
    }
}
