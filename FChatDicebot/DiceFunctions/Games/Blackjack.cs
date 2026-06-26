using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class Blackjack : IGame
    {
        private const int RoundEndingWaitMsPerPlayer = 1000;
        private const int RoundEndingWaitMs = 1000 * 2;

        public string GetGameName()
        {
            return "Blackjack";
        }

        public int GetMaxPlayers()
        {
            return 10;
        }

        public int GetMinPlayers()
        {
            return 1;
        }

        public bool AllowAnte()
        {
            return true;
        }

        public bool UsesFlatAnte()
        {
            return false;
        }

        public bool KeepSessionDefault()
        {
            return true;
        }

        public int GetMinimumMsBetweenGames()
        {
            return 60000;
        }

        public string GetGameHelp()
        {
            string thisGameCommands = "setante #, currentturn, bet #, changebet #, setminbet #, setmaxbet #, setpushreturnschips (on / off)\n" +
                "(as current player only): hit, stand, doubledown";
            string thisGameStartupOptions = "# (sets your personal bet amount), minbet:# (sets minimum bet amount), x1/x2/x3/x4 (sets deck count), maxbet:# (set maximum bet amount), pushcarrieschips (sets pushreturnships to 'off')" +
                "\nThe default rules are: 2x deck used, no ante, no minbet, no maxbet";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, true, false);
        }

        public string GetStartingDisplay()
        {
            return TextFormat.Emoji("dbblackjack1") + TextFormat.Emoji("dbblackjack2");
        }

        public string GetEndingDisplay()
        {
            return "";
        }

        public string GameStatus(GameSession session)
        {
            if(session.BlackjackGameData != null)
            {
                string maxBet = session.BlackjackGameData.maxBet >= 0 ? session.BlackjackGameData.maxBet + "" : "none";

                string pushchips = "returns chips";
                if (!session.BlackjackGameData.PushReturnsBets)
                {
                    pushchips = "carries chips";
                }

                string output = "Rules: " + session.BlackjackGameData.decksNumber + " decks used, min bet: " + session.BlackjackGameData.minBet + ", max bet: " + maxBet + ", push " + pushchips;

                if (session.BlackjackGameData.BlackjackBets != null && session.BlackjackGameData.BlackjackBets.Count > 0)
                {
                    if (output != null)
                        output += "\n";
                    output += "Current Bets: " + session.BlackjackGameData.PrintBets();
                }
                if (session.BlackjackGameData.BlackjackPlayers != null && session.BlackjackGameData.BlackjackPlayers.Count > 0)
                {
                    if (output != null)
                        output += "\n";
                    output += "Current Hands: " + session.BlackjackGameData.PrintPlayers();

                    output += "\n" + session.BlackjackGameData.PrintCurrentPlayerTurn();
                }
                
                return output;
            }
            else
                return "(players and bets not found)";
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            string outputString = "";
            if(!session.BlackjackGameData.RulesSet && terms != null && terms.Count() > 0)
            {
                int deckNumber = 2;
                if (terms.Contains("x1") || terms.Contains("single"))
                    deckNumber = 1;
                if (terms.Contains("x2") || terms.Contains("double"))
                    deckNumber = 2;
                if (terms.Contains("x3") || terms.Contains("triple"))
                    deckNumber = 3;
                if (terms.Contains("x4") || terms.Contains("quadrouple"))
                    deckNumber = 4;
                string maxBet = "";

                foreach (string s in terms)
                {
                    if (s.Contains("minbet:"))
                    {
                        string g = s.Replace("minbet:", "");
                        int minBetResult = 0;
                        int.TryParse(g, out minBetResult);

                        session.BlackjackGameData.minBet = minBetResult;
                    }
                    else if (s.StartsWith("maxbet:"))
                    {
                        string mod = s.Replace("maxbet:", "");
                        int newMaxBet = -1;
                        int.TryParse(mod, out newMaxBet);
                        if (newMaxBet < ante)
                        {
                            maxBet = "(maximum bet cannot be less than ante: set to none)";
                        }
                        else if (newMaxBet >= 0)
                        {
                            maxBet = "(max bet: " + newMaxBet + ")";
                            session.BlackjackGameData.maxBet = newMaxBet;
                        }
                    }
                }

                string pushchips = " (push returns chips)";
                if (terms.Contains("pushcarrieschips"))
                {
                    session.BlackjackGameData.PushReturnsBets = false;
                    pushchips = " (push carries chips)";
                }

                session.BlackjackGameData.RulesSet = true;
                session.BlackjackGameData.decksNumber = deckNumber;

                outputString += "Rules set: (minbet: " + session.BlackjackGameData.minBet + ") (decks: " + deckNumber + ") " + maxBet + pushchips + "\n";
            }

            if(session.BlackjackGameData.minBet > ante)
            {
                ante = session.BlackjackGameData.minBet;
            }

            BlackjackBet thisCharacterBet = new BlackjackBet()
            {
                betAmount = ante,
                characterName = characterName
            };
            session.BlackjackGameData.BlackjackBets.Add(thisCharacterBet);

            messageString = outputString + thisCharacterBet.ToString();
            return true;
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            string outputString = "[i]Setting up Blackjack...[/i]";
            SavedData.ChannelSettings channelSettings = botMain.GetChannelSettings(session.GetMessageAddress());
            session.BlackjackGameData.CardPrintSetting = channelSettings.CardPrintSetting;

            bool resetDeck = false;

            if (session.BlackjackGameData.BlackjackPlayers != null && session.BlackjackGameData.BlackjackPlayers.Count > 0)
            {
                Deck d = diceBot.GetDeck(session.GetMessageAddress(), DeckType.Playing, null);
                if(d.GetCardsRemaining() < session.BlackjackGameData.BlackjackBets.Count * 5)
                    resetDeck = true;
            }
            else
            {
                    resetDeck = true;
            }
            if(resetDeck)
                diceBot.ResetDeck(false, session.BlackjackGameData.decksNumber, session.GetMessageAddress(), channelSettings.CardPrintSetting, DeckType.Playing, null);

            //handle pushes from last round
            List<BlackjackBet> addedPush = new List<BlackjackBet>();
            if(session.BlackjackGameData.BlackjackPlayers != null && session.BlackjackGameData.BlackjackPlayers.Count > 0)
            {
                //old round data could include pushed bets
                List<BlackjackPlayer> pushPlayers = session.BlackjackGameData.BlackjackPlayers.Where(b => b.PushAmount > 0).ToList();
                if(pushPlayers != null && pushPlayers.Count() > 0)
                {
                    foreach(BlackjackPlayer p in pushPlayers)
                    {
                        addedPush.Add(new BlackjackBet() { betAmount = p.PushAmount, characterName = p.PlayerName });
                    }
                }
            }

            session.BlackjackGameData.BlackjackPlayers = new List<BlackjackPlayer>();
            //set up starting data 
            foreach(BlackjackBet blackjackBet in session.BlackjackGameData.BlackjackBets)
            {
                int pushAmount = 0;
                BlackjackBet pushedBet = addedPush.FirstOrDefault(a => a.characterName == blackjackBet.characterName);
                if (pushedBet != null)
                    pushAmount += pushedBet.betAmount;

                session.BlackjackGameData.BlackjackPlayers.Add(new BlackjackPlayer()
                {
                    Active = true,
                    BetAmount = blackjackBet.betAmount + pushAmount,
                    Busted = false,
                    PlayerHand = null,
                    PlayerName = blackjackBet.characterName
                });
            }
            session.BlackjackGameData.ShufflePlayers(diceBot.random);
            
            //put in all antes
            foreach(BlackjackPlayer bet in session.BlackjackGameData.BlackjackPlayers)
            {
                ChipPile pile = diceBot.GetChipPile(new MessageAddress(session.GetMessageAddress(), bet.PlayerName), false);

                if(pile.Chips >= bet.BetAmount)
                {
                    bet.CannotAfford = false;
                    diceBot.BetChips(new MessageAddress(session.GetMessageAddress(), bet.PlayerName), bet.BetAmount, false);
                }
                else
                {
                    bet.CannotAfford = true;
                }
            }
            //claim pot with dealer
            diceBot.ClaimPot(new MessageAddress(session.GetMessageAddress(), DiceBot.DealerPlayerAlias), 1);//false, false);
            botMain.BotCommandController.SaveChipsToDisk("Blackjack");

            //draw all hands incl. dealer
            //string drawOut = "";
            DrawCardResult drawResult = new DrawCardResult();
            foreach(BlackjackPlayer bet in session.BlackjackGameData.BlackjackPlayers)
            {
                if(!bet.CannotAfford)
                {
                    MessageAddress playerAddress = new MessageAddress(session.GetMessageAddress(), bet.PlayerName);
                    drawResult = diceBot.DrawCards(2, false, true, DeckType.Playing, null, new MessageAddress(session.GetMessageAddress(), bet.PlayerName), false, DeckType.NONE, null);//, out drawOut);
                    Hand h2 = diceBot.GetHand(DeckType.Playing, null, new MessageAddress(session.GetMessageAddress(), bet.PlayerName), null);
                    bet.PlayerHand = h2;
                    bet.HiddenCard = h2.GetCardAtIndex(0);

                    botMain.SendPrivateMessage("Blackjack hand drawn: " + h2.Print(false, channelSettings.CardPrintSetting, false), new MessageAddress(session.GetMessageAddress(), bet.PlayerName));
                }
            }

            drawResult = diceBot.DrawCards(2, false, true, DeckType.Playing, null, new MessageAddress(session.GetMessageAddress(), DiceBot.DealerPlayerAlias), false, DeckType.NONE, null);
            Hand hdealer = diceBot.GetHand(DeckType.Playing, null, new MessageAddress(session.GetMessageAddress(), DiceBot.DealerPlayerAlias), null);

            //guarantee dealer gets 2card blackjack for testing
            //hdealer.Reset();
            //hdealer.AddCard(new DeckCard() { number = 1, suit = 1 }, diceBot.random);
            //hdealer.AddCard(new DeckCard() { number = 12, suit = 1 }, diceBot.random);

            session.BlackjackGameData.DealerPlayer = new BlackjackPlayer()
            {
                Active = true,
                BetAmount = 0,
                Busted = false,
                PlayerHand = hdealer,
                PlayerName = DiceBot.DealerPlayerAlias,
                HiddenCard = hdealer.GetCardAtIndex(0)
            };

            //track dealer hidden card like a player
            //track player hidden card from drawing it on deal
            string dealerStuff = "Dealer cards: " + BlackjackPlayer.GetHandString(hdealer, session.BlackjackGameData.DealerPlayer.HiddenCard, channelSettings.CardPrintSetting) + "\n";
            string playersPrint = dealerStuff + session.BlackjackGameData.PrintPlayers();

            session.BlackjackGameData.BlackjackPlayers.RemoveAll(ab => ab.CannotAfford);

            if(session.BlackjackGameData.BlackjackPlayers.Count == 0)
                return outputString + "\n" + playersPrint + "\n[b]all players were removed for failing to meet ante[/b].";

            session.BlackjackGameData.currentPlayerIndex = 0;
            session.State = DiceFunctions.GameState.GameInProgress;

            HandResult initialDealerHandResult = EvaluateBlackjackHand(hdealer);
            if (initialDealerHandResult.blackjackBonus)
            {
                //end hand early, all players lose but some might tie if they have blackjack
                //string currentTurn = session.BlackjackGameData.PrintCurrentPlayerTurn();

                FinishRound(diceBot, botMain, session);

                return outputString + "\n" + playersPrint + "\n" + "The rounded ended early! [i]The dealer drew a two-card blackjack.[/i] [sub]player turns skipped.[/sub]";
            }
            else
            {
                string currentTurn = session.BlackjackGameData.PrintCurrentPlayerTurn();

                return outputString + "\n" + playersPrint + "\n" + currentTurn + " [sub]!gc hit, !gc stand, !gc doubledown[/sub]";
            }

        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {

        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            string outputstring = "Goodbye " + TextFormat.GetCharacterUserTags( characterName);

            BlackjackBet bet = session.BlackjackGameData.BlackjackBets.FirstOrDefault(q => q.characterName == characterName);
            session.BlackjackGameData.BlackjackBets.Remove(bet);

            if (session.BlackjackGameData.BlackjackPlayers.Count(c => c.PlayerName == characterName) > 0)
            {
                BlackjackPlayer thisPlayer = session.BlackjackGameData.BlackjackPlayers.FirstOrDefault(d => d.PlayerName == characterName);

                int indexOfPlayer = session.BlackjackGameData.BlackjackPlayers.IndexOf(thisPlayer);

                if(thisPlayer.PushAmount > 0)
                {
                    botMain.DiceBot.AddChips(new MessageAddress(session.GetMessageAddress(), characterName), thisPlayer.PushAmount, false);
                    outputstring += ", your carried push amount of " + thisPlayer.PushAmount + " was returned.";
                    botMain.BotCommandController.SaveChipsToDisk("BlackjackPlayerLeft");
                }

                session.BlackjackGameData.BlackjackPlayers.Remove(thisPlayer);

                if (indexOfPlayer < session.BlackjackGameData.currentPlayerIndex)
                {
                    session.BlackjackGameData.currentPlayerIndex -= 1;
                }
                else if (indexOfPlayer == session.BlackjackGameData.currentPlayerIndex)
                {
                    session.BlackjackGameData.currentPlayerIndex -= 1;
                    PassTurnToNextPlayer(botMain.DiceBot, botMain, session);

                    outputstring += ", " + session.BlackjackGameData.PrintCurrentPlayerTurn();
                }
            }

            return outputstring;
        }

        public HandResult EvaluateBlackjackHand(Hand h)
        {
            HandResult result = new HandResult();

            if (h != null && h.CardsCount() >= 1)
            {
                for (int i = 0; i < h.CardsCount(); i++)
                {
                    DeckCard d = h.GetCardAtIndex(i);

                    if (d.number > 1 && d.number < 11)
                    {
                        result.score += d.number;
                    }
                    else if (d.number > 10)
                        result.score += 10;
                    else if (d.number == 1)
                    {
                        result.containsAce = true;
                        result.score += 1;
                    }

                    if (result.score > 21)
                    {
                        result.busted = true;
                    }
                }
            }

            if (result.score < 12 && result.containsAce)
                result.score += 10;
                
            if (result.score == 21 && result.containsAce && h.CardsCount() == 2)
                result.blackjackBonus = true;

            return result;
        }

        public void PassTurnToNextPlayer(DiceBot diceBot, BotMain botMain, GameSession session)
        {
            BlackjackGameData data = session.BlackjackGameData;

            if(data != null && data.BlackjackPlayers != null)
            {
                data.currentPlayerIndex += 1;
                data.ForceStandVotes = new List<String>();
                if (data.currentPlayerIndex == data.BlackjackPlayers.Count)
                {
                    TakeDealerTurn(diceBot, botMain, session);
                    session.BlackjackGameData.currentPlayerIndex += 1;
                }
                if( data.currentPlayerIndex > data.BlackjackPlayers.Count)
                {
                    FinishRound(diceBot, botMain, session);
                }
            }
        }

        public void TakeDealerTurn(DiceBot diceBot, BotMain botMain, GameSession session)
        {
            string dealerTurnOutput = "";

            MessageAddress dealerAddress = new MessageAddress(session.GetMessageAddress(), DiceBot.DealerPlayerAlias);
            Hand dealerHand = session.BlackjackGameData.DealerPlayer.PlayerHand;// diceBot.GetHand(DeckType.Playing, null, dealerAddress, null);

            if(dealerHand == null)
                dealerTurnOutput = "ERROR: dealer hand not found";
            else
            {             
                HandResult lastHandResult = EvaluateBlackjackHand(dealerHand);
                while(lastHandResult.score < 17)
                {
                    if (!string.IsNullOrEmpty(dealerTurnOutput))
                        dealerTurnOutput += ", ";
                    string junkDraw = "";
                    lastHandResult = Hit(diceBot, dealerAddress, session.BlackjackGameData.CardPrintSetting, out junkDraw);
                    dealerTurnOutput += "\n" + junkDraw;// "hit for " + junkDraw;
                }

                //if (!string.IsNullOrEmpty(dealerTurnOutput))
                //    dealerTurnOutput += ", ";
                dealerTurnOutput += ": ";

                if (lastHandResult.busted)
                    dealerTurnOutput += "[color=red]BUSTED[/color].";
                else
                    dealerTurnOutput += "stand.";
            }

            botMain.SendFutureMessage("The dealer is taking her turn: " + dealerTurnOutput, session.GetMessageAddress(), true, RoundEndingWaitMs);
        }

        public void FinishRound(DiceBot diceBot, BotMain botMain, GameSession session)
        {
            string outputString = "";
            SavedData.ChannelSettings channelSettings = botMain.GetChannelSettings(session.GetMessageAddress());

            if (session.BlackjackGameData.BlackjackPlayers != null)
            {
                Hand dealerHand = session.BlackjackGameData.DealerPlayer.PlayerHand;//  diceBot.GetHand(DeckType.Playing, null, new MessageAddress(session.GetMessageAddress(), DiceBot.DealerPlayerAlias), null);
                HandResult dealerResult = EvaluateBlackjackHand(dealerHand);

                //check who beat the dealer
                foreach (BlackjackPlayer bjp in session.BlackjackGameData.BlackjackPlayers)
                {
                    HandResult res = EvaluateBlackjackHand(bjp.PlayerHand);

                    if(res.busted)
                    {
                        bjp.LastRoundResult = LastRoundResult.Lost;
                    }
                    else if(dealerResult.busted)
                    {
                        if (res.blackjackBonus)
                            bjp.LastRoundResult = LastRoundResult.WonWithBonus;
                        else
                            bjp.LastRoundResult = LastRoundResult.Won;
                    }
                    else if (res.score > dealerResult.score)
                    {
                        if (res.blackjackBonus)
                            bjp.LastRoundResult = LastRoundResult.WonWithBonus;
                        else
                            bjp.LastRoundResult = LastRoundResult.Won;
                    }
                    else if (res.score < dealerResult.score)
                    {
                        bjp.LastRoundResult = LastRoundResult.Lost;
                    }
                    else if(res.score == dealerResult.score)
                    {
                        if (dealerResult.blackjackBonus && !res.blackjackBonus)
                            bjp.LastRoundResult = LastRoundResult.Lost;
                        else if (!dealerResult.blackjackBonus && res.blackjackBonus)
                            bjp.LastRoundResult = LastRoundResult.WonWithBonus;
                        else
                            bjp.LastRoundResult = LastRoundResult.Push;
                    }
                }

                //award prizes if any
                foreach (BlackjackPlayer bjp in session.BlackjackGameData.BlackjackPlayers)
                {
                    if(!string.IsNullOrEmpty(outputString))
                        outputString += "\n";

                    outputString += TextFormat.GetCharacterUserTags(bjp.PlayerName);
                    MessageAddress playerAddress = new MessageAddress(session.GetMessageAddress(), bjp.PlayerName);
                    switch(bjp.LastRoundResult)
                    {
                        case LastRoundResult.Forfeit:
                            outputString += " forfeit this round.";
                            break;
                        case LastRoundResult.NONE:
                            outputString += " (error: result not found)";
                            break;
                        case LastRoundResult.Lost:
                            outputString += " lost this round.";
                            break;
                        case LastRoundResult.Won:
                            int award = bjp.BetAmount * 2;
                            diceBot.AddChips(playerAddress, award, false);
                            outputString += " [b]WON[/b] this round. [color=green]" + award + " " + BotMain.CurrencyPlaceholder + "s[/color] awarded.";
                            break;
                        case LastRoundResult.WonWithBonus:
                            int award2 = (bjp.BetAmount * 5) / 2; //pays 3:2
                            diceBot.AddChips(playerAddress, award2, false);
                            outputString += " [b]WON WITH BLACKJACK[/b] this round. [color=green]" + award2 + " " + BotMain.CurrencyPlaceholder + "s[/color] awarded.";
                            break;
                        case LastRoundResult.Push:
                            if(session.BlackjackGameData.PushReturnsBets)
                            {
                                int award3 = bjp.BetAmount; //pays 1:1
                                diceBot.AddChips(playerAddress, award3, false);
                                outputString += " [b]pushed[/b] this round. [color=green]" + award3 + " " + BotMain.CurrencyPlaceholder + "s[/color] awarded.";
                            }
                            else
                            {
                                //record pushes
                                outputString += " [b]pushed[/b] this round. [sub]Their bet of " + bjp.BetAmount + " will be added next round.[/sub]";
                                bjp.PushAmount = bjp.BetAmount;
                            }
                            break;
                    }

                    HandResult res = EvaluateBlackjackHand(bjp.PlayerHand);
                    outputString += " " + (bjp.PlayerHand != null ? bjp.PlayerHand.Print(false, channelSettings.CardPrintSetting, false) : "(cards not found)") + " = " + res.ToString();

                    if (bjp.DoubledDown)
                    {
                        bjp.DoubledDown = false;
                        bjp.BetAmount = bjp.BetAmount / 2;
                    }
                }

                HandResult res2 = EvaluateBlackjackHand(dealerHand);
                outputString = "The dealer's hand: " + (dealerHand != null ? dealerHand.Print(false, channelSettings.CardPrintSetting, false) : "(dealer cards not found)") + " = " + res2.ToString() + "\n" + outputString;

                diceBot.EndHand(session.GetMessageAddress(), false, channelSettings.CardPrintSetting, DeckType.Playing, null);
                //save chips modifications to disk
                botMain.BotCommandController.SaveChipsToDisk("BlackjackFinishRound");
            }

            //print out all character results
            botMain.SendFutureMessage("[color=yellow]Round Finished![/color]\n" + outputString, session.GetMessageAddress(), true, RoundEndingWaitMs * 2);
            session.State = GameState.Finished;
        }

        public HandResult Hit(DiceBot diceBot, MessageAddress address, PrintSetting printSetting, out string truedraw)
        {
            DrawCardResult drawCardResult = diceBot.DrawCards(1, false, true, DeckType.Playing, null, address, false, DeckType.NONE, null);
            truedraw = drawCardResult.TrueDraw;
            Hand h = diceBot.GetHand(DeckType.Playing, null, address, null);
            HandResult HandResult = EvaluateBlackjackHand(h);

            DeckCard drawnCard = drawCardResult.Cards[0];// h.GetCardAtIndex(h.CardsCount() - 1);
            truedraw = Utils.GetCharacterStringFromSpecialName(address.character) + " hit and drew a " + drawnCard.Print(printSetting);
            return HandResult;
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, MessageAddress address, GameSession session, string[] terms, string[] rawTerms)
        {
            string returnString = "";

            BlackjackPlayer currentPlayer = session.BlackjackGameData.GetCurrentPlayer();
            bool currentPlayerIssuedCommand = currentPlayer != null && currentPlayer.PlayerName == address.character;
            SavedData.ChannelSettings channelSettings = botMain.GetChannelSettings(address);

            if(terms.Contains("forcestand"))
            {
                var bp = session.BlackjackGameData.GetPlayer(address.character);

                if (session.State != GameState.GameInProgress)
                    returnString = "Failed: This command can only be used while the game is in progress.";
                else if (session.BlackjackGameData.ForceStandVotes.Contains(address.character))
                    returnString = "Failed: Each player in the game only has one vote to force stand.";
                else
                {
                    bool characterIsAdmin = Utils.IsCharacterTrusted(botMain.AccountSettings.TrustedCharacters, address)
                        || Utils.IsCharacterAdmin(botMain.AccountSettings.AdminCharacters, address.character);
                    if (characterIsAdmin)
                    {
                        session.BlackjackGameData.ForceStandVotes.Add(address.character);
                        session.BlackjackGameData.ForceStandVotes.Add(address.character);
                        session.BlackjackGameData.ForceStandVotes.Add(address.character);
                        session.BlackjackGameData.ForceStandVotes.Add(address.character);
                    }
                    session.BlackjackGameData.ForceStandVotes.Add(address.character);

                    int currentVotes = session.BlackjackGameData.ForceStandVotes.Count();
                    int requiredVotes = Math.Max(2, session.BlackjackGameData.BlackjackPlayers.Count() / 2);

                    if (currentVotes >= requiredVotes)
                    {
                        string thisCharacter = session.BlackjackGameData.GetCurrentPlayer().PlayerName;
                        PassTurnToNextPlayer(diceBot, botMain, session);

                        returnString = TextFormat.GetCharacterUserTags(thisCharacter) + " stands. " + session.BlackjackGameData.PrintCurrentPlayerTurn();
                    }
                    else
                        returnString = TextFormat.GetCharacterUserTags(address.character) + " has voted to force stand: " + currentVotes + " / " + (requiredVotes) + " votes.";
                }
            }
            else if (terms.Contains("hit") || terms.Contains("stand") || terms.Contains("doubledown") || terms.Contains("fold"))
            {
                if (session.State != GameState.GameInProgress)
                    returnString = "Failed: This command can only be used while the game is in progress.";
                else if (currentPlayer == null)
                    returnString = "Error: Current player not found.";
                else if (!currentPlayerIssuedCommand)
                    returnString = "Failed: Only the current player can access this command.";
                else
                {
                    //fold / surrender?

                    //stand
                    if(terms.Contains("stand"))
                    {
                        PassTurnToNextPlayer(diceBot, botMain, session);
                        returnString = TextFormat.GetCharacterUserTags(address.character) + " stands. " + session.BlackjackGameData.PrintCurrentPlayerTurn();
                    }

                    //hit
                    if(terms.Contains("hit") || terms.Contains("doubledown"))
                    {
                        bool doubledown = false;
                        string betString = "";
                        if (terms.Contains("doubledown"))
                        {
                            doubledown = true;
                            //double the bet
                            ChipPile pile = diceBot.GetChipPile(address, false);

                            if(currentPlayer.PlayerHand.CardsCount() > 2)
                            {
                                return "Failed: You cannot double down after already drawing another card with hit.";
                            }
                            else if(pile.Chips >= currentPlayer.BetAmount)
                            {
                                diceBot.GiveChips(address, DiceBot.DealerPlayerAlias, currentPlayer.BetAmount, false);
                                BlackjackBet bet = session.BlackjackGameData.BlackjackBets.FirstOrDefault(a => a.characterName == address.character);
                                //bet.betAmount = currentPlayer.BetAmount * 2; (leave this data the same, it persists between rounds)
                                betString += "Added " + currentPlayer.BetAmount + " more " + BotMain.CurrencyPlaceholder + "s to the bet. ";
                                currentPlayer.BetAmount = currentPlayer.BetAmount * 2;
                                currentPlayer.DoubledDown = true;

                                botMain.BotCommandController.SaveChipsToDisk("BlackjackGameCommand");
                            }
                            else
                            {
                                return "Failed: You cannot afford to double down, you do not have enough " + BotMain.CurrencyPlaceholder + "s remaining.";
                            }
                        }

                        string drawString = "";

                        var result = Hit(diceBot, address, channelSettings.CardPrintSetting, out drawString);

                        Hand h2 = diceBot.GetHand(DeckType.Playing, null, address, null);

                        string handInformation = "\nBlackjack hand: " + h2.Print(false, channelSettings.CardPrintSetting, false);

                        if(result.busted)
                        {
                            PassTurnToNextPlayer(diceBot, botMain, session);
                            drawString += handInformation + " = [color=red]BUSTED[/color] at " + result.score;
                            drawString += "\n" + session.BlackjackGameData.PrintCurrentPlayerTurn();
                        }
                        else
                        {
                            botMain.SendPrivateMessage(drawString + handInformation + " = " + result.score, address);
                        }

                        if(doubledown && !result.busted)
                        {
                            PassTurnToNextPlayer(diceBot, botMain, session);
                            drawString += "\n" + session.BlackjackGameData.PrintCurrentPlayerTurn();
                        }
                        //whisper to player what their draw/ result is
                        //show in channel what card was drawn
                        if (!result.busted && !doubledown)
                            drawString += " [sub](and it's still their turn)[/sub]";

                        returnString = betString + drawString;
                    }

                }

            }
            else if (terms.Contains("bet") || terms.Contains("changebet"))
            {
                BlackjackBet bet = session.BlackjackGameData.BlackjackBets.FirstOrDefault(a => a.characterName == address.character);
                int betAmount = Utils.GetNumberFromInputs(terms);

                if(bet == null)
                    returnString = "Failed: You do not have a blackjack bet at this table. Join the game first with !joingame (#) to set your starting bet and join the game.";
                else if (betAmount <= 0)
                    returnString = "Failed: You must specify a non-zero number to use as your bet.";
                else if (betAmount < session.BlackjackGameData.minBet)
                    returnString = "Failed: You must specify a number above the minimum bet for this session (" + session.BlackjackGameData.minBet + ")";
                else if (betAmount > session.BlackjackGameData.maxBet && session.BlackjackGameData.maxBet >= 0)
                {
                    int originalBetAmount = betAmount;
                    betAmount = session.BlackjackGameData.maxBet;
                    returnString = TextFormat.GetCharacterUserTags(address.character) + "'s bet for upcoming rounds has been changed to " + betAmount + ". (reduced from " + originalBetAmount + " by session maximum bet)";
                    bet.betAmount = betAmount;
                }
                else
                {
                    returnString = TextFormat.GetCharacterUserTags(address.character) + "'s bet for upcoming rounds has been changed to " + betAmount + ".";
                    bet.betAmount = betAmount;
                }
            }
            else if (terms.Contains("currentturn") || terms.Contains("showturn"))
            {
                if (session.State != GameState.GameInProgress)
                    returnString = "Failed: This command can only be used while the game is in progress.";
                else
                    returnString = session.BlackjackGameData.PrintCurrentPlayerTurn();
            }
            else if (terms.Contains("setminbet") || terms.Contains("setante"))
            {
                int amount = Utils.GetNumberFromInputs(terms);
                if(amount >= 0)
                {
                    session.BlackjackGameData.minBet = amount;
                    returnString = "Set the minimum bet for this session to " + amount + " and updated player bets.";
                    foreach(BlackjackBet bet in session.BlackjackGameData.BlackjackBets)
                    {
                        if (bet.betAmount < amount)
                            bet.betAmount = amount;
                    }
                }
                else
                {
                    returnString = "Failed: no positive bet amount was found.";
                }
            }
            else if (terms.Contains("setmaxbet") || terms.Contains("maxbet"))
            {
                int amount = Utils.GetNumberFromInputs(terms);
                if(terms.Contains("false") || terms.Contains("none") || terms.Contains("off"))
                {
                    session.BlackjackGameData.maxBet = -1;
                    returnString = "Removed maximum bet for this session.";
                }
                else if (amount >= 1)
                {
                    session.BlackjackGameData.maxBet = amount;
                    returnString = "Set the maximum bet for this session to [b]" + amount + "[/b]. (updated all bets)";

                    foreach(var bet in session.BlackjackGameData.BlackjackBets)
                    {
                        if(bet.betAmount > session.BlackjackGameData.maxBet)
                        {
                            bet.betAmount = session.BlackjackGameData.maxBet;
                        }
                    }
                }
                else
                {
                    returnString = "Failed: start dice need to be at least 1.";
                }
            }
            else if (terms.Contains("setpushreturn") || terms.Contains("setpushreturnschips"))
            {
                if (terms.Length < 2)
                {
                    returnString = "Error: improper command format. Use 'setpushreturn (on) or (off)'.";
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
                        session.BlackjackGameData.PushReturnsBets = setValue;
                        returnString = "'Push Returns Chips' rule set to was set to " + (setValue ? "ON" : "OFF");
                    }
                    else
                    {
                        returnString = "Error: Input was invalid. Value must be set to on/ true, or off/ false";
                    }
                }
            }
            else { returnString += "Failed: No such command exists for " + GetGameName(); }

            return returnString;
        }
    }

    public class BlackjackGameData
    {
        public bool RulesSet;
        public int decksNumber = 2;
        public int minBet = 0;
        public int maxBet = -1;

        public bool PushReturnsBets = true;

        public PrintSetting CardPrintSetting = null;

        public List<BlackjackBet> BlackjackBets = new List<BlackjackBet>();
        public List<BlackjackPlayer> BlackjackPlayers = new List<BlackjackPlayer>();

        public BlackjackPlayer DealerPlayer = new BlackjackPlayer();

        public int currentPlayerIndex = -1;

        public List<string> ForceStandVotes = new List<string>();

        public BlackjackPlayer GetCurrentPlayer()
        {
            if(BlackjackPlayers == null || currentPlayerIndex < 0 || currentPlayerIndex > BlackjackPlayers.Count - 1 )
                return null;

            return BlackjackPlayers.ElementAt(currentPlayerIndex);
        }

        public BlackjackPlayer GetPlayer(string characterName)
        {
            if(BlackjackPlayers == null || BlackjackPlayers.Count == 0)
                return null;
            return BlackjackPlayers.FirstOrDefault(a => a.PlayerName == characterName);
        }

        public string PrintPlayers()
        {
            string rtn = "";

            if(BlackjackPlayers != null && BlackjackPlayers.Count >= 1)
            {
                foreach(BlackjackPlayer p in BlackjackPlayers)
                {
                    if (!string.IsNullOrEmpty(rtn))
                        rtn += "\n";
                    rtn += p.Print(CardPrintSetting);
                } 
            }
            return rtn;
        }

        public string PrintBets()
        {
            string rtn = "";

            if (BlackjackBets != null && BlackjackBets.Count >= 1)
            {
                foreach (BlackjackBet b in BlackjackBets)
                {
                    if (!string.IsNullOrEmpty(rtn))
                        rtn += ", ";
                    rtn += b.ToString();
                }
            }
            return rtn;
        }

        public void ShufflePlayers(System.Random r)
        {
            if(BlackjackPlayers != null && BlackjackPlayers.Count > 1)
            {
                List<BlackjackPlayer> tempList = new List<BlackjackPlayer>();
                tempList.AddRange(BlackjackPlayers);
                BlackjackPlayers = new List<BlackjackPlayer>();

                while(tempList.Count > 0)
                {
                    int randomIndex = r.Next(tempList.Count);
                    BlackjackPlayers.Add(tempList.ElementAt(randomIndex));
                    tempList.RemoveAt(randomIndex);
                }
            }
        }

        public string PrintCurrentPlayerTurn()
        {
            string outputString = "";
            if (BlackjackPlayers == null || BlackjackPlayers.Count == 0)
                return "(no players found)";

            if (currentPlayerIndex >= BlackjackPlayers.Count)
                outputString += "it is now the dealer's turn.";
            else
                outputString += "it is now " + TextFormat.GetCharacterUserTags(GetCurrentPlayer().PlayerName) + "'s turn";

            return outputString;
        }
    }

    public class HandResult
    {
        public int score;
        public bool blackjackBonus;
        public bool busted;
        public bool containsAce;

        public override string ToString()
        {
            return score + " " + (busted? "[color=red](BUSTED)[/color]" : "" ) + (blackjackBonus? "[color=yellow]Two-Card Blackjack[/color]" : "");
        }
    }

    public class BlackjackBet
    {
        public int betAmount;
        public string characterName;

        public override string ToString()
        {
            return TextFormat.GetCharacterUserTags(characterName) + " will bet " + betAmount;
        }
    }

    public class BlackjackPlayer
    {
        public int BetAmount;
        public int PushAmount;
        public bool DoubledDown;

        public DeckCard HiddenCard;
        public Hand PlayerHand;

        public string PlayerName;
        public bool Active;
        public bool Busted;
        public bool CannotAfford;

        public LastRoundResult LastRoundResult;

        public static string GetHandString(Hand hand, DeckCard hiddenCard, PrintSetting printSetting)
        {

            if (hand != null && hand.CardsCount() >= 2)
            {
                bool hiddenFirst = false;
                string handString = "";
                for (int i = 0; i < hand.CardsCount(); i++)
                {
                    DeckCard d = hand.GetCardAtIndex(i);
                    bool hiding = false;
                    if (!hiddenFirst && hiddenCard.number == d.number && hiddenCard.suit == d.suit)
                    {
                        hiding = true;

                        hiddenFirst = true;
                    }

                    if(hiding)
                    {
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(handString))
                            handString += ", ";

                        handString += d.Print(printSetting);
                    }
                }


                return "(1 hidden) " +  handString;
            }
            else
                return "(cards not drawn)";
        }

        public string Print(PrintSetting printSetting)
        {
            string betAmount = "has bet: [b]" + BetAmount + "[/b] " + BotMain.CurrencyPlaceholder + "s";
            if (CannotAfford)
            {
                betAmount = "(could not afford the ante and has not joined)";
            }

            return TextFormat.GetCharacterUserTags(PlayerName) + " " + betAmount + ", " + GetHandString(PlayerHand, HiddenCard, printSetting) + (Active ? "" : " (inactive)");
        }
    }

    public enum LastRoundResult
    {
        NONE,
        Won,
        WonWithBonus,
        Lost,
        Push,
        Forfeit
    }
}
