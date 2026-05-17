using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    //client identification to pass when starting up
    public class IDNclient : iSocketCommand
    {
        public string method;
        public string account;
        public string ticket;
        public string character;
        public string cname;
        public string cversion;
    }
}
