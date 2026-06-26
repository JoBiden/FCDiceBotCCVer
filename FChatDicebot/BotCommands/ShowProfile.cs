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
    public class ShowProfile : ChatBotCommand
    {
        public ShowProfile()
        {
            Name = "showprofile";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string messageString = "";

            CharacterData characterData = bot.DiceBot.GetCharacterData(address);

            //allow input of character name to show profile
            string allInputs = TextFormat.GetPlayerNameFromInputs(bot, rawTerms, false);
            MessageAddress shownCharacterAddress = new MessageAddress(address, address.character);
            if (!string.IsNullOrEmpty(allInputs))
            {
                CharacterData tempD = bot.DiceBot.GetCharacterData(new MessageAddress() { character = allInputs, channel = address.channel, guild = address.guild });
                if (tempD != null)
                {
                    characterData = tempD;
                    shownCharacterAddress = new MessageAddress(address, characterData.Character);
                }
            }

            ChipPile pile = bot.DiceBot.GetChipPile(shownCharacterAddress);

            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            if(characterData == null)
            {
                messageString = "Error: Unable to retrieve Character Data.";
            }
            else if(pile == null)
            {
                messageString = "Error: Unable to retrieve " + BotMain.CurrencyPlaceholder + "s Pile.";
            }
            else
            {
                double slotsTimeSinceLastSpin = DoubleTime.GetCurrentTimestampSeconds() - characterData.LastSlotsSpin;
                if (slotsTimeSinceLastSpin < 0)
                    slotsTimeSinceLastSpin = 0;
                string slotsCdString = DoubleTime.PrintTimeFromSeconds(thisChannel.SlotsCooldownSeconds - slotsTimeSinceLastSpin);//BotMain.SlotsSpinCooldownSeconds - slotsTimeSinceLastSpin);

                double currentDoubleTime = DoubleTime.GetCurrentTimestampSeconds();
                double timeSinceWork = currentDoubleTime - characterData.LastWorkedTime;
                double secondsRemain = thisChannel.WorkCooldownSeconds - timeSinceWork;// BotMain.WorkCooldownSeconds - timeSinceWork;
                string workCdString = DoubleTime.PrintTimeFromSeconds(secondsRemain);

                double timeSinceForecast = currentDoubleTime - characterData.LastLuckForecastTime;
                secondsRemain = thisChannel.LuckForecastCooldownSeconds - timeSinceForecast;
                string luckCdString = DoubleTime.PrintTimeFromSeconds(secondsRemain);

                double timeSinceGreet = currentDoubleTime - characterData.LastGreeted;
                secondsRemain = BotMain.GreetCharacterCooldownSeconds - timeSinceGreet;
                string greetCdString = DoubleTime.PrintTimeFromSeconds(secondsRemain);

                double dungeonDelveTime = DoubleTime.GetCurrentTimestampSeconds() - characterData.LastDungeonDelve;
                if (dungeonDelveTime < 0)
                    dungeonDelveTime = 0;
                string dungeonCdString = DoubleTime.PrintTimeFromSeconds(thisChannel.DungeonDelveCooldownSeconds - dungeonDelveTime);


                secondsRemain = (characterData.TimeoutDuration + characterData.TimeoutStartTime) - currentDoubleTime;
                string timeoutString = DoubleTime.PrintTimeFromSeconds(secondsRemain);

                string lastMessageDateString = DoubleTime.ConvertFromSecondsTimestamp(characterData.LastCommand).ToString();

                string holdingPotionRow = "Dice: [b]" + (characterData.DiceUnlocked? "[color=yellow]Gold[/color]" : "Normal") +"[/b] Holding Potion: [b]" + (characterData.Inventory.Count(a => a.GetItemCategory() == ItemCategory.Potion) > 0) + "[/b]";
                messageString = string.Format("Name: [b]{0}[/b]\nChannel: {1}, " + BotMain.CurrencyPlaceholderCapital + "s: [b]{2}[/b]\nWorked: [b]{3}[/b], Slots: [b]{4}[/b], Potions: [b]{5}[/b], Luck Forecast: [b]{6}[/b], Dungeon Delve: [b]{7}[/b]{8}{9}{10}{11}" +
                    "\n{12}\nWork Cooldown: [b]{13}[/b], Slots Cooldown: [b]{14}[/b], Luck Cooldown: [b]{15}[/b], Dungeon Delve Cooldown: [b]{16}[/b], Greet Cooldown: [b]{17}[/b], Timeout Remaining: [b]{18}[/b], Last Command in Channel: [b]{19}[/b]",
                    TextFormat.GetCharacterUserTags(characterData.Character), characterData.Channel, pile.Chips, characterData.TimesWorked, characterData.TimesSlotsSpun, characterData.TimesPotionGenerated, characterData.TimesLuckForecast, characterData.TimesDungeonDelved, "","","","",
                    holdingPotionRow, workCdString, slotsCdString, luckCdString, dungeonCdString, greetCdString, timeoutString, lastMessageDateString);
            }

            bot.SendMessageInChannel(messageString, address);
        }
    }
}
