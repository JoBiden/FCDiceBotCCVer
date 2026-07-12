using Amazon.Runtime.Documents;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.BotCommands.Support;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;

namespace FChatDicebot.BotCommands
{
    public class ChateauVolunteer : ChatBotCommand
    {
        public ChateauVolunteer()
        {
            Name = "volunteer";
            Aliases = new string[] { "v" };
            Category = "General";
            ShortDescription = "Volunteer for a job you're not currently employed in";
            LongDescription = "Volunteer at the Chateau to earn currency and try out a job you aren't employed in. Similar to !work but can be a different job each day. Each session presents choices that lead to different rewards. Use !v followed by a number to select your choice. You can only volunteer once per Chateau day.";
            Usage = "!volunteer {job}\n!v {choice number}";
            RelatedCommands = new string[] { "work", "bank", "dossier" };
            CooldownDuration = "1 day";
            CooldownAppliesTo = "initiator";
            IdentifierCategory = "job";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
            //maintenance note: this command is similar to ChateauWork, consider refactoring shared logic into a base class. For now, echo changes to both commands.
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string characterName = address.character;
            string channel = address.channel;
            Profile userProfile = MonDB.getProfile(characterName);
            PendingDuty dutyInProgress = MonDB.getPendingDuty(characterName);
            string message = "";
            Random random = new Random();

            // Check if duty is from previous day and clean it up
            if (dutyInProgress != null && dutyInProgress.startTime < DateTime.UtcNow.Date)
            {
                MonDB.removePendingDuty(dutyInProgress.Id);
                dutyInProgress = null;
                message += ChateauInteractionHandler.dutyFromPreviousDayText() + " Enough about the past though. Let's see what volunteer opportunity awaits you today... \n\n";
            }

            if (dutyInProgress != null) // responding to duty in progress
            {
                string[] providedInts = commandController.GetIntsFromCommandTermsAsStrings(rawTerms);
                int dutyChoice = -1;
                int dutyChoicesCount = dutyInProgress.dutyResults.Count;
                if (providedInts != null)
                {
                    dutyChoice = Int32.Parse(providedInts.First());
                    if (dutyChoice < 1 || dutyChoice > dutyChoicesCount)
                    {
                        message += ("Looks like you provided a number that wasn't an option. Please provide a whole number that is between 1 and " + dutyChoicesCount + ".");
                    }
                    else //valid choice
                    {
                        dutyChoice -= 1; //adjust for 0 index
                        DutyResult chosenResult = dutyInProgress.dutyResults[dutyChoice];
                        message += chosenResult.resultText + "\n\n";

                        //pick a random currency reward based on weights
                        int totalWeight = 0;
                        foreach (KeyValuePair<string, Reward> rewardEntry in chosenResult.rewardList)
                        {
                            totalWeight += rewardEntry.Value.weight;
                        }
                        int randomWeightPick = random.Next(0, totalWeight);
                        // Poverty curse: the rolled reward is shown for transparency, but no
                        // currency is actually credited. Timer/job experience still apply
                        // (the day's effort is still spent). See CurseProcessor.CatalogMap.
                        bool hasPovertyCurse = CurseInstance.LoadAll(userProfile)
                            .Any(c => string.Equals(c.Curse, "poverty", StringComparison.OrdinalIgnoreCase));
                        foreach (KeyValuePair<string, Reward> rewardEntry in chosenResult.rewardList)
                        {
                            randomWeightPick -= rewardEntry.Value.weight;
                            if (randomWeightPick <= 0)
                            {
                                //grant this reward
                                int rewardAmount = random.Next(rewardEntry.Value.min, rewardEntry.Value.max + 1); //+1 because upper bound is exclusive
                                if (hasPovertyCurse)
                                {
                                    message += "You would have received [b]" + rewardAmount + " " + rewardEntry.Value.currency + "[/b], but your poverty curse makes it vanish in a poof of smoke before you can claim it.\n";
                                }
                                else
                                {
                                    message += "You received [b]" + rewardAmount + " " + rewardEntry.Value.currency + "[/b]!\n";
                                    if (userProfile.currencies.ContainsKey(rewardEntry.Value.currency))
                                    {
                                        userProfile.currencies[rewardEntry.Value.currency] += rewardAmount;
                                    }
                                    else
                                    {
                                        userProfile.currencies.Add(rewardEntry.Value.currency, rewardAmount);
                                    }
                                }
                                break;
                            }
                        }

                        CoolDown volunteerTimer = new CoolDown();
                        volunteerTimer.timerEnd = DateTime.UtcNow.Date.AddDays(1);
                        if (userProfile.timers.ContainsKey("volunteer"))
                        {
                            userProfile.timers["volunteer"] = volunteerTimer;
                        }
                        else
                        {
                            userProfile.timers.Add("volunteer", volunteerTimer);
                        }
                        if (userProfile.jobExperience.ContainsKey(dutyInProgress.job))
                        {
                            userProfile.jobExperience[dutyInProgress.job] += 1;
                        }
                        else
                        {
                            userProfile.jobExperience.Add(dutyInProgress.job, 1);
                        }
                        MonDB.removePendingDuty(dutyInProgress.Id);
                        MonDB.setProfile(characterName, userProfile);
                        // Check for system title achievements after work completion
                        string titleNotification = ChateauSystemTitles.CheckAndGrantTitles(characterName);
                        if (!string.IsNullOrEmpty(titleNotification))
                        {
                            message += titleNotification;
                        }
                    }
                }
                else //no valid integer provided
                {
                    message += ("It looks like you didn't provide a number for your choice. Please provide a whole number that is between 1 and " + dutyChoicesCount + ".");
                }


            }
            else // no duty in progress, need to extract job and start new volunteer duty
            {
                // Extract the job from command terms
                string identifierType = "job";
                string volunteerJob = commandController.GetIdentifierFromCommandTerms(rawTerms, identifierType);

                // Check if no job was provided
                if (volunteerJob == null)
                {
                    message += ChateauInteractionHandler.typeNotFoundText(identifierType);
                }
                // Check if trying to volunteer for current job
                else if (userProfile.characteristics.ContainsKey("job") && userProfile.characteristics["job"].ToLower() == volunteerJob.ToLower())
                {
                    message += "You're already employed as " + Utils.AnOrA(Utils.JobToText(volunteerJob)) + " " + Utils.JobToText(volunteerJob) + "! If you want to do duties for your current job, use !work instead. Volunteering is for exploring other career paths.";
                }
                else if (userProfile.timers.ContainsKey("volunteer") && userProfile.timers["volunteer"].timerEnd > DateTime.UtcNow) //already volunteered
                {
                    TimeSpan timeLeft = userProfile.timers["volunteer"].timerEnd - DateTime.UtcNow;
                    message += ("You've already volunteered today! You can volunteer again when a new Chateau day starts in " + Utils.GetTimeSpanPrint(timeLeft) + ".");
                }
                else //can volunteer
                {
                    List<Duty> dutyList = MonDB.getDutiesByJob(volunteerJob);
                    if (dutyList.Count == 0)
                    {
                        //populate list with a generic duty if none exist for this job
                        dutyList = MonDB.getDutiesByJob("default");
                        dutyList[0].job = volunteerJob;
                    }
                    Duty duty = dutyList[random.Next(dutyList.Count)];
                    List<DutyResult> validResults = new List<DutyResult>();
                    message += duty.startText + "\n";
                    foreach (KeyValuePair<string, DutyResult> keyvaluepair in duty.dutyResults)
                    {
                        string conditionalKey = DutyConditionalSupport.Key(keyvaluepair.Value.conditional);
                        switch (DutyConditionalSupport.Kind(keyvaluepair.Value.conditional))
                        {
                            case "non": //none
                                validResults.Add(keyvaluepair.Value);
                                break;
                            case "job": //job experience
                                if (checkDictionaryConditional(conditionalKey, keyvaluepair.Value.conditional.value, userProfile.jobExperience))
                                {
                                    validResults.Add(keyvaluepair.Value);
                                }
                                break;
                            case "trn": //training
                                if (userProfile.lists.ContainsKey("trainings"))
                                {
                                    if (userProfile.lists["trainings"].Contains(conditionalKey))
                                    {
                                        validResults.Add(keyvaluepair.Value);
                                    }
                                }
                                break;
                            case "cur": //currency
                                if (checkDictionaryConditional(conditionalKey, keyvaluepair.Value.conditional.value, userProfile.currencies))
                                {
                                    validResults.Add(keyvaluepair.Value);
                                }
                                break;
                            case "mon": //monster/species trait or category
                                if (userProfile.characteristics.ContainsKey("monster"))
                                {
                                    if (MonDB.getIdentifier(userProfile.characteristics["monster"]).categories.Contains<string>(conditionalKey))
                                    {
                                        validResults.Add(keyvaluepair.Value);
                                    }
                                }
                                break;
                        }
                    }
                    int choiceCount = 0;
                    foreach (DutyResult resultOption in validResults)
                    {
                        choiceCount++;
                        message += "!v " + choiceCount + ": " + resultOption.choiceName + " | ";
                    }

                    message = message.TrimEnd(' ', '|');


                    PendingDuty pendingDuty = new PendingDuty();
                    pendingDuty.dutyResults = validResults;
                    pendingDuty.job = duty.job;
                    pendingDuty.dutyLabel = duty.label;
                    pendingDuty.awaitingInputFrom = characterName;
                    pendingDuty.startTime = DateTime.UtcNow;

                    MonDB.addPendingDuty(pendingDuty);
                }
            }

            bot.SendPrivateMessage(message, characterName);

            bool checkDictionaryConditional(string labelToCheck, int valueToCompare, Dictionary<string, int> dictionaryToCheck)
            {

                if (dictionaryToCheck.ContainsKey(labelToCheck))
                {
                    if (dictionaryToCheck[labelToCheck] >= valueToCompare)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}





