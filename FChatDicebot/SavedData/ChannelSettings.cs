using FChatDicebot.DiceFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.SavedData
{
    public class ChannelSettings
    {
        public string Name;

        public char CommandPrefix;
        public bool UseEicons; //todo: create feature and apply 
        public bool GreetNewUsers;
        public bool AllowTableRolls;
        public bool AllowCustomTableRolls;
        public bool AllowSlots;
        public bool AllowTableInfo;
        public bool AllowChips;
        public bool AllowGames;
        public bool AllowChess;
        public bool AllowChessEicons;
        public ChipsClearanceLevel ChipsClearance;
        public int SlotsMultiplierLimit;
        public bool StartWith500Chips; //now defunct : not used anywhere (except update setting)
        public int StartingChips;
        public bool RemoveSlotsCooldown;
        public bool RemoveLuckForecastCooldown;
        public bool DefaultSlotsFruit;
        public bool UseVcAccountForChips;
        public bool StartupChannel;
        public bool AllowWork;
        public int WorkMultiplier;
        public int WorkTierRange;
        public int WorkBaseAmount;
        public PrintSetting CardPrintSetting;
        public bool UseDefaultPotions;
        public int PotionChipsCost;

        //todo: these settings are currently inactive
        public bool OnlyChannelOpsCanUseAnyBotCommands;
        public bool OnlyChannelOpsCanUseDeckControls;
        public bool OnlyChannelOpsCanUseTableCommands;

        public bool Essential;
    }

    public enum ChipsClearanceLevel
    {
        NONE,
        ChannelOp,
        DicebotAdmin
    }

    public enum SlotsMultiplierLimit
    {
        NoLimit,
        TimesOneHundred,
        TimesTwenty,
        TimesTen,
        NoMultiplierAllowed
    }
}
