using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    //set status of character
    public class STAclient : iSocketCommand
    {
        public string status; //enum. valid: online, looking, busy, dnd, idle, away 
        public string statusmsg;
        //public string character; //unclear whether this is required from docs
    }

    public class STAStatus
    {
        public const string Online = "online";
        public const string Looking = "looking";
        public const string Busy = "busy";
        public const string Dnd = "dnd";
        public const string Idle = "idle";
        public const string Away = "away";
    }
}
