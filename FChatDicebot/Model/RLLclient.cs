using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    //roll dice in channel. dice is 9d100, bottle, 1d6+1d20, 1d6+10
    public class RLLclient : iSocketCommand
    {
        public string channel;
        public string dice;
    }
}
