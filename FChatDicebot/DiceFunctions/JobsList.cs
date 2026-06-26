using FChatDicebot.SavedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class JobsList : ICustomUserContent
    {
        public List<TierEntry> JobTiers;

        public string Name; //not currently used but available here
        public string Description; //not currently used but available here
        public string WorkMessage;
        public bool ShowResultDescription;
        public bool ShowResultName;
        public bool ShowResultJobTier;
        public bool Nsfw;

        //public List<TableEntry> TableEntries;

        public JobRollResult GetRollResult(System.Random rnd, int tierGuaranteed, bool includeRollLabel)
        {
            JobRollResult rtn = new JobRollResult();

            int maxTier = JobTiers.Max(a => a.Tier);
            TierEntry tierRolled = null;
            if (tierGuaranteed <= 0)
            {
                float roll = (float)rnd.NextDouble();
                float currentTotal = 0;
                int currentTier = 0;
                foreach (TierEntry entry in JobTiers)// (float f in TierProbabilities)
                {
                    currentTotal += entry.Probability;
                    currentTier++;
                    if (roll < currentTotal)
                    {
                        tierRolled = entry;// currentTier;
                        break;
                    }
                }
            }
            else
            {
                tierRolled = JobTiers.FirstOrDefault(a => a.Tier == tierGuaranteed);
            }

            if (tierRolled == null)
                tierRolled = JobTiers.First();

            if (tierRolled == null)
            {
                rtn.ResultString = "Error: No valid job tier rolled.";
                return rtn;
            }
            else if (tierRolled.Jobs == null || tierRolled.Jobs.Count == 0)
            {
                rtn.ResultString = "Error: No valid jobs found in tier " + tierRolled.Tier +  ".";
                return rtn;
            }

            bool rareJob = (float)rnd.NextDouble() <= tierRolled.RareProbability;

            List<JobEntry> validJobs = tierRolled.Jobs.Where(a => a.RareJob == rareJob).ToList();

            if (validJobs.Count == 0)
            {
                validJobs = tierRolled.Jobs;
            }

            if (validJobs.Count == 0)
            {
                rtn.ResultString = "Error: no valid jobs found for tier " + tierRolled.Tier;
                rtn.jobTier = tierRolled.Tier;
                rtn.jobNumber = -1;
            }
            else
            {
                int jobRolled = rnd.Next(0, validJobs.Count);
                JobEntry jobChosen = validJobs[jobRolled];
                rtn.jobTier = tierRolled.Tier;
                rtn.jobNumber = jobRolled;
                int tierIndex = tierRolled.Tier - 1;
                rtn.color = string.IsNullOrEmpty(tierRolled.TierColor) ? "gray" : tierRolled.TierColor;

                string jobTier = ShowResultJobTier ? " [i](Tier " + tierRolled.Tier + ")[/i]" :"";

                rtn.ResultString = "[color=" + rtn.color + "]" + jobChosen.Print(ShowResultName, ShowResultDescription) + jobTier + "[/color]";
                rtn.rewardMultiplier = jobChosen.RareJob ? tierRolled.RareMultiplier : 1;

                if (jobChosen.RareJob && !jobChosen.DoNotShowRareJobNote)
                {
                    rtn.ResultString += " [color=yellow][b](Rare Job!)[/b][/color]";
                }
            }
            return rtn;
        }

        public bool IsNsfw()
        {
            return Nsfw;
        }
    }

    public class JobsListOLD : ICustomUserContent
    {
        public List<JobEntryOLD> Entries;
        //public List<JobEntry> RareEntries;

        public List<float> TierProbabilities;
        //public List<float> TierRareProbabilities;
        //public List<float> TierRareMultipliers;
        //public float RareMultiplier;
        public List<string> TierColors;

        //public int RollBonus = 0;
        //public int DieSides = 0;
        public string Name; //not currently used but available here
        public string Description; //not currently used but available here
        public bool OnlyShowResultDescription;
        public bool Nsfw;

        public JobsList ConvertToNewJobsList()
        {
            JobsList newJobsList = new JobsList() { Description = this.Description, Name = this.Name, Nsfw = this.Nsfw, ShowResultDescription = false, ShowResultName = true, ShowResultJobTier = false
                , JobTiers = new List<TierEntry>(), WorkMessage = "[CHARACTERNAME] spent the day working as [AJOBPRINT] for [b][EARNEDAMOUNT] [CURRENCYNAME][/b]." };

            int countTier = 1;
            foreach (float f in TierProbabilities)
            {
                string tierColor = (TierColors == null || TierColors.Count < countTier) ? "green" : TierColors[countTier - 1];
                TierEntry tier = new TierEntry() { Jobs = new List<JobEntry>(), Probability = f, RareMultiplier = 3, RareProbability = 0, Tier = countTier, TierColor = tierColor };

                List<JobEntryOLD> relevantEntries = Entries.Where(a => a.Tier == countTier).ToList();
                List<JobEntry> newEntries = new List<JobEntry>();

                foreach (JobEntryOLD jeo in relevantEntries)
                {
                    JobEntry newEntry = new JobEntry() { Name = jeo.Name, Description = jeo.Description, DoNotShowRareJobNote = false, RareJob = false };
                    newEntries.Add(newEntry);
                }

                if (relevantEntries != null && relevantEntries.Count >= 0)
                {
                    tier.Jobs = newEntries;// JobEntry relevantEntries.add
                }

                newJobsList.JobTiers.Add(tier);
                countTier++;
            }

            return newJobsList;
        }

        //public List<TableEntry> TableEntries;

        public JobRollResult GetRollResult(System.Random rnd, int tierGuaranteed, bool includeRollLabel)
        {
            JobRollResult rtn = new JobRollResult();

            int maxTier = Entries.Max(a => a.Tier);
            int tierRolled = tierGuaranteed;
            if (tierGuaranteed <= 0)
            {
                float roll = (float)rnd.NextDouble();
                float currentTotal = 0;
                int currentTier = 0;
                foreach (float f in TierProbabilities)
                {
                    currentTotal += f;
                    currentTier++;
                    if (roll < currentTotal)
                    {
                        tierRolled = currentTier;
                        break;
                    }
                }
                if (tierRolled == 0)
                    tierRolled = 1;
            }

            List<JobEntryOLD> validJobs = Entries.Where(a => a.Tier == tierRolled).ToList();
            if (validJobs.Count == 0)
            {
                rtn.ResultString = "no valid jobs found for tier " + tierRolled;
                rtn.jobTier = tierRolled;
                rtn.jobNumber = -1;
            }
            else
            {
                int jobRolled = rnd.Next(0, validJobs.Count - 1);
                JobEntryOLD jobChosen = validJobs[jobRolled];
                rtn.jobTier = tierRolled;
                rtn.jobNumber = jobRolled;
                int tierIndex = tierRolled - 1;
                rtn.color = (TierColors != null && TierColors.Count >= tierIndex) ? TierColors[tierIndex] : "gray";
                rtn.rewardMultiplier = 1;
                rtn.ResultString = jobChosen.Print(true, false);// includeRollLabel, false);
            }
            return rtn;
        }

        public bool IsNsfw()
        {
            return Nsfw;
        }
    }

    public class JobRollResult
    {
        public string ResultString;
        public string jobName;
        public string color;
        public int jobTier;
        public int jobNumber;
        public float rewardMultiplier;
        public JobEntry jobEntry;
    }
}
