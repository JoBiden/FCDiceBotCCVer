using FChatDicebot.SavedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class TierEntry
    {
        public int Tier;
        public float Probability;
        public float RareProbability;
        public float RareMultiplier;
        public string TierColor;

        public List<JobEntry> Jobs;

        //public string Name;
        //public string Description;

        public string Print(bool includeJobList)//, bool includeName, bool includeDescription)
        {
            string rtnString = "";
            rtnString += "Tier " + Tier + "\n";
            if (includeJobList)
            {
                if(Jobs != null)
                    rtnString += "Jobs: " + Utils.PrintList(Jobs.Select(a => (a.Name + (a.RareJob? " (rare)" : ""))).ToList());
                //if (RareJobs != null)
                //    rtnString += "Rare Jobs: " +  Utils.PrintList(RareJobs.Select(a => a.Name).ToList());
            }
            rtnString += " Tier probability " + Probability;
            rtnString += " Rare probability " + RareProbability;
            rtnString += " Rare multiplier " + RareMultiplier;
            rtnString += " Tier Color " + TierColor;
            //if (includeName)
            //{
            //    if (!string.IsNullOrEmpty(rtnString))
            //        rtnString += " ";
            //    rtnString += "[b]" + Name + "[/b]";
            //}

            //if (includeDescription)
            //{
            //    if (!string.IsNullOrEmpty(rtnString))
            //        rtnString += ": ";
            //    rtnString += Description;
            //}
            return rtnString;
        }
    }
}
