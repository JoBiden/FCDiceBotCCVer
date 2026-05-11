using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands
{
    public class UpdateSetting : ChatBotCommand
    {
        public UpdateSetting()
        {
            Name = "updatesetting";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedChannels;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            SettingType changed = SettingType.NONE;

            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            bool setValue = false;
            int setNumber = 0;
            char setChar = '!';

            if (terms != null && terms.Length >= 1)
            {
                if (terms.Contains("useeicons"))
                    changed = SettingType.UseEicons;
                if (terms.Contains("greetnewusers"))
                    changed = SettingType.GreetNewUsers;
                if (terms.Contains("allowtablerolls"))
                    changed = SettingType.AllowTableRolls;
                if (terms.Contains("allowcustomtablerolls"))
                    changed = SettingType.AllowCustomTableRolls;
                if (terms.Contains("allowtableinfo"))
                    changed = SettingType.AllowTableInfo;
                if (terms.Contains("allowchips"))
                    changed = SettingType.AllowChips;
                if (terms.Contains("allowslots"))
                    changed = SettingType.AllowSlots;
                if (terms.Contains("allowgames"))
                    changed = SettingType.AllowGames;
                if (terms.Contains("allowchesseicons"))
                    changed = SettingType.AllowChessEicons;
                if (terms.Contains("allowchess"))
                    changed = SettingType.AllowChess;
                if (terms.Contains("usevcaccount"))
                    changed = SettingType.UseVcAccountForChips;
                if (terms.Contains("startupchannel"))
                    changed = SettingType.StartupChannel;
                if (terms.Contains("startwith500chips"))
                    changed = SettingType.StartWith500Chips;
                if (terms.Contains("startingchips"))
                    changed = SettingType.StartingChips;
                if (terms.Contains("removeslotscooldown"))
                    changed = SettingType.RemoveSlotsCooldown;
                if (terms.Contains("removeluckforecastcooldown"))
                    changed = SettingType.RemoveLuckForecastCooldown;
                if (terms.Contains("defaultslotsfruit"))
                    changed = SettingType.DefaultSlotsFruit;
                if (terms.Contains("onlyopaddchips"))
                    changed = SettingType.OnlyOpAddChips;
                if (terms.Contains("slotsmultiplierlimit"))
                    changed = SettingType.SlotsMultiplierLimit;
                if (terms.Contains("onlyopbotcommands"))
                    changed = SettingType.OnlyOpBotCommands;
                if (terms.Contains("onlyopdeckcontrols"))
                    changed = SettingType.OnlyOpDeckControls;
                if (terms.Contains("onlyoptablecommands"))
                    changed = SettingType.OnlyOpTableCommands;
                if (terms.Contains("commandprefix"))
                    changed = SettingType.CommandPrefix;
                if (terms.Contains("allowwork"))
                    changed = SettingType.AllowWork;
                if (terms.Contains("workmultiplier"))
                    changed = SettingType.WorkMultiplier;
                if (terms.Contains("worktierrange"))
                    changed = SettingType.WorkTierRange;
                if (terms.Contains("workbaseamount"))
                    changed = SettingType.WorkBaseAmount;
                if (terms.Contains("usefourcolortradingcards"))
                    changed = SettingType.UseFourColorTradingCards;
                if (terms.Contains("usetaroticons"))
                    changed = SettingType.UseTarotIcons;
                if (terms.Contains("usedefaultpotions"))
                    changed = SettingType.UseDefaultPotions;
                if (terms.Contains("potionchipscost"))
                    changed = SettingType.PotionChipsCost;

                if (terms.Contains("on") || terms.Contains("true"))
                    setValue = true;
                else if (terms.Contains("off") || terms.Contains("false"))
                    setValue = false;
                else if(terms.Length > 1)
                {
                    int numberFound = Utils.GetNumberFromInputs(terms);
                    setNumber = numberFound;
                    if (numberFound < 0)
                        numberFound = 0;

                    var lastTerm = terms[terms.Length - 1];
                    if(lastTerm.Length == 1)
                        setChar = lastTerm.ToCharArray()[0];
                }
            }



            switch (changed)
            {
                case SettingType.NONE:
                    break;
                case SettingType.UseEicons:
                    thisChannel.UseEicons = setValue;
                    break;
                case SettingType.GreetNewUsers:
                    thisChannel.GreetNewUsers = setValue;
                    break;
                case SettingType.AllowTableRolls:
                    thisChannel.AllowTableRolls = setValue;
                    break;
                case SettingType.AllowCustomTableRolls:
                    thisChannel.AllowCustomTableRolls = setValue;
                    break;
                case SettingType.AllowTableInfo:
                    thisChannel.AllowTableInfo = setValue;
                    break;
                case SettingType.AllowChips:
                    thisChannel.AllowChips = setValue;
                    break;
                case SettingType.AllowSlots:
                    thisChannel.AllowSlots = setValue;
                    break;
                case SettingType.AllowGames:
                    thisChannel.AllowGames = setValue;
                    break;
                case SettingType.AllowChess:
                    thisChannel.AllowChess = setValue;
                    break;
                case SettingType.AllowChessEicons:
                    thisChannel.AllowChessEicons = setValue;
                    break;
                case SettingType.UseVcAccountForChips:
                    thisChannel.UseVcAccountForChips = setValue;
                    break;
                case SettingType.StartupChannel:
                    thisChannel.StartupChannel = setValue;
                    break;
                case SettingType.RemoveSlotsCooldown:
                    thisChannel.RemoveSlotsCooldown = setValue;
                    break;
                case SettingType.RemoveLuckForecastCooldown:
                    thisChannel.RemoveLuckForecastCooldown = setValue;
                    break;
                case SettingType.DefaultSlotsFruit:
                    thisChannel.DefaultSlotsFruit = setValue;
                    break;
                case SettingType.StartWith500Chips:
                    thisChannel.StartWith500Chips = setValue;
                    break;
                case SettingType.StartingChips:
                    thisChannel.StartingChips = setNumber;
                    break;
                case SettingType.OnlyOpAddChips:
                    {
                        if(thisChannel.ChipsClearance != ChipsClearanceLevel.DicebotAdmin)
                        {
                            if(setValue)
                            {
                                thisChannel.ChipsClearance = ChipsClearanceLevel.ChannelOp;
                            }
                            else
                            {
                                thisChannel.ChipsClearance = ChipsClearanceLevel.NONE;
                            }
                        }
                    }
                    break;
                case SettingType.SlotsMultiplierLimit:
                    thisChannel.SlotsMultiplierLimit = setNumber;
                    break;
                case SettingType.OnlyOpBotCommands:
                    thisChannel.OnlyChannelOpsCanUseAnyBotCommands = setValue;
                    break;
                case SettingType.OnlyOpDeckControls:
                    thisChannel.OnlyChannelOpsCanUseDeckControls = setValue;
                    break;
                case SettingType.OnlyOpTableCommands:
                    thisChannel.OnlyChannelOpsCanUseTableCommands = setValue;
                    break;
                case SettingType.CommandPrefix:
                    thisChannel.CommandPrefix = setChar;
                    break;
                case SettingType.AllowWork:
                    {
                        if (thisChannel.ChipsClearance != ChipsClearanceLevel.DicebotAdmin)
                        {
                            thisChannel.AllowWork = setValue;
                        }
                    }
                    break;
                case SettingType.WorkMultiplier:
                    if (setNumber < 0)
                        setNumber = 0;
                    if (setNumber > BotMain.MaximumWorkMultiplier)
                        setNumber = BotMain.MaximumWorkMultiplier;
                    thisChannel.WorkMultiplier = setNumber;
                    break;
                case SettingType.WorkTierRange:
                    if (setNumber < 0)
                        setNumber = 0;
                    if (setNumber > BotMain.MaximumWorkTierRange)
                        setNumber = BotMain.MaximumWorkTierRange;
                    thisChannel.WorkTierRange = setNumber;
                    break;
                case SettingType.WorkBaseAmount:
                    if (setNumber < 0)
                        setNumber = 0;
                    if (setNumber > BotMain.MaximumWorkBaseAmount)
                        setNumber = BotMain.MaximumWorkBaseAmount;
                    thisChannel.WorkBaseAmount = setNumber;
                    break;
                case SettingType.UseFourColorTradingCards:
                    if(thisChannel.CardPrintSetting == null)
                    {
                        thisChannel.CardPrintSetting = new PrintSetting() { FourColorPlayingCards = setValue };
                    }
                    else
                        thisChannel.CardPrintSetting.FourColorPlayingCards = setValue;
                    break;
                case SettingType.UseTarotIcons:
                    if (thisChannel.CardPrintSetting == null)
                    {
                        thisChannel.CardPrintSetting = new PrintSetting() { TarotIcons = setValue };
                    }
                    else
                        thisChannel.CardPrintSetting.TarotIcons = setValue;
                    break;
                case SettingType.UseDefaultPotions:
                    thisChannel.UseDefaultPotions = setValue;
                    break;
                case SettingType.PotionChipsCost:
                    if (setNumber < 0)
                        setNumber = 0;
                    if (setNumber > BotMain.MaximumPotionCost)
                        setNumber = BotMain.MaximumPotionCost;
                    thisChannel.PotionChipsCost = setNumber;
                    break;
            }

            if (changed == SettingType.NONE)
            {
                string output = "Setting not found. Be sure to specify which setting to change, followed by 'true' or 'false'. Settings use the same name displayed in the !viewsettings command.";

                bot.SendMessageInChannel(output, channel);
            }
            else
            {
                commandController.SaveChannelSettingsToDisk();

                string output = "(Channel setting updated) " + Utils.GetCharacterUserTags(characterName) + " set " + changed + " to ";
                if (changed == SettingType.CommandPrefix)
                    output += setChar.ToString();
                else if (changed == SettingType.SlotsMultiplierLimit || changed == SettingType.StartingChips
                    || changed == SettingType.WorkMultiplier || changed == SettingType.WorkTierRange || changed == SettingType.WorkBaseAmount 
                    || changed == SettingType.PotionChipsCost)
                    output += setNumber.ToString();
                else
                    output += setValue.ToString();

                if ((changed == SettingType.OnlyOpAddChips || changed == SettingType.AllowWork) && thisChannel.ChipsClearance == ChipsClearanceLevel.DicebotAdmin)
                {
                    output = "(" + changed + " Channel setting cannot be updated for this channel) ";
                }

                bot.SendMessageInChannel(output, channel);
            }
        }
    }

    public enum SettingType
    {
        NONE,
        UseEicons,
        GreetNewUsers,
        AllowTableRolls,
        AllowCustomTableRolls,
        AllowTableInfo,
        AllowChips,
        AllowSlots,
        AllowGames,
        UseVcAccountForChips,
        StartupChannel,
        OnlyOpAddChips,
        OnlyOpBotCommands,
        OnlyOpDeckControls,
        OnlyOpTableCommands,
        StartWith500Chips,
        RemoveSlotsCooldown,
        RemoveLuckForecastCooldown,
        DefaultSlotsFruit,
        CommandPrefix,
        AllowChessEicons,
        AllowChess,
        SlotsMultiplierLimit,
        StartingChips,
        AllowWork,
        WorkMultiplier,
        WorkTierRange,
        WorkBaseAmount,
        UseFourColorTradingCards,
        UseTarotIcons,
        UseDefaultPotions,
        PotionChipsCost
    }
}
        ////todo: apply settings to code
        //public bool UseEicons;
        //public bool UseVcAccountForChips;
        //OnlyOpBotCommands,
        //OnlyOpDeckControls,
        //OnlyOpTableCommands,
