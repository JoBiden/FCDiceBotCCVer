using FChatDicebot.SavedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace FChatDicebot.DiceFunctions
{
    public class Enchantment : ICustomUserContent
    {
        public string suffix;
        public string prefix;
        public string explanation;
        public string CreatedBy;
        public string OverrideEicon;
        public double Rarity; //defaults to 1.0, roll beneath this number on (0-1) double to keep the result. if not, reroll
        public EnchantmentFlag Flag;
        public bool HidePotionDetails;
        public bool HideValue;
        public bool Nsfw;
        public int Value;

        public Enchantment(bool nsfw, string prefix, string suffix, string explanation, string createdBy, double rarity = 1, EnchantmentFlag flag = EnchantmentFlag.NONE)
        {
            this.Nsfw = nsfw;
            this.suffix = suffix;
            this.prefix = prefix;
            this.explanation = explanation;
            this.Rarity = rarity;
            this.Flag = flag;
            this.CreatedBy = createdBy;
            this.OverrideEicon = null;
            this.HidePotionDetails = false;
            this.HideValue = false;
            this.Value = 500;
        }

        public void Copy(Enchantment enchantment)
        {
            this.Nsfw = enchantment.Nsfw;
            this.suffix = enchantment.suffix;
            this.prefix = enchantment.prefix;
            this.explanation = enchantment.explanation;
            this.Rarity = enchantment.Rarity;
            this.Flag = enchantment.Flag;
            this.CreatedBy = enchantment.CreatedBy;
            this.OverrideEicon = enchantment.OverrideEicon;
            this.HidePotionDetails = enchantment.HidePotionDetails;
            this.HideValue = enchantment.HideValue;
            this.Value = enchantment.Value;
        }

        public bool IsNsfw()
        {
            return Nsfw;
        }
    }

    public enum EnchantmentFlag
    {
        NONE,
        WhisperPotionRevealFake,
        RequireRollBondage,
        RequireRollHumiliation,
        RequireRollSexToy,
        RequireRollPunishment,
        UserGenerated
    }

    //this is used to avoid printint out the CreatedBy field to make it invisible to people who use !potionJson
    public class IgnoreCreatedByResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (property.PropertyName.Equals("CreatedBy", StringComparison.OrdinalIgnoreCase))
            {
                property.ShouldSerialize = _ => false;
            }

            return property;
        }
    }
}
