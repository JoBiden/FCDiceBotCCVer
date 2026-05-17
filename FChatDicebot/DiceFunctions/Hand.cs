using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class Hand : CardCollection
    {
        public string Character;

        public Hand()
        {
            Reset();
            CollectionName = BotMain.HandCollectionName;
        }

        public override void AddCard(DeckCard d, Random r)
        {
            Cards.Add(d);
        }

        public int HandPokerValue()
        {
            return 0;
        }
    }
}
