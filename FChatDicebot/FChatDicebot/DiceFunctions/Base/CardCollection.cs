using System;
using System.Collections.Generic;
using System.Text;

namespace FChatDicebot.DiceFunctions
{
    public abstract class CardCollection
    {
        public string Id;
        public string DeckTypeId;
        protected List<DeckCard> Cards;
        protected string CollectionName = "";

        public override string ToString()
        {
            string rtnString = "";
            if (Cards == null || Cards.Count == 0)
                return CollectionName + " empty";

            rtnString = GetCollectionString(true, null);

            return rtnString;
        }

        public string Print(bool showHandSize, PrintSetting printSetting, bool includeAddress = true)
        {
            string rtnString = "";
            if (Cards == null || Cards.Count == 0)
                return CollectionName + " empty";

            List<string> hiddenCollections = new List<string>() { BotMain.HandCollectionName, BotMain.DeckCollectionName, 
                BotMain.BurnCollectionName, BotMain.HiddenInPlayCollectionName};//dealer not needed, it uses 'hand collection name' already

            if (showHandSize && hiddenCollections.Contains(CollectionName))
            {
                string cardsS = "";
                if (Cards.Count > 1 || Cards.Count == 0)
                    cardsS = "s";

                string count = Cards.Count.ToString();
                if(this.GetType() == typeof(Deck))
                {
                    Deck d = (Deck)this;
                    count = d.GetCardsRatio();
                }
                rtnString += " " + count + " card" + cardsS;
            }
            else
            {
                rtnString = GetCollectionString(includeAddress, printSetting);
            }

            return rtnString;
        }

        private string GetCollectionString(bool includeAddress, PrintSetting printSetting)
        {
            string rtnString = "";
            int counter = 1;
            foreach (DeckCard d in Cards)
            {
                if (!string.IsNullOrEmpty(rtnString))
                {
                    rtnString += ", ";
                }
                rtnString += (includeAddress? "[sub](" + counter + ")[/sub] ":"") + d.Print(printSetting);
                counter += 1;
            }
            return rtnString;
        }

        public void SetCollectionName(string newName)
        {
            CollectionName = newName;
        }

        public string GetCollectionName()
        {
            return CollectionName;
        }

        public bool Empty()
        {
            return Cards == null || Cards.Count == 0;
        }

        public void Reset()
        {
            Cards = new List<DeckCard>();
        }

        public DeckCard GetCardAtIndex(int i)
        {
            if (i < Cards.Count && i >= 0)
                return Cards[i];
            else
                return null;
        }

        public virtual void AddCard(DeckCard d, Random r)
        {
            Cards.Add(d);
        }

        public int CardsCount()
        {
            return Cards.Count;
        }
    }
}
