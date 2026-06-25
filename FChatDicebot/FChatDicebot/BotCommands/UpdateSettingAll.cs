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
    //NOTE: admin only used to manually edit settings for every channel at once
    public class UpdateSettingAll : ChatBotCommand
    {
        public UpdateSettingAll()
        {
            Name = "updatesettingall";
            RequireBotAdmin = true;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedChannels;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            //foreach (var settings in bot.SavedChannelSettings)
            //{
            //    settings.AllowNsfw = true;
            //}

            //commandController.SaveChannelSettingsToDisk();

            //foreach (var table in bot.SavedTables)
            //{
            //    if(table.Table != null)
            //        table.Table.Nsfw = true;
            //}

            //commandController.SaveCustomTablesToDisk();

            //foreach(var deck in bot.SavedDecks)
            //{
            //    deck.Nsfw = true;
            //}
            //commandController.SaveCustomDecksToDisk();

            //SlotsSetting bondage = commandController.GetDefaultSlotsSetting(SlotsType.Bondage);
            //bondage.Nsfw = true;

            //Utils.WriteToFileAsData(bot.SavedSlots, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedSlotsFileName));

            //string output = "(All channel data updated) set nsfw allow to try, updated tables to mark them as nsfw, updated bondage to snfw, updated decks to mark as nsfw";
            foreach (var settings in bot.SavedChannelSettings)
            {
                settings.DungeonDelveCooldownSeconds = BotMain.DungeonDelveCooldownSeconds;
                //if (settings.dungeondelvecooldownseconds == 200 && settings.LuckForecastCooldownSeconds == 300)
                //{
                //    settings.LuckForecastChipsCost = 10;
                //    settings.LuckForecastCooldownSeconds = 72000;
                //}
            }


            //foreach (var settings in bot.SavedChannelSettings)
            //{
            //    settings.PotionCommandsAlias = "potion";
            //    settings.SpoilerAllOutputs = false;

            //    //settings.CurrencyName = BotMain.DefaultCurrencyName;// = true;
            //    //settings.CurrencyNamePlural = BotMain.DefaultCurrencyNamePlural;// = true;
            //    //settings.ShowCommasInNumbers = false;
            //    //settings.LuckForecastCooldownSeconds = 300;
            //    //settings.LuckForecastChipsCost = 200;
            //    //settings.GreetUsersOnlyOnceEver = false;
            //    //settings.SinglePlayerGameCooldownSeconds = 300;
            //    //settings.GreetingMessage = "Welcome to the channel, [CHARACTERNAME]!";

            //    //      TotalBotMessages = 0,
            //    //    LastBotmessageToChannel = 0,
            //    //    LastBlackjackGameTime = 0,
            //    ////    LastRouletteSpinTime = 0,
            //    //settings.ChannelDisplayName = "NoName";
            //    //settings.DirectoryListing = "Join our channel!";
            //    //settings.ShowInDirectory = false;
            //    ////settings.AllowDirectory = false;
            //    //settings.CreationDate = DoubleTime.GetCurrentTimestampSeconds();

            //    //settings.WorkCommandAlias = "work";
            //}


            bot.BotCommandController.SaveChannelSettingsToDisk();

            string output = "all channels set dungeondelvecooldownseconds";
            bot.SendMessageInChannel(output, address);
        }
    }

}