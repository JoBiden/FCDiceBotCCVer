using FChatDicebot.DiceFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.SavedData
{
    public class SavedPotion
    {
        public string Channel;
        public string OriginCharacter;
        public Enchantment Enchantment;
        public bool DefaultPotion;

        public void Copy(SavedPotion potion)
        {
            Channel = potion.Channel;
            OriginCharacter = potion.OriginCharacter;
            Enchantment = potion.Enchantment;
            DefaultPotion = potion.DefaultPotion;
        }
    }
}
