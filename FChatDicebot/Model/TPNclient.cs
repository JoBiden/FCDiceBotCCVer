using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    //set typing status of character
    public class TPNclient : iSocketCommand
    {
        public string status; //enum. valid: clear, paused, typing
        public string character;
    }
}
