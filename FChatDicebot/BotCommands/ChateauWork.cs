using Amazon.Runtime.Documents;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;

namespace FChatDicebot.BotCommands
{
    public class ChateauWork : ChatBotCommand
    {
        public ChateauWork()
        {
            Name = "work";
            Aliases = new string[] { "w" };
            Category = "General";
            ShortDescription = "Perform your job duties to earn currency";
            LongDescription = "Work at your current job to earn currency. You must be employed (via !employ) before you can work. Each work session presents you with choices that may lead to different rewards. Work builds job experience which unlocks additional options. Use !w followed by a number to select your choice. You can only work once per Chateau day.";
            Usage = "!work\n!w [choice number]";
            RelatedCommands = new string[] { "volunteer", "employ", "bank", "dossier" };
            CooldownDuration = "1 day";
            CooldownAppliesTo = "initiator";
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
            //maintenance note: this command is similar to ChateauVolunteer, consider refactoring shared logic into a base class. For now, echo changes to both commands.
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            Profile userProfile = MonDB.getProfile(characterName);
            PendingDuty dutyInProgress = MonDB.getPendingDuty(characterName);
            string message = "";
            Random random = new Random();

            if (dutyInProgress != null && dutyInProgress.startTime < DateTime.UtcNow.Date) //duty in progress but from previous day, remove it
            {
                MonDB.removePendingInteraction(dutyInProgress.Id);
                dutyInProgress = null;
                message += ChateauInteractionHandler.dutyFromPreviousDayText() + " Enough about the past though. Let's focus on your new task for today... \n\n"; 
            }

            if (!userProfile.characteristics.ContainsKey("job")) //no job, can not work
            {
                message += ChateauInteractionHandler.noJobText();
            } 
            else if (dutyInProgress != null) // responding to duty in progress
            {
                string[] providedInts = commandController.GetIntsFromCommandTermsAsStrings(rawTerms);
                int dutyChoice = -1;
                int dutyChoicesCount = dutyInProgress.dutyResults.Count;
                if (providedInts != null) {
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
                        // Capture the rolled reward so the employer kickback can be computed from it
                        // after the loop, independently of the worker's own poverty curse (B6.3).
                        int grantedRewardAmount = 0;
                        string grantedRewardCurrency = null;
                        foreach (KeyValuePair<string, Reward> rewardEntry in chosenResult.rewardList)
                        {
                            randomWeightPick -= rewardEntry.Value.weight;
                            if (randomWeightPick <= 0)
                            {
                                //grant this reward
                                int rewardAmount = random.Next(rewardEntry.Value.min, rewardEntry.Value.max + 1); //+1 because upper bound is exclusive
                                grantedRewardAmount = rewardAmount;
                                grantedRewardCurrency = rewardEntry.Value.currency;
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
                        
                        CoolDown workTimer = new CoolDown();
                        workTimer.timerEnd = DateTime.UtcNow.Date.AddDays(1);
                        if (userProfile.timers.ContainsKey("work"))
                        {
                            userProfile.timers["work"] = workTimer;
                        }
                        else
                        {
                            userProfile.timers.Add("work", workTimer);
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

                        // MANOR employer kickback: when a worker employed by someone else completes a
                        // duty, their employer earns a bonus on top of (and without reducing) the
                        // worker's reward. Computed from the rolled amount independently of the
                        // worker's poverty curse — only the cursed party is impacted (B6.3). The
                        // employer is never push-notified (TOS); they discover it via !business.
                        if (grantedRewardAmount > 0
                            && userProfile.characteristics.ContainsKey("employer")
                            && userProfile.characteristics["employer"] != characterName)
                        {
                            string employerUserName = userProfile.characteristics["employer"];
                            Profile employerProfile = MonDB.getProfile(employerUserName);
                            if (employerProfile != null)
                            {
                                string employerDisplayName = MonDB.getDisplayName(employerUserName);
                                if (string.IsNullOrEmpty(employerDisplayName))
                                    employerDisplayName = employerUserName;

                                string kickbackLine = ApplyEmployerKickback(characterName, employerProfile,
                                    employerDisplayName, grantedRewardAmount, grantedRewardCurrency);
                                if (!string.IsNullOrEmpty(kickbackLine))
                                {
                                    MonDB.setProfile(employerUserName, employerProfile);
                                    message += kickbackLine;
                                }
                            }
                        }

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
            else if (userProfile.timers.ContainsKey("work") && userProfile.timers["work"].timerEnd > DateTime.UtcNow) //no duty in progress and already worked
            {
                TimeSpan timeLeft = userProfile.timers["work"].timerEnd - DateTime.UtcNow;
                message += ("You've already worked today! You can work again when a new Chateau day starts in " + string.Format("{0:D2}h:{1:D2}m:{2:D2}s", timeLeft.Hours, timeLeft.Minutes, timeLeft.Seconds) + ".");
            }
            else //no duty in progress and can work
            {
                List<Duty> dutyList = MonDB.getDutiesByJob(userProfile.characteristics["job"]);
                if (dutyList.Count == 0)
                {
                    //populate list with a generic duty if none exist for this job
                    dutyList = MonDB.getDutiesByJob("default");
                    dutyList[0].job = userProfile.characteristics["job"];
                }
                Duty duty = dutyList[random.Next(dutyList.Count)];
                List<DutyResult> validResults = new List<DutyResult>();
                message += duty.startText + "\n";
                foreach (KeyValuePair<string, DutyResult> keyvaluepair in duty.dutyResults)
                {
                    switch (keyvaluepair.Value.conditional.type.Substring(0, 3))
                    {
                        case "non": //none
                            validResults.Add(keyvaluepair.Value);
                            break;
                        case "job": //job experience
                            if (checkDictionaryConditional(keyvaluepair.Value.conditional.type, keyvaluepair.Value.conditional.value, userProfile.jobExperience))
                            {
                                validResults.Add(keyvaluepair.Value);
                            }
                            break;
                        case "trn": //training
                            if (userProfile.lists.ContainsKey("trainings"))
                            {
                                if (userProfile.lists["trainings"].Contains(keyvaluepair.Value.conditional.type.Substring(3)))
                                {
                                    validResults.Add(keyvaluepair.Value);
                                }
                            }
                            break;
                        case "cur": //currency
                            if (checkDictionaryConditional(keyvaluepair.Value.conditional.type, keyvaluepair.Value.conditional.value, userProfile.currencies))
                            {
                                validResults.Add(keyvaluepair.Value);
                            }
                            break;
                        case "mon": //monster/species trait or category
                            if (userProfile.characteristics.ContainsKey("monster"))
                            {
                                if (MonDB.getIdentifier(userProfile.characteristics["monster"]).categories.Contains<string>(keyvaluepair.Value.conditional.type.Substring(3)))
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
                    message += "!w " + choiceCount + ": " + resultOption.choiceName + " | ";
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

        /// <summary>
        /// The MANOR kickback an employer earns from a worker's rolled reward: a flat 25%,
        /// floored, with a minimum of 1 whenever the roll was positive, and 0 otherwise.
        /// Pure — extracted so the rounding rules can be unit-tested without a work session.
        /// </summary>
        public static int EmployerCut(int rewardAmount)
        {
            return rewardAmount > 0
                ? Math.Max(1, (int)Math.Floor(rewardAmount * 0.25))
                : 0;
        }

        /// <summary>
        /// Applies the employer kickback to <paramref name="employerProfile"/> (wallet + the
        /// employeeEarnings ledger) and returns the worker-facing line to append to the work
        /// PM, or an empty string when no kickback is paid. Pays nothing — no credit, no ledger
        /// entry, and no worker-facing line — when the cut rounds to 0 or the employer carries
        /// their own poverty curse (we neither pay it nor leak the employer's curse to the
        /// worker). The caller is responsible for the eligibility checks that need DB access
        /// (employer key present, employed by someone other than self, employer profile exists)
        /// and for persisting the mutated employer profile when a non-empty line is returned.
        /// </summary>
        public static string ApplyEmployerKickback(string workerUserName, Profile employerProfile,
            string employerDisplayName, int rewardAmount, string currency)
        {
            int cut = EmployerCut(rewardAmount);
            if (cut <= 0)
                return string.Empty;

            // The employer's own poverty curse voids their cut entirely.
            bool employerHasPovertyCurse = CurseInstance.LoadAll(employerProfile)
                .Any(c => string.Equals(c.Curse, "poverty", StringComparison.OrdinalIgnoreCase));
            if (employerHasPovertyCurse)
                return string.Empty;

            // Credit the employer's wallet (add the currency key if absent).
            if (employerProfile.currencies == null)
                employerProfile.currencies = new Dictionary<string, int>();
            if (employerProfile.currencies.ContainsKey(currency))
                employerProfile.currencies[currency] += cut;
            else
                employerProfile.currencies.Add(currency, cut);

            // Increment the per-employee, per-currency ledger.
            if (employerProfile.employeeEarnings == null)
                employerProfile.employeeEarnings = new Dictionary<string, Dictionary<string, int>>();
            if (!employerProfile.employeeEarnings.ContainsKey(workerUserName))
                employerProfile.employeeEarnings[workerUserName] = new Dictionary<string, int>();
            Dictionary<string, int> ledger = employerProfile.employeeEarnings[workerUserName];
            if (ledger.ContainsKey(currency))
                ledger[currency] += cut;
            else
                ledger.Add(currency, cut);

            return "Your employer " + employerDisplayName + " also gets [b]" + cut + " " + currency +
                "[/b] for your diligent work, courtesy of the Chateau MANOR.\n";
        }
    }
}
