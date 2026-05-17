using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class TableEntry
    {
        public int Roll;
        public int Range;
        public string Name;
        public string Description;
        public List<TableRollTrigger> Triggers;

        public string ToString(bool includeRoll, bool onlyDescription)
        {
            string rtnString = "";
            if(includeRoll && !onlyDescription)
            {
                rtnString += string.Format("({0}{1}) ", Roll, (Range > 1? ("-" + (Roll + Range).ToString()) : "") );
            }

            string triggersAddition = "";
            if(Triggers != null && Triggers.Count > 0)
            {
                foreach(TableRollTrigger t in Triggers)
                {
                    if (triggersAddition != "")
                        triggersAddition += ", ";

                    triggersAddition += t.ToString();
                }

                if(Triggers[0].Command.StartsWith("!"))
                {
                    triggersAddition = " [i](perform: " + triggersAddition + ")[/i]";
                }
            }

            if(onlyDescription)
            {
                rtnString += Description;
            }
            else
            {
                rtnString += "[b]" + Name + ": [/b]" + Description + triggersAddition;
            }

            return rtnString;
        }
    }

    public class TableRollTrigger
    {
        public string Command;

        public override string ToString()
        {
            return Command;
        }
    }
}
