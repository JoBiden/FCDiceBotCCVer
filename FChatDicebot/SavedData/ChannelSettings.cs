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
        public string GreetingMessage;
        public bool GreetUsersOnlyOnceEver;
        public bool AllowTableRolls;
        public bool AllowCustomTableRolls;
        public bool AllowSlots;
        public bool AllowTableInfo;
        public bool AllowChips;
        public bool AllowGames;
        public bool AllowSettingChannelDescription;
        public bool AllowChess;
        public bool AllowChessEicons;
        public bool AllowNsfw;
        public bool AllowRPG;
        public ChipsClearanceLevel ChipsClearance;
        public int SlotsMultiplierLimit;
        public bool StartWith500Chips; //now defunct : not used anywhere (except update setting)
        public int StartingChips;
        public bool RemoveSlotsCooldown;
        public bool RemoveLuckForecastCooldown;
        public bool DefaultSlotsFruit;
        public SlotsType DefaultSlotsType;
        public bool UseVcAccountForChips;
        public bool StartupChannel;
        public bool AllowWork;
        public int WorkMultiplier;
        public int WorkTierRange;
        public int WorkBaseAmount;
        public bool OnlyOpViewOthersChips;
        public PrintSetting CardPrintSetting;
        public bool UseDefaultPotions;
        public int PotionChipsCost;
        public int WorkCooldownSeconds;
        public int SlotsCooldownSeconds;
        public int DungeonDelveCooldownSeconds;
        public bool ShowSpoilerSlots;
        public bool ShowCommasInNumbers;
        public string CurrencyName;
        public string CurrencyNamePlural;

        public int LuckForecastChipsCost;
        public int SinglePlayerGameCooldownSeconds;
        public int LuckForecastCooldownSeconds;

        public bool ShowInDirectory;
        public string ChannelDisplayName;
        public string DirectoryListing;

        public string WorkCommandAlias;
        public string PotionCommandsAlias;
        public bool SpoilerAllOutputs;

        //todo: these settings are currently inactive
        public bool OnlyChannelOpsCanUseAnyBotCommands;
        public bool OnlyChannelOpsCanUseDeckControls;
        public bool OnlyChannelOpsCanUseTableCommands;

        public bool Essential;

        public bool IsDiscordChannel()
        {
            return !HasNonNumericCharacters(Name);
        }

        public static bool HasNonNumericCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false; // Treat empty string as numeric (or change to true if needed)

            foreach (char c in input)
            {
                if (!char.IsDigit(c))
                    return true; // Found a non-numeric character
            }

            return false; // Only numbers were found
        }

        //channel data here
        public double LastRouletteSpinTime;
        public double LastBlackjackGameTime;
        public double LastDungeonDelveTime;
        public double LastBotmessageToChannel;
        public double CreationDate;
        public double TotalBotMessages;

        public double LastChannelOpsRequestTime;
        private string[] ChannelOps;// { get; private set; }
        public bool ChannelOpsDirty = true;

        public string[] GetChannelOps()
        {
            if (NeedNewChannelOps())
                return null;
            else
                return ChannelOps;
        }

        public bool NeedNewChannelOps()
        {
            return ChannelOpsDirty || LastChannelOpsRequestTime + BotMain.ChannelOpsRefreshSeconds < DoubleTime.GetCurrentTimestampSeconds();
        }

        public void SetChannelOps(string[] channelOps)
        {
            ChannelOpsDirty = false;
            ChannelOps = channelOps;
            LastChannelOpsRequestTime = DoubleTime.GetCurrentTimestampSeconds();
        }
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

    public enum SlotsType
    {
        Default,
        Bondage,
        Fruit,
        Melty,
        Custom
    }
}
