using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    //server hello recieved
    public class MSGserver : iSocketCommand
    {
        public string message;
        public string character;
        public string channel;
    }
}
