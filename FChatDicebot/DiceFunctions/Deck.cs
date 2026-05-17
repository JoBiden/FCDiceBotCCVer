using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class Deck : CardCollection
    {
        public DeckType DeckType;

        protected int DeckPosition = 0;

        public Deck(DeckType type)
        {
            DeckType = type;
            CollectionName = BotMain.DeckCollectionName;
        }

        public void FillDeck(bool addJokers, int copyNumber, FChatDicebot.SavedData.SavedDeck savedDeck = null)
        {
            if(copyNumber <= 1)
                Cards = new List<DeckCard>();

            switch(DeckType)
            {
                case DiceFunctions.DeckType.Playing:
                    {
                        for (int suit = 0; suit < 4; suit++)
                        {
                            for (int num = 1; num < 14; num++)
                            {
                                Cards.Add(new DeckCard() { number = num, suit = suit });
                            }
                        }

                        if (addJokers)
                        {
                            Cards.Add(new DeckCard() { joker = true, suit = 0 });
                            Cards.Add(new DeckCard() { joker = true, suit = 2 });
                        }
                    }
                    break;
                case DiceFunctions.DeckType.Uno:
                    {
                        for (int suit = 4; suit < 8; suit++)
                        {
                            for (int num = 1; num < 14; num++)
                            {
                                if(num == 10)
                                {
                                    Cards.Add(new DeckCard() { number = 0, suit = suit });
                                }
                                else
                                {
                                    Cards.Add(new DeckCard() { number = num, suit = suit });
                                    Cards.Add(new DeckCard() { number = num, suit = suit });
                                }
                            }
                        }

                        for (int i = 0; i < 4; i++ )
                        {
                            Cards.Add(new DeckCard() { specialName = "[color=yellow]W[/color][color=blue]I[/color][color=red]L[/color][color=green]D[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=yellow]W[/color][color=blue]I[/color][color=red]L[/color][color=green]D[/color] [color=yellow]D[/color][color=blue]R[/color][color=red]A[/color][color=green]W[/color] [color=yellow]4[/color]" });
                        }
                    }
                    break;
                case DiceFunctions.DeckType.Tarot:
                    {
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The World[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]Judgement[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Sun[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Moon[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Star[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Tower[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Devil[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]Temperance[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]Death[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Hanged Man[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]Justice[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Wheel of Fortune[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Hermit[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]Strength[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Chariot[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Lovers[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Hierophant[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Emperor[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Empress[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The High Priestess[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Magician[/color]" });
                        Cards.Add(new DeckCard() { specialName = "[color=orange]The Fool[/color]" });

                        for (int suit = 8; suit < 12; suit++)
                        {
                            for (int num = 1; num < 15; num++)
                            {
                                Cards.Add(new DeckCard() { number = num, suit = suit });
                            }
                        }
                    }
                    break;
                case DeckType.ManyThings:
                    {
                        Cards.Add(new DeckCard() { specialName = "Balance" });
                        Cards.Add(new DeckCard() { specialName = "Comet" });
                        Cards.Add(new DeckCard() { specialName = "Donjon" });
                        Cards.Add(new DeckCard() { specialName = "Euryale" });
                        Cards.Add(new DeckCard() { specialName = "The Fates" });
                        Cards.Add(new DeckCard() { specialName = "Flames" });
                        Cards.Add(new DeckCard() { specialName = "Fool" });
                        Cards.Add(new DeckCard() { specialName = "Gem" });
                        Cards.Add(new DeckCard() { specialName = "Idiot" });
                        Cards.Add(new DeckCard() { specialName = "Jester" });
                        Cards.Add(new DeckCard() { specialName = "Key" });
                        Cards.Add(new DeckCard() { specialName = "Knight" });
                        Cards.Add(new DeckCard() { specialName = "Moon" });
                        Cards.Add(new DeckCard() { specialName = "Rogue" });
                        Cards.Add(new DeckCard() { specialName = "Ruin" });
                        Cards.Add(new DeckCard() { specialName = "Skull" });
                        Cards.Add(new DeckCard() { specialName = "Star" });
                        Cards.Add(new DeckCard() { specialName = "Sun" });
                        Cards.Add(new DeckCard() { specialName = "Talons" });
                        Cards.Add(new DeckCard() { specialName = "Throne" });
                        Cards.Add(new DeckCard() { specialName = "Vizier" });
                        Cards.Add(new DeckCard() { specialName = "The Void" });
                    }
                    break;
                case DiceFunctions.DeckType.BreakerRumble:
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "[color=orange]Spank[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=orange]Slam[/color]" });
                        }
                        for (int i = 0; i < 7; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "Rest & Relax" });
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "Key" });
                        }
                        for (int i = 0; i < 5; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "Scheme" });
                            Cards.Add(new DeckCard() { specialName = "Bribe the Ref" });
                            Cards.Add(new DeckCard() { specialName = "[color=orange]Bash[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=orange]Pin Down[/color]" });
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "[color=cyan]Reversal[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=cyan]Resilient[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=cyan]Prestige[/color]" });

                            Cards.Add(new DeckCard() { specialName = "Taunt" });
                            Cards.Add(new DeckCard() { specialName = "[color=brown]Chastity[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=brown]Collar[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=brown]Arm Binder[/color]" });
                            Cards.Add(new DeckCard() { specialName = "Blackmail" });
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "Escape Artist" });
                            Cards.Add(new DeckCard() { specialName = "[color=brown]Butt Plug[/color]" });
                            //Cards.Add(new DeckCard() { specialName = "[color=brown]Corset[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=cyan]Snatch[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=cyan]High Stakes[/color]" });
                        }
                        for (int i = 0; i < 2; i++ )
                        {
                            Cards.Add(new DeckCard() { specialName = "[color=cyan]Close Watch[/color]" });
                            //Cards.Add(new DeckCard() { specialName = "Hot Potato" });
                        }
                    }
                    break;
                case DiceFunctions.DeckType.BreakerRumbleClassic:
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "[color=orange]Spank[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=orange]Slam[/color]" });
                        }
                        for (int i = 0; i < 7; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "Rest & Relax" });
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "Key" });
                        }
                        for (int i = 0; i < 5; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "Scheme" });
                            Cards.Add(new DeckCard() { specialName = "Bribe the Ref" });
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "[color=cyan]Reversal[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=cyan]Resilient[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=cyan]Prestige[/color]" });

                            Cards.Add(new DeckCard() { specialName = "Taunt" });
                            Cards.Add(new DeckCard() { specialName = "[color=brown]Chastity[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=brown]Collar[/color]" });
                            Cards.Add(new DeckCard() { specialName = "[color=brown]Arm Binder[/color]" });
                            Cards.Add(new DeckCard() { specialName = "Blackmail" });
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "Escape Artist" });
                        }
                        for (int i = 0; i < 2; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "[color=cyan]Close Watch[/color]" });
                        }
                    }
                    break;
                case DiceFunctions.DeckType.BreakerRumbleExtra:
                    {
                        for (int i = 0; i < 25; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "[color=yellow]Gloat[/color]" });
                        }
                        for (int i = 0; i < 5; i++)
                        {
                            Cards.Add(new DeckCard() { specialName = "[color=yellow]Mega Gloat[/color]" });
                        }
                    }
                    break;

                case DeckType.Custom:
                    {
                        if(savedDeck != null)
                        {
                            CreateFromDeckList(savedDeck.DeckList);
                        }
                    }
                    break;
            }

        }

        public void InsertNewCards(List<DeckCard> addingCards)
        {
            if (Cards == null)
                Cards = new List<DeckCard>();

            foreach (DeckCard d in addingCards)
            {
                Cards.Add(d);
            }
        }

        public override void AddCard(DeckCard d, Random r)
        {
            var exist = Cards.FirstOrDefault(abb => abb.Equals(d));
            if (exist != null)
            {
                int yar = Cards.IndexOf(exist);
                if(yar < DeckPosition)
                {
                    Cards.Remove(exist);
                    DeckPosition -= 1;
                }
            }

            Cards.Add(d);
            //get random position among remaining spots
            int remainingCards = Cards.Count - DeckPosition;

            int randomSpot = r.Next(remainingCards) + DeckPosition;

            for (int i = Cards.Count - 1; i > randomSpot && i >= 1; i--)
            {
                int swapPos = r.Next(remainingCards);
                SwapPositions(i, i - 1);
            }
        }

        public void CreateFromDeckList(string deckList)
        {
            if(string.IsNullOrEmpty(deckList))
                return;

            string[] allcards = deckList.Split(',');
            if (allcards == null || allcards.Count() == 0)
                return;

            Cards = new List<DeckCard>();

            foreach (string s in allcards)
            {
                if(s.Contains('|'))
                {
                    string[] splitDesc = s.Split('|');
                    Cards.Add(new DeckCard() { specialName = splitDesc[0], description = splitDesc[1] });
                }
                else
                {
                    Cards.Add(new DeckCard() { specialName = s });
                }
            }
        }

        public string GetDeckList(PrintSetting printSetting)
        {
            if (Cards == null)
                return null;

            string deckList = "";
            foreach (DeckCard d in Cards)
            {
                if (!string.IsNullOrEmpty(deckList))
                    deckList += ",";

                deckList += d.Print(printSetting);

                if (!string.IsNullOrEmpty(d.description))
                    deckList += "|" + d.description;
            }

            return deckList;
        }

        public void ShuffleFullDeck(System.Random r)
        {
            if (Cards == null || Cards.Count == 0)
                return;

            List<int> newPositions = new List<int>();
            for (int i = 0; i < Cards.Count; i++)
            {
                int swapPos = r.Next(Cards.Count);
                SwapPositions(i, swapPos);
            }

            DeckPosition = 0;
        }

        public void ShuffleRemainingDeck(System.Random r)
        {
            if (Cards == null || Cards.Count == 0)
                return;

            List<int> newPositions = new List<int>();

            int remainingCards = Cards.Count - DeckPosition;

            for (int i = 0; i < remainingCards; i++)
            {
                int swapPos = r.Next(remainingCards);
                SwapPositions(DeckPosition + i, DeckPosition + swapPos);
            }
        }

        public void SwapPositions(int pos, int pos2)
        {
            DeckCard dc = Cards[pos];
            Cards[pos] = Cards[pos2];
            Cards[pos2] = dc;
        }

        public DeckCard DrawCard()
        {
            if (Cards == null || DeckPosition >= Cards.Count)
                return null;

            DeckCard returnCard = Cards[DeckPosition];
            DeckPosition++;

            return returnCard;
        }

        public void ResetCardStates()
        {
            if(Cards != null && Cards.Count > 0)
            {
                foreach(DeckCard d in Cards)
                {
                    d.cardState = null;
                }
            }
        }

        public int GetCardsRemaining()
        {
            return Cards.Count() - DeckPosition;
        }

        public int GetTotalCards()
        {
            return Cards.Count();
        }

        public int GetNumberCardsDrawn()
        {
            return DeckPosition;
        }

        public string GetCardsRatio()
        {
            return GetCardsRemaining() + " / " + GetTotalCards();
        }

        public bool ContainsJokers()
        {
            return DeckType == DeckType.Playing && Cards.Count() % 52 != 0;
        }
    }

    public enum DeckType
    {
        Playing,
        Tarot,
        ManyThings,
        Uno,
        BreakerRumble,
        BreakerRumbleExtra,
        BreakerRumbleClassic,
        Skipbo, //hard to use with current play rules, need piles of some kind
        Placeholder,
        NONE,
        Custom
    }
}
