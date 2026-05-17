using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot
{
    public class BuyCommand
    {
        public const int PingCountAmount = 4;

        public string ItemString;
        public string CharacterName;
        public string ChannelName;
        public int PingCount;
        public bool Confirmed;
        public bool Remove;

        public int GetChipsAmount()
        {
            if (string.IsNullOrEmpty(ItemString))
                return 0;
            else
            {
                string remainder = ItemString.ToLower().Replace("chips", "").Trim();
                int output = 0;
                int.TryParse(remainder, out output);

                return output;
            }
        }
    }
}
