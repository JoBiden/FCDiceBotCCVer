using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class RollTable
    {
        public int RollBonus = 0;
        public int DieSides = 0;
        public string Name;
        public string Description;
        public bool OnlyShowResultDescription;

        public List<TableEntry> TableEntries;

        public TableRollResult GetRollResult(System.Random rnd, int rollModifier, bool includeRollLabel, bool includeSecondaryRolls)
        {
            string rtnString = "";

            int rollAmount = rnd.Next(0, DieSides) + 1 + RollBonus + rollModifier;
            string rollBonusString = RollBonus != 0 ? (" " + (RollBonus > 0 ? "+" : "") + RollBonus.ToString()) : "";
            string rollModifierString = Utils.GetRollModifierString(rollModifier);

            string nameString = "[b]" + Name + "[/b]";
            string rollString = nameString;
            if(!OnlyShowResultDescription)
                rollString += "\n[i]Rolled [b]" + rollAmount + "[/b] (1d" + DieSides + rollBonusString + rollModifierString + ")[/i]";

            TableEntry foundEntry = TableEntries.FirstOrDefault(a => a.Roll == rollAmount);

            if(foundEntry == null)
            {
                foreach(TableEntry t in TableEntries)
                {
                    if(rollAmount >= t.Roll && rollAmount <= t.Roll + t.Range)
                    {
                        foundEntry = t;
                        break;
                    }
                }
            }

            List<TableRollTrigger> triggeredRolls = null;
            if(foundEntry != null)
            {
                rtnString = foundEntry.ToString(includeRollLabel, OnlyShowResultDescription);

                if(includeSecondaryRolls)
                {
                    triggeredRolls = foundEntry.Triggers;
                }
            }
            else
            {
                rtnString = "(no roll entry found)";
            }

            TableRollResult result = new TableRollResult()
            {
                ResultString = rollString + "\n" + rtnString,
                TriggeredRolls = triggeredRolls
            };

            return result;
        }

        public int GetRollRange(out int minRoll)
        {
            minRoll = TableEntries.Min(a => a.Roll);
            int maxNum = TableEntries.Max(a => a.Roll + a.Range);

            return maxNum;
        }

        public string GetTableEntryList()
        {
            string rtnString = "";
            if (TableEntries != null && TableEntries.Count > 0)
            {
                foreach (TableEntry t in TableEntries)
                {
                    if (!string.IsNullOrEmpty(rtnString))
                        rtnString += "\n";

                    rtnString += t.ToString(true, false);
                }
            }
            return rtnString;
        }
    }

    public class TableRollResult
    {
        public string ResultString;
        public List<TableRollTrigger> TriggeredRolls;
    }
}
