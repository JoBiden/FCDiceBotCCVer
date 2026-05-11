using FChatDicebot.BotCommands.Base;
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
            string thisGameCommands = "setante #, currentturn, bet #, changebet #, setminbet #, setmaxbet #\n" +
                "(as current player only): hit, stand, doubledown";
            string thisGameStartupOptions = "# (sets your personal bet amount), minbet:# (sets minimum bet amount), x1/x2/x3/x4 (sets deck count), maxbet:# (set maximum bet amount)" +
                "\nThe default rules are: 2x deck used, no ante, no minbet, no maxbet";
            
            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, true, false);
        }

        public string GetStartingDisplay()
        {
            return "[eicon]dbblackjack1[/eicon][eicon]dbblackjack2[/eicon]";
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

                string output = "Rules: " + session.BlackjackGameData.decksNumber + " decks used, min bet: " + session.BlackjackGameData.minBet + ", max bet: " + maxBet ;

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

                session.BlackjackGameData.RulesSet = true;
                session.BlackjackGameData.decksNumber = deckNumber;

                outputString += "Rules set: (minbet: " + session.BlackjackGameData.minBet + ") (decks: " + deckNumber + ") " + maxBet + "\n";
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
            SavedData.ChannelSettings channelSettings = botMain.GetChannelSettings(session.ChannelId);
            session.BlackjackGameData.CardPrintSetting = channelSettings.CardPrintSetting;

            bool resetDeck = false;

            if (session.BlackjackGameData.BlackjackPlayers != null && session.BlackjackGameData.BlackjackPlayers.Count > 0)
            {
                Deck d = diceBot.GetDeck(session.ChannelId, DeckType.Playing, null);
                if(d.GetCardsRemaining() < session.BlackjackGameData.BlackjackBets.Count * 5)
                    resetDeck = true;
            }
            else
            {
                    resetDeck = true;
            }
            if(resetDeck)
                diceBot.ResetDeck(false, session.BlackjackGameData.decksNumber, session.ChannelId, channelSettings.CardPrintSetting, DeckType.Playing, null);

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
                ChipPile pile = diceBot.GetChipPile(bet.PlayerName, session.ChannelId, false);

                if(pile.Chips >= bet.BetAmount)
                {
                    bet.CannotAfford = false;
                    diceBot.BetChips(bet.PlayerName, session.ChannelId, bet.BetAmount, false);
                }
                else
                {
                    bet.CannotAfford = true;
                }
            }
            //claim pot with dealer
            diceBot.ClaimPot(DiceBot.DealerPlayerAlias, session.ChannelId, 1);//false, false);
            botMain.BotCommandController.SaveChipsToDisk("Blackjack");

            //draw all hands incl. dealer
            string drawOut = "";
            foreach(BlackjackPlayer bet in session.BlackjackGameData.BlackjackPlayers)
            {
                if(!bet.CannotAfford)
                {
                    diceBot.DrawCards(2, false, true, session.ChannelId, DeckType.Playing, null, bet.PlayerName, false, DeckType.NONE, null, out drawOut);
                    Hand h2 = diceBot.GetHand(session.ChannelId, DeckType.Playing, null, bet.PlayerName);
                    bet.PlayerHand = h2;

                    botMain.SendPrivateMessage("Blackjack hand drawn: " + h2.Print(false, channelSettings.CardPrintSetting, false), bet.PlayerName);
                }
            }

            diceBot.DrawCards(2, false, true, session.ChannelId, DeckType.Playing, null, DiceBot.DealerPlayerAlias, false, DeckType.NONE, null, out drawOut);
            Hand hdealer = diceBot.GetHand(session.ChannelId, DeckType.Playing, null, DiceBot.DealerPlayerAlias);

            string dealerStuff = "Dealer cards: " + BlackjackPlayer.GetHandString(hdealer, channelSettings.CardPrintSetting) + "\n";
            string playersPrint = dealerStuff + session.BlackjackGameData.PrintPlayers();

            session.BlackjackGameData.BlackjackPlayers.RemoveAll(ab => ab.CannotAfford);

            if(session.BlackjackGameData.BlackjackPlayers.Count == 0)
                return outputString + "\n" + playersPrint + "\n[b]all players were removed for failing to meet ante[/b].";

            session.BlackjackGameData.currentPlayerIndex = 0;
            string currentTurn = session.BlackjackGameData.PrintCurrentPlayerTurn();

            session.State = DiceFunctions.GameState.GameInProgress;
            
            return outputString + "\n" + playersPrint + "\n" + currentTurn + " [sub]!gc hit, !gc stand, !gc doubledown[/sub]";
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {

        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            string outputstring = "Goodbye " + Utils.GetCharacterUserTags( characterName);

            BlackjackBet bet = session.BlackjackGameData.BlackjackBets.FirstOrDefault(q => q.characterName == characterName);
            session.BlackjackGameData.BlackjackBets.Remove(bet);

            if (session.BlackjackGameData.BlackjackPlayers.Count(c => c.PlayerName == characterName) > 0)
            {
                BlackjackPlayer thisPlayer = session.BlackjackGameData.BlackjackPlayers.FirstOrDefault(d => d.PlayerName == characterName);

                int indexOfPlayer = session.BlackjackGameData.BlackjackPlayers.IndexOf(thisPlayer);

                if(thisPlayer.PushAmount > 0)
                {
                    botMain.DiceBot.AddChips(characterName, session.ChannelId, thisPlayer.PushAmount, false);
                    outputstring += ", your push amount of " + thisPlayer.PushAmount + " was returned.";
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
                    PassTurnToNextPlayer(botMain.DiceBot, botMain, session, session.ChannelId);

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

        public void PassTurnToNextPlayer(DiceBot diceBot, BotMain botMain, GameSession session, string channel)
        {
            BlackjackGameData data = session.BlackjackGameData;

            if(data != null && data.BlackjackPlayers != null)
            {
                data.currentPlayerIndex += 1;
                data.ForceStandVotes = new List<String>();
                if (data.currentPlayerIndex == data.BlackjackPlayers.Count)
                {
                    TakeDealerTurn(diceBot, botMain, session, channel);
                    session.BlackjackGameData.currentPlayerIndex += 1;
                }
                if( data.currentPlayerIndex > data.BlackjackPlayers.Count)
                {
                    FinishRound(diceBot, botMain, session, channel);
                }
            }
        }

        public void TakeDealerTurn(DiceBot diceBot, BotMain botMain, GameSession session, string channel)
        {
            string dealerTurnOutput = "";

            Hand dealerHand = diceBot.GetHand(channel, DeckType.Playing, null, DiceBot.DealerPlayerAlias);

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
                    lastHandResult = Hit(diceBot, channel, DiceBot.DealerPlayerAlias, session.BlackjackGameData.CardPrintSetting, out junkDraw);
                    dealerTurnOutput += "hit for " + junkDraw;
                }

                if (!string.IsNullOrEmpty(dealerTurnOutput))
                    dealerTurnOutput += ", ";

                if (lastHandResult.busted)
                    dealerTurnOutput += "[color=red]BUSTED[/color].";
                else
                    dealerTurnOutput += "stand.";
            }

            botMain.SendFutureMessage("The dealer is taking her turn: " + dealerTurnOutput, channel, null, true, RoundEndingWaitMs);
        }

        public void FinishRound(DiceBot diceBot, BotMain botMain, GameSession session, string channel)
        {
            string outputString = "";
            SavedData.ChannelSettings channelSettings = botMain.GetChannelSettings(channel);

            if (session.BlackjackGameData.BlackjackPlayers != null)
            {
                Hand dealerHand = diceBot.GetHand(channel, DeckType.Playing, null, DiceBot.DealerPlayerAlias);
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

                    outputString += Utils.GetCharacterUserTags(bjp.PlayerName);

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
                            diceBot.AddChips(bjp.PlayerName, channel, award, false);
                            outputString += " [b]WON[/b] this round. [color=green]" + award + " chips[/color] awarded.";
                            break;
                        case LastRoundResult.WonWithBonus:
                            int award2 = (bjp.BetAmount * 5) / 2; //pays 3:2
                            diceBot.AddChips(bjp.PlayerName, channel, award2, false);
                            outputString += " [b]WON WITH BLACKJACK[/b] this round. [color=green]" + award2 + " chips[/color] awarded.";
                            break;
                        case LastRoundResult.Push:
                            //record pushes
                            outputString += " [b]pushed[/b] this round.";
                            bjp.PushAmount = bjp.BetAmount;
                            break;
                    }

                    HandResult res = EvaluateBlackjackHand(bjp.PlayerHand);
                    outputString += " " + (bjp.PlayerHand != null ? bjp.PlayerHand.Print(false, channelSettings.CardPrintSetting, false) : "(cards not found)") + " = " + res.ToString();

                }

                HandResult res2 = EvaluateBlackjackHand(dealerHand);
                outputString = "The dealer's hand: " + (dealerHand != null ? dealerHand.Print(false, channelSettings.CardPrintSetting, false) : "(dealer cards not found)") + " = " + res2.ToString() + "\n" + outputString;

                diceBot.EndHand(channel, false, channelSettings.CardPrintSetting, DeckType.Playing, null);
                //save chips modifications to disk
                botMain.BotCommandController.SaveChipsToDisk("BlackjackFinishRound");
            }

            //print out all character results
            botMain.SendFutureMessage("[color=yellow]Round Finished![/color]\n" + outputString, channel, null, true, RoundEndingWaitMs * 2);
            session.State = GameState.Finished;
        }

        public HandResult Hit(DiceBot diceBot, string channel, string character, PrintSetting printSetting, out string truedraw)
        {
            diceBot.DrawCards(1, false, true, channel, DeckType.Playing, null, character, false, DeckType.NONE, null, out truedraw);
            Hand h = diceBot.GetHand(channel, DeckType.Playing, null, character);
            HandResult HandResult = EvaluateBlackjackHand(h);

            DeckCard drawnCard = h.GetCardAtIndex(h.CardsCount() - 1);
            truedraw = Utils.GetCharacterStringFromSpecialName(character) + " hit and drew a " + drawnCard.Print(printSetting);
            return HandResult;
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, GameSession session, string[] terms, string[] rawTerms)
        {
            string returnString = "";

            BlackjackPlayer currentPlayer = session.BlackjackGameData.GetCurrentPlayer();
            bool currentPlayerIssuedCommand = currentPlayer != null && currentPlayer.PlayerName == character;
            SavedData.ChannelSettings channelSettings = botMain.GetChannelSettings(channel);

            if(terms.Contains("forcestand"))
            {
                var bp = session.BlackjackGameData.GetPlayer(character);

                if (session.State != GameState.GameInProgress)
                    returnString = "Failed: This command can only be used while the game is in progress.";
                else if (session.BlackjackGameData.ForceStandVotes.Contains(character))
                    returnString = "Failed: Each player in the game only has one vote to force stand.";
                else
                {
                    bool characterIsAdmin = Utils.IsCharacterTrusted(botMain.AccountSettings.TrustedCharacters, character, channel) 
                        || Utils.IsCharacterAdmin(botMain.AccountSettings.AdminCharacters, character);
                    if (characterIsAdmin)
                    {
                        session.BlackjackGameData.ForceStandVotes.Add(character);
                        session.BlackjackGameData.ForceStandVotes.Add(character);
                        session.BlackjackGameData.ForceStandVotes.Add(character);
                        session.BlackjackGameData.ForceStandVotes.Add(character);
                    }
                    session.BlackjackGameData.ForceStandVotes.Add(character);

                    int currentVotes = session.BlackjackGameData.ForceStandVotes.Count();
                    int requiredVotes = Math.Max(2, session.BlackjackGameData.BlackjackPlayers.Count() / 2);

                    if (currentVotes >= requiredVotes)
                    {
                        string thisCharacter = session.BlackjackGameData.GetCurrentPlayer().PlayerName;
                        PassTurnToNextPlayer(diceBot, botMain, session, channel);

                        returnString = Utils.GetCharacterUserTags(thisCharacter) + " stands. " + session.BlackjackGameData.PrintCurrentPlayerTurn();
                    }
                    else
                        returnString = Utils.GetCharacterUserTags(character) + " has voted to force stand: " + currentVotes + " / " + (requiredVotes) + " votes.";
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
                        PassTurnToNextPlayer(diceBot, botMain, session, channel);
                        returnString = Utils.GetCharacterUserTags(character) + " stands. " + session.BlackjackGameData.PrintCurrentPlayerTurn();
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
                            ChipPile pile = diceBot.GetChipPile(character, session.ChannelId, false);

                            if(currentPlayer.PlayerHand.CardsCount() > 2)
                            {
                                return "Failed: You cannot double down after already drawing another card with hit.";
                            }
                            else if(pile.Chips >= currentPlayer.BetAmount)
                            {
                                diceBot.GiveChips(currentPlayer.PlayerName, DiceBot.DealerPlayerAlias, channel, currentPlayer.BetAmount, false);
                                betString += "Added " + currentPlayer.BetAmount + " more chips to the bet. ";

                                botMain.BotCommandController.SaveChipsToDisk("BlackjackGameCommand");
                            }
                            else
                            {
                                return "Failed: You cannot afford to double down, you do not have enough chips remaining.";
                            }
                        }

                        string drawString = "";

                        var result = Hit(diceBot, channel, character, channelSettings.CardPrintSetting, out drawString);

                        Hand h2 = diceBot.GetHand(channel, DeckType.Playing, null, character);

                        string handInformation = "\nBlackjack hand: " + h2.Print(false, channelSettings.CardPrintSetting, false);

                        if(result.busted)
                        {
                            PassTurnToNextPlayer(diceBot, botMain, session, channel);
                            drawString += handInformation + " = [color=red]BUSTED[/color] at " + result.score;
                            drawString += "\n" + session.BlackjackGameData.PrintCurrentPlayerTurn();
                        }
                        else
                        {
                            botMain.SendPrivateMessage(drawString + handInformation + " = " + result.score, character);
                        }

                        if(doubledown && !result.busted)
                        {
                            PassTurnToNextPlayer(diceBot, botMain, session, channel);
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
                BlackjackBet bet = session.BlackjackGameData.BlackjackBets.FirstOrDefault(a => a.characterName == character);
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
                    returnString = Utils.GetCharacterUserTags(character) + "'s bet for upcoming rounds has been changed to " + betAmount + ". (reduced from " + originalBetAmount + " by session maximum bet)";
                    bet.betAmount = betAmount;
                }
                else
                {
                    returnString = Utils.GetCharacterUserTags(character) + "'s bet for upcoming rounds has been changed to " + betAmount + ".";
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
            else { returnString += "Failed: No such command exists"; }

            return returnString;
        }
    }

    public class BlackjackGameData
    {
        public bool RulesSet;
        public int decksNumber = 2;
        public int minBet = 0;
        public int maxBet = -1;

        public PrintSetting CardPrintSetting = null;

        public List<BlackjackBet> BlackjackBets = new List<BlackjackBet>();
        public List<BlackjackPlayer> BlackjackPlayers = new List<BlackjackPlayer>();

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
                outputString += "it is now " + Utils.GetCharacterUserTags(GetCurrentPlayer().PlayerName) + "'s turn";

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
            return Utils.GetCharacterUserTags(characterName) + " will bet " + betAmount;
        }
    }

    public class BlackjackPlayer
    {
        public int BetAmount;
        public int PushAmount;

        public Hand PlayerHand;

        public string PlayerName;
        public bool Active;
        public bool Busted;
        public bool CannotAfford;

        public LastRoundResult LastRoundResult;

        public static string GetHandString(Hand hand, PrintSetting printSetting)
        {

            if (hand != null && hand.CardsCount() >= 2)
            {
                bool hiddenFirst = false;
                string handString = "";
                for (int i = 0; i < hand.CardsCount(); i++)
                {
                    DeckCard d = hand.GetCardAtIndex(i);
                    if (!hiddenFirst)
                    {
                        handString += "(1 hidden)";
                        hiddenFirst = true;
                    }
                    else
                    {
                        if (i >= 1)
                            handString += ", ";

                        handString += d.Print(printSetting);
                    }
                }


                return handString;
            }
            else
                return "(cards not drawn)";
        }

        public string Print(PrintSetting printSetting)
        {
            string betAmount = "has bet: [b]" + BetAmount + "[/b] chips";
            if (CannotAfford)
            {
                betAmount = "(could not afford the ante and has not joined)";
            }

            return Utils.GetCharacterUserTags(PlayerName) + " " + betAmount + ", " + GetHandString(PlayerHand, printSetting) + (Active ? "" : " (inactive)");
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
