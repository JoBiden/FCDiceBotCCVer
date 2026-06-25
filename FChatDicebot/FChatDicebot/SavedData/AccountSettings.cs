using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.SavedData
{
    public class AccountSettings
    {
        public string AccountName;
        public string AccountPassword;
        public string CharacterName;
        public string CName;
        public string DiscordBotToken;
        public string MonsterGeneratorPresharedKey;
        public List<string> AdminCharacters;
        public List<ChannelCharacter> TrustedCharacters;
        public List<string> AllowedChessEiconChannels;

        public List<string> FullCosmeticsUnlockCharacters;
    }
}
