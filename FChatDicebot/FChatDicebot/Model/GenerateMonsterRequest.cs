using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    public class GenerateMonsterRequest : MGWebRequest
    {
        public double targetChallengeRating;
        public bool shortBlock = true;
        public bool requestSaveString = false;
    }
    public class GenerateMonsterReply : MGWebRequest
    {
        public string monsterPrintout;
        public string saveString;

    }
}
