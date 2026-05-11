using FChatDicebot.BotCommands.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class Poker : IGame
    {
        private const int RoundEndingWaitMsPerPlayer = 1000;
        private const int RoundEndingWaitMs = 1000 * 2;

        public string GetGameName()
        {
            return "Poker";
        }

        public int GetMaxPlayers()
        {
            return 10;
        }

        public int GetMinPlayers()
        {
            return 2;
        }

        public bool AllowAnte()
        {
            return true;
        }

        public bool UsesFlatAnte()
        {
            return true; //this is required for setting the ante in game startup
        }

        public bool KeepSessionDefault()
        {
            return true;
        }

        public int GetMinimumMsBetweenGames()
        {
            return 0;
        }

        public string GetGameHelp()
        {
            string thisGameCommands = "setante #, currentturn, turn, setante #, setmaxbet #, cancelhand, removetokens, forcestand, setrules (rules as text), setallowtokens (true/false), setblinds #, setsmallblind #, setbigblind #, setallowcheckraise" +
                "\n(as current player only): call, fold, raise #, allin, bettoken # (token name as text) (i.e.: !gc bettoken 100 watch)";
            string thisGameStartupOptions = "### (sets ante amount), 5carddraw/5cardstud/texasholdem/dbholdem (sets ruleset), tokens (allows betting with tokens), x1/x2/x3/x4 (sets deck count), nocheckraise (turn off checkraise for turns), maxbet:# (set max bet), blinds:# (set blinds), smallblind:# (set small blind), bigblind:# (set big blind)." +
                "\nThe default rules are: 5 card stud, hand size 5, no ante, no blinds, no tokens, 1x deck used, checkraise allowed" +
                "\nThe poker rules available are: 5cardstud (5 card hands), 5carddraw (5 card hands, redraw step), Ncardstud (use 6-10 as N, sets hand size), Ncarddraw (use 6-10 as N, sets hand size), texasholdem (2 card hands, community hand of 5), dbholdem (3 card hands, community hand of 5)";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, false, false);
        }

        public string GetStartingDisplay()
        {
            return "[eicon]dbpoker1[/eicon][eicon]dbpoker2[/eicon][eicon]dbpoker3[/eicon]";
        }

        public string GetEndingDisplay()
        {
            return "";
        }

        public string GameStatus(GameSession session)
        {
            if(session.PokerData != null)
            {
                string anteString = session.Ante > 0 ? session.Ante + "" : "none";
                string blindsString = session.PokerData.SmallBlind > 0 || session.PokerData.BigBlind > 0 ? ", small blind: " + session.PokerData.SmallBlind
                    + ", big blind: " + session.PokerData.BigBlind + " chips" : "(no blinds)";
                string output = "Rules: " + session.PokerData.Rules + ", decks used: " + session.PokerData.decksNumber + ", ante: " + anteString + blindsString 
                    + ", max bet: " + (session.PokerData.MaximumBet >= 0 ? session.PokerData.MaximumBet + "" : "none")
                    + ", hand size: " + session.PokerData.MaxHandSize + ", token betting: " + (session.PokerData.AllowTokens ? "[b]on[/b]" : "off") 
                    + ", checkraise allowed: " + session.PokerData.AllowCheckRaise + ".\n";

                if (session.State == GameState.GameInProgress && session.PokerData.PokerPlayers != null && session.PokerData.PokerPlayers.Count > 0)
                {
                    output += "Current bet level: [b]" + session.PokerData.CurrentBetLevel + "[/b] chips. The pot contains [color=yellow]" + session.PokerData.GetPotTotalChipsNotTokens() + "[/color] chips. " + session.PokerData.GetPotTokensString();

                    if (output != null)
                        output += "\n";
                    output += "Current Players:\n" + session.PokerData.PrintPlayers();

                    output += "\n" + PrintCurrentPlayerTurn(session, false);
                }
                
                return output;
            }
            else
                return "(players and bets not found)";
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            string outputString = "";
            if(!session.PokerData.RulesSet && terms != null && terms.Count() > 0)
            {
                string maxbet = "(no maximum bet)";
                string handSize = "(5 card hands)";
                string blindsError = "";
                int newCardTotal = 5;
                bool allowTokens = false;
                bool checkRaise = true;

                int deckNumber = 1;
                if (terms.Contains("x1") || terms.Contains("single"))
                    deckNumber = 1;
                if (terms.Contains("x2") || terms.Contains("double"))
                    deckNumber = 2;
                if (terms.Contains("x3") || terms.Contains("triple"))
                    deckNumber = 3;
                if (terms.Contains("x4") || terms.Contains("quadrouple"))
                    deckNumber = 4;
                if (terms.Contains("tokens") || terms.Contains("allowtokens"))
                    allowTokens = true;
                if (terms.Contains("checkraise") || terms.Contains("allowcheckraise") )
                    checkRaise = true;
                if (terms.Contains("nocheckraise") || terms.Contains("forbidcheckraise"))
                    checkRaise = false;

                PokerRuleset ruleset = PokerRuleset.FiveCardStud;
                if (terms.Contains("5carddraw") || terms.Contains("draw") || terms.Contains("fivecarddraw"))
                    ruleset = PokerRuleset.FiveCardDraw;
                if (terms.Contains("5cardstud") || terms.Contains("stud") || terms.Contains("fivecardstud"))
                    ruleset = PokerRuleset.FiveCardStud;
                if (terms.Contains("texasholdem") || terms.Contains("holdem") || terms.Contains("texas"))
                {
                    ruleset = PokerRuleset.TexasHoldem;
                    newCardTotal = 2;
                    handSize = "(2 card hands)";
                }
                if (terms.Contains("dicebotholdem") || terms.Contains("dbholdem") || terms.Contains("dicebot"))
                {
                    ruleset = PokerRuleset.DBHoldem;
                    newCardTotal = 3;
                    handSize = "(3 card hands)";
                }

                foreach (string s in terms)
                {
                    if (s.EndsWith("cardstud"))
                    {
                        string mod = s.Replace("cardstud", "").Replace("-", "");
                        ruleset = PokerRuleset.NCardStud;
                        newCardTotal = ParseNewHandSize(mod, out handSize);
                        if (newCardTotal == 5)
                            ruleset = PokerRuleset.FiveCardStud;
                    }
                    else if (s.EndsWith("carddraw"))
                    {
                        string mod = s.Replace("carddraw", "").Replace("-", "");
                        ruleset = PokerRuleset.NCardDraw;
                        newCardTotal = ParseNewHandSize(mod, out handSize);
                        if (newCardTotal == 5)
                            ruleset = PokerRuleset.FiveCardDraw;
                    }
                    else if (s.StartsWith("maxbet:"))
                    {
                        string mod = s.Replace("maxbet:", "");
                        int newMaxBet = -1;
                        int.TryParse(mod, out newMaxBet);
                        if (newMaxBet < ante)
                        {
                            maxbet = "(maximum bet cannot be less than ante: set to none)";
                        }
                        else if(newMaxBet >= 0)
                        {
                            maxbet = "(max bet: " + newMaxBet + ")";
                            session.PokerData.MaximumBet = newMaxBet;
                        }
                    }
                    else if (s.StartsWith("smallblind:") || s.StartsWith("small:"))
                    {
                        string mod = s.Replace("smallblind:", "").Replace("small:", "");
                        int newBlind = -1;
                        int.TryParse(mod, out newBlind);
                        if (newBlind < 0)
                        {
                            blindsError = " (small blind cannot be less than zero: set to none)";
                        }
                        else if (newBlind >= 0)
                        {
                            session.PokerData.SmallBlind = newBlind;
                        }
                    }
                    else if (s.StartsWith("bigblind:") || s.StartsWith("big:"))
                    {
                        string mod = s.Replace("bigblind:", "").Replace("big:", "");
                        int newBlind = -1;
                        int.TryParse(mod, out newBlind);
                        if (newBlind < 0)
                        {
                            blindsError = " (big blind cannot be less than zero: set to none)";
                        }
                        else if (newBlind >= 0)
                        {
                            session.PokerData.BigBlind = newBlind;
                        }
                    }
                    else if (s.StartsWith("setblinds:") || s.StartsWith("setblind:") || s.StartsWith("blinds:") || s.StartsWith("blind:"))
                    {
                        string mod = s.Replace("setblinds:", "").Replace("setblind:", "").Replace("blinds:", "").Replace("blind:", "");
                        int newBlind = -1;
                        int.TryParse(mod, out newBlind);
                        if (newBlind < 0)
                        {
                            blindsError = " (blinds cannot be less than zero: set to none)";
                        }
                        else if (newBlind >= 0)
                        {
                            session.PokerData.BigBlind = newBlind;
                            session.PokerData.SmallBlind = Math.Min(newBlind, newBlind / 2);
                        }
                    }
                }

                session.PokerData.RulesSet = true;
                session.PokerData.decksNumber = deckNumber;
                session.PokerData.Rules = ruleset;
                session.PokerData.MaxHandSize = newCardTotal;
                session.PokerData.AllowTokens = allowTokens;
                session.PokerData.AllowCheckRaise = checkRaise;

                if(session.PokerData.SmallBlind > session.PokerData.BigBlind)
                {
                    blindsError += " (small blind cannot exceed big blind. Set to big blind amount)";
                    session.PokerData.SmallBlind = session.PokerData.BigBlind;
                }

                outputString += "Rules set: (game type: " + ruleset + ") (decks: " + deckNumber + ") (ante: " + session.Ante + ") (blinds: " + session.PokerData.SmallBlind + " small / " + session.PokerData.BigBlind + " big) " + maxbet + " " + handSize + " " + (allowTokens ? "(allowing token betting) " : "") + "(checkraise allowed: " + checkRaise + " ) " + blindsError;
            }

            if (GetMinCards(session.Players.Count() + 1, session) > 52)
            {
                outputString += "[color=orange]Warning: more than " + session.Players.Count() + " players is not recommended with these rules because the deck might run out of cards.[/color]";
            }

            if(session.PokerData != null && session.PokerData.CurrentTurnOrder != null && session.PokerData.CurrentTurnOrder.Count() > 0)
            {
                session.PokerData.CurrentTurnOrder.Add(characterName);
            }

            messageString = outputString;
            return true;
        }

        public int GetMinCards(int playerCount, GameSession session)
        {
            int minCards = 0;

            switch (session.PokerData.Rules)
            {
                case PokerRuleset.DBHoldem:
                    minCards = playerCount * 3 + 5 + 3; //3 flop 1 turn 1 river //3 burn - 1 at each step
                    break;
                case PokerRuleset.TexasHoldem:
                    minCards = playerCount * 2 + 5 + 3; //3 flop 1 turn 1 river //3 burn - 1 at each step
                    break;
                case PokerRuleset.FiveCardStud:
                case PokerRuleset.NCardStud:
                    minCards = playerCount * session.PokerData.MaxHandSize;
                    break;
                case PokerRuleset.FiveCardDraw:
                case PokerRuleset.NCardDraw:
                    minCards = playerCount * session.PokerData.MaxHandSize * 2;
                    break;
            }
            return minCards;
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            string outputString = "[i]Setting up Poker...[/i]";
            SavedData.ChannelSettings channelSettings = botMain.GetChannelSettings(session.ChannelId);
            session.PokerData.CardPrintSetting = channelSettings.CardPrintSetting;

            bool resetDeck = false;

            if (session.PokerData.PokerPlayers != null && session.PokerData.PokerPlayers.Count > 0)
            {
                Deck d = diceBot.GetDeck(session.ChannelId, DeckType.Playing, null);
                int minCards = GetMinCards(session.Players.Count(), session);
                if (d.GetCardsRemaining() < minCards)
                    resetDeck = true;
                if (d.CardsCount() < minCards)
                    outputString += " [color=brown]Warning: The number of players and hand sizes in this game could cause the deck to run out of cards with the current rules.[/color]";
            }
            else
            {
                resetDeck = true;
            }
            //note: resetting the deck seems to be 'the thing to do' each game between hands in poker so just auto resetting here
            resetDeck = true;

            if(resetDeck)
                diceBot.ResetDeck(false, session.PokerData.decksNumber, session.ChannelId, channelSettings.CardPrintSetting, DeckType.Playing, null);

            session.PokerData.PokerPlayers = new List<PokerPlayer>();
            //set up starting data 
            foreach (string playerName in session.Players)
            {
                session.PokerData.PokerPlayers.Add(new PokerPlayer()
                {
                    Active = true,
                    BetAmount = 0,
                    HandEvaluation = null,
                    PlayerHand = null,
                    Folded = false,
                    AllIn = false,
                    CannotAfford = false,
                    HasActedThisRound = false,
                    PlayerName = playerName
                });
            }

            if (session.PokerData.CurrentTurnOrder != null && session.PokerData.CurrentTurnOrder.Count() > 0)
            {
                session.PokerData.ShiftPlayersLeft();
            }
            else
            {
                session.PokerData.ShufflePlayers(diceBot.random);

                session.PokerData.CurrentTurnOrder = new List<string>();
                foreach(PokerPlayer player in session.PokerData.PokerPlayers)
                {
                    session.PokerData.CurrentTurnOrder.Add(player.PlayerName);
                }
            }

            session.PokerData.CurrentBetLevel = session.Ante + session.PokerData.BigBlind;
            session.PokerData.CurrentPotTotal = 0;
            session.PokerData.CurrentPotTokenBets = new List<TokenBet>();

            session.PokerData.PokerBettingPhase = PokerBettingPhase.InitialHandBets;

            bool smallBlind = true;
            bool bigBlind = false;
            //put in all antes
            foreach(PokerPlayer bet in session.PokerData.PokerPlayers)
            {
                ChipPile pile = diceBot.GetChipPile(bet.PlayerName, session.ChannelId, false);

                if(pile.Chips >= session.Ante + session.PokerData.BigBlind)
                {
                    bet.CannotAfford = false;
                    int totalBetAmount = session.Ante;
                    if(smallBlind)
                    {
                        smallBlind = false;
                        bigBlind = true;
                        if( session.PokerData.SmallBlind > 0)
                        {
                            totalBetAmount += session.PokerData.SmallBlind;
                        }
                    }
                    else if (bigBlind && session.PokerData.BigBlind > 0)
                    {
                        bigBlind = false;
                        totalBetAmount += session.PokerData.BigBlind;
                    }
                    IncreaseBet(diceBot, botMain, session, session.ChannelId, bet, totalBetAmount);
                }
                else
                {
                    bet.HasActedThisRound = true;
                    bet.CannotAfford = true;
                }
            }

            string drawOut = "";
            foreach(PokerPlayer bet in session.PokerData.PokerPlayers)
            {
                int initialCardsDrawn = session.PokerData.MaxHandSize;// 5;

                if(!bet.CannotAfford)
                {
                    diceBot.DrawCards(initialCardsDrawn, false, true, session.ChannelId, DeckType.Playing, null, bet.PlayerName, false, DeckType.NONE, null, out drawOut);
                    Hand h2 = diceBot.GetHand(session.ChannelId, DeckType.Playing, null, bet.PlayerName);
                    bet.PlayerHand = h2;

                    botMain.SendPrivateMessage("Poker hand drawn: " + h2.Print(false, channelSettings.CardPrintSetting), bet.PlayerName);
                }
            }

            string playersPrint = session.PokerData.PrintPlayers();

            session.PokerData.PokerPlayers.RemoveAll(ab => ab.CannotAfford);

            if(session.PokerData.PokerPlayers.Count == 0)
                return outputString + "\n" + playersPrint + "\n[b]all players were removed for failing to meet ante[/b].";
            else if (session.PokerData.PokerPlayers.Count < GetMinPlayers())
            {
                ReturnAllBets(diceBot, session);
                return outputString + "\n" + playersPrint + "\n[b]there are not enough players left to play who can meet ante[/b]. Ante bets have been returned.";
            }

            //don't claim pot with dealer (although we could...) //TODO: make a poker bets pot entity and use that instead of normal pot
            botMain.BotCommandController.SaveChipsToDisk("Poker setup");

            session.PokerData.currentPlayerIndex = 0;
            string currentTurn = PrintCurrentPlayerTurn(session, false);

            session.State = DiceFunctions.GameState.GameInProgress;
            
            return outputString + "\n" + playersPrint + "\n" + currentTurn + " [sub]!gc raise ###, !gc check, !gc fold, !gc allin[/sub]";
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {

        }

        public void ReturnAllBets(DiceBot diceBot, GameSession session)
        {
            foreach (PokerPlayer player in session.PokerData.PokerPlayers)
            {
                if (player.BetAmount > 0)
                {
                    diceBot.GiveChips(DiceBot.PotPlayerAlias, player.PlayerName, session.ChannelId, player.BetAmount, false);
                    player.BetAmount = 0;
                }
            }
        }

        public void ReturnAllTokenBets(DiceBot diceBot, GameSession session)
        {
            foreach (TokenBet bet in session.PokerData.CurrentPotTokenBets)
            {
                if(bet.TokenAmount > 0)
                {
                    PokerPlayer player = session.PokerData.GetPlayer(bet.CharacterName);
                    player.BetAmount -= bet.TokenAmount;
                    player.ActualChipsAmountBet -= bet.TokenAmount;
                    player.HasActedThisRound = false;
                    session.PokerData.CurrentPotTotal -= bet.TokenAmount;
                }
            }
            session.PokerData.CurrentBetLevel = session.PokerData.PokerPlayers.Max(a => a.BetAmount);
            session.PokerData.CurrentPotTokenBets = new List<TokenBet>();
        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            string outputstring = "Goodbye " + Utils.GetCharacterUserTags( characterName);

            if (session.PokerData.PokerPlayers.Count(c => c.PlayerName == characterName) > 0)
            {
                PokerPlayer thisPlayer = session.PokerData.PokerPlayers.FirstOrDefault(d => d.PlayerName == characterName);

                int indexOfPlayer = session.PokerData.PokerPlayers.IndexOf(thisPlayer);

                if (thisPlayer.BetAmount > 0)
                {
                    botMain.DiceBot.GiveChips(DiceBot.PotPlayerAlias, thisPlayer.PlayerName, session.ChannelId, thisPlayer.BetAmount, false);
                    session.PokerData.CurrentPotTotal -= thisPlayer.BetAmount;

                    outputstring += ", your bet amount of " + thisPlayer.BetAmount + " was returned.";
                    botMain.BotCommandController.SaveChipsToDisk("PokerPlayerLeft");
                }

                if (session.PokerData.PokerPlayers != null)
                    session.PokerData.PokerPlayers.Remove(thisPlayer);
                if (session.PokerData.CurrentTurnOrder != null)
                    session.PokerData.CurrentTurnOrder.Remove(characterName);

                if (indexOfPlayer < session.PokerData.currentPlayerIndex)
                {
                    session.PokerData.currentPlayerIndex -= 1;
                }
                else if (indexOfPlayer == session.PokerData.currentPlayerIndex)
                {
                    session.PokerData.currentPlayerIndex -= 1;

                    if (session.Players.Count() == 0)
                        return outputstring;

                    bool ended = PassTurnToNextPlayer(botMain.DiceBot, botMain, session, session.ChannelId);

                    if(session.State != GameState.Finished) //round not finished by passing turn
                    {
                        outputstring += ", " + PrintCurrentPlayerTurn(session, ended);
                    }
                }
            }

            return outputstring;
        }

        public int GetPokerNumberScoreForCard(DeckCard d)
        {
            if (d.number == 1)
                return 14;
            else
                return d.number;
        }

        public PokerHandResult EvaluatePokerHand(Hand h, bool folded, Hand communityHand)
        {
            PokerHandResult result = new PokerHandResult();
            result.folded = folded;
            result.score = 100;//this is to tiebreak if one hand remains the others folded before 5 cards

            if ((h != null && h.CardsCount() >= 5 ) || (h != null && h.CardsCount() >= 1 && communityHand != null && (h.CardsCount() + communityHand.CardsCount()) >= 5))
            {
                List<DeckCard> allHandCards = new List<DeckCard>();
                for(int i = 0; i < h.CardsCount(); i++)
                {
                    allHandCards.Add(h.GetCardAtIndex(i));
                }
                if(communityHand != null)
                {
                    for (int i = 0; i < communityHand.CardsCount(); i++)
                    {
                        allHandCards.Add(communityHand.GetCardAtIndex(i));
                    }
                }

                var groupedCardsByNumber = allHandCards
                    .GroupBy(card => card.number);
                var groupedCardsBySuit = allHandCards.GroupBy(card => card.suit);

                PokerHandType evaluatedType = PokerHandType.HighCard;
                long scoreBonus = 0;
                long handTypeMultiplier = 100000000000;
                long firstTiebreak = 1000000000;
                long secondTiebreak = 10000000;
                long thirdTiebreak = 100000;
                long fourthTiebreak = 1000;
                long fifthTiebreak = 10;
                //find if there are 2 of a kind
                var twoSameNumber = groupedCardsByNumber.Where(group => group.Count() == 2).OrderByDescending(a => GetPokerNumberScoreForCard(a.ElementAt(0)));
                
                if (twoSameNumber != null && twoSameNumber.Count() == 1)
                {
                    DeckCard pairCard = twoSameNumber.ElementAt(0).First();
                    scoreBonus = GetPokerNumberScoreForCard(pairCard) * firstTiebreak; //1 mil
                    List<DeckCard> restOfHand = allHandCards.Where(a => a.number != pairCard.number).OrderByDescending(a => GetPokerNumberScoreForCard(a)).ToList();// a.number).ToList();
                    scoreBonus += GetPokerNumberScoreForCard(restOfHand[0]) * thirdTiebreak;
                    scoreBonus += GetPokerNumberScoreForCard(restOfHand[1]) * fourthTiebreak;
                    scoreBonus += GetPokerNumberScoreForCard(restOfHand[2]) * fifthTiebreak;
                    evaluatedType = PokerHandType.OnePair;
                }
                if (twoSameNumber != null && twoSameNumber.Count() >= 2)
                {
                    DeckCard pairCard1 = twoSameNumber.ElementAt(0).First();
                    DeckCard pairCard2 = twoSameNumber.ElementAt(1).First();
                    long scorebonus1 = GetPokerNumberScoreForCard(pairCard1);
                    long scorebonus2 = GetPokerNumberScoreForCard(pairCard2);
                    long bigBonus = Math.Max(scorebonus1, scorebonus2);
                    long smallBonus = Math.Min(scorebonus1, scorebonus2);
                    scoreBonus = bigBonus * firstTiebreak + smallBonus * secondTiebreak;
                    DeckCard lastCard = allHandCards.FirstOrDefault(a => a.number != pairCard1.number && a.number != pairCard2.number);
                    scoreBonus += GetPokerNumberScoreForCard(lastCard) * thirdTiebreak;

                    evaluatedType = PokerHandType.TwoPair;
                }

                var threeSameNumberGroup = groupedCardsByNumber.Where(group => group.Count() == 3);

                IGrouping<int, DeckCard> threeSameNumber = null;
                if(threeSameNumberGroup != null)
                {
                    threeSameNumberGroup = threeSameNumberGroup.OrderByDescending(a => GetPokerNumberScoreForCard(a.ElementAt(0)));
                    threeSameNumber = threeSameNumberGroup.FirstOrDefault();
                }
                
                if (threeSameNumber != null && threeSameNumber.Count() > 0)//has count of 3, one for each card, if a grouping is found
                {
                    DeckCard pairCard1 = threeSameNumber.First();
                    scoreBonus = GetPokerNumberScoreForCard(pairCard1) * firstTiebreak;
                    List<DeckCard> restOfHand = allHandCards.Where(a => a.number != pairCard1.number).OrderByDescending(a => GetPokerNumberScoreForCard(a)).ToList();
                    scoreBonus += GetPokerNumberScoreForCard(restOfHand[0]) * secondTiebreak;
                    scoreBonus += GetPokerNumberScoreForCard(restOfHand[1]) * thirdTiebreak;
                    
                    evaluatedType = PokerHandType.ThreeOfAKind;
                }
                if (threeSameNumber != null && threeSameNumber.Count() > 0 && twoSameNumber != null && twoSameNumber.Count() >= 1)
                {

                    DeckCard pairCard1 = threeSameNumber.First();
                    DeckCard pairCard2 = twoSameNumber.ElementAt(0).First();
                    long scorebonus1 = GetPokerNumberScoreForCard(pairCard1);
                    long scorebonus2 = GetPokerNumberScoreForCard(pairCard2);
                    scoreBonus = scorebonus1 * firstTiebreak + scorebonus2 * secondTiebreak;

                    evaluatedType = PokerHandType.FullHouse;
                }

                var fourSameNumberGroup = groupedCardsByNumber.Where(group => group.Count() == 4);
                IGrouping<int, DeckCard> fourSameNumber = null;

                if (fourSameNumberGroup != null)
                {
                    fourSameNumberGroup = fourSameNumberGroup.OrderByDescending(a => GetPokerNumberScoreForCard(a.ElementAt(0)));
                    fourSameNumber = fourSameNumberGroup.FirstOrDefault();
                }

                if (fourSameNumber != null && fourSameNumber.Count() > 0)
                {
                    evaluatedType = PokerHandType.FourOfAkind;
                    DeckCard pairCard1 = fourSameNumber.First();
                    DeckCard lastCard = allHandCards.FirstOrDefault(a => a.number != pairCard1.number);
                    scoreBonus = GetPokerNumberScoreForCard(pairCard1) * firstTiebreak;
                    scoreBonus += GetPokerNumberScoreForCard(lastCard) * secondTiebreak;
                }

                
                var fiveSameSuit = groupedCardsBySuit.FirstOrDefault(group => group.Count() >= 5);

                if (fiveSameSuit != null && fiveSameSuit.Count() > 0)
                {
                    if (evaluatedType < PokerHandType.Flush)
                    {
                        evaluatedType = PokerHandType.Flush;
                        List<DeckCard> restOfHand = fiveSameSuit.OrderByDescending(a => GetPokerNumberScoreForCard(a)).ToList();
                        scoreBonus = GetPokerNumberScoreForCard(restOfHand[0]) * firstTiebreak;
                        scoreBonus += GetPokerNumberScoreForCard(restOfHand[1]) * secondTiebreak;
                        scoreBonus += GetPokerNumberScoreForCard(restOfHand[2]) * thirdTiebreak;
                        scoreBonus += GetPokerNumberScoreForCard(restOfHand[3]) * fourthTiebreak;
                        scoreBonus += GetPokerNumberScoreForCard(restOfHand[4]) * fifthTiebreak;
                    }
                }

                bool hasStraight = false;
                var uniqueNumber = groupedCardsByNumber.Where(group => group.Count() >= 1);

                bool hasAce = allHandCards.Count(a => a.number == 1) > 0;

                int highestStraightStartingNumber = -1;
                List<int> straightStartingNumbers = new List<int>();

                if(uniqueNumber.Count() >= 5 )
                {
                    var sortedNumbers = groupedCardsByNumber.OrderBy(group => group.Key);

                    // Iterate through the grouped cards
                    foreach (var group in sortedNumbers)
                    {
                        // Check for a straight
                        int straightCount = 1;
                        int startingCardNumber = group.FirstOrDefault() == null? -1 : (group.FirstOrDefault().number);
                        int currentStraightNumber = startingCardNumber;
                        for(int i = 0; i < 4; i++)
                        {
                            currentStraightNumber += 1;

                            // Check if the next card's number is one greater
                            if (allHandCards.Any(c => c.number == currentStraightNumber))
                            {
                                straightCount++;
                            }
                            else
                            {
                                straightCount = 1;
                                i = 5;
                            }

                            // Check if we have a straight of 5
                            if (straightCount == 5 || (straightCount == 4 && hasAce && startingCardNumber == 10))
                            {
                                hasStraight = true;
                                highestStraightStartingNumber = startingCardNumber;
                                straightStartingNumbers.Add(startingCardNumber);
                                break;
                            }
                        }
                    }
                }

                if (hasStraight)
                {
                    if (evaluatedType < PokerHandType.Straight)
                    {
                        evaluatedType = PokerHandType.Straight;

                        var highestCard = highestStraightStartingNumber + 4;
                        scoreBonus = firstTiebreak * highestCard;
                    }

                    if (fiveSameSuit != null && fiveSameSuit.Count() > 0 && straightStartingNumbers.Count() > 0)
                    {
                        //veryify a 5-suit is the one causing (a) straight
                        bool suitIntegrity = false;
                        int relevantStraightStartingNumber = -1;
                        foreach(int suitTestStraightNumber in straightStartingNumbers)
                        {
                            for (int suit = 0; suit < 4; suit++)
                            {
                                for (int i = 0; i < 5; i++)
                                {
                                    DeckCard relevantCard = allHandCards.FirstOrDefault(a => GetPokerNumberScoreForCard(a) == suitTestStraightNumber + i && a.suit == suit);
                                    if (relevantCard == null)
                                    {
                                        i = 10;
                                    }
                                    else if (i == 4)
                                    {
                                        suitIntegrity = true;
                                        relevantStraightStartingNumber = suitTestStraightNumber;
                                    }
                                }
                            }//NOTE: (not sure if currently accurate) will fail for 3♦ 4♦ 5♦ 6♦ 7♦ 8♥ 2♠ 12♦ 13♦, because startingStraightNumber is always the highest straight starting #
                        }

                        if(suitIntegrity)
                        {
                            if (evaluatedType < PokerHandType.StraightFlush)
                            {
                                evaluatedType = PokerHandType.StraightFlush;

                                var highestCard = relevantStraightStartingNumber + 4;
                                scoreBonus = firstTiebreak * highestCard;
                            }

                            if (relevantStraightStartingNumber == 10 && allHandCards.Count(a => a.number == 10) >= 1
                                && allHandCards.Count(a => a.number == 11) >= 1
                                && allHandCards.Count(a => a.number == 12) >= 1
                                && allHandCards.Count(a => a.number == 13) >= 1
                                && allHandCards.Count(a => a.number == 1) >= 1)
                                evaluatedType = PokerHandType.RoyalFlush;
                        }

                    }
                }

                if(evaluatedType == PokerHandType.HighCard)
                {
                    var orderedHand = allHandCards.OrderByDescending(a => GetPokerNumberScoreForCard(a)).ToList();
                    long bonus = GetPokerNumberScoreForCard(orderedHand[0]) * firstTiebreak +
                        GetPokerNumberScoreForCard(orderedHand[1]) * secondTiebreak +
                        GetPokerNumberScoreForCard(orderedHand[2]) * thirdTiebreak +
                        GetPokerNumberScoreForCard(orderedHand[3]) * fourthTiebreak +
                        GetPokerNumberScoreForCard(orderedHand[4]) * fifthTiebreak;
                    scoreBonus = bonus;
                }

                result.HandType = evaluatedType;

                result.score = ((int)evaluatedType + 1) * handTypeMultiplier; 

                result.score = result.score + scoreBonus;
            }

            if (folded)
                result.score = 0;

            return result;
        }

        public bool PassTurnToNextPlayer(DiceBot diceBot, BotMain botMain, GameSession session, string channel)
        {
            PokerData data = session.PokerData;

            if(data != null && data.PokerPlayers != null)
            {
                PokerPlayer thisPlayer = data.GetCurrentPlayer();
                if (thisPlayer == null)
                    return false;

                thisPlayer.HasActedThisRound = true;
                data.currentPlayerIndex += 1;

                if (data.currentPlayerIndex >= data.PokerPlayers.Count)
                {
                    data.currentPlayerIndex = 0;
                }

                thisPlayer = data.GetCurrentPlayer();

                data.ForceStandVotes = new List<String>();

                if (PlayersAllFinishedThisRound(session))
                {
                    FinishRound(diceBot, botMain, session, channel);
                    return true;
                }
                else if (thisPlayer.Folded || thisPlayer.AllIn || thisPlayer.CannotAfford)
                {
                    return PassTurnToNextPlayer(diceBot, botMain, session, channel);
                }
            }
            return false;
        }

        public void FinishRound(DiceBot diceBot, BotMain botMain, GameSession session, string channel)
        {
            SavedData.ChannelSettings settings = botMain.GetChannelSettings(channel);

            List<PokerPlayer> activeHandPlayers = session.PokerData.PokerPlayers.Where(a => a.Active && !a.Folded && !a.CannotAfford).ToList();
            string activePlayers = "";
            foreach(PokerPlayer player in activeHandPlayers)
            {
                if (!string.IsNullOrEmpty(activePlayers))
                    activePlayers += ", ";
                activePlayers += Utils.GetCharacterUserTags(player.PlayerName);
            }
            if(!string.IsNullOrEmpty(activePlayers))
                activePlayers = "Active Players: " + activePlayers;

            bool finished = false;
            switch(session.PokerData.Rules)
            {
                case PokerRuleset.FiveCardStud:
                case PokerRuleset.NCardStud:
                    FinishHand(diceBot, botMain, session, channel);
                    break;
                case PokerRuleset.DBHoldem:
                case PokerRuleset.TexasHoldem:
                    //3 flop 1 turn 1 river, bet on each round
                    //started with 2 cards for each player
                    if (OnePlayerRemains(session, true) || CurrentBetLevelIsMaxBet(session))
                    {
                        Hand dealerHand = diceBot.GetHand(session.ChannelId, DeckType.Playing, null, DiceBot.DealerPlayerAlias);
                        if(dealerHand.CardsCount() < 5 && session.PokerData.PokerPlayers.Count(a => !a.CannotAfford && !a.Folded && a.Active) > 1)
                        {
                            string drawnString = "";
                            string drawResultCards = diceBot.DrawCards(5 - dealerHand.CardsCount(), false, true, session.ChannelId, DeckType.Playing, null, DiceBot.DealerPlayerAlias, false, DeckType.NONE, null, out drawnString);

                            botMain.SendFutureMessage("There are no more actions to take in this hand. Drawing the remaining cards for the dealer...", session.ChannelId, null, true, 900);
                        }
                        session.PokerData.PokerBettingPhase = PokerBettingPhase.BettingPhase4;

                    }

                    switch(session.PokerData.PokerBettingPhase)
                    {
                        case PokerBettingPhase.InitialHandBets: //3 card flop
                            {
                                string drawnString = "";
                                string drawResultBurn = diceBot.DrawCards(1, false, true, session.ChannelId, DeckType.Playing, null, DiceBot.BurnCardsPlayerAlias, true, DeckType.NONE, null, out drawnString);
                                string drawResultCards = diceBot.DrawCards(3, false, true, session.ChannelId, DeckType.Playing, null, DiceBot.DealerPlayerAlias, false, DeckType.NONE, null, out drawnString);
                                Hand dealerHand = diceBot.GetHand(session.ChannelId, DeckType.Playing, null, DiceBot.DealerPlayerAlias);
                                DeckCard lastCard = dealerHand.GetCardAtIndex(dealerHand.CardsCount() - 1);

                                ResetAllPlayerActed(session);
                                session.PokerData.PokerBettingPhase = PokerBettingPhase.BettingPhase2;
                                session.PokerData.currentPlayerIndex = 0;
                                session.PokerData.PassTurnToFirstActivePlayer();
                                string burnString = "Burned [color=red]🔥[/color] one card face down...";
                                if(session.PokerData.Rules == PokerRuleset.DBHoldem)
                                {
                                    Hand burnHand = diceBot.GetHand(session.ChannelId, DeckType.Playing, null, DiceBot.BurnCardsPlayerAlias);
                                    DeckCard lastBurnCard = burnHand.GetCardAtIndex(burnHand.CardsCount() - 1);
                                    burnString = "Burned [color=red]🔥[/color] " + lastBurnCard.Print(settings.CardPrintSetting) + " from the deck...";
                                }

                                botMain.SendFutureMessage("Initial Betting Round ended; the flop round is starting.\n" + burnString + "\nAnd the [color=cyan]draws[/color] are: " + dealerHand.Print(false, settings.CardPrintSetting, false) + "\n"
                                    + "Current Community Cards: " + dealerHand.Print(false, settings.CardPrintSetting, false) + "\n"
                                    + activePlayers + "\n" 
                                    + "Place your bets. [sub]!gc raise #, !gc check, !gc fold, !gc all-in[/sub] " + PrintCurrentPlayerTurn(session, finished), channel, null, true, RoundEndingWaitMsPerPlayer * session.PokerData.PokerPlayers.Count() + RoundEndingWaitMs);
                            }
                            break;
                        case PokerBettingPhase.BettingPhase2: //1 card turn
                            {
                                string drawnString = "";
                                string drawResultBurn = diceBot.DrawCards(1, false, true, session.ChannelId, DeckType.Playing, null, DiceBot.BurnCardsPlayerAlias, true, DeckType.NONE, null, out drawnString);
                                string drawResultCards = diceBot.DrawCards(1, false, true, session.ChannelId, DeckType.Playing, null, DiceBot.DealerPlayerAlias, false, DeckType.NONE, null, out drawnString);
                                Hand dealerHand = diceBot.GetHand(session.ChannelId, DeckType.Playing, null, DiceBot.DealerPlayerAlias);
                                DeckCard lastCard = dealerHand.GetCardAtIndex(dealerHand.CardsCount() - 1);
                                
                                ResetAllPlayerActed(session);
                                session.PokerData.PokerBettingPhase = PokerBettingPhase.BettingPhase3;
                                session.PokerData.currentPlayerIndex = 0;
                                session.PokerData.PassTurnToFirstActivePlayer();
                                string burnString = "Burned [color=red]🔥[/color] one card face down...";
                                if (session.PokerData.Rules == PokerRuleset.DBHoldem)
                                {
                                    Hand burnHand = diceBot.GetHand(session.ChannelId, DeckType.Playing, null, DiceBot.BurnCardsPlayerAlias);
                                    DeckCard lastBurnCard = burnHand.GetCardAtIndex(burnHand.CardsCount() - 1);
                                    burnString = "Burned [color=red]🔥[/color] " + lastBurnCard.Print(settings.CardPrintSetting) + " from the deck...";
                                }

                                botMain.SendFutureMessage("Flop Betting Round ended; the turn round is starting.\n" + burnString + "\nAnd the [color=cyan]draw[/color] is: " + lastCard.Print(settings.CardPrintSetting) + "\n"
                                    + "Current Community Cards: " + dealerHand.Print(false, settings.CardPrintSetting, false) + "\n"
                                    + activePlayers + "\n" 
                                    + "Place your bets. [sub]!gc raise #, !gc check, !gc fold, !gc all-in[/sub] " + PrintCurrentPlayerTurn(session, finished), channel, null, true, RoundEndingWaitMsPerPlayer * session.PokerData.PokerPlayers.Count() + RoundEndingWaitMs);
                            }
                            break;
                        case PokerBettingPhase.BettingPhase3: //1 card river
                            {
                                string drawnString = "";
                                string drawResultBurn = diceBot.DrawCards(1, false, true, session.ChannelId, DeckType.Playing, null, DiceBot.BurnCardsPlayerAlias, true, DeckType.NONE, null, out drawnString);
                                string drawResultCards = diceBot.DrawCards(1, false, true, session.ChannelId, DeckType.Playing, null, DiceBot.DealerPlayerAlias, false, DeckType.NONE, null, out drawnString);
                                Hand dealerHand = diceBot.GetHand(session.ChannelId, DeckType.Playing, null, DiceBot.DealerPlayerAlias);
                                DeckCard lastCard = dealerHand.GetCardAtIndex(dealerHand.CardsCount() - 1);

                                ResetAllPlayerActed(session);
                                session.PokerData.PokerBettingPhase = PokerBettingPhase.BettingPhase4;
                                session.PokerData.currentPlayerIndex = 0;
                                session.PokerData.PassTurnToFirstActivePlayer();
                                string burnString = "Burned [color=red]🔥[/color] one card face down...";
                                if (session.PokerData.Rules == PokerRuleset.DBHoldem)
                                {
                                    Hand burnHand = diceBot.GetHand(session.ChannelId, DeckType.Playing, null, DiceBot.BurnCardsPlayerAlias);
                                    DeckCard lastBurnCard = burnHand.GetCardAtIndex(burnHand.CardsCount() - 1);
                                    burnString = "Burned [color=red]🔥[/color] " + lastBurnCard.Print(settings.CardPrintSetting) + " from the deck...";
                                }

                                botMain.SendFutureMessage("Turn Betting Round ended; the river round is starting.\n" + burnString + "\nAnd the [color=cyan]draw[/color] is: " + lastCard.Print(settings.CardPrintSetting) + "\n"
                                    + "Current Community Cards: " + dealerHand.Print(false, settings.CardPrintSetting, false) + "\n"
                                    + activePlayers + "\n" 
                                    + "Place your bets. [sub]!gc raise #, !gc check, !gc fold, !gc all-in[/sub] " + PrintCurrentPlayerTurn(session, finished), channel, null, true, RoundEndingWaitMsPerPlayer * session.PokerData.PokerPlayers.Count() + RoundEndingWaitMs);
                            }
                            break;
                        case PokerBettingPhase.BettingPhase4://finish round
                            session.PokerData.PokerBettingPhase = PokerBettingPhase.FinishedBetting;
                            FinishHand(diceBot, botMain, session, channel);
                            break;

                    }
                    break;
                case PokerRuleset.FiveCardDraw:
                case PokerRuleset.NCardDraw:
                    if (OnePlayerRemains(session, true) || 
                        (CurrentBetLevelIsMaxBet(session) && session.PokerData.PokerBettingPhase == PokerBettingPhase.DrawPhase) )
                    {
                        session.PokerData.PokerBettingPhase = PokerBettingPhase.BettingPhase2;
                        botMain.SendFutureMessage("There are no more actions to take in this hand. Skipping to completion...", session.ChannelId, null, true, 900);
                    }

                    switch(session.PokerData.PokerBettingPhase)
                    {
                        case PokerBettingPhase.InitialHandBets:
                            ResetAllPlayerActed(session);
                            session.PokerData.PokerBettingPhase = PokerBettingPhase.DrawPhase;
                            session.PokerData.currentPlayerIndex = 0;
                            session.PokerData.PassTurnToFirstActivePlayer();
                            botMain.SendFutureMessage("Initial Betting Round ended; the draw round is starting.\n"
                                    + activePlayers + "\n" + 
                                    "Choose your cards to redraw [i](specify each card number)[/i]. [sub]!gc draw 1 2 3 4 5, !gc keep[/sub] " + PrintCurrentPlayerTurn(session, finished), channel, null, true, RoundEndingWaitMsPerPlayer * session.PokerData.PokerPlayers.Count() + RoundEndingWaitMs);
                            break;
                        case PokerBettingPhase.DrawPhase:
                            ResetAllPlayerActed(session);
                            session.PokerData.PokerBettingPhase = PokerBettingPhase.BettingPhase2;
                            session.PokerData.currentPlayerIndex = 0;
                            session.PokerData.PassTurnToFirstActivePlayer();
                            botMain.SendFutureMessage("Player draws ended; the final betting round is starting.\n"
                                    + activePlayers + "\n" + 
                                    "Place your final bets. [sub]!gc raise #, !gc check, !gc allin, !gc fold[/sub] " + PrintCurrentPlayerTurn(session, finished), channel, null, true, RoundEndingWaitMsPerPlayer * session.PokerData.PokerPlayers.Count() + RoundEndingWaitMs);
                            break;
                        case PokerBettingPhase.BettingPhase2:
                            session.PokerData.PokerBettingPhase = PokerBettingPhase.FinishedBetting;
                            FinishHand(diceBot, botMain, session, channel);
                            break;

                    }
                    break;
            }
        }

        public void FinishHand(DiceBot diceBot, BotMain botMain, GameSession session, string channel)
        {
            SavedData.ChannelSettings channelSettings = botMain.GetChannelSettings(channel);

            int remainingPot = session.PokerData.GetPotTotalChipsNotTokens();
            int actualPotChips = remainingPot;

            string outputString = "";
            string playerHandsString = "";


            if (session.PokerData.PokerPlayers != null)
            {
                bool showAnyHand = session.PokerData.PokerPlayers.Count(a => a.Active && !a.Folded && !a.CannotAfford) > 1;

                Hand dealerHand = (session.PokerData.Rules == PokerRuleset.TexasHoldem || session.PokerData.Rules == PokerRuleset.DBHoldem) ? 
                    diceBot.GetHand(session.ChannelId, DeckType.Playing, null, DiceBot.DealerPlayerAlias) : null;
                //score each hand to check whose hand is the best
                foreach (PokerPlayer player in session.PokerData.PokerPlayers)
                {

                    player.HandEvaluation = EvaluatePokerHand(player.PlayerHand, player.Folded, dealerHand);

                    if (!string.IsNullOrEmpty(playerHandsString))
                        playerHandsString += "\n";

                    string handPrintout = (player.HandEvaluation.folded || !showAnyHand) ? " (cards hidden) " : player.PlayerHand.Print(false, channelSettings.CardPrintSetting, false);
                    string handEvaluationPrintout = (player.HandEvaluation.folded || !showAnyHand) ? "" : " (" + player.HandEvaluation.ToString() + ") ";
                    playerHandsString += Utils.GetCharacterUserTags(player.PlayerName) + " 's hand : " + handPrintout + handEvaluationPrintout + 
                        ((player.Folded || player.CannotAfford)? "[color=gray](folded)[/color]":"[color=green](active)[/color] ") + (player.AllIn? "[color=red](all in)[/color]":"");
                }

                playerHandsString = dealerHand == null ? playerHandsString : ("[color=yellow]Community Cards:[/color] " + dealerHand.Print(false, channelSettings.CardPrintSetting, false) + "\n" + playerHandsString);

                List<PokerPlayer> playerHandsRanked = new List<PokerPlayer>();
                playerHandsRanked = session.PokerData.PokerPlayers.OrderByDescending(a => a.HandEvaluation.score).ToList();

                int currentHandIndex = 0;

                List<PokerBetNonsense> payoutAmounts = new List<PokerBetNonsense>();
                //split the pot according to who won
                while(remainingPot > 0 && currentHandIndex < playerHandsRanked.Where(a => a.HandEvaluation.score > 0).Count())
                {
                    //determine who gets paid
                    List<PokerPlayer> currentPaid = playerHandsRanked.Where(a => a.HandEvaluation.score == playerHandsRanked[currentHandIndex].HandEvaluation.score).ToList();

                    List<PokerPlayer> eligibleToPay = playerHandsRanked.Where(a => a.HandEvaluation.score <= currentPaid[0].HandEvaluation.score).ToList(); //eligible players to pay 
                    //determine maximum payout
                    int potPayout = remainingPot / currentPaid.Count();
                    for(int i = 0; i < currentPaid.Count(); i++)
                    {
                        PokerPlayer player = currentPaid[i];
                        int maxPayout = eligibleToPay.Sum(a => Math.Min(currentPaid[i].ActualChipsAmountBet, a.ActualChipsAmountBet)); //paying up to their max bet # each up to the max payout of this player

                        int amountPaid = Math.Min(maxPayout, potPayout);
                        payoutAmounts.Add(new PokerBetNonsense() { AmountPaid = amountPaid, MaxPayout = maxPayout, CharacterName = player.PlayerName, HandEvaluation = currentPaid[i].HandEvaluation });
                    }

                    foreach(PokerBetNonsense thispaid in payoutAmounts)
                    {
                        if(currentPaid.Select(a => a.PlayerName).Contains(thispaid.CharacterName))
                        {
                            remainingPot -= thispaid.AmountPaid;
                        }
                    }

                    currentHandIndex += currentPaid.Count();
                }

                if(payoutAmounts.Count() == 0 && playerHandsRanked.Count() > 0)
                {
                    long maxScore = playerHandsRanked.Max(a => a.HandEvaluation.score);
                    foreach(var hand in playerHandsRanked.Where(a => a.HandEvaluation.score == maxScore))
                    {
                        payoutAmounts.Add( new PokerBetNonsense() { AmountPaid = 0, MaxPayout = 0, CharacterName = hand.PlayerName, HandEvaluation = hand.HandEvaluation, Folded = hand.Folded });
                    }
                }

                if (remainingPot > 0) //I'm not sure what this is solvingggg
                {
                    List<PokerBetNonsense> betnonsenseTemp = new List<PokerBetNonsense>(payoutAmounts);
                    betnonsenseTemp.RemoveAll(a => a.MaxPayout <= a.AmountPaid);
                    if(betnonsenseTemp.Count() > 0)
                    {
                        int remainingSplit = remainingPot / betnonsenseTemp.Count();

                        foreach (PokerBetNonsense player in betnonsenseTemp)
                        {
                            int amountAdded = Math.Min(remainingSplit, player.MaxPayout - player.AmountPaid);
                            player.AmountPaid += amountAdded;
                            remainingPot -= amountAdded;
                        }
                    }
                }
                
                if(payoutAmounts != null && payoutAmounts.Count() > 0)
                {
                    string handOutput = showAnyHand ? " with " + payoutAmounts[0].HandEvaluation.HandType : " by surrender";
                    outputString += Utils.GetCharacterIconTags(payoutAmounts[0].CharacterName) + " [color=green]won[/color]" + "!\n"; 
                }

                bool firstAward = true;
                foreach (PokerBetNonsense payoutAward in payoutAmounts)
                {
                    outputString += "\n";
                    
                    if(payoutAward.AmountPaid > 0)
                        outputString += Utils.GetCharacterUserTags(payoutAward.CharacterName) + " is awarded " + payoutAward.AmountPaid + " chips.";
                    else
                        outputString += Utils.GetCharacterUserTags(payoutAward.CharacterName) + " did not win any chips.";

                    if (firstAward && session.PokerData.CurrentPotTokenBets != null && session.PokerData.CurrentPotTokenBets.Count() > 0)
                        outputString += " They are also awarded: " + session.PokerData.GetPotTokensString();

                    firstAward = false;
                    diceBot.GiveChips(DiceBot.PotPlayerAlias, payoutAward.CharacterName, channel, payoutAward.AmountPaid, false);
                }

                diceBot.ClaimPot(DiceBot.DealerPlayerAlias, channel, 1); //remove extras from pot

                diceBot.EndHand(channel, false, channelSettings.CardPrintSetting, DeckType.Playing, null);
                //save chips modifications to disk
                botMain.BotCommandController.SaveChipsToDisk("Pokerroundfinish");
            }


            string fullPotString = "The pot contains [color=yellow]" + actualPotChips + " chips[/color]. " + session.PokerData.GetPotTokensString();
            botMain.SendFutureMessage("[color=yellow]Round Finished![/color]\n" + fullPotString + "\n" + playerHandsString + "\n" + outputString, channel, null, true, RoundEndingWaitMsPerPlayer + RoundEndingWaitMs);
            session.State = GameState.Finished;

            ResetAllPlayerStatus(session);
            session.PokerData.CurrentBetLevel = 0;
            session.PokerData.CurrentPotTotal = 0;
        }

        public void ResetAllPlayerActed(GameSession session)
        {
            foreach (PokerPlayer player in session.PokerData.PokerPlayers)
            {
                player.ResetStatusHandPhase();
            }
        }

        public void ResetAllPlayerStatus(GameSession session)
        {
            foreach(PokerPlayer player in session.PokerData.PokerPlayers)
            {
                player.ResetStatusFull();
            }
        }

        public string DrawCards(DiceBot diceBot, string channel, string character, List<int> cardIndexes, out string truedraw)
        {
            int discardNumber = 0;
            diceBot.DiscardCards(cardIndexes, false, channel, DeckType.Playing, null, character, out discardNumber);

            string drawString = diceBot.DrawCards(cardIndexes.Count(), false, true, channel, DeckType.Playing, null, character, false, DeckType.NONE, null, out truedraw);

            return Utils.GetCharacterStringFromSpecialName(character) + " discarded " + discardNumber + " cards to draw. " + drawString;
        }

        public string IncreaseBet(DiceBot diceBot, BotMain botMain, GameSession session, string channel, PokerPlayer player, int additionalBetAmount, bool usedTokenBet = false, string tokenTitle = "")
        {
            player.ActualChipsAmountBet = player.BetAmount;
            player.BetAmount = player.BetAmount + additionalBetAmount;

            ChipPile pile = diceBot.GetChipPile(player.PlayerName, channel, false);
            bool allin = false;
            if(!usedTokenBet && pile.Chips < additionalBetAmount)
            {
                additionalBetAmount = pile.Chips;
                allin = true;
                player.AllIn = true;
            }
            player.ActualChipsAmountBet += additionalBetAmount;

            string giveChipsOutput = "";
            if (usedTokenBet)
            {
                string characterString = diceBot.SpecialCharacterName(player.PlayerName)? Utils.GetCharacterStringFromSpecialName(player.PlayerName) : Utils.GetCharacterUserTags(player.PlayerName);
                giveChipsOutput = characterString + " added a token worth " + additionalBetAmount + " chips to the pot.";

                session.PokerData.CurrentPotTokenBets.Add(new TokenBet() { CharacterName = player.PlayerName, TokenAmount = additionalBetAmount, TokenName = tokenTitle });
            }else
            {
                giveChipsOutput = diceBot.GiveChips(player.PlayerName, DiceBot.PotPlayerAlias, channel, additionalBetAmount, allin);
            }

            session.PokerData.CurrentPotTotal += additionalBetAmount;

            if (player.BetAmount > session.PokerData.CurrentBetLevel)
                session.PokerData.CurrentBetLevel = player.BetAmount;

            return giveChipsOutput;
        }

        public string PrintCurrentPlayerTurn(GameSession session, bool roundEnded)
        {
            if (PlayersAllFinishedThisRound(session) || roundEnded)
                return "(The round has finished)";
            else
                return session.PokerData.PrintCurrentPlayerTurn();
        }

        public int ParseNewHandSize(string mod, out string handSizePrint)
        {
            handSizePrint = "";
            int newHandSize = -1;
            int.TryParse(mod, out newHandSize);
            if (newHandSize < 5)
            {
                newHandSize = 5;
                handSizePrint = "(hand size cannot be less 5: set to 5)";
            }
            else if (newHandSize > 10)
            {
                newHandSize = 10;
                handSizePrint = "(hand size cannot be more than 10: set to 10)";
            }
            else if (newHandSize >= 0)
            {
                handSizePrint = "(" + newHandSize + " card hands)";
                
            }

            return newHandSize;
        }

        public bool PlayersAllFinishedThisRound(GameSession session)
        {
            return (session.PokerData.PokerPlayers.Count(a => !a.HasActedThisRound || (a.BetAmount < session.PokerData.CurrentBetLevel && !a.Folded && !a.AllIn && !a.CannotAfford && a.Active)) == 0) ||
                OnePlayerRemains(session, false);
        }

        public bool OnePlayerRemains(GameSession session, bool countAllin)
        {
            if(countAllin)
                return (session.PokerData.PokerPlayers.Count(a => a.Folded || a.CannotAfford || a.AllIn || !a.Active) == session.PokerData.PokerPlayers.Count() - 1);
            else
                return (session.PokerData.PokerPlayers.Count(a => a.Folded || a.CannotAfford || !a.Active) == session.PokerData.PokerPlayers.Count() - 1);
        }

        public bool CurrentBetLevelIsMaxBet(GameSession session)
        {
            return (session.PokerData.MaximumBet >= 0 && session.PokerData.MaximumBet == session.PokerData.CurrentBetLevel);
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, GameSession session, string[] terms, string[] rawTerms)
        {
            string returnString = "";

            PokerPlayer currentPlayer = session.PokerData.GetCurrentPlayer();
            bool currentPlayerIssuedCommand = currentPlayer != null && currentPlayer.PlayerName == character;
            SavedData.ChannelSettings channelSettings = botMain.GetChannelSettings(channel);

            if (terms.Contains("forcestand") || terms.Contains("forcecheck"))
            {
                var bp = session.PokerData.GetPlayer(character);

                if (session.State != GameState.GameInProgress)
                    returnString = "Failed: This command can only be used while the game is in progress.";
                else if (session.PokerData.ForceStandVotes.Contains(character))
                    returnString = "Failed: Each player in the game only has one vote to force stand.";
                else
                {
                    bool characterIsAdmin = Utils.IsCharacterTrusted(botMain.AccountSettings.TrustedCharacters, character, channel) 
                        || Utils.IsCharacterAdmin(botMain.AccountSettings.AdminCharacters, character);
                    if (characterIsAdmin)
                    {
                        session.PokerData.ForceStandVotes.Add(character);
                        session.PokerData.ForceStandVotes.Add(character);
                        session.PokerData.ForceStandVotes.Add(character);
                        session.PokerData.ForceStandVotes.Add(character);
                    }
                    session.PokerData.ForceStandVotes.Add(character);

                    int currentVotes = session.PokerData.ForceStandVotes.Count();
                    int requiredVotes = Math.Max(2, session.PokerData.PokerPlayers.Count() / 2);

                    if (currentVotes >= requiredVotes)
                    {
                        string betChangeAmount = "";

                        if (session.PokerData.CurrentBetLevel > currentPlayer.BetAmount)
                        {
                            betChangeAmount = IncreaseBet(diceBot, botMain, session, channel, currentPlayer, session.PokerData.CurrentBetLevel - currentPlayer.BetAmount);
                        }

                        int currentBetLevel = session.PokerData.CurrentBetLevel;

                        string forcedCharacter = session.PokerData.GetCurrentPlayer().PlayerName;
                        bool finished = PassTurnToNextPlayer(diceBot, botMain, session, channel);

                        returnString = Utils.GetCharacterUserTags(forcedCharacter) + " [color=cyan]checks[/color]. They will meet the current bet of " + currentBetLevel + ".\n"
                            + betChangeAmount + " " + PrintCurrentPlayerTurn(session, finished);
                    }
                    else
                        returnString = Utils.GetCharacterUserTags(character) + " has voted to force stand: " + currentVotes + " / " + (requiredVotes) + " votes.";
                }
            }
            else if (terms.Contains("forceallin") || terms.Contains("forcefold"))
            {
                var bp = session.PokerData.GetPlayer(character);

                if (session.State != GameState.GameInProgress)
                    returnString = "Failed: This command can only be used while the game is in progress.";
                else if (session.PokerData.ForceStandVotes.Contains(character))
                    returnString = "Failed: Each player in the game only has one vote to force stand.";
                else
                {
                    bool characterIsAdmin = Utils.IsCharacterTrusted(botMain.AccountSettings.TrustedCharacters, character, channel)
                        || Utils.IsCharacterAdmin(botMain.AccountSettings.AdminCharacters, character);
                    if (characterIsAdmin)
                    {
                        PokerPlayer forcedPokerPlayer =session.PokerData.GetCurrentPlayer(); 
                        string forcedCharacter = forcedPokerPlayer.PlayerName;
                        string resultString = "";
                        if(terms.Contains("forceallin"))
                        {
                            ChipPile pile = diceBot.GetChipPile(forcedCharacter, channel, false);
                            resultString = "(All In) " + IncreaseBet(diceBot, botMain, session, channel, currentPlayer, pile.Chips);
                            forcedPokerPlayer.AllIn = true;
                        }
                        if(terms.Contains("forcefold"))
                        {
                            forcedPokerPlayer.Folded = true;
                            resultString = "(Fold) " + Utils.GetCharacterUserTags(forcedCharacter) + " folds.";
                        }

                        if (!string.IsNullOrEmpty(resultString))
                        {

                            bool finished = PassTurnToNextPlayer(diceBot, botMain, session, channel);

                            returnString = Utils.GetCharacterUserTags(character) + " forced: " +  resultString +".\n"
                                + PrintCurrentPlayerTurn(session, finished);
                        }

                    }
                    else
                        returnString = "Failed: " + Utils.GetCharacterUserTags(character) + " is not an admin and cannot perform this command.";
                }
            }
            else if (terms.Contains("changebet") || terms.Contains("betchange"))
            {
                var bp = session.PokerData.GetPlayer(character);
                
                int newCurrentBet = Utils.GetNumberFromInputs(terms);

                if (session.State != GameState.GameInProgress)
                    returnString = "Failed: This command can only be used while the game is in progress.";
                else if (session.PokerData.CurrentBetLevel <= 0)
                    returnString = "Failed: The current bet is 0.";
                else if (newCurrentBet <= 0)
                    returnString = "Failed: New bet amount not found. Use !gc changebet ## (with the number of the new bet level)";
                else if (session.PokerData.MaximumBet > 0 && newCurrentBet > session.PokerData.MaximumBet)
                    returnString = "Failed: New bet amount was higher than the current maximum bet (" + session.PokerData.MaximumBet + ")";
                else
                {
                    session.PokerData.CurrentBetLevel = newCurrentBet;
                    foreach(var player in session.PokerData.PokerPlayers)
                    {
                        if (player.BetAmount > session.PokerData.CurrentBetLevel)
                        {
                            player.BetAmount = session.PokerData.CurrentBetLevel;
                        }
                        if(player.ActualChipsAmountBet > session.PokerData.CurrentBetLevel)
                        {
                            int difference = player.ActualChipsAmountBet - session.PokerData.CurrentBetLevel;
                            diceBot.GiveChips(DiceBot.PotPlayerAlias, player.PlayerName, session.ChannelId, difference, false);
                            player.ActualChipsAmountBet = session.PokerData.CurrentBetLevel;

                            session.PokerData.CurrentPotTotal -= difference;

                            if (player.AllIn && difference > 0)
                                player.AllIn = false;
                        }
                    }

                    botMain.BotCommandController.SaveChipsToDisk("poker changebet");
                    returnString = "Updated the current bet to " + newCurrentBet + " chips. All bets above this amount were reduced to it and chips were refunded.";
                }
            }
            else if (terms.Contains("fold") || terms.Contains("surrender") || terms.Contains("call") || terms.Contains("check") || terms.Contains("stand") 
                || terms.Contains("raise") || terms.Contains("bet") || terms.Contains("allin") || terms.Contains("all-in")
                || terms.Contains("draw") || terms.Contains("nodraws") || terms.Contains("nodraw") || terms.Contains("redraw") || terms.Contains("keep")
                || terms.Contains("raisetoken") || terms.Contains("bettoken") || terms.Contains("token"))
            {
                bool onlyBetPhase = 
                    terms.Contains("call") || terms.Contains("check")
                    || terms.Contains("raise") || terms.Contains("bet") || terms.Contains("allin") || terms.Contains("all-in")
                    || terms.Contains("raisetoken") || terms.Contains("bettoken") || terms.Contains("token");
                bool onlyDrawPhase = terms.Contains("draw") || terms.Contains("nodraws") || terms.Contains("nodraw") || terms.Contains("redraw") || terms.Contains("keep");

                bool token = terms.Contains("raisetoken") || terms.Contains("bettoken") || terms.Contains("token");

                if (session.State != GameState.GameInProgress)
                    returnString = "Failed: This command can only be used while the game is in progress.";
                else if (!currentPlayerIssuedCommand)
                    returnString = "Failed: Only the current player can access this command.";
                else if (onlyDrawPhase && session.PokerData.PokerBettingPhase != PokerBettingPhase.DrawPhase )
                    returnString = "Failed: This command can only be used during the draw phase.";
                else if (onlyBetPhase && session.PokerData.PokerBettingPhase == PokerBettingPhase.DrawPhase)
                    returnString = "Failed: This command can only be used during the betting phase.";
                else if (token && !session.PokerData.AllowTokens)
                    returnString = "Failed: Token bets are not allowed in this session of poker.";
                else if (currentPlayer == null)
                    returnString = "Error: Current player not found.";
                else
                {
                    //fold / surrender?
                    if (terms.Contains("fold") || terms.Contains("surrender"))
                    {
                        currentPlayer.Folded = true;

                        bool finished = PassTurnToNextPlayer(diceBot, botMain, session, channel);
                        returnString = Utils.GetCharacterUserTags(character) + " [color=gray]folds[/color]. " + PrintCurrentPlayerTurn(session, finished);
                    }
                    //stand
                    else if (terms.Contains("call") || terms.Contains("check") || (terms.Contains("stand") && session.PokerData.PokerBettingPhase != PokerBettingPhase.DrawPhase))
                    {
                        string betChangeAmount = "";

                        if(session.PokerData.CurrentBetLevel > currentPlayer.BetAmount)
                        {
                            betChangeAmount = IncreaseBet(diceBot, botMain, session, channel, currentPlayer, session.PokerData.CurrentBetLevel - currentPlayer.BetAmount);
                        }

                        int currentBetLevel = session.PokerData.CurrentBetLevel;

                        bool finished = PassTurnToNextPlayer(diceBot, botMain, session, channel);
                        returnString = Utils.GetCharacterUserTags(character) + " [color=cyan]checks[/color]. They will meet the current bet of " + currentBetLevel + ".\n"
                            + betChangeAmount + " " + PrintCurrentPlayerTurn(session, finished);
                    }
                    //raise
                    else if (terms.Contains("raise") || terms.Contains("bet") || terms.Contains("allin") || terms.Contains("all-in")
                        || terms.Contains("raisetoken") || terms.Contains("bettoken") || terms.Contains("token"))
                    {
                        //first call the current raise
                        int firstAmountIncreased = session.PokerData.CurrentBetLevel - currentPlayer.BetAmount;
                        
                        //now determine how much can be raised
                        int amountRaised = 0;
                        var cp = diceBot.GetChipPile(currentPlayer.PlayerName, channel, false);

                        if(terms.Contains("allin") || terms.Contains("all-in") || terms.Contains("all"))
                        {
                            amountRaised = cp.Chips - firstAmountIncreased;
                        }
                        else
                        {
                            amountRaised = Utils.GetNumberFromInputs(terms);
                        }

                        int originalRaise = amountRaised;
                        string raiseChange = "";
                        if (session.PokerData.MaximumBet >= 0 && amountRaised + session.PokerData.CurrentBetLevel > session.PokerData.MaximumBet)
                        {
                            amountRaised = session.PokerData.MaximumBet - (session.PokerData.CurrentBetLevel);
                            amountRaised = Math.Max(0, amountRaised);
                            raiseChange = " Maximum bet exceeded: This session's max bet size is " + session.PokerData.MaximumBet + " chips. (Raise was changed from " + originalRaise + " to " + amountRaised + " chips instead).";
                        }

                        if (amountRaised > 0 && currentPlayer.HasActedThisRound && !token)
                        {
                            returnString = "Failed: you have already raised this round. You may not raise again. [sub]To meet the current bet use !gc call[/sub]";
                        }
                        else if (originalRaise <= 0 && (terms.Contains("raise") || terms.Contains("bet")))
                        {
                            returnString = "Failed: raise amount not found. Try putting a number to raise, such as !gc raise 500";
                        }
                        else if(!token && amountRaised + firstAmountIncreased > cp.Chips)
                        {
                            //error
                            returnString = "Failed: you do not have enough chips in your pile to raise by " + amountRaised + " and meet the current bet. Try allin. (requested " + amountRaised + ")(held " + cp.Chips + ")";
                        }
                        else
                        {
                            if(amountRaised > 0 && session.PokerData.AllowCheckRaise)
                            {
                                ResetAllPlayerActed(session);
                            }

                            string outputFromIncreasedBet = "";
                            string currentTurn = "";
                            if(token)
                            {
                                int amountRaisedShown = amountRaised - firstAmountIncreased;

                                string tokenTitle = Utils.GetFullStringOfInputs(terms);
                                tokenTitle = tokenTitle.Replace("raisetoken", "").Replace("bettoken", "").Replace("token", "").Replace(amountRaised.ToString(), "").Trim();
                                tokenTitle = Utils.SanitizeInput(tokenTitle).Trim();

                                outputFromIncreasedBet = IncreaseBet(diceBot, botMain, session, channel, currentPlayer, amountRaised, token, tokenTitle);
                                currentTurn = "[sub](and it's still their turn)[/sub]";
                                if(amountRaisedShown >= 0)
                                {
                                    bool finished = PassTurnToNextPlayer(diceBot, botMain, session, channel);
                                    currentTurn = PrintCurrentPlayerTurn(session, finished);
                                }

                                amountRaisedShown = Math.Max(amountRaisedShown, 0);

                                if(amountRaisedShown > 0)
                                {
                                    returnString = Utils.GetCharacterUserTags(character) + " has [color=yellow]raised[/color] by " + amountRaisedShown +
                                        ". The current total bet to meet is " + session.PokerData.CurrentBetLevel + " chips. " + raiseChange + "\n" +
                                        outputFromIncreasedBet + " " + (currentPlayer.AllIn ? "[color=red]all in[/color] " : "") + currentTurn;
                                }
                                else
                                {
                                    returnString = Utils.GetCharacterUserTags(character) + " has bet a [color=yellow]token[/color] worth " + amountRaised + ". They are up to " + currentPlayer.BetAmount + " chips bet." +
                                        ". The current total bet to meet is " + session.PokerData.CurrentBetLevel + " chips. " + raiseChange + "\n" +
                                        outputFromIncreasedBet + " " + (currentPlayer.AllIn ? "[color=red]all in[/color] " : "") + currentTurn;
                                }
                            }
                            else
                            {
                                outputFromIncreasedBet = IncreaseBet(diceBot, botMain, session, channel, currentPlayer, amountRaised + firstAmountIncreased);
                                bool finished = PassTurnToNextPlayer(diceBot, botMain, session, channel);
                                currentTurn = PrintCurrentPlayerTurn(session, finished);

                                returnString = Utils.GetCharacterUserTags(character) + " has [color=yellow]raised[/color] by " + amountRaised +
                                    ". The current total bet to meet is " + session.PokerData.CurrentBetLevel + " chips. " + raiseChange + "\n" +
                                    outputFromIncreasedBet + " " + (currentPlayer.AllIn ? "[color=red]all in[/color] " : "") + currentTurn;
                            }
                           
                        }
                    }
                    //nodraw / keep
                    else if (terms.Contains("nodraw") || terms.Contains("keep") || terms.Contains("nodraws") || (terms.Contains("stand") && session.PokerData.PokerBettingPhase == PokerBettingPhase.DrawPhase))
                    {
                        bool finished = PassTurnToNextPlayer(diceBot, botMain, session, channel);
                        returnString = Utils.GetCharacterUserTags(character) + " [color=gray]keeps[/color]. " + PrintCurrentPlayerTurn(session, finished);
                    }
                    //draw / redraw
                    else if (terms.Contains("draw") || terms.Contains("redraw"))
                    {
                        if(terms.Count() < 2)
                        {
                            returnString = "Failed: This command requires either a card index to redraw or 'all'.";
                        }
                        else
                        {
                            CardCommandOptions options = new CardCommandOptions(botMain.BotCommandController, terms, character); //sets all, # indexes for discards
                            int numberDiscards = 0;
                            options.redraw = true;
                            options.secretDraw = true;
                            options.deckType = DeckType.Playing;
                            options.jokers = false;

                            string outputQuestion = botMain.DiceBot.MoveCardsFromTo(options.moveCardsList, options.all, options.secretDraw, channel, options.deckType, null, options.characterDrawName, CardPileId.Hand, CardPileId.Burn, out numberDiscards);

                            string trueDraw = "";
                            string outputQuestion2 = botMain.DiceBot.DrawCards(numberDiscards, false, true, channel, DeckType.Playing, null, character, true, options.fromExtraDeckType, null, out trueDraw);// .MoveCardsFromTo(options.moveCardsList, options.all, options.secretDraw, channel, options.deckType, options.characterDrawName, CardPileId.Hand, CardPileId.Discard, out numberDiscards);

                            botMain.SendPrivateMessage("You redrew " + numberDiscards + " cards for Poker:\n" +  trueDraw, character);
                            bool finished = PassTurnToNextPlayer(botMain.DiceBot, botMain, session, channel);
                            returnString = Utils.GetCharacterUserTags(character) + " [color=cyan]draws[/color]. Discarding " + numberDiscards + " cards.\nRedrawing " + numberDiscards + " cards... " + PrintCurrentPlayerTurn(session, finished);
                        }
                    }

                }
            }
            else if (terms.Contains("currentturn") || terms.Contains("showturn"))
            {
                if (session.State != GameState.GameInProgress)
                    returnString = "Failed: This command can only be used while the game is in progress.";
                else
                    returnString = "Current turn for Poker: " + PrintCurrentPlayerTurn(session, false);
            }
            else if (terms.Contains("setsmallblind") || terms.Contains("smallblind") ||terms.Contains("bigblind") ||terms.Contains("setbigblind"))
            {
                int amount = Utils.GetNumberFromInputs(terms);
                if (amount >= 0)
                {
                    if(terms.Contains("setsmallblind") || terms.Contains("smallblind") )
                    {
                        if (amount > session.PokerData.BigBlind)
                        {
                            returnString = " (small blind cannot exceed big blind. Set to big blind amount)";
                            amount = session.PokerData.BigBlind;
                        }
                        session.PokerData.SmallBlind = amount;
                        returnString += "Set the small blind this session to " + session.PokerData.SmallBlind + " chips. (blinds: " + session.PokerData.SmallBlind + " small / " + session.PokerData.BigBlind + " big).";
                    }
                    else if (terms.Contains("setbigblind") || terms.Contains("bigblind"))
                    {
                        session.PokerData.BigBlind = amount;
                        returnString = "Set the big blind this session to " + session.PokerData.BigBlind + " chips. (blinds: " + session.PokerData.SmallBlind + " small / " + session.PokerData.BigBlind + " big).";
                    }
                }
                else
                {
                    returnString = "Failed: no positive chip amount was found to set blind.";
                }
            }
            else if (terms.Contains("setblinds") || terms.Contains("blinds") || terms.Contains("blind") || terms.Contains("setblind"))
            {
                int amount = Utils.GetNumberFromInputs(terms);
                if (amount >= 0)
                {
                    session.PokerData.BigBlind = amount;
                    session.PokerData.SmallBlind = Math.Min(amount, amount / 2);
                    returnString = "Set betting blinds for this session. (blinds: " + session.PokerData.SmallBlind + " small / " + session.PokerData.BigBlind + " big).";
                }
                else
                {
                    returnString = "Failed: no positive chip amount was found to set blinds.";
                }
            }
            else if (terms.Contains("setmaxbet"))
            {
                int amount = Utils.GetNumberFromInputs(terms);
                if (amount >= 0)
                {
                    session.PokerData.MaximumBet = amount;
                    returnString = "Set the maximum bet this session to [b]" + amount + "[/b] chips.";
                }
                else
                {
                    returnString = "Failed: no positive bet amount was found.";
                }
            }
            else if (terms.Contains("setallowtokens") || terms.Contains("setallowcheckraise"))
            {
                if (terms.Length < 2)
                {
                    returnString = "Error: improper command format. Use 'setallowtokens (on) or (off)'.";
                }
                else
                {
                    string allInputs = Utils.GetFullStringOfInputs(rawTerms);
                    string trueFalse = allInputs.Substring(allInputs.IndexOf(' ')).Trim().ToLower();

                    bool setValue = false;
                    bool successfulParse = bool.TryParse(trueFalse, out setValue);

                    if (trueFalse == "on" || trueFalse == "off")
                    {
                        successfulParse = true;
                        if (trueFalse == "on")
                        {
                            setValue = true;
                        }
                    }

                    if (successfulParse)
                    {
                        if (terms.Contains("setallowtokens"))
                        {
                            session.PokerData.AllowTokens = setValue;
                            returnString = "'Allow Token Betting' rule set to was set to " + (setValue ? "ON" : "OFF");
                        }
                        if (terms.Contains("setallowcheckraise"))
                        {
                            session.PokerData.AllowTokens = setValue;
                            returnString = "'Allow Check Raise' rule set to was set to " + (setValue ? "ON" : "OFF");
                        }
                    }
                    else
                    {
                        returnString = "Error: Input was invalid. Value must be set to on/ true, or off/ false";
                    }
                }
            }
            else if (terms.Contains("cancelhand") || terms.Contains("aborthand"))
            {
                if (session.State != GameState.GameInProgress)
                {
                    returnString = "Failed: a hand has not been started.";
                }
                else
                {
                    ReturnAllBets(diceBot, session);
                    session.PokerData.currentPlayerIndex = 0;
                    ResetAllPlayerStatus(session);
                    session.PokerData.CurrentBetLevel = 0;
                    session.PokerData.PokerPlayers = new List<PokerPlayer>();
                    diceBot.ResetDeck(false, session.PokerData.decksNumber, session.ChannelId, channelSettings.CardPrintSetting, DeckType.Playing, null);

                    session.State = GameState.Unstarted;
                    returnString = "This hand has been [color=red]cancelled[/color]. All bets have been returned and the deck was reset and shuffled. (You can start again with !startgame poker).";
                }
            }
            else if (terms.Contains("removetokens"))
            {
                if (session.State != GameState.GameInProgress)
                {
                    returnString = "Failed: a hand has not been started.";
                }
                else if (session.PokerData.CurrentPotTokenBets == null || session.PokerData.CurrentPotTokenBets.Count() == 0)
                {
                    returnString = "Failed: there are currently no token bets in the pot.";
                }
                else
                {
                    ReturnAllTokenBets(diceBot, session);

                    returnString = "All token bets have been [color=red]removed[/color]. Player bet amounts returned to normal. (Does not change whether token bets are enabled, use !setallowtokens on / off for that).";
                }
            }
            else if (terms.Contains("setrules") || terms.Contains("setmode") || terms.Contains("changerules") || terms.Contains("changemode"))
            {
                string handSize = "(5 card hand size)";
                int newHandSize = 5;
                PokerRuleset newRuleset = PokerRuleset.Unset;
                if (terms.Contains("5carddraw") || terms.Contains("draw") || terms.Contains("fivecarddraw"))
                    newRuleset = PokerRuleset.FiveCardDraw;
                if (terms.Contains("5cardstud") || terms.Contains("stud") || terms.Contains("fivecardstud"))
                    newRuleset = PokerRuleset.FiveCardStud;
                if (terms.Contains("texasholdem") || terms.Contains("holdem") || terms.Contains("texas"))
                {
                    newRuleset = PokerRuleset.TexasHoldem;
                    handSize = "(2 card hand size)";
                    newHandSize = 2;
                }
                if (terms.Contains("dicebotholdem") || terms.Contains("dbholdem") || terms.Contains("dicebot"))
                {
                    newRuleset = PokerRuleset.DBHoldem;
                    newHandSize = 3;
                    handSize = "(3 card hands)";
                }
                bool nochange = newRuleset == PokerRuleset.Unset;

                if(nochange)
                {
                    foreach (string s in terms)
                    {
                        if (s.EndsWith("cardstud"))
                        {
                            string mod = s.Replace("cardstud", "").Replace("-", "");
                            newRuleset = PokerRuleset.NCardStud;
                            newHandSize = ParseNewHandSize(mod, out handSize);
                        }
                        if (s.EndsWith("carddraw"))
                        {
                            string mod = s.Replace("carddraw", "").Replace("-", "");
                            newRuleset = PokerRuleset.NCardDraw;
                            newHandSize = ParseNewHandSize(mod, out handSize);
                        }
                    }
                    nochange = newRuleset == PokerRuleset.Unset;
                }

                if (nochange)
                {
                    returnString = "Failed: no rules found to change to.";
                }
                else if(session.State != GameState.Unstarted && session.State != GameState.Finished)
                {
                    returnString = "Failed: cannot change game rules of a game in progress.";
                }
                else
                {
                    session.PokerData.Rules = newRuleset;
                    session.PokerData.MaxHandSize = newHandSize;
                    returnString = "Updated Poker game rules to " + session.PokerData.Rules + ". " + handSize;
                }
            }
            else { returnString += "Failed: No such command exists"; }

            return returnString;
        }
    }

    public class PokerData
    {
        public bool RulesSet;
        public int decksNumber = 1; //default to 1 deck
        public PokerRuleset Rules;
        public PrintSetting CardPrintSetting = null;
        public int MaxHandSize = 5;

        public int CurrentPotTotal = 0;
        public List<TokenBet> CurrentPotTokenBets = new List<TokenBet>();
        public bool AllowTokens = false;
        public bool AllowCheckRaise = true;

        public int CurrentBetLevel = 0;

        public int MaximumBet = -1;

        public List<BlackjackBet> PotBetAmounts = new List<BlackjackBet>();
        public List<PokerPlayer> PokerPlayers = new List<PokerPlayer>();

        public List<string> CurrentTurnOrder = new List<string>();

        public int currentPlayerIndex = -1;

        public int SmallBlind;
        public int BigBlind;

        public List<string> ForceStandVotes = new List<string>();

        public PokerBettingPhase PokerBettingPhase;

        public int GetPotTotalChipsNotTokens()
        {
            int maxTotal = CurrentPotTotal;
            if (CurrentPotTokenBets != null && CurrentPotTokenBets.Count() > 0)
            {
                int tokenChipsAmount = CurrentPotTokenBets.Sum(a => a.TokenAmount);
                maxTotal -= tokenChipsAmount;
                maxTotal = Math.Max(maxTotal, 0);
            }
            return maxTotal;
        }

        public PokerPlayer GetCurrentPlayer()
        {
            if(PokerPlayers == null || currentPlayerIndex < 0 || currentPlayerIndex > PokerPlayers.Count - 1 )
                return null;

            return PokerPlayers.ElementAt(currentPlayerIndex);
        }

        public string GetPotTokensString()
        {
            string returnString = "";
            if(CurrentPotTokenBets != null)
            {
                foreach(var bet in CurrentPotTokenBets)
                {
                    if (!string.IsNullOrEmpty(returnString))
                        returnString += ", ";

                    returnString += bet.Print();
                }
            }
            return returnString;
        }

        public void PassTurnToFirstActivePlayer()
        {
            var player = GetCurrentPlayer();
            int skips = 0;
            while ((player.Folded || player.AllIn || player.CannotAfford) && skips < PokerPlayers.Count())
            {
                skips += 1;
                currentPlayerIndex += 1;
                if (currentPlayerIndex >= PokerPlayers.Count())
                    currentPlayerIndex = 0;
                player = GetCurrentPlayer();
            }
        }

        public PokerPlayer GetPlayer(string characterName)
        {
            if(PokerPlayers == null || PokerPlayers.Count == 0)
                return null;
            return PokerPlayers.FirstOrDefault(a => a.PlayerName == characterName);
        }

        public string PrintPlayers()
        {
            string rtn = "";

            if(PokerPlayers != null && PokerPlayers.Count >= 1)
            {
                bool smallBlind = true;
                bool bigBlind = false;
                foreach(PokerPlayer p in PokerPlayers)
                {
                    if (!string.IsNullOrEmpty(rtn))
                        rtn += "\n";
                    if (p.CannotAfford)
                    {
                        rtn += p.Print(CardPrintSetting);
                        continue;
                    }

                    if(smallBlind)
                    {
                        rtn += p.Print(CardPrintSetting);
                        if (SmallBlind > 0)
                        {
                             rtn += " (small blind)";
                        }
                        smallBlind = false;
                        bigBlind = true;
                    }
                    else if (bigBlind && BigBlind > 0)
                    {

                        rtn += p.Print(CardPrintSetting) + " (big blind)";
                        bigBlind = false;
                    }
                    else
                    {
                        rtn += p.Print(CardPrintSetting);
                    }
                } 
            }
            return rtn;
        }

        public void ShufflePlayers(System.Random r)
        {
            if(PokerPlayers != null && PokerPlayers.Count > 1)
            {
                List<PokerPlayer> tempList = new List<PokerPlayer>();
                tempList.AddRange(PokerPlayers);
                PokerPlayers = new List<PokerPlayer>();

                while(tempList.Count > 0)
                {
                    int randomIndex = r.Next(tempList.Count);
                    PokerPlayers.Add(tempList.ElementAt(randomIndex));
                    tempList.RemoveAt(randomIndex);
                }
            }
        }

        public void ShiftPlayersLeft()
        {
            //shift the turn order list of names and then make the unordered list of poker players match it
            if (CurrentTurnOrder != null && CurrentTurnOrder.Count > 1)
            {
                List<string> tempList = new List<string>();
                tempList.AddRange(CurrentTurnOrder);
                tempList.RemoveAt(0);
                tempList.Add(CurrentTurnOrder[0]);
                CurrentTurnOrder = new List<string>();

                foreach (string p in tempList)
                {
                    CurrentTurnOrder.Add(p);
                }
            }
            if (PokerPlayers != null && PokerPlayers.Count == CurrentTurnOrder.Count)
            {
                List<PokerPlayer> tempList = new List<PokerPlayer>();
                for(int i = 0; i < CurrentTurnOrder.Count; i++)
                {
                    PokerPlayer pokerPlayer = PokerPlayers.FirstOrDefault(a => a.PlayerName == CurrentTurnOrder[i]);
                    tempList.Add(pokerPlayer);
                }

                PokerPlayers = new List<PokerPlayer>();

                foreach (PokerPlayer p in tempList)
                {
                    PokerPlayers.Add(p);
                }
            }
        }

        public string PrintCurrentPlayerTurn()
        {
            string outputString = "";
            if (PokerPlayers == null || PokerPlayers.Count == 0)
                return "(no players found)";

            if (currentPlayerIndex >= PokerPlayers.Count)
                outputString += "it is now the dealer's turn.";
            else
                outputString += "it is now " + Utils.GetCharacterUserTags(GetCurrentPlayer().PlayerName) + "'s turn";

            return outputString;
        }
    }

    public class PokerHandResult
    {
        public long score;

        public bool folded;

        public PokerHandType HandType;

        public string Result;

        public override string ToString()
        {
            return HandType.ToString() + " :: score = " + score;
        }
    }

    public enum PokerHandType
    {
        HighCard,
        OnePair,
        TwoPair, //3
        ThreeOfAKind,
        Straight,
        Flush, //6
        FullHouse,
        FourOfAkind,
        StraightFlush, //9
        RoyalFlush
    }

    public class PokerPlayer
    {
        public int BetAmount;
        public int ActualChipsAmountBet; //for resolving all-ins, not tokens

        public Hand PlayerHand;

        public string PlayerName;

        public bool Folded;
        public bool AllIn;

        public bool HasActedThisRound;

        public bool Active;
        public PokerHandResult HandEvaluation;
        public bool CannotAfford;

        public void ResetStatusHandPhase()
        {
            HasActedThisRound = false;
        }

        public void ResetStatusFull()
        {
            PlayerHand = null;
            Folded = false;
            AllIn = false;
            HasActedThisRound = false;
            Active = true;
            HandEvaluation = null;
            CannotAfford = false;
            BetAmount = 0;
            ActualChipsAmountBet = 0;
        }

        public static string GetHandString(Hand hand, PrintSetting cardPrintSetting)
        {
            if (hand != null)
            {
                string handString = hand.Print(true, cardPrintSetting);

                return handString;
            }
            else
                return "(cards not drawn)";
        }

        public string Print(PrintSetting cardPrintSetting)
        {
            
            string betAmount = "has bet: [b]" + ActualChipsAmountBet + "[/b] chips" + (AllIn? " [color=red](All-In)[/color]":"");
            if (CannotAfford)
            {
                betAmount = "(could not afford the ante and has not joined)";
            }

            return Utils.GetCharacterUserTags(PlayerName) + " " + betAmount + ", dealt" + GetHandString(PlayerHand, cardPrintSetting) + "." + (Folded? "(folded)":"") + (Active ? "" : " (inactive)");
        }
    }

    public class TokenBet
    {
        public string TokenName;
        public int TokenAmount;
        public string CharacterName;

        public string Print()
        {
            string tokenName = string.IsNullOrEmpty(TokenName) ? "Token" : TokenName;
            return "(" + CharacterName + "'s " + tokenName + ": " + TokenAmount + ")";
        }
    }

    public class PokerBetNonsense
    {
        public int MaxPayout;
        public int AmountPaid;
        public string CharacterName;
        public PokerHandResult HandEvaluation;
        public bool Folded;

    }

    public enum PokerRuleset
    {
        FiveCardStud,
        FiveCardDraw,
        TexasHoldem,
        DBHoldem, //3 cards, show burns
        NCardStud,
        NCardDraw,
        Unset
    }

    public enum PokerBettingPhase
    {
        InitialHandBets,
        DrawPhase,
        BettingPhase2,
        BettingPhase3,
        BettingPhase4,
        BettingPhase5,
        FinishedBetting
    }
}
