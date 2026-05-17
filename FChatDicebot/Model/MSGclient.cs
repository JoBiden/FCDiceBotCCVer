using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    //send message to channel
    public class MSGclient : iSocketCommand
    {
        public string channel;
        public string message;
    }
}
