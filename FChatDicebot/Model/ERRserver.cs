using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    //error recieved
    public class ERRserver : iSocketCommand
    {
        public string message;
        public int number; //26 = channel not found, 28 = already in channel cannot join again
    }
}
