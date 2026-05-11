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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string messageString = "";
            CharacterData characterData = bot.DiceBot.GetCharacterData(characterName, channel);
            ChipPile pile = bot.DiceBot.GetChipPile(characterName, channel);

            if(characterData == null)
            {
                messageString = "Error: Unable to retrieve Character Data.";
            }
            else if(pile == null)
            {
                messageString = "Error: Unable to retrieve Chip Pile.";
            }
            else
            {
                double slotsTimeSinceLastSpin = Utils.GetCurrentTimestampSeconds() - characterData.LastSlotsSpin;
                if (slotsTimeSinceLastSpin < 0)
                    slotsTimeSinceLastSpin = 0;
                string slotsCdString = Utils.PrintTimeFromSeconds(BotMain.SlotsSpinCooldownSeconds - slotsTimeSinceLastSpin);

                double currentDoubleTime = Utils.GetCurrentTimestampSeconds();
                double timeSinceWork = currentDoubleTime - characterData.LastWorkedTime;
                double secondsRemain = BotMain.WorkCooldownSeconds - timeSinceWork;
                string workCdString = Utils.PrintTimeFromSeconds(secondsRemain);

                double timeSinceForecast = currentDoubleTime - characterData.LastLuckForecastTime;
                secondsRemain = BotMain.LuckForecastCooldownSeconds - timeSinceForecast;
                string luckCdString = Utils.PrintTimeFromSeconds(secondsRemain);

                double timeSinceGreet = currentDoubleTime - characterData.LastGreeted;
                secondsRemain = BotMain.GreetCharacterCooldownSeconds - timeSinceGreet;
                string greetCdString = Utils.PrintTimeFromSeconds(secondsRemain);

                string holdingPotionRow = "Dice: [b]" + (characterData.DiceUnlocked? "[color=yellow]Gold[/color]" : "Normal") +"[/b] Holding Potion: [b]" + (characterData.Inventory.Count(a => a.GetItemCategory() == ItemCategory.Potion) > 0) + "[/b]";
                messageString = string.Format("Name: [b]{0}[/b]\nChannel: {1}, Chips: [b]{2}[/b]\nWorked: [b]{3}[/b], Slots: [b]{4}[/b], Potions: [b]{5}[/b], Luck Forecast: [b]{6}[/b]\n{7}\nWork Cooldown: [b]{8}[/b], Slots Cooldown: [b]{9}[/b], Luck Cooldown: [b]{10}[/b], Greet Cooldown: [b]{11}[/b]", Utils.GetCharacterUserTags(characterData.Character), characterData.Channel, pile.Chips,
                    characterData.TimesWorked, characterData.TimesSlotsSpun, characterData.TimesPotionGenerated, characterData.TimesLuckForecast, holdingPotionRow,
                    workCdString, slotsCdString, luckCdString, greetCdString);
            }

            bot.SendMessageInChannel(messageString, channel);
        }
    }
}
