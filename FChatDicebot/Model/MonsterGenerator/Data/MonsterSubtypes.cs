using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonsterGenerator2_Net47.Models.Data
{
    public class MonsterSubtypes
    {
        //TODO: restrict subtypes based on alignment AND/OR restrict alignment based on subtypes

        public const int Adlet = 1, Aeon = 2, Agathion = 3, Air = 4;
        public const int Angel = 5, Aquatic = 6, Archon = 7, Asura = 8;
        public const int Augmented = 9, Automaton = 10, Azata = 11, Behemoth = 12;
        public const int Catfolk = 13, Chaotic = 14, Clockwork = 15, Cold = 16;
        public const int Colossus = 17, Daemon = 18, DarkFolk = 19, DeepOne = 20;
        public const int Demodand = 21, Demon = 22, Devil = 23, Div= 24;
        public const int Dwarf = 25, Earth = 26, Elemental = 27, Elf = 28;
        public const int Evil = 29, Extraplanar = 30, Fire = 31, Giant = 32;
        public const int Gnome = 33, Goblinoid = 34, Godspawn = 35, Good = 36;
        public const int GreatOldOne = 37, Halfling = 38, Herald = 39, Hive = 40;
        public const int Human = 41, Incorporeal = 42, Inevitable = 43, Kaiju = 44;
        public const int Kami = 45, Kasatha = 46, Kitsune = 47, Kyton = 48;
        public const int Lawful = 49, Leshy = 50, Mortic = 51, Mythic = 52;
        public const int Native = 53, Nightshade = 54, Oni = 55, Orc = 56;
        public const int Protean = 57, Psychopomp = 58, Qlippoth = 59, Rakshasa = 60;
        public const int Ratfolk = 61, Reptilian = 62, Robot= 63, Samsaran = 64;
        public const int Sasquatch = 65, Shapechanger = 66, Swarm = 67, Troop = 68;
        public const int Udaeus = 69, Unbreathing = 70, Vanara = 71, Vishkanya = 72;
        public const int Water = 73, Wayang = 74, WildHunt= 75;

        public List<MonsterSubtype> Subtypes;

        private RandomGenerationTable Table;

        private static MonsterSubtypes Instance;
        public static MonsterSubtypes GetInstance()
        {
            if (Instance == null)
                Instance = new MonsterSubtypes();

            return Instance;
        }

        public MonsterSubtypes()
        {
            PopulateTypes();
            Table = new RandomGenerationTable();
            Table.FillRandomGenerationTable<MonsterSubtype>(Subtypes);
        }

        private void PopulateTypes()
        {
            Subtypes = new List<MonsterSubtype>();
            Subtypes.Add(new MonsterSubtype() {
                Id = Adlet, //todo: racial stats for just a regular race
                Name = "Adlet",
                Explanation = "Strange humanoid wolf creatures called adlets, and to creatures related to them.",
                Rarity = FeatureElement.RareFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.MonstrousHumanoid },
                AbilityIds = new List<int>(),
                ClassSkillIds = new List<int>()
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Aeon,
                Name = "Aeon",
                Explanation = "Aeons are a race of neutral outsiders who roam the planes maintaining the balance of reality.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityCold, MonsterAbilities.ImmunityPoison,
                    MonsterAbilities.ImmunityCriticalHits, MonsterAbilities.ResistElectricity10, MonsterAbilities.ResistFire10,
                    MonsterAbilities.Envisaging, MonsterAbilities.ExtensionOfAll, MonsterAbilities.VoidForm},
                ClassSkillIds = new List<int>()
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Agathion,
                Name = "Agathion",
                Explanation = "Agathions are beast-aspect outsiders native to Nirvana.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.LowLightVision, MonsterAbilities.ImmunityElectricity,
                    MonsterAbilities.ImmunityPetrification, MonsterAbilities.ResistCold10, MonsterAbilities.ResistSonic10,
                    MonsterAbilities.LayOnHandsRacial, MonsterAbilities.BonusVsPoison4, MonsterAbilities.BaseLanguagesCelestialInfernalDraconic,
                    MonsterAbilities.SpeakWithAnimalsSu, MonsterAbilities.Truespeech},
                ClassSkillIds = new List<int>()
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Air,
                Name = "Air",
                Explanation = "This subtype is usually used for outsiders with a connection to the Elemental Planes of Air.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider, MonsterTypes.Aberration, MonsterTypes.Dragon, MonsterTypes.Fey },
                AbilityIds = new List<int>() { MonsterAbilities.FlySpeed},
                ClassSkillIds = new List<int>() { Skills.Fly }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Angel,
                Name = "Angel",
                Explanation = "Angels are a race of celestials, or good outsiders, native to the good-aligned outer planes.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.LowLightVision, MonsterAbilities.Darkvision60,
                    MonsterAbilities.ImmunityPetrification, MonsterAbilities.ImmunityCold, MonsterAbilities.ImmunityAcid,
                    MonsterAbilities.ResistElectricity10, MonsterAbilities.ResistFire10,
                    MonsterAbilities.BonusVsPoison4, MonsterAbilities.ProtectiveAura, MonsterAbilities.Truespeech},
                ClassSkillIds = new List<int>()
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Aquatic,
                Name = "Aquatic",
                Explanation = "A creature who lives underwater. Can breathe water.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Animal, MonsterTypes.Vermin, MonsterTypes.MonstrousHumanoid,
                    MonsterTypes.MagicalBeast, MonsterTypes.Fey, MonsterTypes.Aberration, MonsterTypes.Ooze },
                AbilityIds = new List<int>() { MonsterAbilities.SwimSpeed },
                ClassSkillIds = new List<int>() { Skills.Swim }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Archon,
                Name = "Archon",
                Explanation = "A race of celestials native to lawful-good aligned planes.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.LowLightVision, MonsterAbilities.Darkvision60,
                    MonsterAbilities.ImmunityPetrification, MonsterAbilities.ImmunityElectricity, MonsterAbilities.TeleportSp,
                    MonsterAbilities.BonusVsPoison4, MonsterAbilities.MenaceAura, MonsterAbilities.Truespeech}
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Asura,
                Name = "Asura",
                Explanation = "A race of lawful evil outsiders.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityCurses, MonsterAbilities.ImmunityDisease,
                    MonsterAbilities.ImmunityPoison, MonsterAbilities.ResistAcid10, MonsterAbilities.ResistElectricity10,
                    MonsterAbilities.Telepathy, MonsterAbilities.ElusiveAura, MonsterAbilities.Regeneration5, MonsterAbilities.SpellResist11Plus,
                    MonsterAbilities.SummonKinGreater, MonsterAbilities.AlignedWeaponsLaw, MonsterAbilities.AlignedWeaponsEvil,
                    MonsterAbilities.BonusPerception4, MonsterAbilities.BonusEscapeArtist6 }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Augmented,
                Name = "Augmented",
                Explanation = "The original type of this create has been changed, by a template or otherwise.",
                Rarity = FeatureElement.DoNotGenerateFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Animal, MonsterTypes.Vermin, MonsterTypes.MonstrousHumanoid,
                    MonsterTypes.MagicalBeast, MonsterTypes.Fey, MonsterTypes.Aberration, MonsterTypes.Ooze,
                    MonsterTypes.Construct, MonsterTypes.Dragon, MonsterTypes.Humanoid, MonsterTypes.Outsider },
                AbilityIds = new List<int>() { }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Automaton,
                Name = "Automaton",
                Explanation = "Automatons are lawful neutral constructs with the extraplanar subtype.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider, MonsterTypes.Construct },
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision120, MonsterAbilities.LowLightVision,
                    MonsterAbilities.ImmunityElectricity, MonsterAbilities.ResistCold10, MonsterAbilities.ResistSonic10,
                    MonsterAbilities.SpellResist11Plus, MonsterAbilities.Telepathy,
                    MonsterAbilities.AlignedWeaponsLaw, MonsterAbilities.MagicWeapons, MonsterAbilities.AutomatonCore,
                    MonsterAbilities.Intelligent, MonsterAbilities.VulnerableMind },
                FeatIds = new List<int>() { Feats.SimpleWeaponProficiency },
                ClassSkillIds = new List<int>() { Skills.Climb, Skills.Craft, Skills.Diplomacy, Skills.Fly,
                    Skills.Intimidate, Skills.KnowledgeArcana, Skills.KnowledgeDungeoneering, Skills.KnowledgeEngineering, Skills.KnowledgeGeography,
                    Skills.KnowledgeHistory, Skills.KnowledgeLocal, Skills.KnowledgeNature, Skills.KnowledgeNobility, Skills.KnowledgePlanes,
                    Skills.KnowledgeReligion, Skills.Perception, Skills.SenseMotive, Skills.Spellcraft,
                    Skills.Stealth, Skills.UseMagicDevice}
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Azata,
                Name = "Azata",
                Explanation = "A race of celestials native to chaotic-good aligned planes.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.LowLightVision, MonsterAbilities.Darkvision60,
                    MonsterAbilities.ImmunityPetrification, MonsterAbilities.ImmunityElectricity,
                    MonsterAbilities.ResistCold10, MonsterAbilities.ResistFire10, MonsterAbilities.Truespeech}
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Behemoth,
                Name = "Behemoth",
                Explanation = "A behemoth is a neutral Colossal magical beast of great strength and power.",
                Rarity = FeatureElement.RareFeature,
                MinimumChallengeRating = 17,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.MagicalBeast },
                AbilityIds = new List<int>() { MonsterAbilities.Blindsense60, MonsterAbilities.ImmunityAbilityDamage,
                    MonsterAbilities.ImmunityAging, MonsterAbilities.ImmunityBleed, MonsterAbilities.ImmunityDisease, MonsterAbilities.ImmunityNegativeLevels,
                    MonsterAbilities.ImmunityFire, MonsterAbilities.ImmunityMindAffecting, MonsterAbilities.ImmunityParalysis,
                    MonsterAbilities.ImmunityPermanentWounds, MonsterAbilities.ImmunityPetrification, MonsterAbilities.ImmunityPoison,
                    MonsterAbilities.ImmunityPolymorph, MonsterAbilities.Regeneration15, MonsterAbilities.SpellResist11Plus,
                    MonsterAbilities.Ruinous, MonsterAbilities.VulnerableWish,
                    MonsterAbilities.Unstoppable, MonsterAbilities.DR5Epic, MonsterAbilities.DR5Epic, MonsterAbilities.DR5Epic}
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Catfolk, //todo: racial stats for just a regular race
                Name = "Catfolk",
                Explanation = "This subtype is applied to the humanoid felines called catfolk and creatures related to catfolk.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid, MonsterTypes.MonstrousHumanoid },
                AbilityIds = new List<int>() { }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Chaotic,
                Name = "Chaotic",
                Explanation = "This subtype is usually applied to outsiders native to the chaotic-aligned Outer Planes",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.AlignedWeaponsChaos }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Clockwork,
                Name = "Clockwork",
                Explanation = "A construct with a winding clockwork mechanism.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Construct },
                AbilityIds = new List<int>() { MonsterAbilities.VulnerabilityElectricity, MonsterAbilities.PlusTwoDodge, MonsterAbilities.Winding },
                FeatIds = new List<int>() { Feats.ImprovedInitiative, Feats.LightningReflexes }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Cold,
                Name = "Cold",
                Explanation = "A creature of particular acclimation to cold.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Animal, MonsterTypes.Vermin, MonsterTypes.MonstrousHumanoid,
                    MonsterTypes.MagicalBeast, MonsterTypes.Fey, MonsterTypes.Aberration, MonsterTypes.Ooze,
                    MonsterTypes.Construct, MonsterTypes.Dragon, MonsterTypes.Humanoid, MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityCold, MonsterAbilities.VulnerabilityFire }
            });

            //not doing colossus

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Daemon,
                Name = "Daemon",
                Explanation = "Daemons are neutral evil outsiders that eat souls and thrive on disaster and ruin.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityAcid, MonsterAbilities.ImmunityDisease, MonsterAbilities.ImmunityPoison,
                    MonsterAbilities.ResistCold10, MonsterAbilities.ResistElectricity10, MonsterAbilities.ResistFire10, MonsterAbilities.Telepathy,
                    MonsterAbilities.SummonKinGreater }
                //speak abyssal, draconic, infernal
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = DarkFolk,
                Name = "Dark Folk",
                Explanation = "Dark folk are reclusive subterranean humanoids with an aversion to light.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.SeeInDarkness, MonsterAbilities.LightBlindness, MonsterAbilities.PoisonUse,
                    MonsterAbilities.DeathThroesBlind }
                //speak dark folk
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = DeepOne,
                Name = "Deep One",
                Explanation = "This subtype is applied to deep ones and creatures related to deep ones, such as deep one hybrids.",
                Rarity = FeatureElement.RareFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Aberration, MonsterTypes.MagicalBeast, MonsterTypes.MonstrousHumanoid },
                AbilityIds = new List<int>() { MonsterAbilities.LowLightVision }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Demodand,
                Name = "Demodand",
                Explanation = "Demodands are chaotic evil outsiders who stalk the Abyss.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityAcid, MonsterAbilities.ImmunityPoison, MonsterAbilities.ResistFire10,
                    MonsterAbilities.ResistCold10, MonsterAbilities.SummonKinGreater, MonsterAbilities.FaithStealingStrike, MonsterAbilities.HereticalSoul,
                    MonsterAbilities.AlignedWeaponsChaos, MonsterAbilities.AlignedWeaponsEvil }
                //speak Abyssal, Celestial, and Common.
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Demon,
                Name = "Demon",
                Explanation = "Demons are chaotic evil outsiders that call the Abyss their home.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityElectricity, MonsterAbilities.ImmunityPoison,
                    MonsterAbilities.ResistCold10, MonsterAbilities.ResistAcid10, MonsterAbilities.ResistFire10, MonsterAbilities.Telepathy,
                    MonsterAbilities.SummonKinGreater, MonsterAbilities.AlignedWeaponsChaos, MonsterAbilities.AlignedWeaponsEvil }
                //speak Abyssal, Celestial, and draconic.
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Devil,
                Name = "Devil",
                Explanation = "Devils are lawful evil outsiders that hail from the plane of Hell.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityFire, MonsterAbilities.ImmunityPoison,
                    MonsterAbilities.ResistCold10, MonsterAbilities.ResistAcid10, MonsterAbilities.SeeInDarkness, MonsterAbilities.Telepathy,
                    MonsterAbilities.SummonKinGreater, MonsterAbilities.AlignedWeaponsLaw, MonsterAbilities.AlignedWeaponsEvil }
                //speak Infernal, Celestial, and draconic.
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Div,
                Name = "Div",
                Explanation = "Divs are neutral evil outsiders that sow misfortune and ruin.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityFire, MonsterAbilities.ImmunityPoison,
                    MonsterAbilities.ResistElectricity10, MonsterAbilities.ResistAcid10, MonsterAbilities.SeeInDarkness, MonsterAbilities.Telepathy,
                    MonsterAbilities.SummonKinGreater }
                //speak  Abyssal, Celestial, and Infernal.
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Dwarf, //todo: racial stats for just a regular race
                Name = "Dwarf",
                Explanation = "This subtype is applied to dwarves and creatures related to dwarves.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision60 }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Earth,
                Name = "Earth",
                Explanation = "This subtype is usually used for outsiders with a connection to the Elemental Planes of Earth.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.BurrowSpeed, MonsterAbilities.Tremorsense60 }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Elemental,
                Name = "Elemental",
                Explanation = "An elemental is a being composed entirely from one of the four classical elements: air, earth, fire, or water.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityBleed, MonsterAbilities.ImmunityParalysis, MonsterAbilities.ImmunityPoison,
                    MonsterAbilities.ImmunitySleep, MonsterAbilities.ImmunityStun, MonsterAbilities.ImmunityFlanking, MonsterAbilities.ImmunityCriticalHits,
                    MonsterAbilities.ImmunityPrecisionDamage }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Elf, //todo: racial stats for just a regular race
                Name = "Elf",
                Explanation = "This subtype is applied to elves and creatures related to elves.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { MonsterAbilities.LowLightVision }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Evil,
                Name = "Evil",
                Explanation = "This subtype is usually applied to Outsiders native to the evil-aligned Outer Planes.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.AlignedWeaponsEvil }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Extraplanar,
                Name = "Extraplanar",
                Explanation = "This subtype is applied to any creature when it is on a plane other than its native plane.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Animal, MonsterTypes.Vermin, MonsterTypes.MonstrousHumanoid,
                    MonsterTypes.MagicalBeast, MonsterTypes.Fey, MonsterTypes.Aberration, MonsterTypes.Ooze,
                    MonsterTypes.Construct, MonsterTypes.Dragon, MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { MonsterAbilities.AlignedWeaponsEvil }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Fire,
                Name = "Fire",
                Explanation = "A creature of particular acclimation to fire.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Animal, MonsterTypes.Vermin, MonsterTypes.MonstrousHumanoid,
                    MonsterTypes.MagicalBeast, MonsterTypes.Fey, MonsterTypes.Aberration, MonsterTypes.Ooze,
                    MonsterTypes.Construct, MonsterTypes.Dragon, MonsterTypes.Humanoid, MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityFire, MonsterAbilities.VulnerabilityCold }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Giant,
                Name = "Giant",
                Explanation = "A giant is a humanoid creature of great strength, usually of at least Large size.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid, MonsterTypes.MonstrousHumanoid, MonsterTypes.Fey },
                AbilityIds = new List<int>() { MonsterAbilities.LowLightVision },
                ClassSkillIds = new List<int>() { Skills.Intimidate, Skills.Perception }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Gnome,
                Name = "Gnome",
                Explanation = "This subtype is applied to gnomes and creatures related to gnomes.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { MonsterAbilities.LowLightVision }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Goblinoid,
                Name = "Goblinoid",
                Explanation = "Goblinoids are stealthy humanoids who live by hunting and raiding and who all speak Goblin.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>(),
                ClassSkillIds = new List<int>() { Skills.Stealth }
            });
            //godspawn subtype

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Good,
                Name = "Good",
                Explanation = "This subtype is usually applied to outsiders native to the good-aligned Outer Planes.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.AlignedWeaponsGood }
            });
            //great old one
            Subtypes.Add(new MonsterSubtype()
            {
                Id = Halfling, //todo: racial stats for just a regular race
                Name = "Halfling",
                Explanation = "This subtype is applied to halflings and creatures related to halflings.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>()
            });
            //Herald

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Hive,
                Name = "Hive",
                Explanation = "The hive are an invasive species of aberrations that consume worlds like locusts.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Aberration },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityAcid, MonsterAbilities.Blind, MonsterAbilities.Blindsense60,
                    MonsterAbilities.Blindsight10, MonsterAbilities.CorrosiveBlood, MonsterAbilities.DeathThroesAcid,
                    MonsterAbilities.HeatAdaptability, MonsterAbilities.HiveMind },
                ClassSkillIds = new List<int>() { Skills.Intimidate, Skills.Perception }
            });
            Subtypes.Add(new MonsterSubtype()
            {
                Id = Human, //todo: racial stats for just a regular race
                Name = "Human",
                Explanation = "This subtype is applied to humans and creatures related to humans.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>()
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Incorporeal,
                Name = "Incorporeal",
                Explanation = "An incorporeal creature has no physical body.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Undead },
                AbilityIds = new List<int>( MonsterAbilities.Incorporeal )
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Inevitable,
                Name = "Inevitable",
                Explanation = "Inevitables are construct-like outsiders built by the axiomites to enforce law.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.LowLightVision, MonsterAbilities.ConstructTraits, MonsterAbilities.Regeneration5,
                    MonsterAbilities.Truespeech },
                ClassSkillIds = new List<int>() { Skills.Acrobatics, Skills.Diplomacy, Skills.Intimidate, Skills.Survival },
                //good saves Fortitude and will
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Kami,
                Name = "Kami",
                Explanation = "Kami are a race of native outsiders who serve to protect what they refer to as “wards”—animals, plants, objects, and even locations—from being harmed or dishonored.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityBleed, MonsterAbilities.ImmunityMindAffecting, MonsterAbilities.ImmunityPetrification,
                    MonsterAbilities.ImmunityPolymorph, MonsterAbilities.ResistAcid10, MonsterAbilities.ResistElectricity10, MonsterAbilities.ResistFire10,
                    MonsterAbilities.Telepathy, MonsterAbilities.FastHeal5, MonsterAbilities.MergeWithWard, MonsterAbilities.Ward  }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Kasatha, //todo: racial stats for just a regular race
                Name = "Kasatha",
                Explanation = "A kasatha is a nimble four-armed humanoid from another planet.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Kitsune, //todo: racial stats for just a regular race
                Name = "Kitsune",
                Explanation = "A kitsune is a shapechanging humanoid fox-person.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Kyton,
                Name = "Kyton",
                Explanation = "Kytons are a race of lawful evil outsiders native to the Plane of Shadow who feed on fear and pain.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityCold, MonsterAbilities.Darkvision60,
                    MonsterAbilities.Regeneration5, MonsterAbilities.UnnervingGaze }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Lawful,
                Name = "Lawful",
                Explanation = "This subtype is usually applied to outsiders native to the lawful-aligned Outer Planes.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.AlignedWeaponsLaw }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Leshy,
                Name = "Leshy",
                Explanation = "A leshy is a nature spirit that inhabits the body of a specially grown plant.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Plant },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityElectricity, MonsterAbilities.ImmunitySonic, MonsterAbilities.Darkvision60,
                    MonsterAbilities.LowLightVision, MonsterAbilities.PassWithoutTraceSLA, MonsterAbilities.ChangeShapePlant, MonsterAbilities.PlantSpeech,
                    MonsterAbilities.VerdantBurst, MonsterAbilities.Intelligent }
                //speak druidic sylvan
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Mortic,
                Name = "Mortic",
                Explanation = "A kitsune is a shapechanging humanoid fox-person.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid }, //good save fort
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision60, MonsterAbilities.NegativeEnergyAffinity, MonsterAbilities.DeathGrasp,
                    MonsterAbilities.UnlivingNature, MonsterAbilities.VulnerableToConsecration },
                ClassSkillIds = new List<int>() { Skills.Intimidate, Skills.Perception, Skills.Stealth }
            });

            //mythic
            Subtypes.Add(new MonsterSubtype()
            {
                Id = Native,
                Name = "Native",
                Explanation = "This subtype is applied only to outsiders. These creatures have mortal ancestors or a strong connection to the Material Plane and can be raised, reincarnated, or resurrected just as other living creatures can be. creatures with this subtype are native to the Material Plane.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.NativeOutsider }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Nightshade,
                Name = "Nightshade",
                Explanation = "Nightshades are monstrous undead composed of shadow and evil.",
                Rarity = FeatureElement.RareFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid }, //good save fort
                AbilityIds = new List<int>() { MonsterAbilities.LowLightVision, MonsterAbilities.DesecratingAura, MonsterAbilities.ChannelNegativeEnergy,
                    MonsterAbilities.Darksense, MonsterAbilities.LightAversion, MonsterAbilities.SummonKinGreater },
                ClassSkillIds = new List<int>() { Skills.Intimidate, Skills.Perception, Skills.Stealth }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Oni,
                Name = "Oni",
                Explanation = "An oni is an evil spirit who takes humanoid form to become a native outsider.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision60, MonsterAbilities.LowLightVision, MonsterAbilities.ChangeShapeHumanoid, MonsterAbilities.Regeneration5 }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                //todo: racial features
                Id = Orc,
                Name = "Orc",
                Explanation = "This subtype is applied to orcs and creatures related to orcs, such as half-orcs.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision60, MonsterAbilities.LowLightVision, MonsterAbilities.LightSensitivity }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Protean,
                Name = "Protean",
                Explanation = "Proteans are serpentine outsiders of pure chaos.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.Blindsense30, MonsterAbilities.ImmunityAcid, MonsterAbilities.ResistElectricity10,
                    MonsterAbilities.ResistSonic10, MonsterAbilities.FlySpeed, MonsterAbilities.Constrict, MonsterAbilities.Grab,
                    MonsterAbilities.FreedomOfMovement, MonsterAbilities.AmorphousAnatomy, MonsterAbilities.ChangeShapeProtean }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Psychopomp,
                Name = "Psychopomp",
                Explanation = "Psychopomps are a race of neutral outsiders who serve the goddess of death and oversee mortal souls.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision60, MonsterAbilities.LowLightVision, MonsterAbilities.ImmunityDisease,
                    MonsterAbilities.ImmunityDeathEffects, MonsterAbilities.ImmunityPoison, MonsterAbilities.ResistElectricity10,
                    MonsterAbilities.ResistCold10, MonsterAbilities.SpiritTouch, MonsterAbilities.SpiritSense }
                //s speak Abyssal, Celestial, and Infernal.
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Qlippoth,
                Name = "Qlippoth",
                Explanation = "Qlippoth are chaotic evil outsiders from the deepest reaches of the Abyss.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityMindAffecting, MonsterAbilities.ImmunityCold,
                    MonsterAbilities.ImmunityPoison, MonsterAbilities.ResistElectricity10, MonsterAbilities.ResistAcid10,
                    MonsterAbilities.ResistFire10, MonsterAbilities.HorrificAppearance, MonsterAbilities.Telepathy }
                //s speak Abyssal
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Rakshasa,
                Name = "Rakshasa",
                Explanation = "A rakshasa is a lawful evil shapeshifter spirit born into the Material Plane.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.Darkvision60, MonsterAbilities.ChangeShapeHumanoidAny, MonsterAbilities.DetectThoughts,
                    MonsterAbilities.SpellResist15Plus, MonsterAbilities.DR5Good, MonsterAbilities.BonusDisguise8,
                    MonsterAbilities.BonusBluff4, MonsterAbilities.CastSpellsSorcerorM3 } // MonsterAbilities.DR5GoodPiercing, //
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Ratfolk,
                Name = "Ratfolk",
                Explanation = "This subtype is applied to the humanoid rodents called ratfolk and creatures related to ratfolk.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() {  }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Reptilian,
                Name = "Reptilian",
                Explanation = "These creatures are scaly and usually cold-blooded. The reptilian subtype is only used to describe a set of humanoid races, not all animals and monsters that are true reptiles.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Robot,
                Name = "Robot",
                Explanation = "These creatures are scaly and usually cold-blooded. The reptilian subtype is only used to describe a set of humanoid races, not all animals and monsters that are true reptiles.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Construct },
                AbilityIds = new List<int>() { MonsterAbilities.Intelligent, MonsterAbilities.VulnerabilityCriticalHits, MonsterAbilities.VulnerabilityElectricity },
                ClassSkillIds = new List<int>() { Skills.Climb, Skills.DisableDevice, Skills.Fly, Skills.KnowledgeArcana, Skills.KnowledgeDungeoneering
                    , Skills.KnowledgeEngineering, Skills.KnowledgeGeography, Skills.KnowledgeHistory, Skills.KnowledgeLocal
                    , Skills.KnowledgeNature, Skills.KnowledgeNobility, Skills.KnowledgePlanes, Skills.KnowledgeReligion, Skills.Linguistics,
                    Skills.Perception, Skills.SenseMotive}
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Samsaran,
                Name = "Samsaran",
                Explanation = "A samsaran is a humanoid creature whose spirit always reincarnates into another samsaran.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Sasquatch,
                Name = "Sasquatch",
                Explanation = "This subtype is applied to the humanoid beings called sasquatches and creatures related to sasquatches.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Shapechanger,
                Name = "Shapechanger",
                Explanation = "A shapechanger has the supernatural ability to assume one or more alternate forms.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid, MonsterTypes.Aberration, MonsterTypes.Dragon,
                    MonsterTypes.Fey, MonsterTypes.MagicalBeast, MonsterTypes.MonstrousHumanoid, MonsterTypes.Outsider},
                AbilityIds = new List<int>() { }
            });

            Subtypes.Add(new MonsterSubtype()
            { //TODO: needs handling based on size fine/diminutive/ tiny/  (cannot be bigger)
                Id = Swarm,
                Name = "Swarm",
                Explanation = "A swarm is a collection of Fine, Diminutive, or Tiny creatures that acts as a single creature.",
                Rarity = FeatureElement.UncommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Vermin, MonsterTypes.Undead, MonsterTypes.Construct },
                AbilityIds = new List<int>() { MonsterAbilities.ImmunityCriticalHits, MonsterAbilities.ImmunityFlanking,
                    MonsterAbilities.SwarmAttack, MonsterAbilities.SwarmDamageResistance, MonsterAbilities.SwarmSpaceReach,
                    MonsterAbilities.ImmunityTargetedEffects, MonsterAbilities.ImmunityStagger, MonsterAbilities.VulnerabilityAreaEffect,
                    MonsterAbilities.Distraction, MonsterAbilities.SwarmDistraction, MonsterAbilities.SwarmTraits }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Udaeus,
                Name = "Udaeus",
                Explanation = "An udaeus is a member of a warlike Mythic humanoid race originally created from dragon teeth.",
                Rarity = FeatureElement.RareFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Vanara,
                Name = "Vanara",
                Explanation = "This subtype is applied to vanaras and creatures related to vanaras.",
                Rarity = FeatureElement.RareFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Vishkanya,
                Name = "Vishkanya",
                Explanation = "This subtype is applied to vishkanyas and creatures related to vishkanyas.",
                Rarity = FeatureElement.RareFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Water,
                Name = "Water",
                Explanation = "This subtype is usually used for Outsiders with a connection to the Elemental Planes of Water.",
                Rarity = FeatureElement.CommonFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Outsider },
                AbilityIds = new List<int>() { MonsterAbilities.SwimSpeed },
                ClassSkillIds = new List<int>() { Skills.Swim }
            });

            Subtypes.Add(new MonsterSubtype()
            {
                Id = Wayang,
                Name = "Wayang",
                Explanation = "A wayang is a gangly humanoid originating from the Shadow Plane.",
                Rarity = FeatureElement.RareFeature,
                RelevantMonsterTypes = new List<int>() { MonsterTypes.Humanoid },
                AbilityIds = new List<int>() { }
            });
            //did not do wild hunt, dumb stuff

            foreach (MonsterSubtype subtype in Subtypes)
            {
                int powersCost = 0;
                int immuneResistCost = 0;
                foreach (int abilityId in subtype.AbilityIds)
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
                subtype.BonusPowerPoints = (int)Math.Floor((double)powersCost * .4) + (int)Math.Floor((double)immuneResistCost * .8);
            }
        }
        public MonsterSubtype GetMonsterSubtype(int id)
        {
            return Subtypes.FirstOrDefault(a => a.Id == id);
        }
        public MonsterSubtype GetRandomMonsterType(System.Random random)
        {
            int id = Table.GetRandom(random);
            return GetMonsterSubtype(id);
        }
    }
}
