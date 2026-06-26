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
    public class ViewSettings : ChatBotCommand
    {
        public ViewSettings()
        {
            Name = "viewsettings";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedChannels;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            string settingsPart1 = string.Format("{0} [b]Channel Settings[/b]: \n" +
                "[b]StartupChannel:[/b] {1}, [b]AllowTableInfo:[/b] {2}, [b]AllowTableRolls:[/b] {3}\n" +
                "[b]AllowCustomTableRolls:[/b] {4}, {5}[b]AllowChips:[/b]{6}, [b]AllowSlots:[/b] {7}\n" +
                "[b]Add Chips Restriction:[/b] {8} (OnlyOpAddChips), [b]StartingChips[/b]: {9}, [b]RemoveSlotsCooldown:[/b] {10}, [b]DefaultSlotsType:[/b] {11}, [b]SlotsMultiplierLimit:[/b] {12}\n" +
                "[b]AllowGames:[/b] {13}, [b]AllowSettingChannelDescription:[/b] {14},{15}{16} [b]AllowChess:[/b] {17}, [b]AllowChessEicons:[/b] {18}, [b]CommandPrefix:[/b] {19}, [b]SinglePlayerGameCooldownSeconds:[/b] {20}\n" +
                "[b]UseFourColorTradingCards:[/b] {21}, [b]SortCards:[/b] {22}, [b]UseTarotIcons:[/b] {23}, [b]UsePlayingCardEmotes:[/b] {24}, [b]UseDefaultPotions:[/b] {25}, [b]PotionChipsCost:[/b] {26} [b]PotionCommandsAlias:[/b] {27}\n"

                ,
                thisChannel.Name,
                thisChannel.StartupChannel, thisChannel.AllowTableInfo, thisChannel.AllowTableRolls,
                thisChannel.AllowCustomTableRolls, "", thisChannel.AllowChips, thisChannel.AllowSlots,
                thisChannel.ChipsClearance.ToString(), thisChannel.StartingChips, thisChannel.RemoveSlotsCooldown, thisChannel.DefaultSlotsType, thisChannel.SlotsMultiplierLimit,
                thisChannel.AllowGames, thisChannel.AllowSettingChannelDescription, "", "", thisChannel.AllowChess, thisChannel.AllowChessEicons, thisChannel.CommandPrefix.ToString(), thisChannel.SinglePlayerGameCooldownSeconds,
                thisChannel.CardPrintSetting != null && thisChannel.CardPrintSetting.FourColorPlayingCards, thisChannel.CardPrintSetting != null && thisChannel.CardPrintSetting.SortCards, thisChannel.CardPrintSetting != null && thisChannel.CardPrintSetting.TarotIcons, thisChannel.CardPrintSetting != null && thisChannel.CardPrintSetting.UsePlayingCardEmotes, thisChannel.UseDefaultPotions, thisChannel.PotionChipsCost, thisChannel.PotionCommandsAlias
                );
            string settingsPart1b = string.Format("[b]AllowWork:[/b] {0}, [b]WorkMultiplier:[/b] {1}, [b]WorkTierRange:[/b] {2}, [b]WorkBaseAmount:[/b] {3}, [b]WorkCommandAlias:[/b] {4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}{15}{16}{17}{18}{19}\n" +
                "[b]OnlyOpViewOthersChips:[/b] {20}, [b]SlotsCooldownSeconds:[/b] {21}, [b]WorkCooldownSeconds:[/b] {22}, [b]ShowSpoilerSlots:[/b] {23}, [b]SpoilerAllOutputs:[/b] {24}, [b]DungeonDelveCooldownSeconds:[/b] {25}{26}{27}{28}{29}\n" +
                "[b]AllowNsfw:[/b] {30}, [b]AllowRpg:[/b] {31}, [b]ShowCommasInNumbers:[/b] {32}, [b]CurrencyName:[/b] {33}, [b]CurrencyNamePlural:[/b] {34}{35}{36}\n"
                ,
                thisChannel.AllowWork, thisChannel.WorkMultiplier, thisChannel.WorkTierRange, thisChannel.WorkBaseAmount, string.IsNullOrEmpty(thisChannel.WorkCommandAlias) ? "(null)" : thisChannel.WorkCommandAlias, "","", "", "", "", "", "", "", "", "", "", "", "", "", "",
                thisChannel.OnlyOpViewOthersChips, thisChannel.SlotsCooldownSeconds, thisChannel.WorkCooldownSeconds, thisChannel.ShowSpoilerSlots, thisChannel.SpoilerAllOutputs, thisChannel.DungeonDelveCooldownSeconds, "","","","",
                thisChannel.AllowNsfw, thisChannel.AllowRPG, thisChannel.ShowCommasInNumbers, thisChannel.CurrencyName, thisChannel.CurrencyNamePlural, "", ""
                );

            string settingsPart2 = string.Format("[b]GreetNewUsers:[/b] {0}, [b]GreetUsersOnlyOnceEver:[/b] {1}, [b]GreetingMessage:[/b] {2}{3}{4}\n" +
                "[b]RemoveLuckForecastCooldown:[/b] {5}, [b]LuckForecastCooldownSeconds:[/b] {6}, [b]LuckForecastChipsCost:[/b] {7}{8}{9}\n" +
                "[b]ShowInDirectory:[/b] {10}, [b]ChannelDisplayName:[/b] {11}, [b]DirectoryListing:[/b] {12}{13}{14}\n"
                ,
                thisChannel.GreetNewUsers, thisChannel.GreetUsersOnlyOnceEver, (string.IsNullOrEmpty(thisChannel.GreetingMessage)? "(null)" : thisChannel.GreetingMessage), "", "",
                thisChannel.RemoveLuckForecastCooldown, thisChannel.LuckForecastCooldownSeconds, thisChannel.LuckForecastChipsCost, "", "",
                thisChannel.ShowInDirectory, thisChannel.ChannelDisplayName, thisChannel.DirectoryListing, "", ""
                );

            string allMessageParts = settingsPart1 + settingsPart1b + settingsPart2;

            if (terms.Contains("s") || terms.Contains("secret"))
            {
                bot.SendPrivateMessage(allMessageParts, address);
            }
            else
            {
                bot.SendMessageInChannel(allMessageParts, address);
            }
        }
    }
}