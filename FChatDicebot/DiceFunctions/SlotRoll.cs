using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class SlotRoll
    {
        public int Id;
        public string Printout;
        public int Match3Winnings;
        public int Match2Winnings;
        public bool WinsJackpot;
        public int AmountAddedToJackpot;

        public string PenaltyMessage;
    }
}
