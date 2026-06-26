using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonsterGenerator2_Net47.Models.Data
{
    //+4 racial bonus on bull rush, trip, disarm, grapple, (stackable)
    //+1 racial bonus on attack rolls and weapon damage rolls (stackable)
    public class MonsterAbilities
    {
        //@HD @CR @DCCON @DCDEX @DCCHA @DCSTR @DCINT @DCWIS @MONNAME @MONNAMES @MONNAME_S @(2*HD) @MONCHA @MONSTR @MONDEX @MONCON @MONINT @MONWIS
        //@MONATTACKDIE @MONATTACKDIEBIG @MONATTACKDIEBIG2 @MONATTACKDIESMALL @PRIMARYATTACK @AC @ACFLAT @ACSTOMACH @HP
        //ABILITYDAMAGELOW ABILITYDAMAGEHIGH ATTACKBONUSMELEE ATTACKBONUSRANGED SPELLLEVELHIGH SPELLLEVELMID SPELLLEVELLOW
        //BREATHLINE BREATHCONE NEGATIVELEVELS ALIGNMENT CMD
        //attack die = claw/ slam x2, attackdiesmall -1, attackdiebig = +1, attackdiebig2 = +2
        public const int LowLightVision = 1, Darkvision60 = 2, Darkvision90 = 3, Darkvision120 = 4, LegStability = 5,
            BonusVsPoison4 = 6, SpeakWithAnimalsSu = 7, Truespeech = 8, LayOnHandsRacial = 9, BaseLanguagesCelestialInfernalDraconic = 10,
            ProtectiveAura = 11, MenaceAura = 12, TeleportSp = 13, ElusiveAura = 14, BonusPerception4 = 15,
            BonusEscapeArtist6 = 16, BonusPerception8 = 17, AlignedWeaponsLaw = 18, AlignedWeaponsChaos = 19, AlignedWeaponsGood = 20,
            AlignedWeaponsEvil = 21, MagicWeapons = 22, SummonKin = 23, Telepathy = 24, Regeneration2 = 25,
            Regeneration5 = 26, Regeneration10 = 27, Regeneration15 = 28, Regeneration20 = 29, FastHeal2 = 30,
            FastHeal5 = 31, FastHeal10 = 32, FastHeal15 = 33, FastHeal20 = 34, SummonKinGreater = 35,
            Blindsense30 = 36, Blindsense60 = 37, Ruinous = 38, Unstoppable = 39, SeeInDarkness = 40,
            LightBlindness = 41, PoisonUse = 42, DeathThroesBlind = 43, DeathThroesAbilityDamage = 44, DeathThroesExplosion = 45,
            LightSensitivity = 46, FaithStealingStrike = 47, HereticalSoul = 48, Tremorsense60 = 49, Blindsight10 = 50,
            Blindsight30 = 51, CorrosiveBlood = 52, DeathThroesAcid = 53, HeatAdaptability = 54, HiveMind = 55,
            Incorporeal = 56, UnnervingGaze = 57, PassWithoutTraceSLA = 58, ChangeShapePlant = 59, PlantSpeech = 60,
            VerdantBurst = 66, DeathGrasp = 67, DesecratingAura = 68, ChannelNegativeEnergy = 69, Darksense = 70,
            LightAversion = 71, ChangeShapeHumanoid = 72, Constrict = 73, Grab = 74, FreedomOfMovement = 75,
            AmorphousAnatomy = 76, ChangeShapeProtean = 77, SpiritTouch = 78, SpiritSense = 79, BonusDisguise8 = 80,
            BonusBluff4 = 81, DetectThoughts = 82, HorrificAppearance = 83, CastSpellsSorcerorM3 = 84, CastSpellsSorcerorHalf = 85,
            CastSpellsSorcerorFull = 86, ChangeShapeHumanoidAny = 87, CastSpellsWizardM3 = 88, CastSpellsWizardHalf = 89, CastSpellsWizardFull = 90,
            ChannelPositiveEnergy = 91, Cfwfq = 92, CastSpellsClericM3 = 93, CastSpellsClericHalf = 94, CastSpellsClericFull = 95,
            afwfwq = 96, Chanqfqfq = 97, CastSpellsDruidM3 = 98, CastSpellsDruidHalf = 99, CastSpellsDruidFull = 100,
            Distraction = 101, SwarmDistraction = 102, UncannyDodge = 103, Evasion = 104, ImprovedUncannyDodge = 105,
            ImprovedEvasion = 106, MinusOneNaturalArmor = 107, Plus4VsTrip = 108, Plus4VsDisarm = 109, Plus4VsGrapple = 110;

        public const int MasterworkWeapon = 111, Trample = 112, Engulf = 113, Plus4VsDirtyTricks = 114, GhostTouchAttacks = 115, //HinderSpellcasting
            UndersizedWeapons = 116, OversizedWeapons = 117, LightShield = 118, HeavyShield = 119, TowerShield = 120,
            PlusOneNaturalArmor = 121, Plus2VsChannel = 122, Bravery2 = 123, TrapSense2 = 124, Plus2VsEnchantment = 125,
            Plus4VsDisease = 126, KnowledgeStealingStrike = 127, ElementalDamage1d6 = 128, SneakAttack1d6 = 129, Plus1Weapon = 130,
            Plus2Weapon = 131, Plus3Weapon = 132, Plus5Weapon = 133, Rage = 134, GreaterRage = 135,
            Crush = 136, SwallowWhole = 137, FastSwallow = 138, PowerfulCharge = 139, Rend = 140,
            Rake = 141, Web = 142, ConDrainOnAttack = 143, DiseaseOnAttackCon = 144, DiseaseOnAttackStr = 145,
            SlowConPoisonOnAttack = 146, FastConPoisonOnAttack = 147, ElementalRaySLA = 148, FearConeSLA = 149, SpellLikeAbilityA = 150,
            SpellLikeAbilityB = 151, SpellLikeAbilityC = 152, Plus4VsBullRush = 153, BloodDrain = 154, BreathWeaponLine = 155,
            IgnitingGaze = 156, FrostNova = 157, Smite = 158, Trip = 159, ParalysisOnAttackEx = 160,
            Pounce = 161, ParalysisOnAttackSu = 162, BreathWeaponCone = 163, GreaterBreathWeaponLine = 164, GreaterBreathWeaponCone = 165,
            EnergyDrainOnAttack = 166, WoundOnAttack = 167, BleedOnAttack = 168, StunOnAttack = 169, SonicWave = 170,
            ForceWave = 171, BlindingGaze = 172, DeathGaze = 173, PetrifyingGaze = 174, DominatingGaze = 175,
            Plus4PrimarySkill = 176, Plus4SecondarySkill = 177, Bonus6Stealth = 178, Track = 179, TracklessStep = 180,
            Camouflage = 181, Scent = 182, Amphibious = 183, Bonus6Perception = 184, Stench = 185,
            AllAroundVision = 186, Tremorsense30 = 187, HideInPlainSight = 188, EarthGlide = 189, SwiftReactions = 190,
            TeleportSLA = 191, FearAura = 192, TrueSeeing = 193, NaturalInvisibility = 194, SlowStrPoisonOnAttack = 195,
            FastStrPoisonOnAttack = 196, SpellLikeAbilityAA = 197, SpellLikeAbilityBB = 198, CharmingGaze = 199, DisintegrationAura = 200,
            PlusTwoDodge = 201, BardicPerformanceInspire = 202, BardicPerformanceFascinate = 203, DeadlyCritical = 204, DeathOnAttack = 205,
            PetrifyOnAttack = 206, ImprovedAttackDamage = 207, DominateOnAttack = 208;

        //light shield, heavy shield, tower shield
        public const int ImmunityCold = 1100, ImmunityElectricity = 1101, ImmunityFire = 1102, ImmunityAcid = 1103,
            ImmunitySonic = 1104, ImmunityPoison = 1105, ImmunityCriticalHits = 1106, ImmunityPetrification = 1107,
            ImmunitySleep = 1108, ImmunityParalysis = 1109, ImmunityMindAffecting = 1110, ImmunityPolymorph = 1111,
            ImmunityStun = 1112, ImmunityFlanking = 1113, ImmunityPrecisionDamage = 1114, ImmunityBleed = 1115,
            ImmunityDisease = 1116, ImmunityDeathEffects = 1117, ImmunityNecromancyEffects = 1118, ImmunityCurses = 1119, ImmunityAging = 1120,
            ImmunityNegativeLevels = 1121, ImmunityAbilityDamage = 1122, ImmunityPermanentWounds = 1123, ImmunityWeaponDamage = 1124, ImmunityStagger = 1125,
            ImmunityTargetedEffects = 1126, ImmunityMagic = 1127;

        public const int VulnerabilityCold = 1200, VulnerabilityElectricity = 1201, VulnerabilityFire = 1202, VulnerabilityAcid = 1203,
            VulnerabilitySonic = 1204, VulnerabilityPoison = 1205, VulnerabilityCriticalHits = 1206, VulnerableMind = 1207,
            VulnerableWish = 1208, VulnerableToConsecration = 1209, VulnerabilityAreaEffect = 1210, VulnerableBludgeoning = 1211,
            VulnerableSlashing = 1212, VulnerablePiercing = 1213;

        public const int ResistCold10 = 1301, ResistElectricity10 = 1302, ResistFire10 = 1303, ResistAcid10 = 1304, ResistSonic10 = 1305,
            ResistCold30 = 1306, ResistElectricity30 = 1307, ResistFire30 = 1308, ResistAcid30 = 1309, ResistSonic30 = 1310,
            SpellResist5Plus = 1311, SpellResist8Plus = 1313, SpellResist11Plus = 1314, SpellResist13Plus = 1315,
            DR5Epic = 1316, DR5Magic = 1317, SpellResist15Plus = 1318, HalfDamageSlashing = 1320, // DR5GoodPiercing = 1319,
            HalfDamagePiercing = 1321, HalfDamageBludgeoning = 1322, DR5Bludgeon = 1323, DR5Slash = 1324, DR5Pierce = 1325,
            DR5Good = 1326, DR5Evil = 1327, DR5Law = 1328, DR5Chaos = 1329, DR5All = 1330,
            DR5Silvered = 1331, DR5Adamantine = 1332, DR5ColdIron = 1333, HalfDamageFromWeapons = 1334;//, DR5Chaos = 1329, DR5All = 1330, ;

        public const int Envisaging = 400, ExtensionOfAll = 401, VoidForm = 402, MergeWithWard = 403, Ward = 404, Winding = 405;

        public const int FlySpeed = 500, SwimSpeed = 501, BurrowSpeed = 502, ClimbSpeed = 503;

        public const int MindlessInt = 700, NoConstitution = 701, CharismaReplacesCon = 702, BonusFeat = 703, Blind = 704, CannotBeRaised = 705,
            UndeadTraits = 706, ConstructTraits = 707, AutomatonCore = 708, Intelligent = 709, NegativeEnergyAffinity = 710,
            UnlivingNature = 711, NativeOutsider = 712, ShortReach = 713, LongReach = 714, SwarmAttack = 715,
            SwarmDamageResistance = 716, SwarmSpaceReach = 717, SwarmTraits = 718, MinusTenFootMove = 719, PlusOneAttribute = 720,
            PlusTenFeetMove = 721, DoesNotBreathe = 722, DoesNotEat = 723, DoesNotSleep = 724;

        public const int DoubleTreasure = 800, TripleTreasure = 801, QuadroupleTreasure = 802, HalfTreasure = 803, NoTreasure = 804;

        public List<Ability> Abilities;

        private RandomGenerationTable Table;

        private static MonsterAbilities Instance;
        public static MonsterAbilities GetInstance()
        {
            if (Instance == null)
                Instance = new MonsterAbilities();

            return Instance;
        }


        public MonsterAbilities()
        {
            PopulateAbilities();
            Table = new RandomGenerationTable();
            Table.FillRandomGenerationTable<Ability>(Abilities);
        }

        private void PopulateAbilities()
        {
            Abilities = new List<Ability>();
            Abilities.Add(new Ability()
            {
                Id = LowLightVision,
                Name = "Low Light Vision",
                Explanation = "Characters with low-light vision have eyes that are so sensitive to light that they can see twice as far as normal in dim light. Low-light vision is color vision. A spellcaster with low-light vision can read a scroll as long as even the tiniest candle flame is next to him as a source of light.\nCharacters with low - light vision can see outdoors on a moonlit night as well as they can during the day.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Darkvision60,
                Name = "Darkvision 60 ft",
                Explanation = "Darkvision is the extraordinary ability to see with no light source at all, out to a range specified for the creature. Darkvision is black-and-white only (colors cannot be discerned). It does not allow characters to see anything that they could not see otherwise—invisible objects are still invisible, and illusions are still visible as what they seem to be. Likewise, darkvision subjects a creature to gaze attacks normally. The presence of light does not spoil darkvision.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Darkvision90,
                Name = "Darkvision 90 ft",
                Explanation = "Darkvision is the extraordinary ability to see with no light source at all, out to a range specified for the creature. Darkvision is black-and-white only (colors cannot be discerned). It does not allow characters to see anything that they could not see otherwise—invisible objects are still invisible, and illusions are still visible as what they seem to be. Likewise, darkvision subjects a creature to gaze attacks normally. The presence of light does not spoil darkvision.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Darkvision120,
                Name = "Darkvision 120 ft",
                Explanation = "Darkvision is the extraordinary ability to see with no light source at all, out to a range specified for the creature. Darkvision is black-and-white only (colors cannot be discerned). It does not allow characters to see anything that they could not see otherwise—invisible objects are still invisible, and illusions are still visible as what they seem to be. Likewise, darkvision subjects a creature to gaze attacks normally. The presence of light does not spoil darkvision.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = LegStability,
                Name = "Leg Stability",
                Explanation = "A creature with 4 or more legs can carry 50% more weight than a normal (2 legged) creature. They also recieve +4 to their CMD vs. trip attacks.",
                InlinePrintout = "@(CMD + 4) vs. trip",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseCMDBonus,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                BonusStatType = BonusStat.CmdConditionalBonus,
                AppliedBonus = 4
            });

            Abilities.Add(new Ability()
            {
                Id = BonusVsPoison4,
                Name = "+4 Save vs Poison",
                Explanation = "Gains a +4 to all saving throws vs. Poison.",
                InlinePrintout = "+4 vs. poison",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseSaveConditional,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Plus4VsDisease,
                Name = "+4 Save vs Disease",
                Explanation = "Gains a +4 to all saving throws vs. Disease.",
                InlinePrintout = "+4 vs. disease",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseSaveConditional,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Plus2VsEnchantment,
                Name = "+2 Save vs Enchantment",
                Explanation = "Gains a +2 to all saving throws vs. Enchantment spells.",
                InlinePrintout = "+2 vs. enchantment",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseSaveConditional,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Plus2VsChannel,
                Name = "+2 Save vs Channel Energy",
                Explanation = "Gains a +2 to all saving throws vs. Channel Energy.",
                InlinePrintout = "+2 vs. channel energy",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseSaveConditional,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                //todo: language abilities separate print
                Id = SpeakWithAnimalsSu,
                Name = "Speak with Animals",
                Explanation = "This ability works like Speak with Animals (caster level equal to the agathion’s Hit Dice): but is a free action and does not require sound.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.LanguageQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                //todo: language abilities separate print
                Id = Truespeech,
                Name = "Truespeech",
                Explanation = "Can speak with any creature that has a language, as though using a tongues spell (caster level equal to monster’s Hit Dice). This ability is always active.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.LanguageQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                //todo: include better explanation and %hitdice or something
                Id = LayOnHandsRacial,
                Name = "Lay On Hands",
                Explanation = "Lay on Hands as a paladin whose level equals the monster's hit dice.",
                ClassFeatureQualified = RequiredClassFeature.LayOnHands,
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                //todo: include better explanation and %hitdice or something
                Id = BaseLanguagesCelestialInfernalDraconic,
                Name = "Celestial Infernal Draconic",
                Explanation = "The monster's base languages are celestial, infernal, and draconic.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.LanguageQuality,
                PrintExplanationLevel = PrintExplanationLevel.Never,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                //todo: include better explanation and %hitdice or something
                Id = ProtectiveAura,
                Name = "Protective Aura",
                Explanation = "Against attacks made or effects created by evil creatures, this ability provides a +4 deflection bonus to AC and a +4 resistance bonus on saving throws to anyone within 20 feet of the angel. Otherwise, it functions as a magic circle against evil effect and a lesser globe of invulnerability, both with a radius of 20 feet and caster level @HD. The defensive benefits from the circle are not included in an angel’s statistics block.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.AuraQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = MenaceAura,
                Name = "Aura of Menace",
                Explanation = "A righteous aura surrounds archons that fight or get angry. Any hostile creature within a 20-foot radius of an archon must succeed on a DC @(DCCHA + 2) Will save to resist its effects. Those who fail take a –2 penalty on attacks, AC, and saves for 24 hours or until they successfully hit the archon that generated the aura. A creature that has resisted or broken the effect cannot be affected again by the same archon’s aura for 24 hours.",
                InlinePrintout = "Aura of Menace (DC @(DCCHA + 2))",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.AuraQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = TeleportSp,
                Name = "Teleport (Sp)",
                Explanation = "Can use greater teleport at will, as the spell (caster level 14th), except that the creature can transport only itself and up to 50 pounds of carried objects.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = BonusPerception4,
                Name = "+4 Perception",
                Explanation = "Grants a +4 racial bonus to perception skill checks.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                AppliedSkillId = Skills.Perception,
                AppliedBonus = 4
            });

            Abilities.Add(new Ability()
            {
                Id = BonusPerception8,
                Name = "+8 Perception",
                Explanation = "Grants a +8 racial bonus to perception skill checks.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                AppliedSkillId = Skills.Perception,
                AppliedBonus = 8
            });

            Abilities.Add(new Ability()
            {
                Id = BonusEscapeArtist6,
                Name = "+6 Escape Arist",
                Explanation = "Grants a +6 racial bonus to escape artist skill checks.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                AppliedSkillId = Skills.EscapeArtist,
                AppliedBonus = 6
            });

            Abilities.Add(new Ability()
            {
                Id = AlignedWeaponsLaw,
                Name = "Lawful Aligned Weapons",
                Explanation = "Natural weapons, as well as any weapons @MONNAME wields, are treated as lawful for the purpose of overcoming damage reduction.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });
            Abilities.Add(new Ability()
            {
                Id = AlignedWeaponsChaos,
                Name = "Chaotic Aligned Weapons",
                Explanation = "Natural weapons, as well as any weapons @MONNAME wields, are treated as chaotic for the purpose of overcoming damage reduction.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });
            Abilities.Add(new Ability()
            {
                Id = AlignedWeaponsGood,
                Name = "Good Aligned Weapons",
                Explanation = "Natural weapons, as well as any weapons @MONNAME wields, are treated as good for the purpose of overcoming damage reduction.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });
            Abilities.Add(new Ability()
            {
                Id = AlignedWeaponsEvil,
                Name = "Evil Aligned Weapons",
                Explanation = "Natural weapons, as well as any weapons @MONNAME wields, are treated as evil for the purpose of overcoming damage reduction.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });
            Abilities.Add(new Ability()
            {
                Id = MagicWeapons,
                Name = "Magic Weapons",
                Explanation = "Natural weapons, as well as any weapons @MONNAME wields, are treated as magical for the purpose of overcoming damage reduction.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });
            Abilities.Add(new Ability()
            {
                Id = SummonKin,
                Name = "Summon Kin",
                Explanation = "Choose a subtype associated with the monster. As though it were using the spell 'Summon Monster', Once per day, it can summon another monster with the same subtype 1-2 CR lower with 50% success chance, or 1d4 monsters 3+ CR lower with 100% success chance.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });
            Abilities.Add(new Ability()
            {
                Id = SummonKinGreater,
                Name = "Summon Kin (Greater)",
                Explanation = "Choose a subtype associated with the monster. As though it were using the spell 'Summon Monster', Once per day, it can summon another monster with the same subtype with a 50% success chance, a monster 1-2 CR lower with 100% success chance, or 1d4 monsters 3+ CR lower with 100% success chance.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });
            Abilities.Add(new Ability()
            {
                Id = Telepathy,
                Name = "Telepathy 100ft",
                Explanation = "The creature can mentally communicate with any other creature within a certain range (specified in the creature’s entry, usually 100 feet) that has a language. It is possible to address multiple creatures at once telepathically, although maintaining a telepathic conversation with more than one creature at a time is just as difficult as simultaneously speaking and listening to multiple people at the same time",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.LanguageQuality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });
            Abilities.Add(new Ability()
            {
                Id = Regeneration2,
                Name = "Regeneration 2",
                Explanation = "A creature with this ability is difficult to kill. Creatures with regeneration heal damage at a fixed rate, as with fast healing, but they cannot die as long as their regeneration is still functioning (although creatures with regeneration still fall unconscious when their hit points are below 0). Certain attack forms, typically fire and acid, cause a creature’s regeneration to stop functioning on the round following the attack. During this round, the creature does not heal any damage and can die normally. The creature’s descriptive text describes the types of damage that cause the regeneration to cease functioning. Attack forms that don’t deal hit point damage are not healed by regeneration. Regeneration also does not restore hit points lost from starvation, thirst, or suffocation. Regenerating creatures can regrow lost portions of their bodies and can reattach severed limbs or body parts if they are brought together within 1 hour of severing. Severed parts that are not reattached wither and die normally.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseHitPoints,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });
            Abilities.Add(new Ability()
            {
                Id = Regeneration5,
                Name = "Regeneration 5",
                Explanation = "A creature with this ability is difficult to kill. Creatures with regeneration heal damage at a fixed rate, as with fast healing, but they cannot die as long as their regeneration is still functioning (although creatures with regeneration still fall unconscious when their hit points are below 0). Certain attack forms, typically fire and acid, cause a creature’s regeneration to stop functioning on the round following the attack. During this round, the creature does not heal any damage and can die normally. The creature’s descriptive text describes the types of damage that cause the regeneration to cease functioning. Attack forms that don’t deal hit point damage are not healed by regeneration. Regeneration also does not restore hit points lost from starvation, thirst, or suffocation. Regenerating creatures can regrow lost portions of their bodies and can reattach severed limbs or body parts if they are brought together within 1 hour of severing. Severed parts that are not reattached wither and die normally.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.DefenseHitPoints,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });
            Abilities.Add(new Ability()
            {
                Id = Regeneration10,
                Name = "Regeneration 10",
                Explanation = "A creature with this ability is difficult to kill. Creatures with regeneration heal damage at a fixed rate, as with fast healing, but they cannot die as long as their regeneration is still functioning (although creatures with regeneration still fall unconscious when their hit points are below 0). Certain attack forms, typically fire and acid, cause a creature’s regeneration to stop functioning on the round following the attack. During this round, the creature does not heal any damage and can die normally. The creature’s descriptive text describes the types of damage that cause the regeneration to cease functioning. Attack forms that don’t deal hit point damage are not healed by regeneration. Regeneration also does not restore hit points lost from starvation, thirst, or suffocation. Regenerating creatures can regrow lost portions of their bodies and can reattach severed limbs or body parts if they are brought together within 1 hour of severing. Severed parts that are not reattached wither and die normally.",
                PointsCost = 4,
                DisplayCategory = DisplayCategory.DefenseHitPoints,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                MinimumChallengeRating = 5
            });
            Abilities.Add(new Ability()
            {
                Id = Regeneration15,
                Name = "Regeneration 15",
                Explanation = "A creature with this ability is difficult to kill. Creatures with regeneration heal damage at a fixed rate, as with fast healing, but they cannot die as long as their regeneration is still functioning (although creatures with regeneration still fall unconscious when their hit points are below 0). Certain attack forms, typically fire and acid, cause a creature’s regeneration to stop functioning on the round following the attack. During this round, the creature does not heal any damage and can die normally. The creature’s descriptive text describes the types of damage that cause the regeneration to cease functioning. Attack forms that don’t deal hit point damage are not healed by regeneration. Regeneration also does not restore hit points lost from starvation, thirst, or suffocation. Regenerating creatures can regrow lost portions of their bodies and can reattach severed limbs or body parts if they are brought together within 1 hour of severing. Severed parts that are not reattached wither and die normally.",
                PointsCost = 5,
                DisplayCategory = DisplayCategory.DefenseHitPoints,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                MinimumChallengeRating = 7
            });
            Abilities.Add(new Ability()
            {
                Id = Regeneration20,
                Name = "Regeneration 20",
                Explanation = "A creature with this ability is difficult to kill. Creatures with regeneration heal damage at a fixed rate, as with fast healing, but they cannot die as long as their regeneration is still functioning (although creatures with regeneration still fall unconscious when their hit points are below 0). Certain attack forms, typically fire and acid, cause a creature’s regeneration to stop functioning on the round following the attack. During this round, the creature does not heal any damage and can die normally. The creature’s descriptive text describes the types of damage that cause the regeneration to cease functioning. Attack forms that don’t deal hit point damage are not healed by regeneration. Regeneration also does not restore hit points lost from starvation, thirst, or suffocation. Regenerating creatures can regrow lost portions of their bodies and can reattach severed limbs or body parts if they are brought together within 1 hour of severing. Severed parts that are not reattached wither and die normally.",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.DefenseHitPoints,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                MinimumChallengeRating = 9
            });
            Abilities.Add(new Ability()
            {
                Id = FastHeal2,
                Name = "Fast Healing 2",
                Explanation = "A creature with the fast healing special quality regains hit points at an exceptional rate, usually 1 or more hit points per round, as given in the creature’s entry. Except where noted here, fast healing is just like natural healing. Fast healing does not restore hit points lost from starvation, thirst, or suffocation, nor does it allow a creature to regrow lost body parts. Unless otherwise stated, it does not allow lost body parts to be reattached. Fast healing continues to function (even at negative hit points) until a creature dies, at which point the effects of fast healing end immediately.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseHitPoints,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });
            Abilities.Add(new Ability()
            {
                Id = FastHeal5,
                Name = "Fast Healing 5",
                Explanation = "A creature with the fast healing special quality regains hit points at an exceptional rate, usually 1 or more hit points per round, as given in the creature’s entry. Except where noted here, fast healing is just like natural healing. Fast healing does not restore hit points lost from starvation, thirst, or suffocation, nor does it allow a creature to regrow lost body parts. Unless otherwise stated, it does not allow lost body parts to be reattached. Fast healing continues to function (even at negative hit points) until a creature dies, at which point the effects of fast healing end immediately.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.DefenseHitPoints,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });
            Abilities.Add(new Ability()
            {
                Id = FastHeal10,
                Name = "Fast Healing 10",
                Explanation = "A creature with the fast healing special quality regains hit points at an exceptional rate, usually 1 or more hit points per round, as given in the creature’s entry. Except where noted here, fast healing is just like natural healing. Fast healing does not restore hit points lost from starvation, thirst, or suffocation, nor does it allow a creature to regrow lost body parts. Unless otherwise stated, it does not allow lost body parts to be reattached. Fast healing continues to function (even at negative hit points) until a creature dies, at which point the effects of fast healing end immediately.",
                PointsCost = 4,
                DisplayCategory = DisplayCategory.DefenseHitPoints,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                MinimumChallengeRating = 5
            });
            Abilities.Add(new Ability()
            {
                Id = FastHeal15,
                Name = "Fast Healing 15",
                Explanation = "A creature with the fast healing special quality regains hit points at an exceptional rate, usually 1 or more hit points per round, as given in the creature’s entry. Except where noted here, fast healing is just like natural healing. Fast healing does not restore hit points lost from starvation, thirst, or suffocation, nor does it allow a creature to regrow lost body parts. Unless otherwise stated, it does not allow lost body parts to be reattached. Fast healing continues to function (even at negative hit points) until a creature dies, at which point the effects of fast healing end immediately.",
                PointsCost = 5,
                DisplayCategory = DisplayCategory.DefenseHitPoints,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                MinimumChallengeRating = 7
            });
            Abilities.Add(new Ability()
            {
                Id = FastHeal20,
                Name = "Fast Healing 20",
                Explanation = "A creature with the fast healing special quality regains hit points at an exceptional rate, usually 1 or more hit points per round, as given in the creature’s entry. Except where noted here, fast healing is just like natural healing. Fast healing does not restore hit points lost from starvation, thirst, or suffocation, nor does it allow a creature to regrow lost body parts. Unless otherwise stated, it does not allow lost body parts to be reattached. Fast healing continues to function (even at negative hit points) until a creature dies, at which point the effects of fast healing end immediately.",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.DefenseHitPoints,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                MinimumChallengeRating = 9
            });

            Abilities.Add(new Ability()
            {
                Id = Blindsense30,
                Name = "Blindsense 30ft",
                Explanation = "Blindsense lets a creature notice things it cannot see, but without the precision of blindsight. The creature with blindsense usually does not need to make Perception checks to notice and locate creatures within range of its blindsense ability, provided that it has line of effect to that creature. Any opponent that cannot be seen has total concealment (50% miss chance) against a creature with blindsense, and the blindsensing creature still has the normal miss chance when attacking foes that have concealment. Visibility still affects the movement of a creature with blindsense. A creature with blindsense is still denied its Dexterity bonus to Armor Class against attacks from creatures it cannot see.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Blindsense60,
                Name = "Blindsense 60ft",
                Explanation = "Blindsense lets a creature notice things it cannot see, but without the precision of blindsight. The creature with blindsense usually does not need to make Perception checks to notice and locate creatures within range of its blindsense ability, provided that it has line of effect to that creature. Any opponent that cannot be seen has total concealment (50% miss chance) against a creature with blindsense, and the blindsensing creature still has the normal miss chance when attacking foes that have concealment. Visibility still affects the movement of a creature with blindsense. A creature with blindsense is still denied its Dexterity bonus to Armor Class against attacks from creatures it cannot see.",
                PointsCost = 4,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Ruinous,
                Name = "Ruinous",
                Explanation = "The creature's natural attacks penetrate damage reduction as if they were epic and magic, and ignore up to 20 points of hardness on objects struck. As a swift action, whenever it strikes a creature or object with a spell effect in place, it can attempt to dispel one randomly determined spell effect on that creature as if with a greater dispel magic (CL 20th).",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.Offense,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                MinimumChallengeRating = 7
            });

            Abilities.Add(new Ability()
            {
                Id = Unstoppable,
                Name = "Unstoppable",
                Explanation = "If the creature starts its turn suffering from any or all of the following conditions, it recovers from them at the end of its turn: blind, confused, dazed, deafened, dazzled, exhausted, fatigued, nauseated, sickened, slowed, staggered, and stunned.",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                MinimumChallengeRating = 7
            });

            Abilities.Add(new Ability()
            {
                Id = SeeInDarkness,
                Name = "See In Darkness",
                Explanation = "The creature can see perfectly in darkness of any kind, including that created by deeper darkness.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = LightBlindness,
                Name = "Light Blindness",
                Explanation = "Creatures with light blindness are blinded for 1 round if exposed to bright light, such as sunlight or the daylight spell. Such creatures are dazzled as long as they remain in areas of bright light.",
                PointsCost = -2,
                DisplayCategory = DisplayCategory.DefenseWeakness,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = LightSensitivity,
                Name = "Light Sensitivity",
                Explanation = "Creatures with light sensitivity are dazzled in areas of bright sunlight or within the radius of a daylight spell.",
                PointsCost = -1,
                DisplayCategory = DisplayCategory.DefenseWeakness,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = PoisonUse,
                Name = "Poison Use",
                Explanation = "A monster with this talent no longer risk poisoning herself when applying poison to a weapon.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = DeathThroesBlind,
                Name = "Death Throes (Blind)",
                Explanation = "When a the creature is slain, its body bursts into a blinding light. All creatures within a 10-foot burst must make a DC @DCCON Reflex save or be blinded for 1d6 rounds.",
                InlinePrintout = "Death Throes (DC (@DCCON), 1d6 rounds)",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = DeathThroesAbilityDamage,
                Name = "Death Throes (Poison)",
                Explanation = "When a the creature is slain, its body bursts into a poisonous shower of blood. All creatures within a 10-foot burst must make a DC @DCCON Fortitude save or suffer 1d4 Strength damage. This is a fast poison that lasts up to 6 rounds, and is cured on one save.",
                InlinePrintout = "Death Throes (DC (@DCCON), 1d4 Strength)",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = DeathThroesExplosion,
                Name = "Death Throes (Explosion)",
                Explanation = "When a the creature is slain, its body bursts in a violent explosion. All creatures within a 20-foot burst suffer @CRd6 fire damage. A successful DC @DCCON Reflex save halves the damage.",
                InlinePrintout = "Death Throes (DC @DCCON, @CRd6 fire)",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = FaithStealingStrike,
                Name = "Faith Stealing Strike",
                Explanation = "When the monster's natural attack or melee weapon damages a creature capable of casting divine spells, that creature must make a DC @DCCHA Will saving throw or be unable to cast any divine spells for 1 round. Once a creature makes this save, it is immune to further faith-stealing strikes from this monster for 24 hours.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = HereticalSoul,
                Name = "Heretical Soul",
                Explanation = "Gains a +4 bonus on saving throws against divine spells. In addition, any attempts to scry on the monster using divine magic automatically fail. The caster can see the scryed area normally, but the monster simply does not appear.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Tremorsense30,
                Name = "Tremorsense (30ft)",
                Explanation = "A creature with tremorsense is sensitive to vibrations in the ground and can automatically pinpoint the location of anything that is in contact with the ground. Aquatic creatures with tremorsense can also sense the location of creatures moving through water.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Tremorsense60,
                Name = "Tremorsense (60ft)",
                Explanation = "A creature with tremorsense is sensitive to vibrations in the ground and can automatically pinpoint the location of anything that is in contact with the ground. Aquatic creatures with tremorsense can also sense the location of creatures moving through water.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Blindsight10,
                Name = "Blindsight (10ft)",
                Explanation = "This ability is similar to blindsense, but is far more discerning. Using nonvisual senses, such as sensitivity to vibrations, keen smell, acute hearing, or echolocation, a creature with blindsight maneuvers and fights as well as a sighted creature. Invisibility, darkness, and most kinds of concealment are irrelevant, though the creature must have line of effect to a creature or object to discern that creature or object. The ability’s range is specified in the creature’s descriptive text. The creature usually does not need to make Perception checks to notice creatures within range of its blindsight ability. Unless noted otherwise, blindsight is continuous, and the creature need do nothing to use it.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Blindsight30,
                Name = "Blindsight (30ft)",
                Explanation = "This ability is similar to blindsense, but is far more discerning. Using nonvisual senses, such as sensitivity to vibrations, keen smell, acute hearing, or echolocation, a creature with blindsight maneuvers and fights as well as a sighted creature. Invisibility, darkness, and most kinds of concealment are irrelevant, though the creature must have line of effect to a creature or object to discern that creature or object. The ability’s range is specified in the creature’s descriptive text. The creature usually does not need to make Perception checks to notice creatures within range of its blindsight ability. Unless noted otherwise, blindsight is continuous, and the creature need do nothing to use it.",
                PointsCost = 4,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = CorrosiveBlood,
                Name = "Corrosive Blood",
                Explanation = "A @MONNAME's blood is highly caustic. Every time the @MONNAME is damaged by a piercing or slashing weapon, the attacking creature takes acid damage according to the table below (or double damage if the attack is a critical hit). Using a reach weapon does not endanger the attacker in this way. If the creature has the swallow whole ability, it adds this damage to its swallow whole damage.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = DeathThroesAcid,
                Name = "Death Throes (Acid)",
                Explanation = "When a @MONNAME dies, it exudes a pool of its corrosive blood in the space it occupies. This pool deals @(HD)d6 points of acid damage for 3 rounds to objects and creatures in those squares with a DC @DCCON Reflex save for half damage. This acid damages whatever surface it is on, and if it deals enough damage to destroy the surface, the acid falls down to any subsequent floor below and continues to deal damage. The save DC is Constitution-based.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = HeatAdaptability,
                Name = "Heat Adaptability",
                Explanation = "Always under the effect of endure elements with regard to hot climates.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = HiveMind,
                Name = "Hive Mind",
                Explanation = "Creatures with the hive subtype have no language of their own, instead communicating simple concepts via pheromone discharge and body language that other creatures with the hive subtype understand. This ability functions within line of sight. If one creature is able to act in the surprise round of combat, all other hive creatures in line of sight can also act, and a creature isn’t flanked unless all hive creatures within line of sight are flanked.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Incorporeal,
                Name = "Incorporeal",
                Explanation = "An incorporeal creature has no physical body. It can be harmed only by other incorporeal creatures, magic weapons or creatures that strike as magic weapons, and spells, spell-like abilities, or supernatural abilities. It is immune to all nonmagical attack forms. Even when hit by spells or magic weapons, it takes only half damage from a corporeal source (except for channel energy). Although it is not a magical attack, holy water can affect incorporeal undead. Corporeal spells and effects that do not cause damage only have a 50% chance of affecting an incorporeal creature. Force spells and effects, such as from a magic missile, affect an incorporeal creature normally.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                BonusSubtype = MonsterSubtypes.Incorporeal
            });

            Abilities.Add(new Ability()
            {
                Id = UnnervingGaze,
                Name = "Unnerving Gaze",
                Explanation = "Has a gaze attack that manipulates the perceptions of those who look upon them. An unnerving gaze has a range of 30 feet, and causes the targets to become frightened. It is a mind-affecting fear effect and can be negated by a DC @DCCHA Will save—. All @MONNAMES are immune to the unnerving gazes of other @MONNAMES. The save DC is Charisma-based.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = PassWithoutTraceSLA,
                Name = "Pass Without Trace",
                Explanation = "Pass Without Trace as a constant spell-like ability at @(2*HD) caster level.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = ChangeShapePlant,
                Name = "Change Shape (Plants)",
                Explanation = "Can transform into plants, with results similar to the tree shape spell. Unlike that spell, this ability only allows transformation into Small plants of the same type of growth the creature is related to. In this form, the creature appears as a particularly healthy specimen of that particular plant. A leshy can assume plant form or revert to its true form as a swift action.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                BonusSubtype = MonsterSubtypes.Shapechanger
            });

            Abilities.Add(new Ability()
            {
                Id = PlantSpeech,
                Name = "Plant Speech",
                Explanation = "Can speak with plants as if subject to a continual speak with plants spell, but only with species they are related to.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.LanguageQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = VerdantBurst,
                Name = "Verdant Burst",
                Explanation = "When slain, the creature explodes in a burst of fertile energies. All plant creatures within 30 feet of a slain @MONNAME heal 1d8 + @MONHD points of damage, and plant life of the same type as the leshy itself quickly infests the area. If the terrain can support this type of plant, the undergrowth is dense enough to make the region into difficult terrain for 24 hours, after which the plant life diminishes to a normal level; otherwise, the plant life has no significant effect on movement and withers and dies within an hour.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.LanguageQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = DeathGrasp,
                Name = "Death Grasp",
                Explanation = "While holding its breath the creature suspends the majority of her living processes. While holding its breath, the creature is immune to ability drain, energy drain, and sleep effects. A @MONNAME who holds its breath is also immune to bleed effects, disease, and poison; any such effects are suspended for as long as the mortic holds her breath, although this doesn’t cure any damage that the bleed effect, disease, or poison has already done. A @MONNAME can hold her breath for a number of rounds equal to 4 times her Constitution score.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.LanguageQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = DesecratingAura,
                Name = "Desecrating Aura",
                Explanation = "All @MONNAMES have a 30-foot- radius emanation equivalent to a desecrate spell centered on a shrine of evil power. Undead within this radius (including the @MONNAME): gain a +2 profane bonus on attack and damage rolls and saving throws, as well as +2 hit points per die, and the save DC of channeled negative energy is increased by +6 (these adjustments are included for the nightshades in their entries). This aura can be negated by dispel evil, but a @MONNAME can reactivate it on its turn as a free action. A desecrating aura suppresses and is suppressed by consecrate or hallow; both effects are negated within any overlapping area of effect.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.AuraQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = ChannelNegativeEnergy,
                Name = "Channel Negative Energy",
                ClassFeatureQualified = RequiredClassFeature.ChannelEnergy,
                Explanation = "A @MONNAME can channel negative energy as level @CR cleric, for @(CR / 2)d6 damage. There is a @DCCHA Will save for half damage. When using the ability @MONNAME can choose whether to heal undead creatures, or cause damage to living creatures inside the 30-foot radius centered on the @MONNAME. It can use this ability @(3+MONCHA) times per day.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = ChannelPositiveEnergy,
                Name = "Channel Positive Energy",
                ClassFeatureQualified = RequiredClassFeature.ChannelEnergy,
                Explanation = "A @MONNAME can channel positive energy as level @CR cleric, for @(CR / 2)d6 damage. There is a @DCCHA Will save for half damage. When using the ability @MONNAME can choose whether to heal undead living, or cause damage to undead creatures inside the 30-foot radius centered on the @MONNAME. It can use this ability @(3+MONCHA) times per day.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = Darksense,
                Name = "Darksense",
                Explanation = "A @MONNAME gains true seeing in dim light and darkness. Regardless of light conditions, they can detect living creatures and their health within 60 feet, as blindsense with deathwatch continuously active. Mind blank and nondetection prevent the latter effect but not the @MONNAMES true seeing.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = LightAversion,
                Name = "Light Aversion",
                Explanation = "A @MONNAME in bright light becomes sickened—the penalties from this condition are doubled when the @MONNAME is in natural sunlight.",
                PointsCost = -3,
                DisplayCategory = DisplayCategory.DefenseWeakness,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ChangeShapeHumanoid,
                Name = "Change Shape (Humanoid One)",
                Explanation = "All @MONNAME are shapechangers with the shapechanger subtype, but a @MONNAME takes only other shapes similar to its normal humanoid form.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                BonusSubtype = MonsterSubtypes.Shapechanger
            });

            Abilities.Add(new Ability()
            {
                Id = ChangeShapeHumanoidAny,
                Name = "Change Shape (Humanoid Any)",
                Explanation = "All @MONNAMES are shapechangers with the shapechanger subtype, shape have the ability to change shape into any humanoid, as if using alter self.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                BonusSubtype = MonsterSubtypes.Shapechanger
            });

            Abilities.Add(new Ability()
            {
                Id = Constrict,
                Name = "Constrict",
                Explanation = "A creature with this special attack can crush an opponent, dealing @MONATTACKDIE + @MONSTR bludgeoning damage, when it makes a successful grapple check (in addition to any other effects caused by a successful check, including additional damage).",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Serpentine, MonsterBodyTypes.Finned, MonsterBodyTypes.Other },
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Grab,
                Name = "Grab",
                Explanation = "If a creature with this special attack hits with the indicated attack (usually a claw or bite attack), it deals normal damage and attempts to start a grapple as a free action without provoking an attack of opportunity. Unless otherwise noted, grab can only be used against targets of a size equal to or smaller than the creature with this ability. If the creature can use grab on creatures of other sizes, it is noted in the creature’s Special Attacks line. The creature has the option to conduct the grapple normally, or simply use the part of its body it used in the grab to hold the opponent. If it chooses to do the latter, it takes a –20 penalty on its CMB check to make and maintain the grapple, but does not gain the grappled condition itself. A successful hold does not deal any extra damage unless the creature also has the constrict special attack. If the creature does not constrict, each successful grapple check it makes during successive rounds automatically deals the damage indicated for the attack that established the hold. Otherwise, it deals constriction damage as well (the amount is given in the creature’s descriptive text).  Creatures with the grab special attack receive a + 4 bonus on combat maneuver checks made to start and maintain a grapple.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = FreedomOfMovement,
                Name = "Freedom Of Movement",
                Explanation = "Has continuous freedom of movement, as per the spell.",
                PointsCost = 4,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = AmorphousAnatomy,
                Name = "Amorphous Anatomy",
                Explanation = "A @MONNAME’s vital organs shift and change shape and position constantly. This grants it a 50% chance to ignore additional damage caused by critical hits and sneak attacks, and grants it immunity to polymorph effects (unless the protean is a willing target). A @MONNAME automatically recovers from physical blindness or deafness after 1 round by growing new sensory organs to replace those that were compromised.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ChangeShapeProtean,
                Name = "Change Shape (Protean)",
                Explanation = "A @MONNAME’s form is not fixed. Once per day as a standard action, a @MONNAME may change shape into any Small, Medium, or Large animal, elemental , giant , humanoid, magical beast, monstrous humanoid, ooze, plant , or vermin. A @MONNAME can resume its true form as a free action, and when it does so, it gains the effects of a heal spell (CL @MONHD).",
                PointsCost = 5,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                BonusSubtype = MonsterSubtypes.Shapechanger
            });

            Abilities.Add(new Ability()
            {
                Id = SpiritTouch,
                Name = "Spirit Touch",
                Explanation = "A @MONNAME’s natural weapons, as well as any weapon it wields, are treated as though they had the ghost touch weapon special ability.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = SpiritSense,
                Name = "Spirit Sense",
                Explanation = "A @MONNAME notices, locates, and can distinguish between living and undead creatures within 60 feet, just as if it possessed the blindsight ability.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = BonusDisguise8,
                Name = "+8 Disguise",
                Explanation = "Grants a +8 racial bonus to disguise skill checks.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                AppliedSkillId = Skills.Disguise,
                AppliedBonus = 8
            });

            Abilities.Add(new Ability()
            {
                Id = BonusBluff4,
                Name = "+4 Bluff",
                Explanation = "Grants a +4 racial bonus to bluff skill checks.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                AppliedSkillId = Skills.Bluff,
                AppliedBonus = 4
            });

            Abilities.Add(new Ability()
            {
                Id = DetectThoughts,
                Name = "Detect Thoughts",
                Explanation = " A @MONNAME can detect thoughts as per the spell of the same name. This effect functions at CL 18th. A @MONNAME can suppress or resume this ability as a free action. When a @MONNAME uses this ability, it always functions as if it had spent 3 rounds concentrating and thus gains the maximum amount of information possible. The Will save DC to resist this effect is equal to @DCCHA.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = HorrificAppearance,
                Name = "Horrific Appearance",
                Explanation = "All @MONNAMES have such horrific and mind-rending shapes that those who gaze upon them suffer all manner of ill effects. A @MONNAME can present itself as a standard action to assault the senses of all living creatures within 30 feet. The exact effects caused by a @MONNAME_S horrific appearance vary by the type of qlippoth. A successful Will save (DC @DCCHA): reduces or negates the effect. This ability is a mind-affecting gaze attack.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = CastSpellsSorcerorM3,
                Name = "Spellcasting (Sorceror CR-3)",
                Explanation = "Casts spells as a sorceror whose caster level is @(CR - 3).",
                ClassFeatureQualified = RequiredClassFeature.CastSpells,
                PointsCost = 7,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = CastSpellsSorcerorHalf,
                Name = "Spellcasting (Sorceror CR/2)",
                Explanation = "Casts spells as a sorceror whose caster level is @(CR / 2).",
                ClassFeatureQualified = RequiredClassFeature.CastSpells,
                PointsCost = 5,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = CastSpellsSorcerorFull,
                Name = "Spellcasting (Sorceror CR)",
                Explanation = "Casts spells as a sorceror whose caster level is @CR.",
                ClassFeatureQualified = RequiredClassFeature.CastSpells,
                PointsCost = 10,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = CastSpellsWizardM3,
                Name = "Spellcasting (Wizard CR-3)",
                Explanation = "Casts spells as a wizard whose caster level is @(CR - 3).",
                ClassFeatureQualified = RequiredClassFeature.CastSpells,
                PointsCost = 7,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = CastSpellsWizardHalf,
                Name = "Spellcasting (Wizard CR/2)",
                Explanation = "Casts spells as a wizard whose caster level is @(CR / 2).",
                ClassFeatureQualified = RequiredClassFeature.CastSpells,
                PointsCost = 5,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = CastSpellsWizardFull,
                Name = "Spellcasting (Wizard CR)",
                Explanation = "Casts spells as a wizard whose caster level is @CR.",
                ClassFeatureQualified = RequiredClassFeature.CastSpells,
                PointsCost = 10,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = CastSpellsClericM3,
                Name = "Spellcasting (Cleric CR-3)",
                Explanation = "Casts spells as a wizard whose caster level is @(CR - 3).",
                ClassFeatureQualified = RequiredClassFeature.CastSpells,
                PointsCost = 7,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = CastSpellsClericHalf,
                Name = "Spellcasting (Cleric CR/2)",
                Explanation = "Casts spells as a wizard whose caster level is @(CR / 2).",
                ClassFeatureQualified = RequiredClassFeature.CastSpells,
                PointsCost = 5,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = CastSpellsClericFull,
                Name = "Spellcasting (Cleric CR)",
                Explanation = "Casts spells as a wizard whose caster level is @CR.",
                ClassFeatureQualified = RequiredClassFeature.CastSpells,
                PointsCost = 10,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = CastSpellsDruidM3,
                Name = "Spellcasting (Druid CR-3)",
                Explanation = "Casts spells as a wizard whose caster level is @(CR - 3).",
                ClassFeatureQualified = RequiredClassFeature.CastSpells,
                PointsCost = 7,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = CastSpellsDruidHalf,
                Name = "Spellcasting (Druid CR/2)",
                Explanation = "Casts spells as a wizard whose caster level is @(CR / 2).",
                ClassFeatureQualified = RequiredClassFeature.CastSpells,
                PointsCost = 5,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = CastSpellsDruidFull,
                Name = "Spellcasting (Druid CR)",
                Explanation = "Casts spells as a wizard whose caster level is @CR.",
                ClassFeatureQualified = RequiredClassFeature.CastSpells,
                PointsCost = 10,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = Distraction,
                Name = "Distraction",
                Explanation = "A creature with this ability can nauseate the creatures that it damages. Any living creature that takes damage from a creature with the distraction ability is nauseated for 1 round; a Fortitude save of DC @DCCON negates the effect.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = SwarmDistraction,
                Name = "Swarm Distraction",
                Explanation = "Spellcasting or concentrating on spells within the area of a swarm requires a caster level check (DC 20 + spell level). Using skills that involve patience and concentration requires a DC 20 Will save.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = UncannyDodge,
                Name = "Uncanny Dodge",
                Explanation = "@MONNAME cannot be caught flat-footed, nor does it lose its Dex bonus to AC if the attacker is invisible. @MONNAME still loses its Dexterity bonus to AC if immobilized. @MONNAME can still lose its Dexterity bonus to AC if an opponent successfully uses the feint action (see Combat) against it.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.ImprovedUncannyDodge }
            });

            Abilities.Add(new Ability()
            {
                Id = ImprovedUncannyDodge,
                Name = "Improved Uncanny Dodge",
                Explanation = "@MONNAME cannot be caught be flanked. This defense denies a rogue the ability to sneak attack the @MONNAME by flanking it, unless the attacker has at @(4+HD) rogue levels.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.UncannyDodge }
            });
            Abilities.Add(new Ability()
            {
                Id = Evasion,
                Name = "Evasion",
                Explanation = "@MONNAME can avoid even magical and unusual attacks with great agility. If it makes a successful Reflex saving throw against an attack that normally deals half damage on a successful save, it instead takes no damage. Evasion can be used only if the @MONNAME is wearing light armor or no armor. A helpless @MONNAME does not gain the benefit of evasion.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.ImprovedEvasion }
            });
            Abilities.Add(new Ability()
            {
                Id = ImprovedEvasion,
                Name = "Improved Evasion",
                Explanation = "@MONNAME can avoid even magical and unusual attacks with great agility. If it makes a successful Reflex saving throw against an attack that normally deals half damage on a successful save, it instead takes no damage. On an unsuccessful save, @MONNAME also only takes half damage. Evasion can be used only if the @MONNAME is wearing light armor or no armor. A helpless @MONNAME does not gain the benefit of evasion.",
                PointsCost = 4,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.Evasion }
            });

            Abilities.Add(new Ability()
            {
                Id = MinusOneNaturalArmor,
                Name = "-1 Natural Armor",
                Explanation = "1 less natural armor than normal",
                PointsCost = -1,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.Never,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.PlusOneNaturalArmor },
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = PlusOneNaturalArmor,
                Name = "+1 Natural Armor",
                Explanation = "1 more natural armor than normal",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.Never,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.MinusOneNaturalArmor },
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = Plus4VsTrip,
                Name = "+4 vs Trip",
                Explanation = "Grants +4 higher CMD against trip attempts.",
                InlinePrintout = "@(CMD + 4) vs. trip",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseCMDBonus,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                BonusStatType = BonusStat.CmdConditionalBonus,
                AppliedBonus = 4
            });

            Abilities.Add(new Ability()
            {
                Id = Plus4VsDisarm,
                Name = "+4 vs Disarm",
                Explanation = "Grants +4 higher CMD against disarm attempts.",
                InlinePrintout = "@(CMD + 4) vs. disarm",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseCMDBonus,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                BonusStatType = BonusStat.CmdConditionalBonus,
                AppliedBonus = 4
            });

            Abilities.Add(new Ability()
            {
                Id = Plus4VsGrapple,
                Name = "+4 vs Grapple",
                Explanation = "Grants +4 higher CMD against grapple attempts.",
                InlinePrintout = "@(CMD + 4) vs. grapple",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseCMDBonus,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                BonusStatType = BonusStat.CmdConditionalBonus,
                AppliedBonus = 4
            });

            Abilities.Add(new Ability()
            {
                Id = Plus4VsDirtyTricks,
                Name = "+4 vs Steal & Dirty Tricks",
                Explanation = "Grants +4 higher CMD against steal and dirty tricks.",
                InlinePrintout = "@(CMD + 4) vs. steal and dirty tricks",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseCMDBonus,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                BonusStatType = BonusStat.CmdConditionalBonus,
                AppliedBonus = 4
            });

            Abilities.Add(new Ability()
            {
                Id = Plus4VsBullRush,
                Name = "+4 vs Bull Rush & Reposition",
                Explanation = "Grants +4 higher CMD against bull rush and reposition.",
                InlinePrintout = "@(CMD + 4) vs. bull rush and reposition",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseCMDBonus,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                BonusStatType = BonusStat.CmdConditionalBonus,
                AppliedBonus = 4
            });

            Abilities.Add(new Ability()
            {
                Id = LightShield,
                Name = "Light Shield",
                Explanation = "The creature is wielding a light shield in one hand, granting +1 shield bonus to AC.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Biped },
                IncompatibleAbilities = new List<int>() { MonsterAbilities.HeavyShield, MonsterAbilities.TowerShield }
            });

            Abilities.Add(new Ability()
            {
                Id = HeavyShield,
                Name = "Heavy Shield",
                Explanation = "The creature is wielding a heavy shield in one hand, granting +2 shield bonus to AC.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Biped },
                IncompatibleAbilities = new List<int>() { MonsterAbilities.LightShield, MonsterAbilities.TowerShield }
            });

            Abilities.Add(new Ability()
            {
                Id = TowerShield,
                Name = "Tower Shield",
                Explanation = "The creature is wielding a heavy shield in one hand, granting +4 shield bonus to AC and -2 to attack rolls.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Biped },
                IncompatibleAbilities = new List<int>() { MonsterAbilities.LightShield, MonsterAbilities.HeavyShield }
            });

            Abilities.Add(new Ability()
            {
                Id = Trample,
                Name = "Trample",
                Explanation = "As a full-round action, a @MONNAME can attempt to overrun any creature that is at least one size category Smaller than itself. This works just like the overrun combat maneuver, but the trampling creature does not need to make a check, it merely has to move over opponents in its path. Targets of a trample take @(MONATTACKDIE) +@(MONSTR * 3 / 2) damage. Targets of a trample can make an attack of opportunity, but at a –4 penalty. If targets forgo an attack of opportunity, they can attempt to avoid the trampling creature and receive a DC @DCSTR Reflex save to take half damage. A trampling creature can only deal trampling damage to each target once per round, no matter how many times its movement takes it over a target creature.",
                InlinePrintout = "trample (@(MONATTACKDIE) +@(MONSTR * 3 / 2), DC @DCSTR)",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Quadruped, MonsterBodyTypes.Arachnid },
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Engulf,
                Name = "Engulf",
                Explanation = "@MONNAMES can engulf creatures in its path as part of a standard action. It cannot make other attacks during a round in which it engulfs. The @MONNAME merely has to move over its opponents, affecting as many as it can cover. Targeted creatures can make attacks of opportunity against the @MONNAME, but if they do so, they are not entitled to a saving throw against the engulf attack. Those who do not attempt attacks of opportunity can attempt a DC @DCSTR Reflex save to avoid being engulfed—on a success, they are pushed back or aside (target’s choice) as the creature moves forward. Engulfed opponents gain the pinned condition, are in danger of suffocating, are trapped within the creature’s body until they are no longer pinned, and may be subject to other special attacks from the @MONNAME.",
                InlinePrintout = "engulf (DC @(DCSTR) +@(MONATTACKDIE) acid)",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Serpentine, MonsterBodyTypes.Other, MonsterBodyTypes.Arachnid },
                MinimumChallengeRating = 3
            });

            Abilities.Add(new Ability()
            {
                Id = MasterworkWeapon,
                Name = "Masterwork Weapon",
                Explanation = "@MONNAME wields a masterwork weapon, improving its attack bonus with weapon attacks by +1.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Biped },
                MinimumChallengeRating = 2
            });

            Abilities.Add(new Ability()
            {
                Id = GhostTouchAttacks,
                Name = "Ghost Touch Attacks",
                Explanation = "@MONNAME attacks as though it was using a ghost touch weapon, and has no miss chance against ethereal creatures.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                MinimumChallengeRating = 2
            });

            Abilities.Add(new Ability()
            {
                Id = UndersizedWeapons,
                Name = "Undersized Weapons",
                Explanation = "@MONNAMES use weapons that are undersized for their size category and do less damage.",
                PointsCost = -2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = OversizedWeapons,
                Name = "Oversized Weapons",
                Explanation = "@MONNAMES use weapons that are oversized for their size category and do more damage.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Bravery2,
                Name = "Bravery +2",
                Explanation = "Gains a +2 on all Will saves vs fear.",
                InlinePrintout = "+2 vs. fear",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseSaveConditional,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = TrapSense2,
                Name = "Trap Sense +2",
                Explanation = "Gains a +2 on all saves vs traps and on dodge to ac against attack rolls from traps (not included in ac).",
                InlinePrintout = "+2 vs. traps",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseSaveConditional,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = KnowledgeStealingStrike,
                Name = "Knowledge Stealing Strike",
                Explanation = "When the monster's natural attack or melee weapon damages a creature capable of casting arcane spells, that creature must make a DC @DCCHA Will saving throw or be unable to cast any arcane spells for 1 round. Once a creature makes this save, it is immune to further knowledge-stealing strikes from this monster for 24 hours.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = ElementalDamage1d6,
                Name = "1d6 Elemental Damage on hit",
                Explanation = "A @MONNAME's @PRIMARYATTACK attacks are coated in fire, and cause an additional 1d6 fire damage on hit.",
                InlinePrintout = "+1d6 fire",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = SneakAttack1d6,
                Name = "1d6 Sneak Attack",
                Explanation = "A @MONNAME can perform sneak attacks, as a rogue. These will deal 1d6 additional precision damage anytime their target would be denied a Dexterity bonus to AC (whether the target actually has a Dexterity bonus or not), or when the @MONNAME flanks her target.",
                ClassFeatureQualified = RequiredClassFeature.SneakAttack,
                InlinePrintout = "+1d6 sneak attack",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = Plus1Weapon,
                Name = "+1 Weapons",
                Explanation = "A @MONNAME's weapons have a +1 enhancement bonus to attacks and damage rolls.",
                InlinePrintout = "+1 ",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.AttackWeaponDescription,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Biped },
                Stackable = true,
                AppliedBonus = 1,
                BonusStatType = BonusStat.WeaponEnhancement,
                MinimumChallengeRating = 2
            });

            Abilities.Add(new Ability()
            {
                Id = Plus2Weapon,
                Name = "+2 Weapons",
                Explanation = "A @MONNAME's weapons have a +2 enhancement bonus to attacks and damage rolls.",
                InlinePrintout = "+2 ",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.AttackWeaponDescription,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Biped },
                Stackable = true,
                AppliedBonus = 2,
                BonusStatType = BonusStat.WeaponEnhancement,
                MinimumChallengeRating = 4
            });

            Abilities.Add(new Ability()
            {
                Id = Plus3Weapon,
                Name = "+3 Weapons",
                Explanation = "A @MONNAME's weapons have a +3 enhancement bonus to attacks and damage rolls.",
                InlinePrintout = "+3 ",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.AttackWeaponDescription,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Biped },
                Stackable = true,
                AppliedBonus = 3,
                BonusStatType = BonusStat.WeaponEnhancement,
                MinimumChallengeRating = 8
            });

            Abilities.Add(new Ability()
            {
                Id = Plus5Weapon,
                Name = "+5 Weapons",
                Explanation = "A @MONNAME's weapons have a +5 enhancement bonus to attacks and damage rolls.",
                InlinePrintout = "+5 ",
                PointsCost = 5,
                DisplayCategory = DisplayCategory.AttackWeaponDescription,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Biped },
                Stackable = true,
                AppliedBonus = 5,
                BonusStatType = BonusStat.WeaponEnhancement,
                MinimumChallengeRating = 15
            });

            Abilities.Add(new Ability()
            {
                Id = Rage,
                Name = "Rage",
                Explanation = "A @MONNAME can call upon inner reserves of strength and ferocity, granting it additional combat prowess. " +
                    "Starting at 1st level, a @MONNAME can rage for @(MONCON + 2 + 2 * HD) rounds per day. Temporary increases to Constitution, such as those gained from rage and spells like bear’s endurance, do not increase the total number of rounds that a @MONNAME can rage per day. A @MONNAME can enter rage as a free action. The total number of rounds of rage per day is renewed after resting for 8 hours, although these hours do not need to be consecutive. While in rage, a @MONNAME gains a +4 morale bonus to it Strength and Constitution, as well as a +2 morale bonus on Will saves. In addition, it takes a –2 penalty to Armor Class. The increase to Constitution grants the @MONNAME 2 hit points per Hit Dice, but these disappear when the rage ends and are not lost first like temporary hit points.While in rage, a @MONNAME cannot use any Charisma, Dexterity, or Intelligence based skills(except Acrobatics, Fly, Intimidate, and Ride) or any ability that requires patience or concentration. A @MONNAME can end its rage as a free action and is fatigued after rage for a number of rounds equal to 2 times the number of rounds spent in the rage. A @MONNAME cannot enter a new rage while fatigued or exhausted but can otherwise enter rage multiple times during a single encounter or combat. If a @MONNAME falls unconscious, its rage immediately ends, placing it in peril of death.",
                ClassFeatureQualified = RequiredClassFeature.Rage,
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = GreaterRage,
                Name = "Greater Rage",
                Explanation = "A @MONNAME can call upon inner reserves of strength and ferocity, granting it additional combat prowess. " +
                    "Starting at 1st level, a @MONNAME can rage for @(MONCON + 2 + 2 * HD) rounds per day. Temporary increases to Constitution, such as those gained from rage and spells like bear’s endurance, do not increase the total number of rounds that a @MONNAME can rage per day. A @MONNAME can enter rage as a free action. The total number of rounds of rage per day is renewed after resting for 8 hours, although these hours do not need to be consecutive. While in rage, a @MONNAME gains a +6 morale bonus to it Strength and Constitution, as well as a +3 morale bonus on Will saves. In addition, it takes a –2 penalty to Armor Class. The increase to Constitution grants the @MONNAME 2 hit points per Hit Dice, but these disappear when the rage ends and are not lost first like temporary hit points.While in rage, a @MONNAME cannot use any Charisma, Dexterity, or Intelligence based skills(except Acrobatics, Fly, Intimidate, and Ride) or any ability that requires patience or concentration. A @MONNAME can end its rage as a free action and is fatigued after rage for a number of rounds equal to 2 times the number of rounds spent in the rage. A @MONNAME cannot enter a new rage while fatigued or exhausted but can otherwise enter rage multiple times during a single encounter or combat. If a @MONNAME falls unconscious, its rage immediately ends, placing it in peril of death.",
                ClassFeatureQualified = RequiredClassFeature.Rage,
                PointsCost = 4,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = BardicPerformanceInspire,
                Name = "Bardic Performance: Inspire Courage",
                Explanation = "A @MONNAME can use the Perform skill to create magical effects on those around it, including itself if desired. It can use this ability for @(2 + MONCHA + 2 * HD) rounds per day. At each level after 1st a @MONNAME can use @MONNAMEic performance for 2 additional rounds per day. Each round, the @MONNAME can produce any one of the types of @MONNAMEic performance that it has mastered, as indicated by its level.\nA @MONNAME can use its performance to inspire courage in its allies (including itself), bolstering them against fear and improving their combat abilities. To be affected, an ally must be able to perceive the @MONNAME’s performance. An affected ally receives a +@(CR /5 + 1) morale bonus on saving throws against charm and fear effects and a +@(CR /5 + 1) competence bonus on attack and weapon damage rolls. Inspire courage is a mind-affecting ability.",
                ClassFeatureQualified = RequiredClassFeature.BardicPerformance,
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = BardicPerformanceFascinate,
                Name = "Bardic Performance: Fascinate",
                Explanation = "A @MONNAME can use its performance to cause one or more creatures to become fascinated with it. Each creature to be fascinated must be within 90 feet, able to see and hear the @MONNAME, and capable of paying attention to it. The @MONNAME must also be able to see the creatures affected. The Distraction of a nearby combat or other dangers prevents the ability from working. A @MONNAME can target @(HD/3 + 1) creature(s) at once with this ability. Each creature within range receives a Will save @DCCHA to negate the effect. If a creature’s saving throw succeeds, the @MONNAME cannot attempt to fascinate that creature again for 24 hours. If its saving throw fails, the creature sits quietly and observes the performance for as long as the @MONNAME continues to maintain it.",
                ClassFeatureQualified = RequiredClassFeature.BardicPerformance,
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Crush,
                Name = "Crush",
                Explanation = "A flying or jumping Huge or larger @MONNAME can land on foes as a standard action, using its whole body to crush them. Crush attacks are effective only against opponents three or more size categories smaller than the @MONNAME. A crush attack affects as many creatures as fit in the @MONNAME’s space. Creatures in the affected area must succeed on a Reflex save DC @DCCON or be pinned, automatically taking @MONATTACKDIEBIG +@(MONSTR * 3 / 2) bludgeoning damage during the next round unless the dragon moves off them. If the dragon chooses to maintain the pin, it must succeed at a combat maneuver check as normal. Pinned foes take damage from the crush each round if they don’t escape. A crush attack deals the indicated damage plus 1-1/2 times the dragon’s Strength bonus.",
                InlinePrintout = "Crush DC @DCCON @MONATTACKDIEBIG +@(MONSTR * 3 / 2) ",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                MinimumSizeCategory = MonsterSizeCategories.Huge
            });

            Abilities.Add(new Ability()
            {
                Id = SwallowWhole,
                Name = "Swallow Whole",
                Explanation = "If @MONNAME begins its turn with an opponent grappled in its mouth (see Grab), it can attempt a new combat maneuver check (as though attempting to pin the opponent). If it succeeds, it swallows its prey, and the opponent takes bite damage. Unless otherwise noted, the opponent can be up to one size category Smaller than the swallowing creature. Being swallowed causes a creature to take @(CR / 2)d6 acid damage each round. A swallowed creature keeps the grappled condition, while the creature that did the swallowing does not. A swallowed creature can try to cut its way free with any light slashing or piercing weapon (by dealing @(HP/10) damage), or it can just try to escape the grapple. The Armor Class of the interior of a creature that swallows whole is @ACSTOMACH. If a swallowed creature cuts its way out, the swallowing creature cannot use swallow whole again until the damage is healed. If the swallowed creature escapes the grapple, success puts it back in the attacker’s mouth, where it may be bitten or swallowed again.",
                InlinePrintout = "swallow whole (@(CR / 2)d6 acid damage, AC @ACSTOMACH, @(HP/10) hp)",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Arachnid, MonsterBodyTypes.Other, MonsterBodyTypes.Serpentine, MonsterBodyTypes.Finned, MonsterBodyTypes.Winged },
                MinimumSizeCategory = MonsterSizeCategories.Large
            });

            Abilities.Add(new Ability()
            {
                Id = FastSwallow,
                Name = "Fast Swallow",
                Explanation = "The @MONNAME can use its swallow whole ability as a free action at any time during its turn, not just at the start of its turn.of the interior of a creature that swallows whole is normally 10 + 1/2 its natural armor bonus, with no modifiers for size or Dexterity. If a swallowed creature cuts its way out, the swallowing creature cannot use swallow whole again until the damage is healed. If the swallowed creature escapes the grapple, success puts it back in the attacker’s mouth, where it may be bitten or swallowed again.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Arachnid, MonsterBodyTypes.Other, MonsterBodyTypes.Serpentine, MonsterBodyTypes.Finned, MonsterBodyTypes.Winged },
                MinimumSizeCategory = MonsterSizeCategories.Large
            });

            Abilities.Add(new Ability()
            {
                Id = PowerfulCharge,
                Name = "Powerful Charge",
                Explanation = "When the @MONNAME makes a charge, its attack deals @(MONATTACKDIEBIG) +@(MONSTR * 2) damage instead of its normal attack damage in addition to the normal benefits and hazards of a charge.",
                InlinePrintout = "powerful charge (@PRIMARYATTACK, @(MONATTACKDIEBIG) +@(MONSTR * 2))",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Rend,
                Name = "Rend",
                Explanation = "If it hits with two or more @PRIMARYATTACK attacks in 1 round, a @MONNAME can cause tremendous damage by latching onto the opponent’s body and tearing flesh. This attack deals an additional @(MONATTACKDIE) +@(MONSTR * 3/2) damage.",
                InlinePrintout = "rend (@PRIMARYATTACK, @(MONATTACKDIE) +@(MONSTR * 3/2))",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Rake,
                Name = "Rake",
                Explanation = "A @MONNAME gains extra natural attacks under certain conditions, typically when it grapples its foe. In addition to the options available to all grapplers, a monster with the rake ability gains two free claw attacks (@PRIMARYATTACK +@ATTACKBONUS, @MONATTACKDIE +@MONSTR damage) that it can use only against a grappled foe. A monster with the rake ability must begin its turn already grappling to use its rake—it can’t begin a grapple and rake in the same turn.",
                InlinePrintout = "rake (@PRIMARYATTACK +@ATTACKBONUSMELEE, @MONATTACKDIE +@MONSTR damage) ",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Quadruped, MonsterBodyTypes.Arachnid, MonsterBodyTypes.Other, MonsterBodyTypes.Winged }
            });

            Abilities.Add(new Ability()
            {
                Id = Web,
                Name = "Web",
                Explanation = "@MONNAME can use webs to support themselves and up to one additional creature of the same size. In addition, @MONNAME can throw a web up to eight times per day. This is similar to an attack with a net but has a maximum range of 50 feet, with a range increment of 10 feet, and is effective against targets up to one size category larger than the web spinner. An entangled creature can escape with a successful Escape Artist check or burst the web with a Strength check. Both are standard actions with a DC @DCCON. Attempts to burst a web by those caught in it take a –4 penalty.\nWeb spinners can create sheets of sticky webbing up to three times their size. They usually position these sheets to snare flying creatures but can also try to trap prey on the ground. Approaching creatures must succeed on a DC 20 Perception check to notice a web; otherwise they stumble into it and become trapped as though by a successful web attack. Attempts to escape or burst the webbing gain a +5 bonus if the trapped creature has something to walk on or grab while pulling free. Each 5-foot-square section of web has @HD hit points and DR 5/—.\nA @MONNAME can move across its own web at its climb speed and can pinpoint the location of any creature touching its web.",
                InlinePrintout = "web (+@ATTACKBONUSRANGED ranged, DC @DCCON @HD hp) ",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Arachnid, MonsterBodyTypes.Other, MonsterBodyTypes.Finned, MonsterBodyTypes.Winged },
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ConDrainOnAttack,
                Name = "Constitution Drain On Attack",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK must succeed on a DC @DCCHA Fortitude save or sufer @ABILITYDAMAGELOW Constitution drain. On each successful attack, the @MONNAME gains 5 temporary hit points.",
                InlinePrintout = "@ABILITYDAMAGELOW con drain",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = DiseaseOnAttackCon,
                Name = "Disease On Attack (Con)",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK must succeed on a DC @DCCON Fortitude save or become infected with @MONNAME Disease: Disease (Ex) (@PRIMARYATTACK-injury, save DC @DCCON, Onset 1d3 days, frequency 1 day, effect @ABILITYDAMAGELOW Constitution damage, cure 2 consecutive saves)",
                InlinePrintout = "disease DC @DCCON",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.DiseaseOnAttackStr }
            });

            Abilities.Add(new Ability()
            {
                Id = DiseaseOnAttackStr,
                Name = "Disease On Attack (Str)",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK must succeed on a DC @DCCON Fortitude save or become infected with @MONNAME Disease: (@PRIMARYATTACK-injury, save DC @DCCON, Onset 1d3 days, frequency 1 day, effect @ABILITYDAMAGELOW Strength damage; cure 2 consecutive saves)",
                InlinePrintout = "disease DC @DCCON",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.DiseaseOnAttackCon }
            });

            Abilities.Add(new Ability()
            {
                Id = SlowConPoisonOnAttack,
                Name = "Poison On Attack (Slow, Con)",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK are affected by poison: (@PRIMARYATTACK-injury, save DC @DCCON, frequency 1/minute for 6 minutes, effect @ABILITYDAMAGELOW Constitution damage; cure 2 consecutive saves)",
                InlinePrintout = "poison DC @DCCON",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.FastConPoisonOnAttack }
            });

            Abilities.Add(new Ability()
            {
                Id = FastConPoisonOnAttack,
                Name = "Poison On Attack (Fast, Con)",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK are affected by poison: (@PRIMARYATTACK-injury, save DC @DCCON, frequency 1/round for 6 rounds, effect @ABILITYDAMAGELOW Constitution damage; cure 2 consecutive saves)",
                InlinePrintout = "poison DC @DCCON",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.SlowConPoisonOnAttack }
            });

            Abilities.Add(new Ability()
            {
                Id = SlowStrPoisonOnAttack,
                Name = "Poison On Attack (Slow, Str)",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK are affected by poison: (@PRIMARYATTACK-injury, save DC @DCCON, frequency 1/minute for 6 minutes, effect @ABILITYDAMAGELOW Strength damage; cure 2 consecutive saves)",
                InlinePrintout = "poison DC @DCCON",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.FastStrPoisonOnAttack }
            });

            Abilities.Add(new Ability()
            {
                Id = FastStrPoisonOnAttack,
                Name = "Poison On Attack (Fast, Str)",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK are affected by poison: (@PRIMARYATTACK-injury, save DC @DCCON, frequency 1/round for 6 rounds, effect @ABILITYDAMAGELOW Strength damage; cure 2 consecutive saves)",
                InlinePrintout = "poison DC @DCCON",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.SlowStrPoisonOnAttack }
            });

            Abilities.Add(new Ability()
            {
                Id = ElementalRaySLA,
                Name = "Elemental Ray",
                Explanation = "A @MONNAME has the ability to create a elemental ray as a Standard Action, as though they were casting a spell. The element used can be cold, fire, acid, or electricity; it always uses a ranged touch attack to hit and causes @(CR)d6 damage. A @MONNAME can use this ability 3 times per day.",
                InlinePrintout = "elemental ray @(CR)d6 3/day",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SpellLike,
            });

            Abilities.Add(new Ability()
            {
                Id = FearConeSLA,
                Name = "Fear Wave",
                Explanation = "A @MONNAME has the ability to use Fear, as the spell, but with DC @DCCHA. A @MONNAME can use this ability 3 times per day.",
                InlinePrintout = "fear wave DC @DCCHA 3/day",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SpellLike,
            });

            Abilities.Add(new Ability()
            {
                Id = SpellLikeAbilityA,
                Name = "Spell-Like Ability (high level 1/day)",
                Explanation = "Select a spell to use once per day up to spell level @SPELLLEVELHIGH.",
                InlinePrintout = "[One level @SPELLLEVELHIGH spell] 1/day",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SpellLike,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = SpellLikeAbilityAA,
                Name = "Spell-Like Ability (high level 3/day)",
                Explanation = "Select a spell to use once per day up to spell level @SPELLLEVELHIGH.",
                InlinePrintout = "[One level @SPELLLEVELHIGH spell] 3/day",
                PointsCost = 4,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SpellLike,
                Stackable = true
            });


            Abilities.Add(new Ability()
            {
                Id = SpellLikeAbilityB,
                Name = "Spell-Like Ability (mid level 3/day)",
                Explanation = "Select a spell to use once per day up to spell level @SPELLLEVELMID.",
                InlinePrintout = "[One level @SPELLLEVELMID spell] 3/day",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SpellLike,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = SpellLikeAbilityBB,
                Name = "Spell-Like Ability (mid level at will)",
                Explanation = "Select a spell to use once per day up to spell level @SPELLLEVELMID.",
                InlinePrintout = "[One level @SPELLLEVELMID spell] at will",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SpellLike,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = SpellLikeAbilityC,
                Name = "Spell-Like Ability (low level at will)",
                Explanation = "Select a spell to use once per day up to spell level @SPELLLEVELLOW.",
                InlinePrintout = "[One level @SPELLLEVELLOW spell] at will",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SpellLike,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = BloodDrain,
                Name = "Blood Drain",
                Explanation = "A @MONNAME can suck blood from a grappled opponent; if the @MONNAME establishes or maintains a pin, it drains blood, dealing @ABILITYDAMAGELOW points of Constitution damage. The @MONNAME heals 5 hit points or gains 5 temporary hit points for 1 hour (up to a maximum number of temporary hit points equal to its full normal hit points) each round it drains blood.",
                //InlinePrintout = "blood drain",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = BreathWeaponLine,
                Name = "Breath Weapon (Line)",
                Explanation = "A @MONNAME can breathe a @BREATHLINE ft. line breath weapon that deals @(CR)d6 (element) damage, with a DC @DCCON Reflex save for half damage. The @MONNAME can use this breath weapon again in 1d4 turns.",
                InlinePrintout = "breath weapon (@BREATHLINE ft. line, DC @DCCON, @(CR)d6 (element))",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = GreaterBreathWeaponLine,
                Name = "Breath Weapon (Line, Greater)",
                Explanation = "A @MONNAME can breathe a @BREATHLINE ft. line breath weapon that deals @(CR)d8 (element) damage, with a DC @DCCON Reflex save for half damage. The @MONNAME can use this breath weapon again in 1d4 turns.",
                InlinePrintout = "breath weapon (@BREATHLINE ft. line, DC @DCCON, @(CR)d8 (element))",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = BreathWeaponCone,
                Name = "Breath Weapon (Cone)",
                Explanation = "A @MONNAME can breathe a @BREATHLINE ft. line breath weapon that deals @(CR)d6 (element) damage, with a DC @DCCON Reflex save for half damage. The @MONNAME can use this breath weapon again in 1d4 turns.",
                InlinePrintout = "breath weapon (@BREATHLINE ft. line, DC @DCCON, @(CR)d6 (element))",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = GreaterBreathWeaponCone,
                Name = "Breath Weapon (Cone, Greater)",
                Explanation = "A @MONNAME can breathe a @BREATHCONE ft. cone breath weapon that deals @(CR)d8 (element) damage, with a DC @DCCON Reflex save for half damage. The @MONNAME can use this breath weapon again in 1d4 turns.",
                InlinePrintout = "breath weapon (@BREATHCONE ft. cone, DC @DCCON, @(CR)d8 (element))",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = ParalysisOnAttackSu,
                Name = "Paralysis On Attack",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK are affected by paralysis for @ABILITYDAMAGEHIGH +1 rounds unless they succeed at a DC @DCCHA Fortitude save.",
                InlinePrintout = "paralysis",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Pounce,
                Name = "Pounce",
                Explanation = "When a @MONNAME makes a charge, it can make a full attack (including rakes, if it has that ability).",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = EnergyDrainOnAttack,
                Name = "Energy Drain On Attack",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK are given @NEGATIVELEVELS. This only occurs once per round on a given creature, regardless of how many times the attacks hit. For each negative level bestowed, the @MONNAME gains 5 temporary hit points. If this negative level has not been removed before 24 hours have passed, the creature must attempt a DC @DCCHA saving throw. If the creature succeeds, the negative level is removed. Otherwise, the drained level becomes permanent.",
                InlinePrintout = "energy drain",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                MinimumChallengeRating = 3
            });

            Abilities.Add(new Ability()
            {
                Id = IgnitingGaze,
                Name = "Igniting Gaze",
                Explanation = "An opponent that meets a @MONNAME_S gaze within @BREATHCONE bursts into flame unless they succeed at a DC @DCCHA Fortitude save. A victim will suffer @(CR / 4)d6 fire damage immediately and will catch on fire, burning each round for 1d6 fire damage. A successful saving throw negates the effect. Each opponent within range of a gaze attack must attempt a saving throw each round at the beginning of his or her turn in the initiative order. The save DC is Charisma-based.",
                InlinePrintout = "igniting gaze (DC @DCCHA, @(CR/4 +1)d6 fire)",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                MinimumChallengeRating = 2
            });

            Abilities.Add(new Ability()
            {
                Id = FrostNova,
                Name = "Frost Nova",
                Explanation = "A @MONNAME can conjure a spectral wave of frost as a spell-like ability three times per day, causing @(CR)d6 cold damage to the main target and @(CR) cold damage to any targets within 10 feet, with a DC @DCINT Fortitude save for half damage.",
                InlinePrintout = "frost nova (DC @DCINT, @(CR)d6 frost) 3/day",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SpellLike
            });

            Abilities.Add(new Ability()
            {
                Id = Smite,
                Name = "Smite",
                ClassFeatureQualified = RequiredClassFeature.SmiteEvil,
                Explanation = "@(HD/4+1) times per day, a @MONNAME can call out to the powers of @ALIGNMENT to aid her in its struggle. As a swift action, the @MONNAME chooses one target within sight to smite. If this target is a different alignment, the @MONNAME adds +@MONCHA to its attack rolls and adds +@HD to all damage rolls made against the target of its smite. Regardless of the target, smite attacks automatically bypass any DR the creature might possess.\nIn addition, while smite evil is in effect, the @MONNAME gains a +@MONCHA deflection bonus to its AC against attacks made by the target of the smite. If the @MONNAME targets a creature that shares the same alignment, the smite is wasted with no effect.\nThe smite effect remains until the target of the smite is dead or the next time the @MONNAME rests and regains its uses of this ability.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = Trip,
                Name = "Trip",
                Explanation = "A @MONNAME can attempt to trip its opponent as a free action without provoking an attack of opportunity if it hits with the specified attack. If the attempt fails, the creature is not tripped in return.",
                InlinePrintout = "trip",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = WoundOnAttack,
                Name = "Wound On Attack",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK are given 1 Constitution damage. If a wounding attack scores a critical hit, the target instead takes 2 Constitution damage (or more, if the attack's critical multiplier is higher than x2).",
                InlinePrintout = "wound",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                MinimumChallengeRating = 3
            });

            Abilities.Add(new Ability()
            {
                Id = BleedOnAttack,
                Name = "Bleed On Attack",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK are given a bleeding wound that deals @MONATTACKDIESMALL damage each round at the start of the affected ctreature's turn. This bleeding can be stopped by a successful DC15 heal skill check or through the application of enough magical healing to bring the target to full hit points.",
                InlinePrintout = "bleed (@MONATTACKDIESMALL)",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = StunOnAttack,
                Name = "Stun On Attack",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK are are stunned for @ABILITYDAMAGELOW rounds unless they succeed at a DC @DCCON Fortitude save.",
                InlinePrintout = "stun",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = SonicWave,
                Name = "Sonic Wave",
                Explanation = "A @MONNAME can cause a @BREATHCONE radius around itself to vibrate with a damaging sonic wave. This attack causes @(CR)d4 sonic damage and allows a DC @DCCON Fortitude save for Half damage. A @MONNAME can use its Sonic Wave once every 1d4 rounds. The save DC is Constitution-based.",
                InlinePrintout = "sonic wave (@(CR)d4, DC @DCCON)",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ForceWave,
                Name = "Force Wave",
                Explanation = "A @MONNAME can send a @BREATHCONE radius wave of telekinetic force around itself that damages nearby creatures and objects. This attack causes @(CR / 2)d4 +@(CR/2) force damage. A @MONNAME can use its Force Wave once every 1d4 rounds.",
                InlinePrintout = "force wave (@(CR / 2)d4 +@(CR/2))",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                MinimumChallengeRating = 2
            });

            Abilities.Add(new Ability()
            {
                Id = PetrifyingGaze,
                Name = "Petrifying Gaze",
                Explanation = "An opponent that meets a @MONNAME_S gaze within @BREATHCONE becomes petrified unless they succeed at a DC @DCCHA Fortitude save. A successful saving throw negates the effect. Each opponent within range of a gaze attack must attempt a saving throw each round at the beginning of his or her turn in the initiative order.  The save DC is Charisma-based.",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                MinimumChallengeRating = 5
            });

            Abilities.Add(new Ability()
            {
                Id = DominatingGaze,
                Name = "Dominating Gaze",
                Explanation = "An opponent that meets a @MONNAME_S gaze within @BREATHCONE becomes dominated (as Dominate Person) for 1 day unless they succeed at a DC @DCCHA Will save. A successful saving throw negates the effect. Each opponent within range of a gaze attack must attempt a saving throw each round at the beginning of his or her turn in the initiative order.  The save DC is Charisma-based.",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                MinimumChallengeRating = 5
            });

            Abilities.Add(new Ability()
            {
                Id = DeathGaze,
                Name = "Death Gaze",
                Explanation = "An opponent that meets a @MONNAME_S gaze within @BREATHCONE dies unless they succeed at a DC @DCCHA Fortitude save. A successful saving throw negates the effect. Each opponent within range of a gaze attack must attempt a saving throw each round at the beginning of his or her turn in the initiative order.  The save DC is Charisma-based.",
                PointsCost = 10,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                MinimumChallengeRating = 8
            });

            Abilities.Add(new Ability()
            {
                Id = CharmingGaze,
                Name = "Charming Gaze",
                Explanation = "An opponent that meets a @MONNAME_S gaze within @BREATHCONE becomes charmed (as Charm Person) for 1 day unless they succeed at a DC @DCCHA Will save. A successful saving throw negates the effect. Each opponent within range of a gaze attack must attempt a saving throw each round at the beginning of his or her turn in the initiative order.  The save DC is Charisma-based.",
                PointsCost = 4,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = BlindingGaze,
                Name = "Blinding Gaze",
                Explanation = "An opponent that meets a @MONNAME_S gaze within @BREATHCONE becomes blinded for 1d8 hours unless they succeed at a DC @DCCHA Will save. A successful saving throw negates the effect. Each opponent within range of a gaze attack must attempt a saving throw each round at the beginning of his or her turn in the initiative order.  The save DC is Charisma-based.",
                PointsCost = 4,
                DisplayCategory = DisplayCategory.SpecialAttack,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = Bonus6Stealth,
                Name = "+6 Stealth",
                Explanation = "Grants a +6 racial bonus to stealth skill checks.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                AppliedSkillId = Skills.Stealth,
                AppliedBonus = 6
            });

            Abilities.Add(new Ability()
            {
                Id = Bonus6Perception,
                Name = "+6 Perception",
                Explanation = "Grants a +6 racial bonus to perception skill checks.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                AppliedSkillId = Skills.Perception,
                AppliedBonus = 6
            });

            Abilities.Add(new Ability()
            {
                Id = Scent,
                Name = "Scent",
                Explanation = "A @MONNAME can detect approaching enemies, sniff out hidden foes, and track by sense of smell.  They can detect opponents by sense of smell within 30 feet. If the opponent is upwind, the range is 60 feet. If it is downwind, the range is 15 feet. Strong scents, such as smoke or rotting garbage, can be detected at twice the ranges noted above. Overpowering scents, such as skunk musk or troglodyte stench, can be detected at three times these ranges.\nThe creature detects another creature's presence but not its specific location. Noting the direction of the scent is a move action. If the creature moves within 5 feet (1 square) of the scent's source, the creature can pinpoint the area that the source occupies, even if it cannot be seen.\nA creature with the Survival skill and the scent ability can follow tracks by smell, making a Survival check to find or follow a track. A creature with the scent ability can attempt to follow tracks using Survival untrained. The typical DC for a fresh trail is 10. The DC increases or decreases depending on how strong the quarry's odor is, the number of creatures, and the age of the trail. For each hour that the trail is cold, the DC increases by 2. The ability otherwise follows the rules for the Survival skill in regards to tracking. Creatures tracking by scent ignore the effects of surface conditions and poor visibility. Creatures with the scent ability can identify familiar odors just as humans do familiar sights. Water, particularly running water, ruins a trail for air-breathing creatures. Water-breathing creatures that have the scent ability, however, can use it in the water easily. False, powerful odors can easily mask other scents. The presence of such an odor completely spoils the ability to properly detect or identify creatures, and the base Survival DC to track becomes 20 rather than 10.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Track,
                Name = "Track",
                Explanation = "A @MONNAME adds @(HD / 2 + 1) to Survival Skill checks made to follow tracks.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = TracklessStep,
                Name = "Trackless Step",
                Explanation = "A @MONNAME leaves no trail and cannot be tracked. It may choose to leave a trail if so desired.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = DisintegrationAura,
                Name = "Disintegration Aura",
                Explanation = "A @MONNAME is surrounded by a magical aura of hatred and evil spirits.  Each round at the beginning of its turn, any hostile targets within [cone] must make a @DCCHA Will save or suffer @(HD/5 +1)d6 damage. The save DC is Charisma-based.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.AuraQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = Camouflage,
                Name = "Camouflage",
                Explanation = "A @MONNAME can use the Stealth skill to hide in any of its native terrains, even if the terrain doesn't grant cover or concealment.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = Amphibious,
                Name = "Amphibious",
                Explanation = "A @MONNAME has the aquatic subtype, but they can survive indefinitely on land.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                RelevantBodyTypes = new List<int>() { MonsterBodyTypes.Finned },
                BonusSubtype = MonsterSubtypes.Aquatic
            });

            Abilities.Add(new Ability()
            {
                Id = Stench,
                Name = "Stench",
                Explanation = "A @MONNAME secretes an oily chemical that nearly every other creature finds offensive. All living creatures (except those with Stench) within 30 feet must succeed on a DC @DCCON Fortitude save or be sickened for @ABILITYDAMAGEHIGH +@HD rounds. Creatures that successfully save cannot be affected by the same creature?s stench for 24 hours. A delay poison or neutralize poison spell removes the effect from the sickened creature. Creatures with immunity to poison are unaffected, and creatures resistant to poison receive their normal bonus on their saving throws.  The save DC is Constitution based.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.AuraQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = AllAroundVision,
                Name = "All-Around Vision",
                Explanation = "A @MONNAME sees in all directions at once.  It cannot be flanked.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = HideInPlainSight,
                Name = "Hide in Plain Sight",
                Explanation = "A @MONNAME can use the Stealth skill to hide even while being observed.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = EarthGlide,
                Name = "Earth Glide",
                Explanation = "When a @MONNAME burrows, it can pass through stone, dirt, or almost any other sort of earth except metal as easily as a fish swims through water. If protected against fire damage, it can even glide through lava.Its burrowing leaves behind no tunnel or hole, nor does it create any ripple or other sign of its presence. A move earth spell cast on an area containing the burrowing creature flings it back 30 feet, stunning it for 1 round unless it succeeds on a DC 15 Fortitude save.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = PlusTwoDodge,
                Name = "+2 Dodge AC",
                Explanation = "This monster gains a +2 dodge bonus to AC.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                AppliedBonus = 2,
                BonusStatType = BonusStat.DodgeAc
            });

            Abilities.Add(new Ability()
            {
                Id = Winding,
                Name = "Winding",
                Explanation = "The construct must be wound with a special key in order to function. As a general rule, a fully wound clockwork @MONNAME can remain active for 1 day per HD, but shorter or longer durations are possible.",
                PointsCost = -1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });
            Abilities.Add(new Ability()
            {
                Id = TeleportSLA,
                Name = "Teleport at Will",
                Explanation = "A @MONNAME can use 'Greater Teleport' at will, as the spell (caster level @HD), except that the @MONNAME can transport only itself and up to 50 pounds of carried objects.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.SpellLikeAbility,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.SpellLike,
                MinimumChallengeRating = 3
            });

            Abilities.Add(new Ability()
            {
                Id = FearAura,
                Name = "Fear Aura",
                Explanation = "A @MONNAME radiates a @BREATHCONE aura that will affect everyone entering or standing within as the 'Fear' spell. This is a mind-affecting fear effect that allows a DC @DCCHA Will save to negate. The save DC is Charisma-based.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.AuraQuality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = TrueSeeing,
                Name = "True Seeing",
                Explanation = "A @MONNAME can see anything within 120 feet as though constantly under a 'True Seeing' spell.",
                PointsCost = 4,
                DisplayCategory = DisplayCategory.SenseQuality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = NaturalInvisibility,
                Name = "Natural Invisibility",
                Explanation = "A @MONNAME is always naturally invisible, as the spell 'Improved Invisibility'.",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });


            Abilities.Add(new Ability()
            {
                Id = DeadlyCritical,
                Name = "Deadly Critical",
                Explanation = "This monster's critical threat range is doubled for its primary attack.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                //AppliedBonus = 1,
                //BonusStatType = BonusStat..DodgeAc
            });

            Abilities.Add(new Ability()
            {
                Id = ImprovedAttackDamage,
                Name = "Improved Attack Damage",
                Explanation = "This monster's damage die type is increased by 1 for its primary attack.",
                Stackable = true,
                PointsCost = 2,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                //AppliedBonus = 1,
                //BonusStatType = BonusStat..DodgeAc
            });

            Abilities.Add(new Ability()
            {
                Id = DeathOnAttack,
                Name = "Death On Attack",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK are instantly slain unless they succeed at at a DC @DCCHA Fortitude saving throw. The save DC is Charisma-based.",
                InlinePrintout = "death",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                MinimumChallengeRating = 5
            });

            Abilities.Add(new Ability()
            {
                Id = DominateOnAttack,
                Name = "Dominate On Attack",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK are dominated (as the spell Dominate Monster) for 1 day unless they succeed at at a DC @DCCHA Will saving throw. The save DC is Charisma-based.",
                InlinePrintout = "dominate",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                MinimumChallengeRating = 5
            });

            Abilities.Add(new Ability()
            {
                Id = PetrifyOnAttack,
                Name = "Petrify On Attack",
                Explanation = "Creatures hit by a @MONNAME_S @PRIMARYATTACK are petrified unless they succeed at at a DC @DCCHA Fortitude saving throw. The save DC is Charisma-based.",
                InlinePrintout = "petrify",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.AttackBonusDamage,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                MinimumChallengeRating = 5
            });

            //todo: mark class abilities with their qualification
            //todo: make relevant body type for ability field (biped)?
            #region immunities
            Abilities.Add(new Ability()
            {

                Id = ImmunityCold,
                Name = "Cold Immunity",
                Explanation = "Takes no damage from cold, and is unaffected by spells and abilities with the cold descriptor.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                IncompatibleAbilities = new List<int>() { VulnerabilityCold },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityElectricity,
                Name = "Electricity Immunity",
                Explanation = "Takes no damage from electricity, and is unaffected by spells and abilities with the electricity descriptor.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                IncompatibleAbilities = new List<int>() { VulnerabilityElectricity },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityFire,
                Name = "Fire Immunity",
                Explanation = "Takes no damage from fire, and is unaffected by spells and abilities with the fire descriptor.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                IncompatibleAbilities = new List<int>() { VulnerabilityFire },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityAcid,
                Name = "Acid Immunity",
                Explanation = "Takes no damage from acid, and is unaffected by spells and abilities with the acid descriptor.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                IncompatibleAbilities = new List<int>() { VulnerabilityAcid },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunitySonic,
                Name = "Sonic Immunity",
                Explanation = "Takes no damage from sonic, and is unaffected by spells and abilities with the sonic descriptor.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                IncompatibleAbilities = new List<int>() { VulnerabilitySonic },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {

                Id = ImmunityPoison,
                Name = "Poison Immunity",
                Explanation = "Takes no damage from poison, cannot be poisoned, and is unaffected by spells and abilities with the poison descriptor.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                IncompatibleAbilities = new List<int>() { VulnerabilityPoison },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityCriticalHits,
                Name = "Critical Hit Immunity",
                Explanation = "Cannot be critically hit by attacks; any critical hits are resolved as though they were just hits.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                IncompatibleAbilities = new List<int>() { VulnerabilityCriticalHits },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityPetrification,
                Name = "Petrification Immunity",
                Explanation = "Cannot be inflicted with petrification.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunitySleep,
                Name = "Sleep Immunity",
                Explanation = "Cannot be inflicted with sleep or forced to sleep.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityParalysis,
                Name = "Paralysis Immunity",
                Explanation = "Cannot be inflicted with paralysis.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityMindAffecting,
                Name = "Mind Affecting Immunity",
                Explanation = "Cannot be influenced by mind affecting affects of any type that allow a save or spells that are mind affecting.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityPolymorph,
                Name = "Polymorph Immunity",
                Explanation = "Cannot be inflicted or changed by polymorph effects of any type.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityStun,
                Name = "Stun Immunity",
                Explanation = "Cannot be inflicted with the stunned condition.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityFlanking,
                Name = "Flanking Immunity",
                Explanation = "Cannot be flanked in combat (the attackers recieve no bonus for doing so).",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityPrecisionDamage,
                Name = "Precision Damage Immunity",
                Explanation = "Takes no extra damage from Sneak Attack or other effects that deal extra precision-based damage.",
                PointsCost = 4,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityBleed,
                Name = "Bleed Immunity",
                Explanation = "Cannot be inflicted by bleed conditions and does not bleed.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityDisease,
                Name = "Disease Immunity",
                Explanation = "Cannot be inflicted by disease conditions.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityDeathEffects,
                Name = "Death Effects Immunity",
                Explanation = "Cannot be slain by instant death effects.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityNecromancyEffects,
                Name = "Necromancy Effects Immunity",
                Explanation = "Cannot be affected by any necromancy effects.",
                PointsCost = 4,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityCurses,
                Name = "Curse Immunity",
                Explanation = "Cannot be affected by any curse effects.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityAging,
                Name = "Aging Immunity",
                Explanation = "Does not age and cannot be made to age through effects.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });
            Abilities.Add(new Ability()
            {
                Id = ImmunityNegativeLevels,
                Name = "Negative Level Immunity",
                Explanation = "Cannot gain negative levels.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });
            Abilities.Add(new Ability()
            {
                Id = ImmunityAbilityDamage,
                Name = "Ability Damage Immunity",
                Explanation = "Takes no ability damage.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });
            Abilities.Add(new Ability()
            {
                Id = ImmunityPermanentWounds,
                Name = "Permanent Wound Immunity",
                Explanation = "No wounds are permanent; all injuries or and forms of damage the creature suffers become curable for this creature.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });
            Abilities.Add(new Ability()
            {
                Id = ImmunityWeaponDamage,
                Name = "Weapon Damage Immunity",
                Explanation = "This creature takes no damage from physical weapons, natural or otherwise.",
                PointsCost = 10,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });
            Abilities.Add(new Ability()
            {
                Id = ImmunityStagger,
                Name = "Immunity Stagger",
                Explanation = "This creature cannot obtain the staggered condition.",
                PointsCost = 10,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityTargetedEffects,
                Name = "Immunity Targeted Effects",
                Explanation = "This creature cannot be targeted by any spell or effect that has a limited number of targets.",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = ImmunityMagic,
                Name = "Magic Immunity",
                Explanation = "This creature is not affected by any spells, spell-like abilities, or supernatural effects.",
                PointsCost = 10,
                DisplayCategory = DisplayCategory.DefenseImmunity,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });
            #endregion

            #region resistances
            Abilities.Add(new Ability()
            {
                Id = ResistAcid10,
                Name = "Resist Acid 10",
                Explanation = "Takes 10 less damage from attacks that deal acid damage.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>() { VulnerabilityAcid },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = ResistCold10,
                Name = "Resist Cold 10",
                Explanation = "Takes 10 less damage from attacks that deal cold damage.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>() { VulnerabilityCold },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = ResistElectricity10,
                Name = "Resist Electricity 10",
                Explanation = "Takes 10 less damage from attacks that deal electricity damage.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>() { VulnerabilityElectricity },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = ResistFire10,
                Name = "Resist Fire 10",
                Explanation = "Takes 10 less damage from attacks that deal fire damage.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>() { VulnerabilityFire },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = ResistSonic10,
                Name = "Resist Sonic 10",
                Explanation = "Takes 10 less damage from attacks that deal sonic damage.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>() { VulnerabilitySonic },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = ResistAcid30,
                Name = "Resist Acid 30",
                Explanation = "Takes 30 less damage from attacks that deal acid damage.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>() { VulnerabilityAcid },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = ResistCold30,
                Name = "Resist Cold 30",
                Explanation = "Takes 30 less damage from attacks that deal cold damage.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>() { VulnerabilityCold },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = ResistElectricity30,
                Name = "Resist Electricity 30",
                Explanation = "Takes 30 less damage from attacks that deal electricity damage.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>() { VulnerabilityElectricity },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = ResistFire30,
                Name = "Resist Fire 30",
                Explanation = "Takes 30 less damage from attacks that deal fire damage.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>() { VulnerabilityFire },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = ResistSonic30,
                Name = "Resist Sonic 30",
                Explanation = "Takes 30 less damage from attacks that deal sonic damage.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>() { VulnerabilitySonic },
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = SpellResist5Plus,
                Name = "Spell Resistance 5 + CR",
                Explanation = "Spell resistance prevents the monster from being affected by most spells unless the caster can overcome their spell resistance score.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Defense,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = SpellResist8Plus,
                Name = "Spell Resistance 8 + CR",
                Explanation = "Spell resistance prevents the monster from being affected by most spells unless the caster can overcome their spell resistance score.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Defense,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = SpellResist11Plus,
                Name = "Spell Resistance 11 + CR",
                Explanation = "Spell resistance prevents the monster from being affected by most spells unless the caster can overcome their spell resistance score.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Defense,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = SpellResist13Plus,
                Name = "Spell Resistance 13 + CR",
                Explanation = "Spell resistance prevents the monster from being affected by most spells unless the caster can overcome their spell resistance score.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Defense,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = SpellResist15Plus,
                Name = "Spell Resistance 15 + CR",
                Explanation = "Spell resistance prevents the monster from being affected by most spells unless the caster can overcome their spell resistance score.",
                PointsCost = 4,
                DisplayCategory = DisplayCategory.Defense,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            //Abilities.Add(new Ability()
            //{
            //    Id = DR5GoodPiercing,
            //    Name = "DR 5/ Good and Piercing",
            //    Explanation = "Prevents up to 5 damage from every attack suffered if the damage is not aligned good and from a piercing weapon.",
            //    PointsCost = 4,
            //    DisplayCategory = DisplayCategory.DefenseDamageReduction,
            //    IncompatibleAbilities = new List<int>(),
            //    PrintExplanationLevel = PrintExplanationLevel.PrintAll,
            //    Rarity = FeatureElement.UncommonFeature,
            //    PowerSource = AbilityPowerSource.Supernatural,
            //    Stackable = false
            //});

            Abilities.Add(new Ability()
            {
                Id = HalfDamageSlashing,
                Name = "Half Damage from Slashing",
                Explanation = "This creature takes half damage from all attacks that deal slashing damage.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = HalfDamagePiercing,
                Name = "Half Damage from Piercing",
                Explanation = "This creature takes half damage from all attacks that deal piercing damage.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = HalfDamageBludgeoning,
                Name = "Half Damage from Bludgeoning",
                Explanation = "This creature takes half damage from all attacks that deal bludgeoning damage.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = HalfDamageFromWeapons,
                Name = "Half Damage from Weapons",
                Explanation = "This creature takes half damage from all attacks that come from natural or weapon attacks.",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = DR5Epic,
                Name = "DR 5/epic",
                Explanation = "A creature with this special quality ignores damage from most weapons and natural attacks. Wounds heal immediately, or the weapon bounces off harmlessly (in either case, the opponent knows the attack was ineffective). The creature takes normal damage from energy attacks (even nonmagical ones), spells, spell-like abilities, and supernatural abilities. A certain kind of weapon can sometimes damage the creature normally, as noted.",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.DefenseDamageReduction,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                MinimumChallengeRating = 17,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = DR5Magic,
                Name = "DR 5/magic",
                Explanation = "A creature with this special quality ignores damage from most weapons and natural attacks. Wounds heal immediately, or the weapon bounces off harmlessly (in either case, the opponent knows the attack was ineffective). The creature takes normal damage from energy attacks (even nonmagical ones), spells, spell-like abilities, and supernatural abilities. A certain kind of weapon can sometimes damage the creature normally, as noted.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseDamageReduction,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = DR5Bludgeon,
                Name = "DR 5/bludgeoning",
                Explanation = "A creature with this special quality ignores damage from most weapons and natural attacks. Wounds heal immediately, or the weapon bounces off harmlessly (in either case, the opponent knows the attack was ineffective). The creature takes normal damage from energy attacks (even nonmagical ones), spells, spell-like abilities, and supernatural abilities. A certain kind of weapon can sometimes damage the creature normally, as noted.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseDamageReduction,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = DR5Slash,
                Name = "DR 5/slashing",
                Explanation = "A creature with this special quality ignores damage from most weapons and natural attacks. Wounds heal immediately, or the weapon bounces off harmlessly (in either case, the opponent knows the attack was ineffective). The creature takes normal damage from energy attacks (even nonmagical ones), spells, spell-like abilities, and supernatural abilities. A certain kind of weapon can sometimes damage the creature normally, as noted.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseDamageReduction,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = DR5Pierce,
                Name = "DR 5/piercing",
                Explanation = "A creature with this special quality ignores damage from most weapons and natural attacks. Wounds heal immediately, or the weapon bounces off harmlessly (in either case, the opponent knows the attack was ineffective). The creature takes normal damage from energy attacks (even nonmagical ones), spells, spell-like abilities, and supernatural abilities. A certain kind of weapon can sometimes damage the creature normally, as noted.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseDamageReduction,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = DR5Good,
                Name = "DR 5/good",
                Explanation = "A creature with this special quality ignores damage from most weapons and natural attacks. Wounds heal immediately, or the weapon bounces off harmlessly (in either case, the opponent knows the attack was ineffective). The creature takes normal damage from energy attacks (even nonmagical ones), spells, spell-like abilities, and supernatural abilities. A certain kind of weapon can sometimes damage the creature normally, as noted.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseDamageReduction,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = DR5Evil,
                Name = "DR 5/evil",
                Explanation = "A creature with this special quality ignores damage from most weapons and natural attacks. Wounds heal immediately, or the weapon bounces off harmlessly (in either case, the opponent knows the attack was ineffective). The creature takes normal damage from energy attacks (even nonmagical ones), spells, spell-like abilities, and supernatural abilities. A certain kind of weapon can sometimes damage the creature normally, as noted.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseDamageReduction,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = DR5Law,
                Name = "DR 5/lawful",
                Explanation = "A creature with this special quality ignores damage from most weapons and natural attacks. Wounds heal immediately, or the weapon bounces off harmlessly (in either case, the opponent knows the attack was ineffective). The creature takes normal damage from energy attacks (even nonmagical ones), spells, spell-like abilities, and supernatural abilities. A certain kind of weapon can sometimes damage the creature normally, as noted.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseDamageReduction,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = true
            });
            Abilities.Add(new Ability()
            {
                Id = DR5Chaos,
                Name = "DR 5/chaotic",
                Explanation = "A creature with this special quality ignores damage from most weapons and natural attacks. Wounds heal immediately, or the weapon bounces off harmlessly (in either case, the opponent knows the attack was ineffective). The creature takes normal damage from energy attacks (even nonmagical ones), spells, spell-like abilities, and supernatural abilities. A certain kind of weapon can sometimes damage the creature normally, as noted.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseDamageReduction,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = true
            });
            Abilities.Add(new Ability()
            {
                Id = DR5All,
                Name = "DR 5/-",
                Explanation = "A creature with this special quality ignores damage from most weapons and natural attacks. Wounds heal immediately, or the weapon bounces off harmlessly (in either case, the opponent knows the attack was ineffective). The creature takes normal damage from energy attacks (even nonmagical ones), spells, spell-like abilities, and supernatural abilities. A certain kind of weapon can sometimes damage the creature normally, as noted.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseDamageReduction,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = true
            });
            Abilities.Add(new Ability()
            {
                Id = DR5Silvered,
                Name = "DR 5/silvered",
                Explanation = "A creature with this special quality ignores damage from most weapons and natural attacks. Wounds heal immediately, or the weapon bounces off harmlessly (in either case, the opponent knows the attack was ineffective). The creature takes normal damage from energy attacks (even nonmagical ones), spells, spell-like abilities, and supernatural abilities. A certain kind of weapon can sometimes damage the creature normally, as noted.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseDamageReduction,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = true
            });
            Abilities.Add(new Ability()
            {
                Id = DR5Adamantine,
                Name = "DR 5/adamantine",
                Explanation = "A creature with this special quality ignores damage from most weapons and natural attacks. Wounds heal immediately, or the weapon bounces off harmlessly (in either case, the opponent knows the attack was ineffective). The creature takes normal damage from energy attacks (even nonmagical ones), spells, spell-like abilities, and supernatural abilities. A certain kind of weapon can sometimes damage the creature normally, as noted.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseDamageReduction,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = true
            });
            Abilities.Add(new Ability()
            {
                Id = DR5ColdIron,
                Name = "DR 5/cold iron",
                Explanation = "A creature with this special quality ignores damage from most weapons and natural attacks. Wounds heal immediately, or the weapon bounces off harmlessly (in either case, the opponent knows the attack was ineffective). The creature takes normal damage from energy attacks (even nonmagical ones), spells, spell-like abilities, and supernatural abilities. A certain kind of weapon can sometimes damage the creature normally, as noted.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.DefenseDamageReduction,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = VulnerableMind,
                Name = "Vulnerable Mind",
                Explanation = "Unlike mindless creatures, this creature is vulnerable to mind-affecting affects.",
                PointsCost = -6,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = VulnerableWish,
                Name = "Vulnerable Miracles and Wishes",
                Explanation = "A spellcaster gains a +6 bonus on its caster level check to penetrate a behemoth’s SR with a miracle or wish spell, and the behemoth suffers a –6 penalty on saves against these spells. A miracle or a wish spell can negate a behemoth’s regeneration, but only for 1d4 rounds per casting.",
                PointsCost = -1,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = VulnerableToConsecration,
                Name = "Vulnerable Consecration",
                Explanation = "A mortic is staggered within the area of a consecrate effect.",
                PointsCost = -2,
                DisplayCategory = DisplayCategory.DefenseResistance,
                IncompatibleAbilities = new List<int>(),
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            #endregion

            #region profile changing abilities
            Abilities.Add(new Ability()
            {
                Id = Envisaging,
                Name = "Envisaging",
                Explanation = "Aeons communicate wordlessly, almost incomprehensibly. Caring little for the wants and desires of other creatures, they have no need to engage in exchanges of dialogue. Instead, aeons mentally scan beings for their thoughts and intentions, and then retaliate with flashes of psychic projections that emit a single concept in response to whatever the other being was thinking. The flash is usually a combination of a visual and aural stimulation, which displays how the aeon perceives future events might work out. For instance, an aeon seeking to raze a city communicates this concept to non-aeons by sending them a vivid image of the city crumbling to ash. An aeon’s envisaging functions as a non-verbal form of telepathy. Aeons cannot read the thoughts of any creature immune to mind-affecting effects.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });
            Abilities.Add(new Ability()
            {
                //TODO: auto bonus to all knowledge skills
                Id = ExtensionOfAll,
                Name = "Extension Of All",
                Explanation = "Through an aeon’s connection to the multiverse, it gains access to strange and abstruse knowledge that filters through all existence. Much of the knowledge is timeless, comprised of events long past, present, and potentially even those yet to come. Aeons gain a racial bonus equal to half their racial Hit Dice on all Knowledge skill checks. This same connection also binds them to other aeons. As a result, they can communicate with each other freely, over great distances as if using telepathy. This ability also works across planes, albeit less effectively, allowing the communication of vague impressions or feelings, not specific details or sights. Due to the vast scope of the aeon race’s multiplanar concerns, though, even the most dire reports of a single aeon rarely inspire dramatic or immediate action.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                //TODO: auto bonus from deflection to AC
                Id = VoidForm,
                Name = "Void Form",
                Explanation = "Though aeons aren’t incorporeal, their forms are only a semi-tangible manifestation of something greater. An aeon’s void form grants it a deflection bonus equal to 1/4 its Hit Dice (rounded down).",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = MergeWithWard,
                Name = "Merge With Ward",
                Explanation = "As a standard action, a kami can merge its body and mind with its ward. When merged, the kami can observe the surrounding region with its senses as if it were using its own body, as well as via any senses its ward might have. It has no control over its ward, nor can it communicate or otherwise take any action other than to emerge from its ward as a standard action. A kami must be adjacent to its ward to merge with or emerge from it. If its ward is a creature, plant, or object, the kami can emerge mounted on the creature provided the kami’s body is at least one size category smaller than the creature. If its ward is a location, the kami may emerge at any point within that location.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = Ward,
                Name = "Ward",
                Explanation = "A kami has a specific ward—a creature with a 2 or lower Intelligence (usually an animal or vermin), a plant (not a plant creature), an object, or a location. The type of ward is listed in parentheses in the kami’s stat block. Several of a kami’s abilities function only when it is either merged with its ward or within 120 feet of it. If a kami’s ward is portable and travels with the kami to another plane, the kami does not gain the extraplanar subtype on that other plane as long as its ward remains within 120 feet. If a ward is destroyed while a kami is merged with it, the kami dies (no save). If a ward is destroyed while a kami is not merged with it, the kami loses its merge with ward ability and its fast healing, and becomes permanently sickened.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural
            });

            Abilities.Add(new Ability()
            {
                Id = FlySpeed,
                Name = "Fly Speed",
                Explanation = "Has a fly speed equal to its land speed.",
                ClassFeatureQualified = RequiredClassFeature.FlySpeed,
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Movement,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = SwimSpeed,
                Name = "Swim Speed",
                Explanation = "Has a swim speed equal to its land speed.",
                ClassFeatureQualified = RequiredClassFeature.SwimSpeed,
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Movement,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = BurrowSpeed,
                Name = "Burrow Speed",
                Explanation = "Has a burrow speed equal to its land speed.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Movement,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = ClimbSpeed,
                Name = "Climb Speed",
                Explanation = "Has a climb speed equal to its land speed.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Movement,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.SupernaturalOrExceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = MindlessInt,
                Name = "Mindless",
                Explanation = "This monster has no Intelligence score, and cannot gain one. Mindless creatures are immune to anything that requires a Will save, and cannot be communicated with.",
                PointsCost = 0,
                DisplayCategory = DisplayCategory.Miscellaneous,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                //SetAttribute = BaseStatistic.Intelligence,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = NoConstitution,
                Name = "No Constitution Score",
                Explanation = "This monster has no Constitution score, and cannot gain one. They effectively have +0 Constitution for the purposes of hit points and Fortitude saves.",
                PointsCost = 0,
                DisplayCategory = DisplayCategory.Miscellaneous,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = CharismaReplacesCon,
                Name = "Charisma adds Hit Points",
                Explanation = "This monster uses their Charisma score in the place of their Constitution score for the purposes of hit points and Fortitude saves.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Miscellaneous,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = BonusFeat,
                Name = "Bonus Feat",
                Explanation = "Grants a bonus feat of any type the monster qualifies for.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Miscellaneous,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = true
            });

            Abilities.Add(new Ability()
            {
                Id = Blind,
                Name = "Blind",
                Explanation = "Cannot see in any way, but has immunity gaze attacks, visual effects, illusions, and any other attack forms that rely on sight.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Miscellaneous,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = CannotBeRaised,
                Name = "Cannot Be Raised",
                Explanation = "Cannot be restored to life with raise dead, reincarnate, or ressurection. It requires a limited wish, wish, miracle, or true ressurection to restore this creature to life.",
                PointsCost = -1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = UndeadTraits,
                Name = "Undead Traits",
                Explanation = "Not subject to nonlethal damage, ability drain, or energy drain. Immune to damage to its physical ability scores (Constitution, Dexterity, and Strength), as well as to exhaustion and fatigue effects.\nCannot heal damage on its own if it has no Intelligence score, although it can be healed. Negative energy(such as an inflict spell) can heal undead creatures. The fast healing special quality works regardless of the creature’s Intelligence score.\nImmunity to any effect that requires a Fortitude save(unless the effect also works on objects or is harmless).\nNot at risk of death from massive damage, but is immediately destroyed when reduced to 0 hit points.\nNot affected by raise dead and reincarnate spells or abilities. Resurrection and true resurrection can affect undead creatures.These spells turn undead creatures back into the living creatures they were before becoming undead.\nUndead creatures are powered by negative energy. Only sentient undead creatures have, or are, souls.",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = ConstructTraits,
                Name = "Construct Traits",
                Explanation = "Cannot heal damage on its own, but often can be repaired via exposure to a certain kind of effect (see the creature’s description for details) or through the use of the Craft Construct feat. Constructs can also be healed through spells such as make whole. A construct with the fast healing special quality still benefits from that quality.\nNot subject to ability damage, ability drain, fatigue, exhaustion, energy drain, or nonlethal damage.\nImmunity to any effect that requires a Fortitude save(unless the effect also works on objects, or is harmless).\nNot at risk of death from massive damage.Immediately destroyed when reduced to 0 hit points or less.\nA construct cannot be raised or resurrected.\nA construct is hard to destroy, and gains bonus hit points based on size (listed in statistics).",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = AutomatonCore,
                Name = "Automaton Core",
                Explanation = "All automatons are powered by a planar core infused with life energy. A destroyed automaton leaves behind an automaton core, which can be harvested with a successful Spellcraft check.",
                PointsCost = -1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Supernatural,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = Intelligent,
                Name = "Intelligent (mindless only)",
                Explanation = "This (normally mindless) creature has an Intelligence score and can gain class levels, feats, and skill points as a normal creature.",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = NegativeEnergyAffinity,
                Name = "Negative Energy Affinity",
                Explanation = "The creature is alive but is healed by negative energy and harmed by positive energy, as if it were an undead creature.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Defense,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = UnlivingNature,
                Name = "Unliving Nature",
                Explanation = "Although a @MONNAME is a living creature, she is treated both as undead and as her normal type and subtype for the purposes of spells and effects (for example, she can be detected with detect undead and rendered immobile by either halt undead or hold person). A @MONNAME never takes a penalty on Disguise checks to disguise herself as an undead creature. @MONNAMES gain a +4 racial bonus on saving throws against mind-affecting effects.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = NativeOutsider,
                Name = "Native Outsider",
                Explanation = "These creatures have mortal ancestors or a strong connection to the Material Plane and can be raised, reincarnated, or resurrected just as other living creatures can be. Creatures with this subtype are native to the Material Plane. Unlike true outsiders, native outsiders need to eat and sleep.",
                PointsCost = -1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false,
                BonusSubtype = MonsterSubtypes.Native
            });

            Abilities.Add(new Ability()
            {
                Id = ShortReach,
                Name = "Short Reach",
                Explanation = "The monster's reach is lower than normal for its size category.",
                PointsCost = -2,
                DisplayCategory = DisplayCategory.Offense,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false,
                IncompatibleAbilities = new List<int>() { LongReach }
            });

            Abilities.Add(new Ability()
            {
                Id = LongReach,
                Name = "Long Reach",
                Explanation = "The monster's reach is longer than normal for its size category.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Offense,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false,
                IncompatibleAbilities = new List<int>() { ShortReach }
            });

            Abilities.Add(new Ability()
            {
                Id = SwarmAttack,
                Name = "Swarm Attack",
                Explanation = "A swarm automatically hits and uses damage dice based on its hit dice, rather than adding Strength or using a weapon. A swarm attack replaces all other attacks.",
                PointsCost = 3,
                DisplayCategory = DisplayCategory.Offense,
                PrintExplanationLevel = PrintExplanationLevel.Always,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = SwarmDamageResistance,
                Name = "Swarm Damage Resistance",
                Explanation = "A swarm takes half damage from piercing and slashing attacks. If a swarm has fine sized creatures, it is instead immune to all weapon damage.",
                PointsCost = 6,
                DisplayCategory = DisplayCategory.DefenseResistance,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = SwarmSpaceReach,
                Name = "Swarm Space",
                Explanation = "A swarm occupies a 10 foot space and has 0 foot reach, regardless of the creature's size.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Offense,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = SwarmTraits,
                Name = "Swarm Traits",
                Explanation = "Swarms are never staggered or reduced to a dying state by damage. Also, they cannot be tripped, grappled, or bull rushed, and they cannot grapple an opponent.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.DoNotGenerateFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });

            Abilities.Add(new Ability()
            {
                Id = PlusTenFeetMove,
                Name = "+10 Foot Move Speed",
                Explanation = "Movement speed increased by 10 feet.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Movement,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = true,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.MinusTenFootMove }
            });

            Abilities.Add(new Ability()
            {
                Id = MinusTenFootMove,
                Name = "-10 Foot Move Speed",
                Explanation = "Movement speed decreased by 10 feet.",
                PointsCost = -1,
                DisplayCategory = DisplayCategory.Movement,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = true,
                IncompatibleAbilities = new List<int>() { MonsterAbilities.PlusTenFeetMove }
            });

            Abilities.Add(new Ability()
            {
                Id = DoesNotBreathe,
                Name = "Does Not Breathe",
                Explanation = "This monster does not breathe, and cannot drown or suffocate. It still suffers from inhaled poisons and airborne diseases unless it is also immune to those separately.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = DoesNotEat,
                Name = "Does Not Eat",
                Explanation = "This monster does not eat or drink, and cannot starve or die of thirst.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = DoesNotSleep,
                Name = "Does Not Sleep",
                Explanation = "This monster does not need to naturally sleep, and is immune to magical sleep effects.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.Detailed,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional
            });

            Abilities.Add(new Ability()
            {
                Id = PlusOneAttribute,
                Name = "+1 Attribute Point",
                Explanation = "Increases the monster's available Attribute Points by 1.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Quality,
                PrintExplanationLevel = PrintExplanationLevel.PrintAll,
                Rarity = FeatureElement.CommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = true
            });
            #endregion

            #region ecology
            Abilities.Add(new Ability()
            {
                Id = DoubleTreasure,
                Name = "Double Treasure",
                Explanation = "Monster gives two times normal treasure.",
                PointsCost = -2,
                DisplayCategory = DisplayCategory.Ecology,
                PrintExplanationLevel = PrintExplanationLevel.Never,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });
            Abilities.Add(new Ability()
            {
                Id = TripleTreasure,
                Name = "Triple Treasure",
                Explanation = "Monster gives three times normal treasure.",
                PointsCost = -4,
                DisplayCategory = DisplayCategory.Ecology,
                PrintExplanationLevel = PrintExplanationLevel.Never,
                Rarity = FeatureElement.RareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });
            Abilities.Add(new Ability()
            {
                Id = QuadroupleTreasure,
                Name = "Quadrouple Treasure",
                Explanation = "Monster gives four times normal treasure.",
                PointsCost = -6,
                DisplayCategory = DisplayCategory.Ecology,
                PrintExplanationLevel = PrintExplanationLevel.Never,
                Rarity = FeatureElement.SuperRareFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });
            Abilities.Add(new Ability()
            {
                Id = HalfTreasure,
                Name = "Half Treasure",
                Explanation = "Monster gives half normal treasure.",
                PointsCost = 1,
                DisplayCategory = DisplayCategory.Ecology,
                PrintExplanationLevel = PrintExplanationLevel.Never,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });
            Abilities.Add(new Ability()
            {
                Id = NoTreasure,
                Name = "No Treasure",
                Explanation = "Monster gives no normal treasure.",
                PointsCost = 2,
                DisplayCategory = DisplayCategory.Ecology,
                PrintExplanationLevel = PrintExplanationLevel.Never,
                Rarity = FeatureElement.UncommonFeature,
                PowerSource = AbilityPowerSource.Exceptional,
                Stackable = false
            });
            #endregion
        }
        public Ability GetAbility(int id)
        {
            return Abilities.FirstOrDefault(a => a.Id == id);
        }
        public Ability GetRandomAbility(System.Random random)
        {
            int id = Table.GetRandom(random);
            return GetAbility(id);
        }
    }
}
