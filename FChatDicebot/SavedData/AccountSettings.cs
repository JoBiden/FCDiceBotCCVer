using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.SavedData
{
    public class AccountSettings
    {
        public string VcAccountName;
        public string VcAccountPassword;
        public string AccountName;
        public string AccountPassword;
        public string CharacterName;
        public string CName;
        public List<string> AdminCharacters;
        public List<ChannelCharacter> TrustedCharacters;
    }
}
