using FChatDicebot.DiceFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.SavedData
{
    public class SavedDeck : ICustomUserContent
    {
        public string DeckId;
        public string OriginCharacter;
        public string DeckList;
        public bool Nsfw;

        public void Copy(SavedDeck deck)
        {
            DeckId = deck.DeckId;
            OriginCharacter = deck.OriginCharacter;
            DeckList = deck.DeckList;
            Nsfw = deck.Nsfw;
        }

        public bool IsNsfw()
        {
            return Nsfw;
        }
    }
}
