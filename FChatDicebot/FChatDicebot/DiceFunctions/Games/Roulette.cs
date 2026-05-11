using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class Roulette : IGame
    {
        public string GetGameName()
        {
            return "Roulette";
        }

        public int GetMaxPlayers()
        {
            return 20;
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
            return false;
        }

        public int GetMinimumMsBetweenGames()
        {
            return 5 * 60 * 1000;
        }

        public string GetGameHelp()
        {
            string thisGameCommands = "bet # [bettype] (adds another bet with amount and bet type)";
            string thisGameStartupOptions = "# (your bet amount) [bettype] (your bet type)" +
                    "\nThe default rules are: the ball lands on 0-36 at random and awards bets based on the odds for that bet." +
                    "\nPossible bet types: red, black, green, firsthalf, secondhalf, first12, second12, third12, even, odd, #0 - #36";

            return GameSession.GetGameHelp(GetGameName(), thisGameCommands, thisGameStartupOptions, true, true);
        }

        public string GetStartingDisplay()
        {
            return "[eicon]dbroulette1[/eicon][eicon]dbroulette2[/eicon]";
        }

        public string GetEndingDisplay()
        {
            return "";
        }

        public string GameStatus(GameSession session)
        {
            string betString = "";
            if (session.RouletteBets != null && session.RouletteBets.Count > 0)
            {
                foreach (RouletteBetData bet in session.RouletteBets)
                {
                    if (!string.IsNullOrEmpty(betString))
                    {
                        betString += ", ";
                    }

                    betString += bet.GetBetString();
                }
                betString = "Current Bets: " + betString;
            }
            return betString;
        }

        private RouletteBetData GetBetFromTerms(string characterName, string[] terms, int betAmount, out string messageString)
        {
            RouletteBet betType = RouletteBet.NONE;
            int betNumber = -1;

            if (terms.Contains("red"))
                betType = RouletteBet.Red;
            else if (terms.Contains("black"))
                betType = RouletteBet.Black;
            else if (terms.Contains("even"))
                betType = RouletteBet.Even;
            else if (terms.Contains("odd"))
                betType = RouletteBet.Odd;
            else if (terms.Contains("first12"))
                betType = RouletteBet.First12;
            else if (terms.Contains("second12"))
                betType = RouletteBet.Second12;
            else if (terms.Contains("third12"))
                betType = RouletteBet.Third12;
            else if (terms.Contains("firsthalf"))
                betType = RouletteBet.OneToEighteen;
            else if (terms.Contains("secondhalf"))
                betType = RouletteBet.NineteenToThirtySix;
            else if (terms.Contains("#0") || terms.Contains("green"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 0;
            }
            else if (terms.Contains("#1"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 1;
            }
            else if (terms.Contains("#2"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 2;
            }
            else if (terms.Contains("#3"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 3;
            }
            else if (terms.Contains("#4"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 4;
            }
            else if (terms.Contains("#5"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 5;
            }
            else if (terms.Contains("#6"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 6;
            }
            else if (terms.Contains("#7"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 7;
            }
            else if (terms.Contains("#8"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 8;
            }
            else if (terms.Contains("#9"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 9;
            }
            else if (terms.Contains("#10"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 10;
            }
            else if (terms.Contains("#11"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 11;
            }
            else if (terms.Contains("#12"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 12;
            }
            else if (terms.Contains("#13"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 13;
            }
            else if (terms.Contains("#14"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 14;
            }
            else if (terms.Contains("#15"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 15;
            }
            else if (terms.Contains("#16"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 16;
            }
            else if (terms.Contains("#17"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 17;
            }
            else if (terms.Contains("#18"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 18;
            }
            else if (terms.Contains("#19"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 19;
            }
            else if (terms.Contains("#20"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 20;
            }
            else if (terms.Contains("#21"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 21;
            }
            else if (terms.Contains("#22"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 22;
            }
            else if (terms.Contains("#23"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 23;
            }
            else if (terms.Contains("#24"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 24;
            }
            else if (terms.Contains("#25"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 25;
            }
            else if (terms.Contains("#26"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 26;
            }
            else if (terms.Contains("#27"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 27;
            }
            else if (terms.Contains("#28"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 28;
            }
            else if (terms.Contains("#29"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 29;
            }
            else if (terms.Contains("#30"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 30;
            }
            else if (terms.Contains("#31"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 31;
            }
            else if (terms.Contains("#32"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 32;
            }
            else if (terms.Contains("#33"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 33;
            }
            else if (terms.Contains("#34"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 34;
            }
            else if (terms.Contains("#35"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 35;
            }
            else if (terms.Contains("#36"))
            {
                betType = RouletteBet.SpecificNumber;
                betNumber = 36;
            }

            if (betType == RouletteBet.NONE)
            {
                messageString = "No bet type was found in the [b]!joingame " + GetGameName() + "[/b] command. Try adding a bet amount and a bet type.";
                return null;
            }
            else if (betAmount <= 0)
            {
                messageString = "No bet amount was found in the [b]!joingame " + GetGameName() + "[/b] command. Try adding a bet amount and a bet type.";
                return null;
            }
            else
            {
                messageString = "";
                return new RouletteBetData()
                {
                    bet = betType,
                    specificNumberBet = betNumber,
                    characterName = characterName,
                    amount = betAmount
                };
            }
        }

        public bool AddGameDataForPlayerJoin(string characterName, GameSession session, BotMain botMain, string[] terms, int ante, out string messageString)
        {
            RouletteBetData rouletteBet = GetBetFromTerms(characterName, terms, ante, out messageString);

            if (rouletteBet != null)
            {
                //create player or bet data object for this game?
                bool addDataSuccess = session.AddGameData(rouletteBet);

                if (addDataSuccess)
                {
                    messageString = "";
                    return true;
                }
                else
                {
                    messageString = "Error: Failed to add bet data to game session for " + GetGameName() + ".";
                    return false;
                }
            }
            return false;
        }

        public string PlayerLeftGame(BotMain botMain, GameSession session, string characterName)
        {
            session.RouletteBets = session.RouletteBets.Where(a => a.characterName != characterName).ToList();

            return " their bet was removed.";
        }

        public string RunGame(System.Random r, String executingPlayer, List<String> playerNames, DiceBot diceBot, BotMain botMain, GameSession session)
        {
            int randomResult = r.Next(37);//0-36

            bool colorGreen = false;
            bool colorBlack = false;
            bool colorRed = false;

            List<int> redResults = new List<int>(){1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36};
            List<int> blackResults = new List<int>(){2, 4, 6, 8, 10, 11, 13, 15, 17, 20, 22, 24, 26, 28, 29, 31, 33, 35};
            List<int> greenResults = new List<int>(){0};
            if(redResults.Contains(randomResult) )
                colorRed = true;
            if(blackResults.Contains(randomResult) )
                colorBlack = true;
            if(greenResults.Contains(randomResult) )
                colorGreen = true;

            string rollResult = "[color=gray]" + randomResult + " (black)[/color]";
            if(colorRed)
                rollResult = "[color=red]" + randomResult + " (red)[/color]";
            if (colorGreen)
                rollResult = "[color=green]" + randomResult + " (green)[/color]";

            string rouletteRollString = "[color=yellow]The dealer tosses in the ball! The wheel is spinning...[/color]\n[color=yellow]...[/color]\n[color=yellow]The ball landed on [/color]" + rollResult + "!";

            string characterBetsString = "";

            foreach(RouletteBetData bet in session.RouletteBets)
            {
                if (!playerNames.Contains(bet.characterName))
                {
                    characterBetsString += bet.characterName + " not found.\n";
                    continue;
                }

                if (!string.IsNullOrEmpty(characterBetsString))
                {
                    characterBetsString += "\n";
                }

                string betstring = "";
                
                ChipPile pile = diceBot.GetChipPile(bet.characterName, session.ChannelId, false);

                if(pile.Chips >= bet.amount)
                {
                    bet.cannotAffordBet = false;
                    betstring = diceBot.BetChips(bet.characterName, session.ChannelId, bet.amount, false);
                    betstring += " Their money is on [b]" + Utils.GetRouletteBetString(bet.bet, bet.specificNumberBet) + "[/b]!";

                }
                else
                {
                    bet.cannotAffordBet = true;
                    betstring = Utils.GetCharacterUserTags(bet.characterName) + " can no longer afford their bet.";
                }

                characterBetsString += betstring;
            }

            ChipPile houseChipsPile = diceBot.GetChipPile(DiceBot.HousePlayerAlias, session.ChannelId, true);
            string claimPotString = diceBot.ClaimPot(DiceBot.HousePlayerAlias, session.ChannelId, 1);

            string betReturns = "";

            foreach (RouletteBetData bet in session.RouletteBets)
            {
                if (!playerNames.Contains(bet.characterName) || bet.cannotAffordBet)
                {
                    continue;
                }

                int betReturn = RouletteBetReturn(randomResult, redResults, blackResults, bet.bet, bet.specificNumberBet);

                string winningsString = Utils.GetCharacterUserTags(bet.characterName) + " has [b]" + (betReturn > 0 ? "won" : "lost") + "![/b] ";

                if(betReturn > 0)
                {
                    int betWonAmount = bet.amount * betReturn;

                    if (houseChipsPile.Chips < betWonAmount + 1)
                    {
                        diceBot.AddChips(DiceBot.HousePlayerAlias, session.ChannelId, betWonAmount + 1, false);
                    }

                    winningsString += diceBot.GiveChips(DiceBot.HousePlayerAlias, bet.characterName, session.ChannelId, betWonAmount, false);
                }

                if (!string.IsNullOrEmpty(betReturns))
                {
                    betReturns += "\n";
                }

                betReturns += winningsString;
            }

            session.State = DiceFunctions.GameState.Finished;

            string outputString = characterBetsString + "\n" + claimPotString + "\n" + rouletteRollString + "\n" + betReturns;
            
            return outputString;
        }

        public void Update(BotMain botMain, GameSession session, double currentTime)
        {

        }

        public int RouletteBetReturn(int rouletteBallResult, List<int> redNumbers, List<int> blackNumbers, RouletteBet betMade, int specificNumberBet)
        {
            switch(betMade)
            {
                case RouletteBet.NONE:
                    return 0;
                case RouletteBet.Red:
                    if (redNumbers.Contains(rouletteBallResult))
                        return 2;
                    else
                        return 0;
                case RouletteBet.Black:
                    if (blackNumbers.Contains(rouletteBallResult))
                        return 2;
                    else
                        return 0;
                case RouletteBet.Even:
                    if (rouletteBallResult % 2 == 0 && rouletteBallResult != 0)
                    {
                        return 2;
                    }
                    else
                        return 0;
                case RouletteBet.Odd:
                    if (rouletteBallResult % 2 == 1 && rouletteBallResult != 0)
                    {
                        return 2;
                    }
                    else
                        return 0;
                case RouletteBet.OneToEighteen:
                    if (rouletteBallResult >= 1 && rouletteBallResult <= 18)
                    {
                        return 2;
                    }
                    else
                        return 0;
                case RouletteBet.NineteenToThirtySix:
                    if (rouletteBallResult >= 19 && rouletteBallResult <= 36)
                    {
                        return 2;
                    }
                    else
                        return 0;
                case RouletteBet.First12:
                    if (rouletteBallResult >= 1 && rouletteBallResult <= 12)
                    {
                        return 3;
                    }
                    else
                        return 0;
                case RouletteBet.Second12:
                    if (rouletteBallResult >= 13 && rouletteBallResult <= 24)
                    {
                        return 3;
                    }
                    else
                        return 0;
                case RouletteBet.Third12:
                    if (rouletteBallResult >= 25 && rouletteBallResult <= 36)
                    {
                        return 3;
                    }
                    else
                        return 0;
                case RouletteBet.SpecificNumber:
                    if (rouletteBallResult == specificNumberBet)
                    {
                        return 36;
                    }
                    else
                        return 0;
            }

            return 0;
        }

        public string IssueGameCommand(DiceBot diceBot, BotMain botMain, string character, string channel, GameSession session, string[] terms, string[] rawTerms)
        {
            string messageString = "";
            if(terms.Contains("addbet") || terms.Contains("bet"))
            {
                int betAmount = Utils.GetNumberFromInputs(terms);
                RouletteBetData rouletteBet = GetBetFromTerms(character, terms, betAmount, out messageString);
                if (rouletteBet == null)
                {
                    messageString = "Error: roulette bet not found. " + messageString;
                }
                else
                {
                    bool addDataSuccess = session.AddGameData(rouletteBet);

                    if (addDataSuccess)
                    {
                        messageString = "Added bet for " + character;
                    }
                    else
                    {
                        messageString = "Error: Failed to add bet data to game session for " + GetGameName() + ".";
                    }
                }
            }

            return messageString;
        }
    }

    public class RouletteBetData
    {
        public string characterName;
        public int amount;
        public int specificNumberBet;
        public RouletteBet bet;
        public bool cannotAffordBet;

        public string GetBetString()
        {
            return Utils.GetCharacterUserTags(characterName) + ": " + amount + " on " + Utils.GetRouletteBetString(bet, specificNumberBet);
        }
    }

    public enum RouletteBet
    {
        NONE,
        Red,
        Black,
        OneToEighteen,
        NineteenToThirtySix,
        First12,
        Second12,
        Third12,
        Even,
        Odd,
        SpecificNumber
    }
}
