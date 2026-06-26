using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model.MonsterGenerator
{
    public class MonsterGeneratorModel
    {
        public string SMonsterName { get; set; }
        public int MonsterId { get; set; }
        public int SType { get; set; }
        public int SBodyType { get; set; }
        public int SChallengeRating { get; set; }
        public int SSizeCategory { get; set; }
        public int SHitDiceRatio { get; set; }
        public int SAlignment { get; set; }
        public List<int> SAbilities { get; set; }
        public List<AbilityModel> SAbilitiesInfo { get; set; }
        public List<int> SAttacks { get; set; }
        public List<AbilityModel> SAttacksInfo { get; set; }

        public List<int> SSkills { get; set; }
        public List<int> SSubtypes { get; set; }
        public List<int> SFeats { get; set; }
        public List<FeatModel> SFeatsInfo { get; set; }

        public int FeatPoints { get; set; }
        public int MaxFeatPoints { get; set; }
        public int SkillPoints { get; set; }
        public int MaxSkillPoints { get; set; }
        public int BaseSkillPoints { get; set; }
        public int AttributePoints { get; set; }
        public int MaxAttributePoints { get; set; }
        public int MentalAttributeMin { get; set; }
        public int AttributeCap { get; set; }
        public int AttributeStrCap { get; set; }
        public int PowerPoints { get; set; }
        public int MaxPowerPoints { get; set; }

        public int BaseAttack { get; set; }
        public int HitDice { get; set; }

        public int SStr { get; set; }
        public int SDex { get; set; }
        public int SCon { get; set; }
        public int SInt { get; set; }
        public int SWis { get; set; }
        public int SCha { get; set; }

        //not directly data related
        public bool UpdateForm { get; set; }
        public int PrintExplanationLevel { get; set; }

        public bool UseAiDescription { get; set; }
        public bool GenerateAiImage { get; set; }
        public int TokensChange { get; set; }
        public string ImageUrl { get; set; }
        public string Environment { get; set; }
        public string Organization { get; set; }
        public string Description { get; set; }
        public string Printout { get; set; }
        public string ServerMessage { get; set; }

        //public void UpdateFromSavedMonster(SavedMonster savedMonster)
        //{
        //    MonsterId = savedMonster.Id;
        //    UseAiDescription = savedMonster.UseAiDescription;
        //    GenerateAiImage = savedMonster.UseAiArt;
        //}
    }

    public class AbilityModel
    {
        public int Id;
        public string Name;
        public int Cost;
        public bool Stackable;
    }
    public class FeatModel
    {
        public int Id;
        public string Name;
    }
}
