using FChatDicebot.DiceFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.SavedData
{
    public class SavedRollTable
    {
        public string TableId;
        public string OriginCharacter;
        public RollTable Table;
        public bool DefaultTable;

        public void Copy(SavedRollTable table)
        {
            TableId = table.TableId;
            OriginCharacter = table.OriginCharacter;
            Table = table.Table;
            DefaultTable = table.DefaultTable;
        }
    }
}
