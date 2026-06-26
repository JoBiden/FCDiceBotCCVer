using FChatDicebot.SavedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class JobEntry
    {
        //public int Tier;
        public string Name;
        public string Description;
        public bool RareJob;
        public bool DoNotShowRareJobNote;

        public string Print(bool includeName, bool includeDescription)
        {
            string rtnString = "";
            //if (includeRoll)
            //{
            //    rtnString += string.Format("(Tier {0})", Tier);
            //}
            if (includeName)
            {
                if (!string.IsNullOrEmpty(rtnString))
                    rtnString += " ";
                rtnString += "[b]" + Name + "[/b]";
            }

            if (includeDescription)
            {
                if (!string.IsNullOrEmpty(rtnString))
                    rtnString += ": ";
                rtnString += Description;
            }

            return rtnString;
        }
    }

    public class JobEntryOLD
    {
        public int Tier;
        public string Name;
        public string Description;
        public bool RareJob;
        public bool DoNotShowRareJobNote;

        public string Print(bool includeName, bool includeDescription)
        {
            string rtnString = "";
            //if (includeRoll)
            //{
            //    rtnString += string.Format("(Tier {0})", Tier);
            //}
            if (includeName)
            {
                if (!string.IsNullOrEmpty(rtnString))
                    rtnString += " ";
                rtnString += "[b]" + Name + "[/b]";
            }

            if (includeDescription)
            {
                if (!string.IsNullOrEmpty(rtnString))
                    rtnString += ": ";
                rtnString += Description;
            }

            return rtnString;
        }
    }
}
