using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    //list of channel ops data, in response to JCH
    public class COLserver : iSocketCommand
    {
        public string channel;
        public string[] oplist;
    }
}
