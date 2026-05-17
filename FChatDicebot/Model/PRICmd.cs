using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    //send message to user
    public class PRICmd : iSocketCommand
    {
        public string character;
        public string recipient;
        public string message;
    }
}
