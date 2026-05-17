using FChatDicebot.DiceFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.SavedData
{
    public class SavedDeck
    {
        public string DeckId;
        public string OriginCharacter;
        public string DeckList;

        public void Copy(SavedDeck deck)
        {
            DeckId = deck.DeckId;
            OriginCharacter = deck.OriginCharacter;
            DeckList = deck.DeckList;
        }
    }
}
