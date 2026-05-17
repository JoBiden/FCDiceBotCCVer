using FChatDicebot.DiceFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.SavedData
{
    //there is currently NOT a global character data: this stores character data by channel
    public class CharacterData
    {
        public string Character;
        public string Channel;

        public bool DiceUnlocked;
        public bool SpecialName;//reserved name for Dice Bot

        public List<InventoryItem> Inventory;

        public int TimesWorked;
        public int TimesSlotsSpun;
        public int TimesLuckForecast;
        public int TimesPotionGenerated;

        public double LastSlotsSpin;// DateTime LastSlotsSpin;
        public double LastLuckForecastTime;
        public double LastWorkedTime;
        public double LastGreeted;
    }
}
