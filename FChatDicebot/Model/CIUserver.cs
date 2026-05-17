using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    //recieve invite to channel
    public class CIUserver : iSocketCommand
    {
        public string sender; //enum. valid: clear, paused, typing
        public string title;
        public string name;
    }
}
