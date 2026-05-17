using FChatDicebot.DiceFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.SavedData
{
    public class SavedSlotsSetting
    {
        public string SlotsId;
        public string ChannelId;
        public string OriginCharacter;
        public SlotsSetting SlotsSetting;
        public bool DefaultSlots;

        public void Copy(SavedSlotsSetting slots)
        {
            SlotsId = slots.SlotsId;
            OriginCharacter = slots.OriginCharacter;
            ChannelId = slots.ChannelId;
            SlotsSetting = slots.SlotsSetting;
            DefaultSlots = slots.DefaultSlots;
        }
    }
}
