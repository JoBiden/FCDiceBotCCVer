using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class VcChipOrder
    {
        public string ChannelId;
        public string Character;
        public MessageAddress GetMessageAddress()
        {
            return new MessageAddress() { channel = ChannelId, character = Character, guild = null };
        }
        public int Chips = 0;
        public string TransactionId;
        public double Created;
        public double LastCheckedTime;
        public int CheckedCount;
        public int OrderStatus;
    }
}
//RequestStatus
//{
//sent = 0
//accepted = 1
//denide = 2
//cancelled = 3
//}
