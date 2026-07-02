using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            SettingType changed = SettingType.NONE;

            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            bool setValue = false;
            int setNumber = 0;
            char setChar = '!';
            string resultString = "";
            string setString = "";

            if (terms != null && terms.Length >= 1)
            {
                string[] remainingStrings = rawTerms;

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
                if (terms.Contains("allowsettingchanneldescription"))
                    changed = SettingType.AllowSettingChannelDescription;
                if (terms.Contains("allownsfw"))
                    changed = SettingType.AllowNsfw;
                if (terms.Contains("allowrpg"))
                    changed = SettingType.AllowRPG;
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
                if (terms.Contains("defaultslotstype"))
                    changed = SettingType.DefaultSlotsType;
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
                if (terms.Contains("allowrandomevents"))
                    changed = SettingType.AllowRandomEvents;
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
                if (terms.Contains("sortcards"))
                    changed = SettingType.SortCards;
                if (terms.Contains("usedefaultpotions"))
                    changed = SettingType.UseDefaultPotions;
                if (terms.Contains("potionchipscost"))
                    changed = SettingType.PotionChipsCost;
                if (terms.Contains("onlyopviewotherschips"))
                    changed = SettingType.OnlyOpViewOthersChips;
                if (terms.Contains("workcooldownseconds"))
                    changed = SettingType.WorkCooldownSeconds;
                if (terms.Contains("slotscooldownseconds"))
                    changed = SettingType.SlotsCooldownSeconds;
                if (terms.Contains("showspoilerslots"))
                    changed = SettingType.ShowSpoilerSlots;
                if (terms.Contains("showcommasinnumbers"))
                    changed = SettingType.ShowCommasInNumbers;
                if (terms.Contains("currencyname"))
                {
                    changed = SettingType.CurrencyName;
                    remainingStrings = Utils.GetRemainingTermsAfterRemovingOneTerm(rawTerms, "currencyname");
                }
                if (terms.Contains("currencynameplural"))
                    changed = SettingType.CurrencyNamePlural;
                if (terms.Contains("greetingmessage"))
                    changed = SettingType.GreetingMessage;
                if (terms.Contains("greetusersonlyonceever"))
                    changed = SettingType.GreetUsersOnlyOnceEver;
                if (terms.Contains("luckforecastchipscost"))
                    changed = SettingType.LuckForecastChipsCost;
                if (terms.Contains("singleplayergamecooldownseconds"))
                    changed = SettingType.SinglePlayerGameCooldownSeconds;
                if (terms.Contains("luckforecastcooldownseconds"))
                    changed = SettingType.LuckForecastCooldownSeconds;
                if (terms.Contains("dungeondelvecooldownseconds"))
                    changed = SettingType.DungeonDelveCooldownSeconds;
                if (terms.Contains("workcommandalias"))
                    changed = SettingType.WorkCommandAlias;
                if (terms.Contains("useplayingcardemotes"))
                    changed = SettingType.UsePlayingCardEmotes;

                if (terms.Contains("showindirectory"))
                    changed = SettingType.ShowInDirectory;
                if (terms.Contains("channeldisplayname"))
                {
                    changed = SettingType.ChannelDisplayName;
                    remainingStrings = Utils.GetRemainingTermsAfterRemovingOneTerm(remainingStrings, "channeldisplayname");
                }
                if (terms.Contains("directorylisting"))
                {
                    changed = SettingType.DirectoryListing;
                    remainingStrings = Utils.GetRemainingTermsAfterRemovingOneTerm(remainingStrings, "directorylisting");
                }
                if (terms.Contains("potioncommandsalias"))
                {
                    changed = SettingType.PotionCommandsAlias;
                    remainingStrings = Utils.GetRemainingTermsAfterRemovingOneTerm(remainingStrings, "potioncommandsalias");
                }
                if (terms.Contains("spoileralloutputs"))
                    changed = SettingType.SpoilerAllOutputs;

                remainingStrings = Utils.GetRemainingTermsAfterRemovingOneTerm(remainingStrings, "currencynameplural");
                remainingStrings = Utils.GetRemainingTermsAfterRemovingOneTerm(remainingStrings, "greetingmessage");
                remainingStrings = Utils.GetRemainingTermsAfterRemovingOneTerm(remainingStrings, "workcommandalias");

                setString = string.Join(" ", remainingStrings);
                setString = Utils.SanitizeInput(setString);
                setString = Utils.LimitStringToNCharacters(setString, BotMain.MaximumSettingStringCharacters);

                if (terms.Contains("on") || terms.Contains("yes") || terms.Contains("true"))
                    setValue = true;
                else if (terms.Contains("off") || terms.Contains("no") || terms.Contains("false"))
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
                case SettingType.AllowSettingChannelDescription:
                    thisChannel.AllowSettingChannelDescription = setValue;
                    break;
                case SettingType.AllowChess:
                    thisChannel.AllowChess = setValue;
                    break;
                case SettingType.AllowChessEicons:
                    thisChannel.AllowChessEicons = setValue;
                    break;
                case SettingType.AllowNsfw:
                    thisChannel.AllowNsfw = setValue;
                    break;
                case SettingType.AllowRPG:
                    thisChannel.AllowRPG = setValue;
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
                case SettingType.DefaultSlotsType:
                    {
                        SlotsType slotTypeFound = SlotsType.Default;
                        resultString = "(Default)";
                        if(terms.Contains("bondage"))
                        {
                            slotTypeFound = SlotsType.Bondage;
                            resultString = "(Bondage Slots)";
                        }
                        else if(terms.Contains("fruit") || terms.Contains("fruits"))
                        {
                            slotTypeFound = SlotsType.Fruit;
                            resultString = "(Fruit Slots)";
                        }
                        else if(terms.Contains("melty"))
                        {
                            slotTypeFound = SlotsType.Melty;
                            resultString = "(Melty Slots)";
                        }

                        thisChannel.DefaultSlotsType = slotTypeFound;
                    }
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
                case SettingType.AllowRandomEvents:
                    thisChannel.AllowRandomEvents = setValue;
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
                case SettingType.SortCards:
                    if (thisChannel.CardPrintSetting == null)
                    {
                        thisChannel.CardPrintSetting = new PrintSetting() { SortCards = setValue };
                    }
                    else
                        thisChannel.CardPrintSetting.SortCards = setValue;
                    break;
                case SettingType.UsePlayingCardEmotes:
                    if (thisChannel.CardPrintSetting == null)
                    {
                        thisChannel.CardPrintSetting = new PrintSetting() { UsePlayingCardEmotes = setValue };
                    }
                    else
                        thisChannel.CardPrintSetting.UsePlayingCardEmotes = setValue;
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
                case SettingType.OnlyOpViewOthersChips:
                    thisChannel.OnlyOpViewOthersChips = setValue;
                    break;
                case SettingType.WorkCooldownSeconds:
                    thisChannel.WorkCooldownSeconds = setNumber;
                    break;
                case SettingType.SlotsCooldownSeconds:
                    thisChannel.SlotsCooldownSeconds = setNumber;
                    break;
                case SettingType.ShowSpoilerSlots:
                    thisChannel.ShowSpoilerSlots = setValue;
                    break;
                case SettingType.ShowCommasInNumbers:
                    thisChannel.ShowCommasInNumbers = setValue;
                    break;
                case SettingType.CurrencyName:
                    thisChannel.CurrencyName = setString.ToLower().Replace(" ", "");
                    thisChannel.CurrencyNamePlural = setString.ToLower().Replace(" ", "") + "s";
                    break;
                case SettingType.CurrencyNamePlural:
                    thisChannel.CurrencyNamePlural = setString.ToLower().Replace(" ", "");
                    break;
                case SettingType.GreetingMessage:
                    thisChannel.GreetingMessage = setString;
                    break;
                case SettingType.GreetUsersOnlyOnceEver:
                    thisChannel.GreetUsersOnlyOnceEver = setValue;
                    break;
                case SettingType.LuckForecastChipsCost:
                    thisChannel.LuckForecastChipsCost = setNumber;
                    break;
                case SettingType.SinglePlayerGameCooldownSeconds:
                    thisChannel.SinglePlayerGameCooldownSeconds = setNumber;
                    break;
                case SettingType.LuckForecastCooldownSeconds:
                    thisChannel.LuckForecastCooldownSeconds = setNumber;
                    break;
                case SettingType.DungeonDelveCooldownSeconds:
                    thisChannel.DungeonDelveCooldownSeconds = setNumber;
                    break;
                case SettingType.WorkCommandAlias:
                    if(bot.BotCommandController.BotCommands.Count(a => a.Name.ToLower() == setString.ToLower()) == 0) //no commands exist with this name yet
                        thisChannel.WorkCommandAlias = setString.ToLower().Replace(" ", "");
                    break;
                case SettingType.ShowInDirectory:
                    thisChannel.ShowInDirectory = setValue;
                    break;
                case SettingType.ChannelDisplayName:
                    thisChannel.ChannelDisplayName = setString;
                    break;
                case SettingType.DirectoryListing:
                    thisChannel.DirectoryListing = setString;
                    break;
                case SettingType.PotionCommandsAlias:
                    if (bot.BotCommandController.BotCommands.Count(a => a.Name.ToLower() == setString.ToLower()) == 0) //no commands exist with this name yet
                        thisChannel.PotionCommandsAlias = setString.ToLower().Replace(" ", "");
                    break;
                case SettingType.SpoilerAllOutputs:
                    thisChannel.SpoilerAllOutputs = setValue;
                    break;

            }

            if (changed == SettingType.NONE)
            {
                string output = "Failed: Setting not found. Be sure to specify which setting to change, followed by 'true' or 'false'. Settings use the same name displayed in the !viewsettings command.";

                bot.SendMessageInChannel(output, address);
            }
            else if (changed == SettingType.WorkCommandAlias && bot.BotCommandController.BotCommands.Count(a => a.Name.ToLower() == setString.ToLower().Replace(" ", "")) > 0)
            {
                string output = "Failed: WorkCommandAlias cannot be set to " + setString.ToLower().Replace(" ", "") + " because another command exists with this name.";

                bot.SendMessageInChannel(output, address);
            }
            else
            {
                commandController.SaveChannelSettingsToDisk();

                string output = "(Channel setting updated) " + TextFormat.GetCharacterUserTags(address.character) + " set " + changed + " to ";
                if (!string.IsNullOrEmpty(resultString))
                    output += resultString;
                else if (changed == SettingType.CurrencyName)
                {
                    output += setString + " (also set CurrencyNamePlural to '" + setString + "s')";
                }
                else if (changed == SettingType.CurrencyNamePlural || changed == SettingType.GreetingMessage || changed == SettingType.WorkCommandAlias
                    || changed == SettingType.PotionCommandsAlias
                     || changed == SettingType.DirectoryListing || changed == SettingType.ChannelDisplayName)
                {
                    output += setString;
                }
                else if (changed == SettingType.CommandPrefix)
                    output += setChar.ToString();
                else if (changed == SettingType.SlotsMultiplierLimit || changed == SettingType.StartingChips
                    || changed == SettingType.WorkMultiplier || changed == SettingType.WorkTierRange || changed == SettingType.WorkBaseAmount
                    || changed == SettingType.PotionChipsCost || changed == SettingType.WorkCooldownSeconds || changed == SettingType.SlotsCooldownSeconds
                    || changed == SettingType.LuckForecastCooldownSeconds || changed == SettingType.LuckForecastChipsCost || changed == SettingType.SinglePlayerGameCooldownSeconds
                    || changed == SettingType.DungeonDelveCooldownSeconds)
                    output += setNumber.ToString();
                else
                    output += setValue.ToString();

                if (changed == SettingType.AllowChessEicons)
                    output += "(Note: This setting requires contact with the bot admin to work and display the chess eicons in your channel)";

                if ((changed == SettingType.OnlyOpAddChips || changed == SettingType.AllowWork) && thisChannel.ChipsClearance == ChipsClearanceLevel.DicebotAdmin)
                {
                    output = "(" + changed + " Channel setting cannot be updated for this channel) ";
                }

                bot.SendMessageInChannel(output, address);
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
        AllowSettingChannelDescription,
        AllowNsfw,
        AllowRPG,
        UseVcAccountForChips,
        StartupChannel,
        OnlyOpAddChips,
        OnlyOpBotCommands,
        OnlyOpDeckControls,
        OnlyOpTableCommands,
        StartWith500Chips,
        RemoveSlotsCooldown,
        RemoveLuckForecastCooldown,
        DefaultSlotsType,
        CommandPrefix,
        AllowChessEicons,
        AllowChess,
        SlotsMultiplierLimit,
        StartingChips,
        AllowWork,
        AllowRandomEvents,
        WorkMultiplier,
        WorkTierRange,
        WorkBaseAmount,
        UseFourColorTradingCards,
        UseTarotIcons,
        UseDefaultPotions,
        PotionChipsCost,
        SortCards,
        OnlyOpViewOthersChips,
        WorkCooldownSeconds,
        SlotsCooldownSeconds,
        ShowSpoilerSlots,
        ShowCommasInNumbers,
        CurrencyName,
        CurrencyNamePlural,
        GreetingMessage,
        GreetUsersOnlyOnceEver,
        SinglePlayerGameCooldownSeconds,
        LuckForecastChipsCost,
        LuckForecastCooldownSeconds,
        WorkCommandAlias,
        UsePlayingCardEmotes,
        ShowInDirectory,
        ChannelDisplayName,
        DirectoryListing,
        PotionCommandsAlias,
        SpoilerAllOutputs,
        DungeonDelveCooldownSeconds
    }
}
        ////todo: apply settings to code
        //public bool UseEicons;
        //public bool UseVcAccountForChips;
        //OnlyOpBotCommands,
        //OnlyOpDeckControls,
        //OnlyOpTableCommands,
