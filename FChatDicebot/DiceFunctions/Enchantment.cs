using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class Enchantment
    {
        public string suffix;
        public string prefix;
        public string explanation;
        public bool kinky;
        public string CreatedBy;
        public string OverrideEicon;
        public double Rarity; //defaults to 1.0, roll beneath this number on (0-1) double to keep the result. if not, reroll
        public EnchantmentFlag Flag;
        public bool HidePotionDetails;

        public Enchantment(bool kinky, string prefix, string suffix, string explanation, double rarity = 1, EnchantmentFlag flag = EnchantmentFlag.NONE)
        {
            this.kinky = kinky;
            this.suffix = suffix;
            this.prefix = prefix;
            this.explanation = explanation;
            this.Rarity = rarity;
            this.Flag = flag;
            this.CreatedBy = DiceBot.DiceBotCharacter;
            this.OverrideEicon = null;
            this.HidePotionDetails = false;
        }

        public void Copy(Enchantment enchantment)
        {
            this.kinky = enchantment.kinky;
            this.suffix = enchantment.suffix;
            this.prefix = enchantment.prefix;
            this.explanation = enchantment.explanation;
            this.Rarity = enchantment.Rarity;
            this.Flag = enchantment.Flag;
            this.CreatedBy = enchantment.CreatedBy;
            this.OverrideEicon = enchantment.OverrideEicon;
            this.HidePotionDetails = enchantment.HidePotionDetails;
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
}
