using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.Model
{
    //join channel
    public class JCHserver : iSocketCommand
    {
        public string title;
        public string channel;
        public string description;
        public JchCharacter character;
    }

    public class JchCharacter
    {
        public string identity;
    }
}
