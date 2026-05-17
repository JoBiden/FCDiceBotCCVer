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
    public class Work : ChatBotCommand
    {
        public Work()
        {
            Name = "work";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            string messageString = "";
            if(!thisChannel.AllowChips || !thisChannel.AllowWork)
            {
                messageString = "Failed: The current settings for this channel do not allow either work or chips.";
            }
            else if(thisChannel.WorkMultiplier <= 0)
            {
                messageString = "Failed: The current settings for this channel have work multipler at 0! In order to earn chips it must be 1 or more. The recommended starting setting is 100.";
            }
            else if (thisChannel.WorkBaseAmount < 0)
            {
                messageString = "Error: The current settings for this channel have WorkBaseAmount below 0! In order to earn chips it must be 0 or more. The recommended starting setting is 0.";
            }
            else if (thisChannel.WorkTierRange <= 0)
            {
                messageString = "Failed: The current settings for this channel have work tier range at 0! In order to earn chips it must be 1 or more. The recommended starting setting is 5.";
            }
            else
            {
                ChipPile chipPile = bot.DiceBot.GetChipPile(characterName, channel, true);

                CharacterData thisCharacterData = bot.DiceBot.GetCharacterData(characterName, channel);
                double currentDoubleTime = Utils.GetCurrentTimestampSeconds();
                double timeSinceWork = currentDoubleTime - thisCharacterData.LastWorkedTime;
                double secondsRemain = BotMain.WorkCooldownSeconds - timeSinceWork;

                if (secondsRemain > 0)
                {
                    string remainString = Utils.PrintTimeFromSeconds(secondsRemain);
                    messageString = "Failed: You must wait another " + remainString + " to work again.";
                }
                else if (thisChannel.AllowChips && chipPile == null)
                {
                    messageString = "Error: Chips pile not found for " + Utils.GetCharacterUserTags(characterName);
                }
                else
                {
                    int randomJob = bot.DiceBot.random.Next(16);
                    int randomTier = bot.DiceBot.random.Next(3);
                    string buildMeter = "";
                    string jobTitle = "";
                    string jobColor = "red";
                    string an = "a";

                    switch(randomTier)
                    {
                        case 0:
                            jobColor = "brown";
                            switch(randomJob)
                            {
                                case 0:
                                    jobTitle = "Ditch Digger"; break;
                                case 1:
                                    jobTitle = "Shampooer"; break;
                                case 2:
                                    jobTitle = "Cashier"; break;
                                case 3:
                                    jobTitle = "Fast Food Chef"; break;
                                case 4:
                                    jobTitle = "Dishwasher"; break;
                                case 5:
                                    jobTitle = "Taxi Driver"; break;
                                case 6:
                                    jobTitle = "Casino Cage Worker"; break;
                                case 7:
                                    jobTitle = "Hand Sewer"; break;
                                case 8:
                                    jobTitle = "Baker"; break;
                                case 9:
                                    jobTitle = "Tire Changer"; break;
                                case 10:
                                    jobTitle = "Stocker"; break;
                                case 11:
                                    jobTitle = "Butcher"; break;
                                case 12:
                                    jobTitle = "Funeral Service Worker"; break;
                                case 13:
                                    jobTitle = "Truck Driver"; break;
                                case 14:
                                    jobTitle = "Tutor"; break;
                                case 15:
                                    jobTitle = "Porter"; break;
                            }
                            break;
                        case 1:
                            jobColor = "cyan";
                            switch(randomJob)
                            {
                                case 0:
                                    an = "an";
                                    jobTitle = "Electrician"; break;
                                case 1:
                                    jobTitle = "Plumber"; break;
                                case 2:
                                    jobTitle = "Sailor"; break;
                                case 3:
                                    jobTitle = "Paramedic"; break;
                                case 4:
                                    jobTitle = "Paralegal"; break;
                                case 5:
                                    jobTitle = "Tractor Driver"; break;
                                case 6:
                                    jobTitle = "Graphic Designer"; break;
                                case 7:
                                    jobTitle = "Statistical Assistant"; break;
                                case 8:
                                    jobTitle = "Machine Maintenance Worker"; break;
                                case 9:
                                    jobTitle = "Postal Service Clerk"; break;
                                case 10:
                                    jobTitle = "Stonemason"; break;
                                case 11:
                                    jobTitle = "Social Worker"; break;
                                case 12:
                                    an = "an";
                                    jobTitle = "Occupational Health and Safety Technician"; break;
                                case 13:
                                    jobTitle = "Drafter"; break;
                                case 14:
                                    an = "an";
                                    jobTitle = "Archivist"; break;
                                case 15:
                                    jobTitle = "Web Developer"; break;
                            }
                            break;
                        case 2:
                            jobColor = "green";
                            switch(randomJob)
                            {
                                case 0:
                                    an = "an";
                                    jobTitle = "Civil Engineer"; break;
                                case 1:
                                    jobTitle = "Nuclear Technician"; break;
                                case 2:
                                    jobTitle = "Psychologist"; break;
                                case 3:
                                    jobTitle = "Physical Therapist"; break;
                                case 4:
                                    jobTitle = "University Professor"; break;
                                case 5:
                                    jobTitle = "Detective"; break;
                                case 6:
                                    jobTitle = "Mechanical Engineer"; break;
                                case 7:
                                    jobTitle = "Database Administrator"; break;
                                case 8:
                                    jobTitle = "Lead Software Developer"; break;
                                case 9:
                                    jobTitle = "Movie Director"; break;
                                case 10:
                                    jobTitle = "Biochemist"; break;
                                case 11:
                                    jobTitle = "Corporate Executive"; break;
                                case 12:
                                    jobTitle = "Commercial Pilot"; break;
                                case 13:
                                    an = "an";
                                    jobTitle = "Economist"; break;
                                case 14:
                                    jobTitle = "Veterinarian"; break;
                                case 15:
                                    jobTitle = "Surgeon"; break;
                            }
                            break;
                    }

                    int workEarnedAmount = ((randomTier * thisChannel.WorkTierRange) + bot.DiceBot.random.Next(thisChannel.WorkTierRange) + 1 + thisChannel.WorkBaseAmount) * thisChannel.WorkMultiplier;

                    buildMeter += "You spent the day working as " + an + " [color=" + jobColor + "][b]" + jobTitle + "[/b][/color] for [b]" + workEarnedAmount + " chips[/b].";

                    //save cd info
                    thisCharacterData.LastWorkedTime = currentDoubleTime;
                    thisCharacterData.TimesWorked += 1;
                    commandController.SaveCharacterDataToDisk();

                    chipPile.Chips += workEarnedAmount;
                    commandController.SaveChipsToDisk("Work");

                    messageString = "[b]" + Utils.GetCharacterUserTags(characterName) + " worked[/b].\n" + buildMeter;
                }
            }

            bot.SendMessageInChannel(messageString, channel);
        }
    }
}
