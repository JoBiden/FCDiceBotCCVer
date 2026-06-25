using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;

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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            string messageString = "";
            if(!thisChannel.AllowChips || !thisChannel.AllowWork)
            {
                messageString = "Failed: The current settings for this channel do not allow either work or " + BotMain.CurrencyPlaceholder + "s.";
            }
            else if(thisChannel.WorkMultiplier <= 0)
            {
                messageString = "Failed: The current settings for this channel have work multipler at 0! In order to earn " + BotMain.CurrencyPlaceholder + "s it must be 1 or more. The recommended starting setting is 100.";
            }
            else if (thisChannel.WorkBaseAmount < 0)
            {
                messageString = "Error: The current settings for this channel have WorkBaseAmount below 0! In order to earn " + BotMain.CurrencyPlaceholder + "s it must be 0 or more. The recommended starting setting is 0.";
            }
            else if (thisChannel.WorkTierRange <= 0)
            {
                messageString = "Failed: The current settings for this channel have work tier range at 0! In order to earn " + BotMain.CurrencyPlaceholder + "s it must be 1 or more. The recommended starting setting is 5.";
            }
            else
            {
                ChipPile chipPile = bot.DiceBot.GetChipPile(address, true);

                CharacterData thisCharacterData = bot.DiceBot.GetCharacterData(address);
                double currentDoubleTime = DoubleTime.GetCurrentTimestampSeconds();
                double timeSinceWork = currentDoubleTime - thisCharacterData.LastWorkedTime;
                double secondsRemain = thisChannel.WorkCooldownSeconds - timeSinceWork;// BotMain.WorkCooldownSeconds - timeSinceWork;

                if (secondsRemain > 0)
                {
                    string remainString = DoubleTime.PrintTimeFromSeconds(secondsRemain);
                    messageString = "Failed: You must wait another " + remainString + " to work again.";
                }
                else if (thisChannel.AllowChips && chipPile == null)
                {
                    messageString = "Error: " + BotMain.CurrencyPlaceholderCapital + "s pile not found for " + TextFormat.GetCharacterUserTags(address.character);
                }
                else
                {
                    //see if the channel has a custom jobs list
                    SavedJobsList channelJobsList = Utils.GetJobsListFromChannel(bot.SavedJobsLists, address.GetChannelKey());

                    JobsList jobsList = channelJobsList == null ? null : channelJobsList.JobsList;

                    bool error = false;

                    if (jobsList == null)
                    {
                        jobsList = new JobsList() { Description = "Default Jobs", Name = "Jobs", Nsfw = false, ShowResultName = true, ShowResultDescription = false };
                        jobsList.WorkMessage = "[CHARACTERNAME] spent the day working as [AJOBPRINT] for [b][EARNEDAMOUNT] [CURRENCYNAME][/b].";
                        jobsList.JobTiers = new List<TierEntry>();

                        TierEntry tier1 = new TierEntry() { Jobs = new List<JobEntry>(), Probability = 0.34f, RareMultiplier = 3, RareProbability = 0, Tier = 1, TierColor = "brown" };

                        tier1.Jobs.Add(new JobEntry() { Name = "Flight Attendant", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Cashier", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Fast Food Chef", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "High School Teacher", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Taxi Driver", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Casino Cage Worker", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Tailor", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "IT Support", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Car Mechanic", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Gang Member", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Dance Instructor", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Actor", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Truck Driver", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Tutor", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Secretary", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Small Streamer", Description = ""});
                        tier1.Jobs.Add(new JobEntry() { Name = "Soldier", Description = ""});


                        TierEntry tier2 = new TierEntry() { Jobs = new List<JobEntry>(), Probability = 0.33f, RareMultiplier = 3, RareProbability = 0, Tier = 2, TierColor = "cyan" };

                        tier2.Jobs.Add(new JobEntry() { Name = "Plumber", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Sailor", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Nurse", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Manager", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Tractor Driver", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Artist", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Electrician", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Military Captain", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Human Resources Supervisor", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Sculptor", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Social Worker", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Government Inspector", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Architect", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Game Designer", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Web Developer", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Full-Time Streamer", Description = ""});
                        tier2.Jobs.Add(new JobEntry() { Name = "Sales Representative", Description = ""});

                        TierEntry tier3 = new TierEntry() { Jobs = new List<JobEntry>(), Probability = 0.33f, RareMultiplier = 3, RareProbability = 0.1f, Tier = 3, TierColor = "green" };

                        tier3.Jobs.Add(new JobEntry() { Name = "Sales Representative", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Nuclear Technician", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Psychologist", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Entrepreneur", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Doctor", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "University Professor", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Detective", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Mechanical Engineer", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Database Administrator", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Military General", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Movie Director", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Biochemist", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Senior Manager", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Crypto Investor", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Police Chief", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Veterinarian", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Surgeon", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Big Youtuber", Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Inventor", Description = ""});

                        tier3.Jobs.Add(new JobEntry() { Name = "Movie Star", RareJob = true, Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Trust Fund Baby", RareJob = true, Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Chief Executive Officer", RareJob = true, Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Huge Streamer", RareJob = true, Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Mafia Boss", RareJob = true, Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Movie Star", RareJob = true, Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Rich Spouse", RareJob = true, Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Lottery Winner", RareJob = true, Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Scam Artist", RareJob = true, Description = ""});
                        tier3.Jobs.Add(new JobEntry() { Name = "Professional Athlete", RareJob = true, Description = ""});

                        if (thisChannel.AllowNsfw)
                        {
                            tier1.Jobs.Add(new JobEntry() { Name = "Hooker", Description = ""});
                            tier2.Jobs.Add(new JobEntry() { Name = "Escort", Description = ""});
                            tier3.Jobs.Add(new JobEntry() { Name = "Courtesan", Description = ""});

                            jobsList.Nsfw = true;
                        }
                        else
                        {
                            tier1.Jobs.Add(new JobEntry() { Name = "Baker", Description = ""});
                            tier2.Jobs.Add(new JobEntry() { Name = "Sports Coach", Description = ""});
                            tier3.Jobs.Add(new JobEntry() { Name = "Civil Engineer", Description = ""});
                        }


                        jobsList.JobTiers.Add(tier1);
                        jobsList.JobTiers.Add(tier2);
                        jobsList.JobTiers.Add(tier3);
                    }

                    JobRollResult jobResult = jobsList.GetRollResult(bot.DiceBot.random, -1, true);
                    if (jobResult == null || jobResult.jobNumber < 0)//failed to generate
                    {
                        messageString = jobResult == null? "Error: jobResult was null" : jobResult.ResultString;
                        error = true;
                    }

                    if (error)
                    {

                        //output already set above
                    }
                    else
                    {
                        int workEarnedAmount = (int)(((Math.Max(0, jobResult.jobTier - 1) * thisChannel.WorkTierRange) + bot.DiceBot.random.Next(thisChannel.WorkTierRange) + 1 + thisChannel.WorkBaseAmount) * thisChannel.WorkMultiplier * jobResult.rewardMultiplier);

                        List<string> vowelsAn = new List<string>() { "a", "e", "i", "o", "u" };
                        string an = string.IsNullOrEmpty(jobResult.ResultString) ? "a" : (vowelsAn.Contains(jobResult.ResultString.Replace("[b]","").Substring(0, 1).ToLower()) ? "an" : "a"); //just set here for now

                        string workString = jobsList.WorkMessage;
                        if (!string.IsNullOrEmpty(workString))
                        {
                            //workString = workString.Replace("[AJOBNAME]", an + " [color = " + jobResult.color + "]" + jobResult.ResultString + "[/color]")
                            workString = workString.Replace("[AJOBPRINT]", an + " " + jobResult.ResultString)

                                .Replace("[CHARACTERNAME]", TextFormat.GetCharacterUserTags(address.character))
                                .Replace("[CHARACTERICON]", TextFormat.GetCharacterIconTags(address.character))
                                .Replace("[JOBPRINT]", jobResult.ResultString)
                                .Replace("[EARNEDAMOUNT]", workEarnedAmount.ToString())
                                .Replace("[CURRENCYNAME]", BotMain.CurrencyPlaceholder + "s");
                        }

                        string buildMeter = "";
                        buildMeter += workString;// "You spent the day working as " + an + jobResult.ResultString + " for [b]" + workEarnedAmount + " " + BotMain.CurrencyPlaceholder + "s[/ b].";// "[color=" + jobColor + "][b]" + jobTitle + "[/b][/color] for [b]" + workEarnedAmount + " " + BotMain.CurrencyPlaceholder + "s[/b].";

                        //save cd info
                        thisCharacterData.LastWorkedTime = currentDoubleTime;
                        thisCharacterData.TimesWorked += 1;
                        commandController.SaveCharacterDataToDisk();

                        chipPile.Chips += workEarnedAmount;
                        commandController.SaveChipsToDisk("Work");
                        messageString = "[b]" + TextFormat.GetCharacterUserTags(address.character) + " worked[/b].\n" + buildMeter;
                    }

                }
            }

            bot.SendMessageInChannel(messageString, address);
        }
    }
}
