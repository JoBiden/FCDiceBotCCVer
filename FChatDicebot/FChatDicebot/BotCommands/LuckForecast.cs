using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class LuckForecast : ChatBotCommand
    {
        public LuckForecast()
        {
            Name = "luckforecast";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            int forecastCost = 200;

            ChipPile chipPile = bot.DiceBot.GetChipPile(characterName, channel, true);

            string messageString = "";
            
            CharacterData thisCharacterData = bot.DiceBot.GetCharacterData(characterName, channel);
            double currentDoubleTime = Utils.GetCurrentTimestampSeconds();
            double timeSinceForecast = currentDoubleTime - thisCharacterData.LastLuckForecastTime;

            if (timeSinceForecast < BotMain.LuckForecastCooldownSeconds && !thisChannel.RemoveLuckForecastCooldown)
            {
                messageString = "Failed: You must wait another " + (BotMain.LuckForecastCooldownSeconds - timeSinceForecast) + " seconds to have another luck forecast.";
            }
            else if (thisChannel.AllowChips && chipPile == null)
            {
                messageString = "Error: Chips pile not found for " + Utils.GetCharacterUserTags(characterName);
            }
            else if (thisChannel.AllowChips && chipPile.Chips < forecastCost)
            {
                messageString = "Failed: You do not have enough chips to hear the luck forecast. Held:(" + chipPile.Chips + ") Required(" + forecastCost + ")";
            }
            else
            {
                //display results
                int randomLuck = bot.DiceBot.random.Next(20);
                string buildMeter = "[color=cyan]";
                for (int i = 0; i < 20; i++ )
                {
                    if (i > 0 && i % 5 == 0)
                        buildMeter += "|";
                    buildMeter += "▇";
                    if (i == randomLuck)
                        buildMeter += "[/color][color=white]";
                }
                buildMeter += "[/color]  ";
                int luckReading = bot.DiceBot.random.Next(8);
                string fortune = "";
                string luckColor = "red";
                if(randomLuck <= 4)
                {
                    switch (luckReading)
                    {
                        case 0:
                            fortune = "Wow, this looks bad.";
                            break;
                        case 1:
                            fortune = "The cloudy future is definitely looking full of storms in your case.";
                            break;
                        case 2:
                            fortune = "I wouldn't bet on you.";
                            break;
                        case 3:
                            fortune = "That's some bad luck.";
                            break;
                        case 4:
                            fortune = "You need all the help you can get.";
                            break;
                        case 5:
                            fortune = "You've gotta pump those numbers up. Those are rookie numbers!";
                            break;
                        case 6:
                            fortune = "Probably best to just stay home where it's safe.";
                            break;
                        case 7:
                            fortune = "Well I'm not saying disaster is [b]guaranteed[/b]...";
                            break;
                    }
                }
                if (randomLuck > 4)
                {

                    luckColor = "yellow";
                    switch (luckReading)
                    {
                        case 0:
                            fortune = "I've seen worse.";
                            break;
                        case 1:
                            fortune = "Below average.";
                            break;
                        case 2:
                            fortune = "Hey you can still make it work if you try!";
                            break;
                        case 3:
                            fortune = "That's some disappointing luck.";
                            break;
                        case 4:
                            fortune = "Maybe you're used to it?";
                            break;
                        case 5:
                            fortune = "Well, I wouldn't expect to win big.";
                            break;
                        case 6:
                            fortune = "Don't let it stop you from having fun!";
                            break;
                        case 7:
                            fortune = "Honestly I wasn't sure I just said a random number.";
                            break;
                    }
                }
                if (randomLuck > 10)
                {
                    luckColor = "green";
                    switch (luckReading)
                    {
                        case 0:
                            fortune = "Not bad.";
                            break;
                        case 1:
                            fortune = "Above average.";
                            break;
                        case 2:
                            fortune = "Don't get too cocky!";
                            break;
                        case 3:
                            fortune = "That's some favorable luck.";
                            break;
                        case 4:
                            fortune = "Maybe you're used to it?";
                            break;
                        case 5:
                            fortune = "You might expect to win big.";
                            break;
                        case 6:
                            fortune = "But your fun broadcast is off the charts!";
                            break;
                        case 7:
                            fortune = "Honestly I wasn't sure I just said a random number.";
                            break;
                    }
                }
                if (randomLuck > 15)
                {
                    luckColor = "green";

                    switch (luckReading)
                    {
                        case 0:
                            fortune = "Hey that's nice.";
                            break;
                        case 1:
                            fortune = "Very good.";
                            break;
                        case 2:
                            fortune = "Nothing can go wrong!";
                            break;
                        case 3:
                            fortune = "Alright, how do I join your team?";
                            break;
                        case 4:
                            fortune = "This is your big day.";
                            break;
                        case 5:
                            fortune = "Stonks.";
                            break;
                        case 6:
                            fortune = "Woohoo!";
                            break;
                        case 7:
                            fortune = "Hey, I'm never wrong.";
                            break;
                    }
                }

                buildMeter += "[color=" + luckColor + "][b]" + ((randomLuck + 1) * 5) + "% Lucky![/b][/color] [sup]" + fortune + "[/sup] ";

                //save cd info
                thisCharacterData.LastLuckForecastTime = currentDoubleTime;
                thisCharacterData.TimesLuckForecast += 1;
                commandController.SaveCharacterDataToDisk();
                
                if(thisChannel.AllowChips)
                {
                    //deduct chips
                    chipPile.Chips -= forecastCost;

                    commandController.SaveChipsToDisk("LuckForecast");

                    messageString = "Deducted " + forecastCost + " chips from " + Utils.GetCharacterUserTags(characterName) + " to read their [b]Luck Forecast[/b]:\n" + buildMeter;
                }
                else
                {
                    messageString = Utils.GetCharacterUserTags(characterName) + "'s [b]Luck Forecast[/b]:\n" + buildMeter;
                }
            }

            bot.SendMessageInChannel(messageString, channel);
        }
    }
}
