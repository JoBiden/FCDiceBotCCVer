using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FChatDicebot.DiceFunctions
{
    public class DiceBot
    {
        private BotMain botMain;
        public System.Random random;
        public const int MaximumDice = 200; //was 100 in 1.30
        public const int MaximumRolls = 400; //was 200 in 1.30
        public const int MaximumSecondaryTableRolls = 3;
        public const int MaximumSides = 10000000; //was 100000 in 1.30
        public const int MaximumCommandLength = 1000;
        public const int MaximumHandSize = 100;
        public const int MaximumDrawSize = 10;

        public const int MinimumChipCashoutSize = 1000;
        public const int CashoutCooldownMs = 1000 * 60 * 60 * 72; //3 days cooldown for cashout
        public const int MaximumChipCashoutSize = 100000;
        public const int MaximumChipBuySize = 50000;

        public static List<char> ValidOperators = new List<char>(){ '+', '-', '*', '/', '>', '<'};// '►', '◄' };

        public const string DealerPlayerAlias = "the_dealer";
        public const string BurnCardsPlayerAlias = "burn_cards";
        public const string DiscardPlayerAlias = "discard_pile";
        public const string HousePlayerAlias = "the_house";

        public const string PotPlayerAlias = "the_pot";

        public const string PlaySuffix = "_inplay";
        public const string HiddenPlaySuffix = "_inplayhidden";
        public const string JackpotSuffix = "_Jackpot";
        public const string PlayerCashoutSuffix = "_Cashout";
        public static string DiceBotCharacter { get; private set; } = "(unset char)";

        public List<Deck> ChannelDecks;
        public List<Hand> Hands;
        public List<ChipPile> ChipPiles;
        public List<CharacterData> CharacterDatas;
        public List<GameSession> GameSessions;
        public List<CountdownTimer> CountdownTimers;
        public PotionGenerator PotionGenerator;
        public List<ChannelDiceRoll> LastRolls;
        public List<VcChipOrder> VcChipOrders;

        public List<IGame> PossibleGames;

        // Money mover for currency-wagered games (the global-wallet replacement for the chip
        // pot helpers). Parallels how games reach BetChips/ClaimPot through this DiceBot.
        public Wager.ChateauWagerBank WagerBank;

        //characters that might be useful for printouts
        //⚀⚁⚂⚃⚄⚅ 🎲

        public DiceBot(BotMain sourceBot)
        {
            botMain = sourceBot;
            random = new System.Random();
            DiceBotCharacter = sourceBot.AccountSettings.CharacterName;

            WagerBank = new Wager.ChateauWagerBank();
            ChannelDecks = new List<Deck>();
            Hands = new List<Hand>();
            ChipPiles = new List<ChipPile>();
            CharacterDatas = new List<CharacterData>();
            GameSessions = new List<GameSession>();
            CountdownTimers = new List<CountdownTimer>();
            PotionGenerator = new DiceFunctions.PotionGenerator(random);
            LastRolls = new List<ChannelDiceRoll>();
            VcChipOrders = new List<VcChipOrder>();

            PossibleGames = new List<IGame>() { new HighRoll(), new Poker(), new BottleSpin(), new Roulette(), new KingsGame(),
                new LiarsDice(), new SlamRoll(), new Blackjack(), new RockPaperScissors(), new Mafia(), new AlphaRoyale(),
                new DungeonDelve(), new Chess() };

            LoadChipPilesFromDisk(BotMain.FileFolder, BotMain.SavedChipsFileName);
            LoadCharacterDataFromDisk(BotMain.FileFolder, BotMain.CharacterDataFileName);
            LoadVcChipOrderDataFromDisk(BotMain.FileFolder, BotMain.VcChipOrdersFileName);

            // Startup-only: refund any wager escrow stranded by a restart mid-game (the in-memory
            // game sessions are gone, so these stakes can never resolve on their own). Guarded so a
            // database hiccup can't stop the bot from starting.
            try
            {
                int refunded = WagerBank.ReconcileAllEscrow();
                if (refunded > 0)
                    Console.WriteLine("Wager escrow reconcile: refunded " + refunded + " stranded stake(s) on startup.");
            }
            catch (System.Exception escrowExc)
            {
                Console.WriteLine("Wager escrow reconcile failed on startup (non-fatal): " + escrowExc.Message);
            }

            Utils.AddToLog(" ", "FINISHED " + DiceBotCharacter + " LOAD :: Chip Piles found " + ChipPiles.Count() + "... Character Datas found " + CharacterDatas.Count());
            Console.WriteLine("FINISHED " + DiceBotCharacter + " LOAD :: Chip Piles found " + ChipPiles.Count() + "... Character Datas found " + CharacterDatas.Count() );
        }

        private void LoadChipPilesFromDisk(string fileFolder, string fileName)
        {
            string path = Utils.GetTotalFileName(fileFolder, fileName);
            try
            {
                BotMain.VerifyDirectoryExists();

                if (File.Exists(path))
                {
                    string fileText = File.ReadAllText(path, Encoding.ASCII);

                    if (BotMain._debug)
                        Console.WriteLine("Chip piles file text " + fileText == null? "null" : fileText.Take(100) + "...");

                    ChipPiles = JsonConvert.DeserializeObject<List<ChipPile>>(fileText);

                    if (BotMain._debug)
                        Console.WriteLine("loaded LoadChipPiles successfully.");
                }
                else
                {
                    ChipPiles = new List<ChipPile>();
                    Console.WriteLine("LoadChipPiles file does not exist.");
                }
            }
            catch (System.Exception exc)
            {
                Console.WriteLine("Exception: Failed to load LoadChipPiles for " + path + "\n" + exc.ToString());
            }
        }

        private void LoadCharacterDataFromDisk(string FileFolder, string CharacterDataFileName)
        {
            string path = Utils.GetTotalFileName(FileFolder, CharacterDataFileName);
            try
            {
                BotMain.VerifyDirectoryExists();

                if (File.Exists(path))
                {
                    string fileText = File.ReadAllText(path, Encoding.ASCII);

                    CharacterDatas = JsonConvert.DeserializeObject<List<CharacterData>>(fileText);
                }
                else
                {
                    CharacterDatas = new List<CharacterData>();
                    Console.WriteLine("LoadCharacterData file does not exist.");
                }
            }
            catch (System.Exception exc)
            {
                Console.WriteLine("Exception: Failed to load LoadCharacterData for " + path + "\n" + exc.ToString());
            }
        }

        private void LoadVcChipOrderDataFromDisk(string fileFolder, string fileName)
        {
            string path = Utils.GetTotalFileName(fileFolder, fileName);
            try
            {
                BotMain.VerifyDirectoryExists();

                if (File.Exists(path))
                {
                    string fileText = File.ReadAllText(path, Encoding.ASCII);

                    VcChipOrders = JsonConvert.DeserializeObject<List<VcChipOrder>>(fileText);

                    if (BotMain._debug)
                        Console.WriteLine("loaded LoadVcChipOrder successfully.");
                }
                else
                {
                    VcChipOrders = new List<VcChipOrder>();
                    Console.WriteLine("LoadVcChipOrder file does not exist.");
                }
            }
            catch (System.Exception exc)
            {
                Console.WriteLine("Exception: Failed to load LoadVcChipOrder for " + path + "\n" + exc.ToString());
            }
        }

        public VcChipOrder GetVcChipOrder(MessageAddress address)// string character, string channel)
        {
            string channelKey = address.GetChannelKey();
            return VcChipOrders.FirstOrDefault(a => a.Character == address.character && a.ChannelId == channelKey);
        }

        public bool AddVcChipOrder(int amount, string character, string channel, string vcTransactionId)
        {
            bool contains = VcChipOrders.Count(a => a.TransactionId == vcTransactionId) > 0;

            VcChipOrders.Add(new VcChipOrder() {
                ChannelId = channel,
                Character = character,
                Chips = amount,
                Created = DoubleTime.GetCurrentTimestampSeconds(),
                LastCheckedTime = DoubleTime.GetCurrentTimestampSeconds(),
                TransactionId = vcTransactionId,
                CheckedCount = 0,
                OrderStatus = 0
            });
            return !contains;
        }

        public string RollFitD(int diceNumber, MessageAddress address)
        {
            int actualRolled = diceNumber <= 0? 2 : diceNumber;
            DiceRoll roll = new DiceRoll(address, botMain)
            {
                DiceRolled = actualRolled,
                DiceSides = 6,
                TextFormat = true
            };
            roll.Roll(random);

            int success = roll.Rolls.Count(a => a.Result == 4 || a.Result == 5);
            int critical = roll.Rolls.Count(a => a.Result == 6);
            int fail = roll.Rolls.Count(a => a.Result < 4);

            if(diceNumber <= 0) //with 0 or less dice, you roll 2 and take the lowest without the possibility of critical success
            {
                success += critical;
                critical = 0;
                if (fail > 0)
                {
                    fail = 1;
                    success = 0;
                }
                else
                {
                    success = 1;
                }
            }

            string failString = "";
            if(fail > 0)
                failString = " fail: [color=red]" + fail + "[/color]";
            string successString = "";
            if(success > 0)
                successString = " success: [color=yellow]" + success + "[/color]";
            string criticalString = "";
            if(critical > 0)
                criticalString = " critical: [color=green]" + critical + "[/color]";

            RecordDiceRoll(roll, address);

            string resultString = string.Format("{0} \nFitD [b]{1}[/b] dice, {2}{3}{4}", roll.ResultString(), diceNumber, failString, successString, criticalString);

            return resultString;
        }

        public string GetRollResult(string[] inputCommands, MessageAddress address, bool debugOutput)
        {
            string allCommands = Utils.CombineStringArray(inputCommands);

            bool sort = false;
            if(allCommands.Contains("sort"))
            {
                allCommands = allCommands.Replace("sort", "");
                sort = true;
            }

            if(allCommands.Length > MaximumCommandLength)
                return ((new DiceRoll() { Error = true, ErrorString = "command exceeds maximum length" }).ResultString());

            List<char> operators = new List<char>();
            string errorString = "";
            List<string> commandTerms = SeparateCommands(allCommands, out operators, out errorString);

            if(debugOutput)
                Console.WriteLine("Commands Separated: " + Utils.PrintList(commandTerms.ToArray()) + " operators found: " + Utils.PrintList(operators.ToArray()) );

            if (errorString != "")
                return ((new DiceRoll() { Error = true, ErrorString = errorString }).ResultString());
            if (operators == null || commandTerms.Count < 1)
                return ( (new DiceRoll() { Error = true, ErrorString = "requires at least 1 input" }).ResultString());
            if (operators.Count != commandTerms.Count - 1)
                return ((new DiceRoll() { Error = true, ErrorString = "bad number of arithmetic operators" }).ResultString());
            if (operators.Count(a => a == '<' || a == '>') > 1)
                return ((new DiceRoll() { Error = true, ErrorString = "only one comparison (< or >) allowed" }).ResultString());

            string totalResultString = "";
            List<DiceRoll> finishedRolls = new List<DiceRoll>();

            foreach(string s in commandTerms)
            {
                DiceRoll d = ParseRollFromCommand(s, address);

                d.Roll(random);

                finishedRolls.Add(d);

                if (totalResultString != "")
                    totalResultString += ", ";

                DiceRollFormat formatUsed = DiceRollFormat.Inherit;
                //if (Utils.IsDiscordMessage(address))
                //    formatUsed = DiceRollFormat.Text;

                totalResultString += d.ResultString(formatUsed, sort);
            }

            try
            {
                if (commandTerms.Count > 1 && operators.Count > 0)
                {
                    totalResultString += "\n[i]Result: [/i]";

                    List<long> allNumbers = new List<long>();
                    foreach (DiceRoll d2 in finishedRolls)
                    {
                        allNumbers.Add(d2.Total);
                    }

                    //put number results in arithmetic string
                    for(int i = 0; i < allNumbers.Count; i++)
                    {
                        if (i > 0)
                        {
                            totalResultString += operators[i - 1] + " ";
                        }
                        totalResultString += allNumbers[i] + " ";
                    }
                    
                    string applyOperatorErrorString = "";
                    ApplyNegatives(ref allNumbers, ref operators, out applyOperatorErrorString);
                    if (applyOperatorErrorString != "")
                        return ((new DiceRoll() { Error = true, ErrorString = applyOperatorErrorString }).ResultString());

                    //OOP first */
                    ApplyOperator(ref allNumbers, ref operators, '*', out applyOperatorErrorString);
                    if(applyOperatorErrorString != "")
                        return ((new DiceRoll() { Error = true, ErrorString = applyOperatorErrorString }).ResultString());

                    ApplyOperator(ref allNumbers, ref operators, '/', out applyOperatorErrorString);
                    if (applyOperatorErrorString != "")
                        return ((new DiceRoll() { Error = true, ErrorString = applyOperatorErrorString }).ResultString());
                    
                    //OOP second +-
                    ApplyOperator(ref allNumbers, ref operators, '+', out applyOperatorErrorString);
                    if (applyOperatorErrorString != "")
                        return ((new DiceRoll() { Error = true, ErrorString = applyOperatorErrorString }).ResultString());

                    ApplyOperator(ref allNumbers, ref operators, '-', out applyOperatorErrorString);
                    if (applyOperatorErrorString != "")
                        return ((new DiceRoll() { Error = true, ErrorString = applyOperatorErrorString }).ResultString());
                    
                    //OOP comparison < > 
                    if (operators.Contains('>') || operators.Contains('<'))
                    {
                        string additionalStr = "";
                        if (allNumbers.Count == 2 && operators.Contains('>'))
                        {
                            additionalStr = allNumbers[0] > allNumbers[1] ? "true":  "false";
                        }
                        else if (allNumbers.Count == 2 && operators.Contains('<'))
                        {
                            additionalStr = allNumbers[0] < allNumbers[1] ? "true" : "false";
                        }
                        else
                        {
                            return ((new DiceRoll() { Error = true, ErrorString = "invalid comparison" }).ResultString());
                        }
                        totalResultString += "= [b]" + additionalStr + "[/b]";
                    }
                    else
                    {
                        totalResultString += "= [b]" + allNumbers[0] + "[/b]";
                    }

                }
            }catch(Exception exc)
            {
                return ((new DiceRoll() { Error = true, ErrorString = "exception on arithmetic: " + exc.ToString() }).ResultString());
            }

            if(finishedRolls != null && finishedRolls.Count() > 0)
                RecordDiceRoll(finishedRolls.Last(), address);

            return totalResultString;
        }

        public void ApplyNegatives(ref List<long> allNumbers, ref List<char> operators, out string error)
        {
            error = "";
            if(allNumbers.Count() == operators.Count() + 1)
            {
                for (int i = 1; i < allNumbers.Count(); i++)
                {
                    if(operators[i-1] == '-')
                    {
                        allNumbers[i] = -1 * allNumbers[i];
                        operators[i-1] = '+';
                    }
                }
            }
            else
            {
                error = "incorrect number of operators";
            }

        }

        public void ApplyOperator(ref List<long> allNumbers, ref List<char> operators, char op, out string error)
        {
            error = "";

            while (operators.Contains(op))
            {
                int thisOperatorIndex = operators.IndexOf(op);
                long result = 0;
                double resultcheck = 0;
                switch(op)
                {
                    case '*':
                        result = allNumbers[thisOperatorIndex] * allNumbers[thisOperatorIndex + 1];
                        resultcheck = (double) allNumbers[thisOperatorIndex] * (double) allNumbers[thisOperatorIndex + 1];
                        break;
                    case '/':
                        result = allNumbers[thisOperatorIndex] / allNumbers[thisOperatorIndex + 1];
                        resultcheck = result;
                        break;
                    case '+':
                        result = allNumbers[thisOperatorIndex] + allNumbers[thisOperatorIndex + 1];
                        resultcheck = (double)allNumbers[thisOperatorIndex] + (double)allNumbers[thisOperatorIndex + 1];
                        break;
                    case '-':
                        result = allNumbers[thisOperatorIndex] - allNumbers[thisOperatorIndex + 1];
                        resultcheck = (double)allNumbers[thisOperatorIndex] - (double)allNumbers[thisOperatorIndex + 1];
                        break;
                }

                if ((double)result != resultcheck)
                {
                    error = "long overflow";
                    return;
                }

                allNumbers.RemoveAt(thisOperatorIndex + 1);
                allNumbers[thisOperatorIndex] = result;
                operators.RemoveAt(thisOperatorIndex);
            }
        }

        private List<string> SeparateCommands(string allCommands, out List<char> operators, out string error)
        {
            operators = new List<char>();
            error = "";

            List<string> commandTerms = new List<string>();

            if(string.IsNullOrEmpty(allCommands))
            {
                return commandTerms;
            }
            
            int index = 0;
            int lastOperatorIndex = -1;
            foreach(char c in allCommands)
            {
                if(ValidOperators.Contains(c))
                {
                    //check for negative
                    if (lastOperatorIndex == index -1)
                    {
                        if(c == '-')
                        {
                            if(allCommands.Length <= index)
                            {
                                error = "invalid - operator";
                                return commandTerms;
                            }
                        }
                        else
                        {
                            error = "operators must be separated by terms";
                            return commandTerms;
                        }
                    }
                    else
                    {
                        operators.Add(c);

                        commandTerms.Add(allCommands.Substring(lastOperatorIndex + 1, index - (lastOperatorIndex + 1)));

                        lastOperatorIndex = index;
                    }
                }

                index++;
            }
            //add last term without an operator after it
            if (lastOperatorIndex + 1 < allCommands.Length)
                commandTerms.Add(allCommands.Substring(lastOperatorIndex + 1, allCommands.Length - (lastOperatorIndex + 1)));

            return commandTerms;
        }

        public DiceRoll ParseRollFromCommand(string command, MessageAddress address)
        {
            if (string.IsNullOrEmpty(command))
                return new DiceRoll() { Error = true, ErrorString = "command empty" };
            
            //try parsing as a plain number if there is no dice roll
            if(!command.Contains('d'))
            {
                int numberResult = 0;
                bool successNumber = int.TryParse(command, out numberResult);
                if (!successNumber)
                    return new DiceRoll() { Error = true, ErrorString = "invalid number input" };
                else
                    return new DiceRoll() { Error = false, TextFormat = true, Total = numberResult };
            }

            if (command.Length < 3 || !command.Contains('d') || command.LastIndexOf('d') != command.IndexOf('d')
                || command.IndexOf('d') == 0 || command.IndexOf('d') == command.Length -1) //d is not the first or last character, contains d
                return new DiceRoll() { Error = true, ErrorString = "invalid dice input" };

            int numberSides = 0;
            int numberDice = 0;
            bool explode = false;
            bool explodeTable = false;
            bool fateDice = false;
            bool alsoCountUnder = false;
            bool alsoCountOver = false;

            string[] split = command.Split('d');
            string splitDiceNumber = split[0];
            string splitDiceSidesAndCommands = split[1];
            bool success1 = int.TryParse(splitDiceNumber, out numberDice);

            string parseError = "";

            //parse out '!'
            if (splitDiceSidesAndCommands.Contains('!'))
            {
                explode = true;
            }
            if (splitDiceSidesAndCommands.Contains('e'))
            {
                explodeTable = true;
            }
            if (splitDiceSidesAndCommands.StartsWith("f"))
            {
                fateDice = true;
            }

            int explodeThreshold = ParseOutDieCommand("!", ref splitDiceSidesAndCommands, out parseError);
            //note: no error for ! on purpose: it is possible to parse out nothing for the number.
            splitDiceSidesAndCommands = splitDiceSidesAndCommands.Replace("!0", "").Replace("!", "");
            if (explodeTable)
            {
                explodeThreshold = ParseOutDieCommand("e", ref splitDiceSidesAndCommands, out parseError);
                //note: no error for ! on purpose: it is possible to parse out nothing for the number.
                splitDiceSidesAndCommands = splitDiceSidesAndCommands.Replace("e0", "").Replace("e", "");
            }

            int keepHighest = ParseOutDieCommand("kh", ref splitDiceSidesAndCommands, out parseError);

            if (!string.IsNullOrEmpty(parseError))
                return new DiceRoll() { Error = true, ErrorString = parseError };

            int keepLowest = ParseOutDieCommand("kl", ref splitDiceSidesAndCommands, out parseError);

            if (!string.IsNullOrEmpty(parseError))
                return new DiceRoll() { Error = true, ErrorString = parseError };
            int removeHighest = ParseOutDieCommand("rh", ref splitDiceSidesAndCommands, out parseError);

            if (!string.IsNullOrEmpty(parseError))
                return new DiceRoll() { Error = true, ErrorString = parseError };

            int removeLowest = ParseOutDieCommand("rl", ref splitDiceSidesAndCommands, out parseError);

            if (!string.IsNullOrEmpty(parseError))
                return new DiceRoll() { Error = true, ErrorString = parseError };

            int rerollNumber = ParseOutDieCommand("rr", ref splitDiceSidesAndCommands, out parseError);

            if (!string.IsNullOrEmpty(parseError))
                return new DiceRoll() { Error = true, ErrorString = parseError };

            if (splitDiceSidesAndCommands.Contains("&co") || splitDiceSidesAndCommands.Contains("&amp;co"))
                alsoCountOver = true;

            int countOver = ParseOutDieCommand("co", ref splitDiceSidesAndCommands, out parseError);

            if (!string.IsNullOrEmpty(parseError))
                return new DiceRoll() { Error = true, ErrorString = parseError };

            if (splitDiceSidesAndCommands.Contains("&cu") || splitDiceSidesAndCommands.Contains("&amp;cu"))
                alsoCountUnder = true;

            int countUnder = ParseOutDieCommand("cu", ref splitDiceSidesAndCommands, out parseError);

            if (!string.IsNullOrEmpty(parseError))
                return new DiceRoll() { Error = true, ErrorString = parseError };

            if (splitDiceSidesAndCommands.Contains("&"))
            {
                if (countOver <= 0 && countUnder <= 0)
                {
                    return new DiceRoll() { Error = true, ErrorString = "Cannot use & without co or cu" };
                }
                else if (!alsoCountOver && !alsoCountUnder)
                {
                    return new DiceRoll() { Error = true, ErrorString = "Place & directly before co or cu" };
                }
                else
                {
                    splitDiceSidesAndCommands = splitDiceSidesAndCommands.Replace("&amp;","").Replace("&", "");
                }
            }


            bool success2 = false;
            if (fateDice)
            {
                success2 = true;
                numberSides = 3;
            }
            else
                success2 = int.TryParse(splitDiceSidesAndCommands, out numberSides);


            if (numberDice > MaximumDice)
                return new DiceRoll() { Error = true, ErrorString = "will only roll up to " + MaximumDice + " base dice" };
            if (numberSides > MaximumSides)
                return new DiceRoll() { Error = true, ErrorString = "will only roll up to " + MaximumSides + " base sides" };
            if (numberSides == 1)
                return new DiceRoll() { Error = true, ErrorString = "1 is not a valid number of sides" };
            if (numberSides <= 0 || numberDice <= 0)
                return new DiceRoll() { Error = true, ErrorString = "number of sides and number of dice must be greater than 0" };
            if (!success1)
                return new DiceRoll() { Error = true, ErrorString = "invalid number of dice" };
            if (!success2)
                return new DiceRoll() { Error = true, ErrorString = "invalid number of sides" };
            if (explodeThreshold != 0 && explodeThreshold <= 1)
                return new DiceRoll() { Error = true, ErrorString = "cannot use an explode threshold of 1 or less."};
            if ((keepHighest > 0 && (keepLowest > 0 || removeHighest > 0 || removeLowest > 0)) ||
                (keepLowest > 0 && (keepHighest > 0 || removeHighest > 0 || removeLowest > 0)) ||
                (removeHighest > 0 && (keepHighest > 0 || keepLowest > 0 || removeLowest > 0)) ||
                (removeLowest > 0 && (keepHighest > 0 || removeHighest > 0 || keepLowest > 0))
                )
                return new DiceRoll() { Error = true, ErrorString = "only one keep/ drop option allowed per roll" };
            if (countOver > 0 && countUnder > 0 && !alsoCountOver && !alsoCountUnder)
                alsoCountUnder = true;
                //return new DiceRoll() { Error = true, ErrorString = "can only use 'co' or 'cu' countover or countunder" };
            if ((keepHighest >= numberDice) ||
                (keepLowest >= numberDice) ||
                (removeHighest >= numberDice) ||
                (removeLowest >= numberDice)
                )
                return new DiceRoll() { Error = true, ErrorString = "number of dice to choose must be less than total number of dice" };


            return new DiceRoll(address, botMain)
            {
                DiceRolled = numberDice,
                DiceSides = numberSides,
                Error = false,
                Explode = explode,
                ExplodeTable = explodeTable,
                ExplodeThreshold = explodeThreshold,
                FateDice = fateDice,
                TextFormat = true,
                KeepHighest = keepHighest,
                KeepLowest = keepLowest,
                RemoveHighest = removeHighest,
                RemoveLowest = removeLowest,
                RerollNumber = rerollNumber,
                CountOver = countOver,
                CountUnder = countUnder,
                AlsoCountOver = alsoCountOver,
                AlsoCountUnder = alsoCountUnder
            };
        }

        private int ParseOutDieCommand(string currentParseCommand, ref string splitDiceSidesAndCommands, out string errorString)
        {
            int resultNumber = 0;
            errorString = null;

            if (splitDiceSidesAndCommands.Contains(currentParseCommand))
            {
                string error = "";
                string remainingString = "";
                resultNumber = keepNumberParse(splitDiceSidesAndCommands, currentParseCommand, out error, out remainingString);

                if (error != "")
                {
                    errorString = error;
                    return resultNumber;
                }

                if (resultNumber > 0)
                {
                    splitDiceSidesAndCommands = remainingString;
                }
            }

            return resultNumber;
        }

        public int keepNumberParse(string parseString, string keepCharacters, out string errorString, out string remainingString)
        {
            errorString = "";
            remainingString = "";

            int indexOfParseString = parseString.IndexOf(keepCharacters);

            if (indexOfParseString == -1)
                return 0;

            if (parseString.Length <= indexOfParseString + keepCharacters.Length || !char.IsDigit(parseString[indexOfParseString + keepCharacters.Length]))
            {
                errorString = "'" + keepCharacters + "' must be followed by a number";
                return 0;
            }

            string khNumber = "";

            string tempString = parseString.Substring(indexOfParseString + keepCharacters.Length);

            while(tempString.Length > 0 && char.IsDigit(tempString[0]))
            {
                khNumber += tempString[0];
                tempString = tempString.Substring(1);
            }

            remainingString = parseString.Replace(keepCharacters + khNumber, "");

            int returnInt = 0;
            bool parsed = int.TryParse(khNumber, out returnInt);

            if (!parsed)
            {
                errorString = "failed to parse " + keepCharacters + " number";
                return 0;
            }
            if (parsed && returnInt <= 0)
            {
                errorString = keepCharacters + " cannot be 0 or less dice";
                return 0;
            }

            return returnInt;
        }

        public void RecordDiceRoll(DiceRoll diceRoll, MessageAddress address)
        {
            string channelKey = address.GetChannelKey();
            if (!string.IsNullOrEmpty(address.GetChannelKey()))
            {
                LastRolls.RemoveAll(a => a.Channel.ToLower() == channelKey.ToLower());
                LastRolls.Add(new ChannelDiceRoll() { Channel = channelKey.ToLower(), Character = address.character, DiceRoll = diceRoll });
            }
        }

        public DiceRoll GetLastDiceRoll(MessageAddress address)
        {
            ChannelDiceRoll cdr = LastRolls.FirstOrDefault(a => a.Channel.ToLower() == address.GetChannelKey().ToLower());
            return cdr == null? null : cdr.DiceRoll;
        }

        public ChannelDiceRoll GetLastChannelDiceRoll(MessageAddress address)
        {
            ChannelDiceRoll cdr = LastRolls.FirstOrDefault(a => a.Channel.ToLower() == address.GetChannelKey().ToLower());
            return cdr;
        }

        // Currency version of SpinSlots: bet/win in a named Chateau currency against the virtual
        // minting house. The jackpot is tracked persistently per (machine, currency) so it grows
        // from everything lost to that machine in that currency, and resets when won.
        public string SpinSlotsCurrency(SlotsSetting slotsSetting, MessageAddress address, int betMultiplier, string currency, FChatDicebot.BotCommands.SlotsTestCommand testCommand)
        {
            int betAmount = betMultiplier * slotsSetting.MinimumBet;
            if (betAmount < slotsSetting.MinimumBet)
                betAmount = slotsSetting.MinimumBet;
            int rewardMultiplier = betMultiplier;

            if (betAmount <= 0)
                return "Error: Slots requires a bet greater than 0.";

            int held = WagerBank.BalanceOf(address.character, currency);
            if (held < betAmount)
                return "Error: " + TextFormat.GetCharacterUserTags(address.character) + " does not have enough " + currency + " (" + betAmount + " needed, " + held + " held).";

            // Load this machine's running jackpot. Every currency played on this machine grows
            // its own column independently (never converted), but a jackpot win sweeps every
            // other currency's column into the payout as a bonus bag — see below.
            string jackpotKey = slotsSetting.Name;
            Dictionary<string, int> jackpotAmounts = MonDB.GetDatabase().GetSlotsJackpotAmounts(jackpotKey);
            int storedJackpot = jackpotAmounts.TryGetValue(currency, out int storedForCurrency) ? storedForCurrency : 0;
            int jackpot = storedJackpot >= slotsSetting.StartingJackpotAmount ? storedJackpot : slotsSetting.StartingJackpotAmount;

            var result = slotsSetting.GetSpinResult(random, betAmount, rewardMultiplier, jackpot, testCommand);

            // Stake to the house, winnings minted back — both in the bet currency.
            WagerBank.Burn(address.character, currency, betAmount);
            if (result.Winnings > 0)
                WagerBank.PayWinner(address.character, currency, result.Winnings);

            string bagText = "";
            if (result.WonJackpot)
            {
                // Sweep every other currency's accumulated total on this machine into the win as
                // a bonus bag, paid out in each of those currencies as-is (no conversion), then
                // clear the whole machine's jackpot — every currency floors back to the starting
                // amount on its next spin.
                var bonusBag = new Dictionary<string, int>();
                foreach (var entry in jackpotAmounts)
                {
                    if (entry.Key == currency || entry.Value <= 0) continue;
                    bonusBag[entry.Key] = entry.Value;
                }
                foreach (var entry in bonusBag)
                    WagerBank.PayWinner(address.character, entry.Key, entry.Value);
                if (bonusBag.Count > 0)
                    bagText = " Plus " + Wager.WagerGameSupport.DisplayBag(bonusBag) + " spills out of the machine!";

                MonDB.GetDatabase().SetSlotsJackpotAmounts(jackpotKey, new Dictionary<string, int>());
            }
            else
            {
                // Persist this currency's new running total (grows from losses).
                jackpotAmounts[currency] = result.NewJackpotAmount;
                MonDB.GetDatabase().SetSlotsJackpotAmounts(jackpotKey, jackpotAmounts);
            }

            int net = result.Winnings - betAmount;
            string betBonusString = betMultiplier > 1 ? "(x" + betMultiplier + ")" : "";
            string netString = "\n[sub]Net " + (net >= 0 ? "+" : "") + net + " " + currency + ".[/sub]";
            string slotsSpinText = TextFormat.Emoji("dbslots1") + TextFormat.Emoji("dbslots2") + " " + TextFormat.GetCharacterIconTags(address.character) + " is spinning the [b]" + slotsSetting.Name + "[/b] slot machine! " + betBonusString + "\n[sub]Putting in " + betAmount + " " + currency + " and pulling the lever...[/sub] " + result.GetJackpotString(currency);
            return slotsSpinText + "\n" + result.ToString() + bagText + netString;
        }

        public string SpinSlots(SlotsSetting slotsSetting, MessageAddress address, int betMultiplier, FChatDicebot.BotCommands.SlotsTestCommand testCommand)
        {
            ChipPile characterChips = GetChipPile(address);
            ChipPile currentJackpot = GetChipPile(new MessageAddress()
            { character = slotsSetting.Name + JackpotSuffix, channel = address.channel, guild = address.guild });
                //slotsSetting.Name + JackpotSuffix, channel);
            if (currentJackpot.Chips < slotsSetting.StartingJackpotAmount)
                currentJackpot.Chips = slotsSetting.StartingJackpotAmount;

            int betAmount = betMultiplier * slotsSetting.MinimumBet;
            if(betAmount < slotsSetting.MinimumBet)
            {
                betAmount = slotsSetting.MinimumBet;
            }
            int rewardMultiplier = betMultiplier;

            if(characterChips == null)
                return "Error: " + BotMain.CurrencyPlaceholderCapital + "s pile not found for " + TextFormat.GetCharacterUserTags(address.character);
            if (betAmount <= 0)
                return "Error: Slots requires a bet greater than 0.";
            if (characterChips.Chips < betAmount)
                return "Error: " + TextFormat.GetCharacterUserTags(address.character) + " does not have sufficient " + BotMain.CurrencyPlaceholder + "s (" + betAmount + ") in their " + BotMain.CurrencyPlaceholder + "s pile (" + characterChips.Chips + ")";

            characterChips.Chips -= betAmount;

            var result = slotsSetting.GetSpinResult(random, betAmount, rewardMultiplier, currentJackpot.Chips, testCommand);

            characterChips.Chips += result.Winnings;
            currentJackpot.Chips = result.NewJackpotAmount;

            string newChips = DisplayChipPile(address, characterChips);
            string betBonusString = betMultiplier > 1 ? "(x" + betMultiplier + ")" : "";
            string slotsSpinText = TextFormat.Emoji("dbslots1") + TextFormat.Emoji("dbslots2") + " " + TextFormat.GetCharacterIconTags(address.character) + " is spinning the [b]" + slotsSetting.Name + "[/b] slot machine! " + betBonusString + "\n[sub]Putting in " + betAmount + " " + BotMain.CurrencyPlaceholder + "s and pulling the lever...[/sub] " + result.GetJackpotString(BotMain.CurrencyPlaceholder + "s");
            return slotsSpinText + "\n" + result.ToString();
        }

        public DrawCardResult DrawCards(int numberDraws, bool includeJoker, bool fromDeck, DeckType deckType, string deckTypeId, MessageAddress address, bool secretDraw, DeckType fromExtraDecktype, string extraDeckId)//, out string trueDraw)
        {
            SavedData.ChannelSettings channelSettings = botMain.GetChannelSettings(address);

            if (numberDraws <= 0)
                numberDraws = 1;

            if (numberDraws > MaximumDrawSize)
                numberDraws = MaximumDrawSize;

            string totalDrawString = "";
            string trueDraw = "";

            DeckType deckTypeDrawnFrom = deckType;
            string deckTypeIdUsed = deckTypeId;
            if(fromExtraDecktype != DeckType.NONE)
            {
                deckTypeDrawnFrom = fromExtraDecktype;
                deckTypeIdUsed = extraDeckId;
            }
            Deck relevantDeck = GetDeck(address, deckTypeDrawnFrom, deckTypeIdUsed);//just for checking help

            DrawCardResult result = new DrawCardResult() { Address = address, OutputString = totalDrawString, TrueDraw = trueDraw, DeckType = deckType };

            if(relevantDeck == null)
            {
                result.OutputString = "Failed: Deck not found " + deckTypeDrawnFrom + " " + deckTypeIdUsed;
                return result;
            }
            string helpOutput = (relevantDeck.GetNumberCardsDrawn() == 0 && secretDraw)? " [sub]note: you can use the 'reveal' parameter to reveal your draws in the channel.[/sub]" : "";

            Hand thisCharacterHand = GetHand(deckType, deckTypeId, address, null);

            int actualDraws = 0;

            List<DeckCard> cardsDrawn = new List<DeckCard>();
            for(int i = 0; i < numberDraws; i++)
            {
                if (!string.IsNullOrEmpty(totalDrawString))
                    totalDrawString += ", ";

                if(thisCharacterHand.CardsCount() >= MaximumHandSize)
                {
                    totalDrawString += "(maximum hand size reached!)";
                    break;
                }

                DeckCard d = DrawCard(random, includeJoker, fromDeck, address, deckTypeDrawnFrom, deckTypeIdUsed);

                if(d!= null)
                {
                    cardsDrawn.Add(d);
                    thisCharacterHand.AddCard(d, random);
                    actualDraws++;

                    totalDrawString += d.Print(channelSettings.CardPrintSetting);
                }
                else
                {
                    totalDrawString += "(out of cards)";
                    break;
                }
            }

            trueDraw = totalDrawString + "";
            if(secretDraw && actualDraws > 0)
            {
                string cardsS = "";
                if (actualDraws > 1 || actualDraws == 0)
                    cardsS = "s";

                totalDrawString = actualDraws + " card" + cardsS;
            }

            //true draw is what is drawn, rather than the output (which might hide draws).
            string collectionName = thisCharacterHand.GetCollectionName();
            if (collectionName != null && collectionName.Count() > 2)
                collectionName = collectionName.Substring(0, 1).ToUpper() + collectionName.Substring(1);//capitalize first in collection name

            trueDraw += "\n[i]Current " + collectionName + ":[/i] " + thisCharacterHand.Print(false, channelSettings.CardPrintSetting);
            totalDrawString += "\n[i]Current " + collectionName + ":[/i] " + thisCharacterHand.Print(secretDraw, channelSettings.CardPrintSetting) + helpOutput;

            result.TrueDraw = trueDraw;
            //result.totaldraw
            result.Cards = cardsDrawn;
            result.OutputString = totalDrawString;
            return result;
            //return totalDrawString;
        }

        public string ViewEntireDeck(MessageAddress address, PrintSetting printSetting, DeckType deckType, string customDeckName)
        {
            Deck d = GetDeck(address, deckType, customDeckName);
            return d.GetDeckList(printSetting);
        }

        public string DiscardCards(List<int> discardsList, bool all, DeckType deckType, string deckTypeId, MessageAddress address, out int actualDiscards)
        {
            return MoveCards(discardsList, all, false, deckType, deckTypeId, address, CardPileId.Hand, CardPileId.Discard, out actualDiscards);
        }

        public string DiscardCardsFromPlay(List<int> discardsList, bool all, DeckType deckType, string deckTypeId, MessageAddress address, out int actualDiscards)
        {
            return MoveCards(discardsList, all, false, deckType, deckTypeId, address, CardPileId.Play, CardPileId.Discard, out actualDiscards);
        }

        public string PlayCards(List<int> playCardsList, bool all, DeckType deckType, string deckTypeId, MessageAddress address, out int actualPlayedCount)
        {
            return MoveCards(playCardsList, all, false, deckType, deckTypeId, address, CardPileId.Hand, CardPileId.Play, out actualPlayedCount);
        }

        public string PlayCardsFromDiscard(List<int> playCardsList, bool all, DeckType deckType, string deckTypeId, MessageAddress address, out int actualPlayedCount)
        {
            return MoveCards(playCardsList, all, false, deckType, deckTypeId, address, CardPileId.Discard, CardPileId.Play, out actualPlayedCount);
        }

        public string MoveCardsFromTo(List<int> playCardsList, bool all,  bool secret, DeckType deckType, string deckTypeId, MessageAddress address,
            CardPileId fromPile, CardPileId toPile, out int actualPlayedCount)
        {
            return MoveCards(playCardsList, all, secret, deckType, deckTypeId, address, fromPile, toPile, out actualPlayedCount);
        }

        private string MoveCards(List<int> moveCardsList, bool all, bool secret, DeckType deckType, string deckTypeId, MessageAddress address,
            CardPileId fromPile, CardPileId toPile,  out int actualMovedCount)
        {
            actualMovedCount = 0;
            SavedData.ChannelSettings channelSettings = botMain.GetChannelSettings(address);

            CardCollection moveFromPile = null;
            CardCollection moveToPile = null;
            switch(fromPile)
            {

                case CardPileId.Hand:
                    moveFromPile = GetHand(deckType, deckTypeId, address, null);
                    break;
                case CardPileId.Play:
                    moveFromPile = GetHand(deckType, deckTypeId, address, PlaySuffix);
                    break;
                case CardPileId.Burn:
                    moveFromPile = GetHand(deckType, deckTypeId, new MessageAddress()
                    {
                        channel = address.channel,
                        guild = address.guild,
                        character = BurnCardsPlayerAlias
                    }, null);
                    break;
                case CardPileId.Discard:
                    moveFromPile = GetHand(deckType, deckTypeId, new MessageAddress()
                    {
                        channel = address.channel,
                        guild = address.guild,
                        character = DiscardPlayerAlias
                    }, null);
                    break;
                case CardPileId.Dealer:
                    moveFromPile = GetHand(deckType, deckTypeId, new MessageAddress()
                    {
                        channel = address.channel,
                        guild = address.guild,
                        character = DealerPlayerAlias
                    }, null);
                    break;
                case CardPileId.Deck:
                    moveFromPile = GetDeck(address, deckType, deckTypeId);
                    break;
                case CardPileId.HiddenInPlay:
                    moveFromPile = GetHand(deckType, deckTypeId, address, HiddenPlaySuffix);
                    break;
            }

            switch (toPile)
            {
                case CardPileId.Hand:
                    moveToPile = GetHand(deckType, deckTypeId, address, null);
                    break;
                case CardPileId.Play:
                    moveToPile = GetHand(deckType, deckTypeId, address, PlaySuffix);
                    break;
                case CardPileId.Burn:
                    moveToPile = GetHand(deckType, deckTypeId, new MessageAddress() {
                        channel = address.channel, guild = address.guild, character = BurnCardsPlayerAlias }, null );
                    break;
                case CardPileId.Discard:
                    moveToPile = GetHand(deckType, deckTypeId, new MessageAddress()
                    {
                        channel = address.channel,
                        guild = address.guild,
                        character = DiscardPlayerAlias
                    }, null);
                    break;
                case CardPileId.Dealer:
                    moveToPile = GetHand(deckType, deckTypeId, new MessageAddress()
                    {
                        channel = address.channel,
                        guild = address.guild,
                        character = DealerPlayerAlias
                    }, null);
                    break;
                case CardPileId.Deck:
                    moveToPile = GetDeck(address, deckType, deckTypeId);
                    break;
                case CardPileId.HiddenInPlay:
                    moveToPile = GetHand(deckType, deckTypeId, address, HiddenPlaySuffix);
                    break;
            }

            if(moveToPile == null)
            {
                return "(Error: moveto pile not found)";
            }
            else if (moveFromPile == null)
            {
                return "(Error: movefrom pile not found)";
            }

            if (moveFromPile.Empty())
            {
                return "(" + Utils.GetPileName(fromPile) + " was empty)";
            }

            if (all)
            {
                moveCardsList = new List<int>();
                for (int i = 0; i < moveFromPile.CardsCount(); i++)
                {
                    moveCardsList.Add(i);
                }
            }

            if (moveCardsList == null || moveCardsList.Count <= 0)
                moveCardsList = new List<int>() { 0 };

            string totalOutputString = "";

            int currentHandSize = moveFromPile.CardsCount();

            List<DeckCard> newHandCards = new List<DeckCard>();
            int cardsMoved = 0;

            for (int i = 0; i < currentHandSize; i++)
            {
                if (moveCardsList.Contains(i))
                {
                    if (!string.IsNullOrEmpty(totalOutputString))
                        totalOutputString += ", ";

                    DeckCard d = moveFromPile.GetCardAtIndex(i);
                    if(!secret)
                    {
                        totalOutputString += d.Print(channelSettings.CardPrintSetting);
                    }
                    cardsMoved++;
                    moveToPile.AddCard(d, random);
                }
                else
                {
                    newHandCards.Add(moveFromPile.GetCardAtIndex(i));
                }
            }

            if (secret)
                totalOutputString += cardsMoved + " cards";

            moveFromPile.Reset();

            foreach (DeckCard d in newHandCards)
            {
                moveFromPile.AddCard(d, random);
            }

            actualMovedCount = currentHandSize - moveFromPile.CardsCount();

            totalOutputString += "\n[i]Current " + moveFromPile.GetCollectionName() + ": [/i]" + moveFromPile.Print(true, channelSettings.CardPrintSetting);
            totalOutputString += "\n[i]Current " + moveToPile.GetCollectionName() + ": [/i]" + moveToPile.Print(true, channelSettings.CardPrintSetting);
            
            return totalOutputString;
        }

        public string TakeCardsFromPlay(List<int> drawFromPlayList, bool all, DeckType deckType, string deckTypeId, MessageAddress address, out int movedCount)
        {
            return MoveCards(drawFromPlayList, all, false, deckType, deckTypeId, address, CardPileId.Play, CardPileId.Hand, out movedCount);
        }

        public string TakeCardsFromDiscard(List<int> drawFromDiscardList, bool all, DeckType deckType, string deckTypeId, MessageAddress address, out int movedCount)
        {
            return MoveCards(drawFromDiscardList, all, false, deckType, deckTypeId, address, CardPileId.Discard, CardPileId.Hand, out movedCount);
        }

        public string GetRollTableResult(List<SavedRollTable> savedTables, string tableName, MessageAddress address, int rollModifier, bool includeLabel, bool includeSecondaryRolls, int dataX, int dataY, int dataZ, int callDepth)
        {
            SavedRollTable savedTable = Utils.GetTableFromId(savedTables, tableName);
            if (savedTable == null || savedTable.Table == null)
            {
                return "Table " + tableName + " not found.";
            }

            RollTable table = savedTable.Table;

            if(table != null)
            {
                TableRollResult res = table.GetRollResult(random, rollModifier, includeLabel, includeSecondaryRolls);
                ChannelSettings settings = botMain.GetChannelSettings(address);

                string returnString = res.ResultString;
                returnString = returnString.Replace("#x", dataX + "");
                returnString = returnString.Replace("#y", dataY + "");
                returnString = returnString.Replace("#z", dataZ + "");
                string addition = "";

                if(res.TriggeredRolls != null && res.TriggeredRolls.Count > 0 && callDepth > 0)
                {
                    if(callDepth > 0)
                    {
                        foreach (TableRollTrigger trt in res.TriggeredRolls)
                        {
                            if(string.IsNullOrEmpty(trt.Command))
                            {
                                continue;
                            }
                            string thisCommand = trt.Command.Replace("#x", dataX + "");
                            thisCommand = thisCommand.Replace("#y", dataY + "");
                            thisCommand = thisCommand.Replace("#z", dataZ + "");

                            if (thisCommand.StartsWith("!"))
                            {
                                if (thisCommand.StartsWith("!rolltable"))
                                {
                                    string commandName = "";
                                    string[] rawTerms = botMain.SeparateCommandTerms(thisCommand, out commandName);

                                    string[] terms = Utils.LowercaseStrings(rawTerms);

                                    string rollResult = BotCommands.RollTable.ParseCommandsAndRoll(botMain, botMain.BotCommandController, terms, address, callDepth - 1);

                                    addition += "\n" + rollResult;
                                }
                                else if (thisCommand.StartsWith("!roll "))
                                {
                                    string commandName = "";
                                    string[] commandTerms = botMain.SeparateCommandTerms(thisCommand, out commandName);

                                    string rollResult = GetRollResult(commandTerms, address, false);

                                    addition += "\n" + rollResult;
                                }
                                else if (thisCommand.StartsWith("!generatepotion"))
                                {
                                    string commandName = "";
                                    string[] rawTerms = botMain.SeparateCommandTerms(thisCommand, out commandName);
                                    
                                    string[] terms = Utils.LowercaseStrings(rawTerms);

                                    GeneratePotionResult potionResult = GeneratePotion(terms, settings, address, false); //don't send private message?
                                    //string rollResult = GeneratePotion(terms, settings, address, false, out privateMessage); //don't send private message?
                                    string rollResult = potionResult.OutputString;
                                    addition += "\n" + rollResult;
                                }
                                else if (thisCommand.StartsWith("!drawcard"))
                                {
                                    string commandName = "";
                                    string[] rawTerms = botMain.SeparateCommandTerms(thisCommand, out commandName);

                                    string[] terms = Utils.LowercaseStrings(rawTerms);

                                    CardCommandOptions options = new CardCommandOptions(botMain.BotCommandController, terms, address.character);

                                    int numberDrawn = Utils.GetNumberFromInputs(terms);
                                    if (numberDrawn > 1)
                                        options.cardsS = "s";

                                    string customDeckName = options.deckTypeId;
                                    string deckTypeString = Utils.GetDeckTypeStringHidePlaying(options.deckType, customDeckName);
                                    string cardDrawingCharacterString = Utils.GetCharacterStringFromSpecialName(options.characterDrawName);

                                    //string trueDraw = "";
                                    var drawResult = DrawCards(numberDrawn, options.jokers, options.deckDraw, options.deckType, options.deckTypeId,
                                        new MessageAddress() { channel = address.channel, guild = address.guild, character = options.characterDrawName }, options.secretDraw, options.fromExtraDeckType, options.extraDeckTypeId);
                                    string messageOutput = "[i]" + cardDrawingCharacterString + ": " + deckTypeString + "Card" + options.cardsS + " drawn:[/i] " + drawResult.ToString();

                                    if (options.secretDraw && !(options.characterDrawName == DiceBot.DealerPlayerAlias || options.characterDrawName == DiceBot.BurnCardsPlayerAlias || options.characterDrawName == DiceBot.DiscardPlayerAlias))
                                    {
                                        string playerMessageOutput = "[i]" + cardDrawingCharacterString + ": " + deckTypeString + "Card" + options.cardsS + " drawn:[/i] " + drawResult.TrueDraw;// trueDraw;
                                        botMain.SendPrivateMessage(playerMessageOutput, address);
                                    }

                                    addition += "\n" + messageOutput;
                                }
                            }
                            else
                            {
                                //nothing - no command present (was roll table here)
                            }

                        }
                    }
                    else
                    {
                        addition = "\n(maximum call depth exceeded!)";
                    }
                }

                return returnString + addition;
            }
            else
            {
                return "(savedTable inner table not found for " + tableName + ")";
            }
        }

        public DeckCard DrawCard(System.Random random, bool includeJoker, bool fromDeck, MessageAddress address, DeckType deckType, string customDeckName)
        {
            DeckCard rtnCard = new DeckCard();

            if (fromDeck)
            {
                Deck thisDeck = GetDeck(address, deckType, customDeckName);
                if (thisDeck != null)
                {
                    DeckCard drawn = thisDeck.DrawCard();

                    if (drawn != null)
                        drawn.cardState = GetRandomCardStateForDraw(random, thisDeck.DeckType);

                    rtnCard = drawn;
                }
                else
                    return null;
            }
            else
            {
                int suite = random.Next(4);
                int number = random.Next(13) + 1;
                int joker = random.Next(54);

                rtnCard.suit = suite;
                rtnCard.number = number;
                rtnCard.joker = (joker < 2 && includeJoker);
            }

            return rtnCard;
        }

        public string GetRandomCardStateForDraw(System.Random rnd, DeckType deckType)
        {
            string cardState = null;
            switch (deckType)
            {
                case DiceFunctions.DeckType.Playing:
                case DiceFunctions.DeckType.ManyThings:
                    break;
                case DiceFunctions.DeckType.Tarot:
                    if (Utils.Percentile(rnd, 50))
                        cardState = "↑";// "Upright";
                    else
                        cardState = "↓";// "Inverted"; //TODO: add option for these in word form instead of symbol by channel setting
                    break;
            }

            return cardState;
        }

        public void ShuffleDeck(Random r, MessageAddress address, PrintSetting printSetting, DeckType deckType, bool fullShuffle, string customDeckName)
        {
            Deck relevantDeck = GetDeck(address, deckType, customDeckName);

            if (relevantDeck != null)
            {
                if (fullShuffle)
                {
                    EndHand(address, false, printSetting, deckType, customDeckName);
                    relevantDeck.ShuffleFullDeck(r);
                }
                else
                    relevantDeck.ShuffleRemainingDeck(r);
            }
        }

        public void ResetDeck(bool jokers, int deckCopies, MessageAddress address, PrintSetting printSetting, DeckType deckType, string customDeckName)
        {
            Deck relevantDeck = GetDeck(address, deckType, customDeckName);

            if(relevantDeck != null)
            {
                EndHand(address, false, printSetting, deckType, customDeckName, true);
                SavedDeck d = null;
                if(deckType == DeckType.Custom)
                    d = Utils.GetDeckFromId(botMain.SavedDecks, customDeckName);

                if(deckCopies < 1)
                    deckCopies = 1;
                for (int currentCopy = 1; currentCopy <= deckCopies; currentCopy++)
                {
                    relevantDeck.FillDeck(jokers, currentCopy, d);
                }

                relevantDeck.ResetCardStates();
                relevantDeck.ShuffleFullDeck(random);
            }
        }

        public Deck GetDeck(MessageAddress address, DeckType deckType, string customDeckName)
        {
            string deckKey = GetDeckKey(address, deckType, customDeckName);
            Deck thisDeck = ChannelDecks.FirstOrDefault(a => a.Id == deckKey);
            if (thisDeck == null)
            {
                thisDeck = new Deck(deckType);
                thisDeck.Id = deckKey;

                if(deckType == DeckType.Custom)
                {
                    SavedDeck d = Utils.GetDeckFromId(botMain.SavedDecks, customDeckName);
                    if (d == null)
                        return null;

                    thisDeck.FillDeck(false, 1, d);
                }
                else
                {
                    thisDeck.FillDeck(false, 1);
                }

                thisDeck.ShuffleFullDeck(random);
                ChannelDecks.Add(thisDeck);
            }

            return thisDeck;
        }

        public void RemoveDeck(Deck removal)
        {
            ChannelDecks.Remove(removal);

        }

        public string EndHand(MessageAddress address, bool reveal, PrintSetting printSettings, DeckType deckType, string deckTypeId, bool deleteCards = false)
        {
            List<Hand> thisChannelHands = GetChannelHands(address, deckType, deckTypeId);

            int totalMoved = 0;
            int totalPiles = 0;

            Hand burnedCards = GetHand(deckType, deckTypeId, address, BurnCardsPlayerAlias);
            string allCardsMoved = "";
            foreach(Hand h in thisChannelHands)
            {
                if(deleteCards)
                {
                    h.Reset();
                }
                else if(h.GetCollectionName() != BotMain.BurnCollectionName)
                {
                    int currentHandSize = h.CardsCount();
                    totalMoved += currentHandSize;
                    if(currentHandSize > 0)
                        totalPiles++;

                    List<DeckCard> newHandCards = new List<DeckCard>();
                    int cardsMoved = 0;

                    string movedHand = h.GetCollectionString(false, printSettings);
                    allCardsMoved += movedHand;

                    for (int i = 0; i < currentHandSize; i++)
                    {
                        DeckCard d = h.GetCardAtIndex(i);
                        //if (!string.IsNullOrEmpty(allCardsMoved))
                        //    allCardsMoved += ", ";
                        //allCardsMoved += d.Print(printSettings);
                        cardsMoved++;
                        burnedCards.AddCard(d, random);
                    }

                    h.Reset();
                }
            }

            if (!reveal)
                allCardsMoved = totalMoved + "";

            string sss = totalPiles != 1 ? "s" : "";
            return "Hand Ended. Moved " + allCardsMoved + " cards from " + totalPiles + " pile" + sss + " to burn pile.";
        }

        public List<InventoryItem> GetItemsOwned(MessageAddress address)
        {
            CharacterData thisCharacter = GetCharacterData(address, false);

            if (thisCharacter != null)
                return thisCharacter.Inventory;
            else
                return null;
        }

        public Potion GetPotionHeld(MessageAddress address)
        {
            List<InventoryItem> items = GetItemsOwned(address);
            if(items.Count > 0)
            {
                InventoryItem own = items.FirstOrDefault(b => b.GetItemCategory() == ItemCategory.Potion);
                if (own != null)
                    return (Potion) own;
            }
            return null;
        }

        public void SetPotionHeld(MessageAddress address, Potion potion)
        {
            CharacterData data = GetCharacterData(address);
            List<InventoryItem> items = data.Inventory;
            if (items.Count > 0)
            {
                InventoryItem own = items.FirstOrDefault(b => b.GetItemCategory() == ItemCategory.Potion);
                if (own != null)
                    items.Remove(own);
            }

            items.Add(potion);
        }

        public bool RemovePotionHeld(MessageAddress address)
        {
            CharacterData data = GetCharacterData(address);
            List<InventoryItem> items = data.Inventory;
            if (items.Count > 0)
            {
                InventoryItem own = items.FirstOrDefault(b => b.GetItemCategory() == ItemCategory.Potion);
                if (own != null)
                {

                    items.Remove(own);
                    return true;
                }
            }
            return false;
        }

        public Hand GetHand(DeckType deckType, string deckTypeId, MessageAddress address, string characterSuffix)
        {
            string deckKey = GetDeckKey(address, deckType, deckTypeId);
            string characterHandName = address.character + characterSuffix;
            Hand h = Hands.FirstOrDefault(a => a.Id == deckKey && a.Character == characterHandName);
            if (h == null)
            {
                h = new Hand();
                h.Id = deckKey;
                h.Character = characterHandName;

                if (characterHandName == DiscardPlayerAlias)
                    h.SetCollectionName(BotMain.DiscardCollectionName);
                if (characterHandName.Contains(PlaySuffix))
                    h.SetCollectionName(BotMain.InPlayCollectionName);
                if (characterHandName.Contains(HiddenPlaySuffix))
                    h.SetCollectionName(BotMain.HiddenInPlayCollectionName);
                if (characterHandName == BurnCardsPlayerAlias)
                    h.SetCollectionName(BotMain.BurnCollectionName);

                Hands.Add(h);
            }

            return h;
        }

        public List<Hand> GetChannelHands(MessageAddress address, DeckType deckType, string deckTypeId)
        {
            string deckKey = GetDeckKey(address, deckType, deckTypeId);
            return Hands.Where(a => a.Id == deckKey).ToList();
        }

        public List<ChipPile> GetChannelChipPiles(MessageAddress address)
        {
            string channelKey = address.GetChannelKey();
            return ChipPiles.Where(a => a.ChannelId.ToLower() == channelKey.ToLower()).ToList();
        }

        public string ListAllChipPiles(MessageAddress address, int startFromNumber = 0)
        {
            string rtnString = "";
            string channelKey = address.GetChannelKey();
            string openingSegment = "Showing all " + BotMain.CurrencyPlaceholder + " piles in " + channelKey + ": ";
            List<ChipPile> channelPiles = GetChannelChipPiles(address);

            if(channelPiles.Count == 0 || startFromNumber > channelPiles.Count)
            {
                rtnString = "No piles found.";
            }
            else
            {
                int count = 1;
                foreach(ChipPile p in channelPiles)
                {
                    if (count >= startFromNumber)
                    {
                        if (!SpecialCharacterName(p.Character))
                        {
                            if (!string.IsNullOrEmpty(rtnString))
                                rtnString += ", ";
                            string charString = GetCharacterNameString(p.Character);
                            rtnString += charString + ": " + p.Chips;
                        }
                    }
                    count++;
                }
            }

            return openingSegment + rtnString;
        }

        public string ListTopNChipPiles(MessageAddress address, int topNumber)
        {
            string rtnString = "";
            string channelKey = address.GetChannelKey();
            string openingSegment = "Showing top " + topNumber + " " + BotMain.CurrencyPlaceholder + " piles in " + channelKey + ": ";
            List<ChipPile> channelPiles = GetChannelChipPiles(address);

            if (channelPiles.Count == 0)
            {
                rtnString = "No piles found.";
            }
            else
            {
                List<ChipPile> channelPilesSorted = channelPiles.Where(b => !SpecialCharacterName(b.Character)).OrderByDescending(a => a.Chips).ToList();

                int countShown = 0;
                foreach (ChipPile p in channelPilesSorted)
                {
                    if (countShown < topNumber)
                    {
                        countShown++;

                        if (!string.IsNullOrEmpty(rtnString))
                            rtnString += ", ";
                        string charString = "#" + countShown + " " + GetCharacterNameString(p.Character);
                        rtnString += charString + ": " + p.Chips;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return openingSegment + rtnString;
        }


        public string AddChips(MessageAddress address, int amount, bool pot)
        {
            ChipPile characterPile = GetChipPile(address);// characterName, channelId);
            ChipPile potPile = GetChipPile(new MessageAddress() {
                channel = address.channel, guild = address.guild, character = PotPlayerAlias });
                //PotPlayerAlias, channelId);
            string characterNameUsed = TextFormat.GetCharacterUserTags(address.character);
            if(pot)
            {
                potPile.Chips += amount;
                characterNameUsed = "[b]the pot[/b]";

                if (potPile.Chips < 0)
                    potPile.Chips = 0;
            }
            else
            {
                characterPile.Chips += amount;

                if (characterPile.Chips < 0)
                    characterPile.Chips = 0;
            }

            string addedAmount = amount + " " + BotMain.CurrencyPlaceholder + "s were added to ";
            if (amount < 0)
                addedAmount = (amount * -1) + " " + BotMain.CurrencyPlaceholder + "s were removed from ";

            return addedAmount + characterNameUsed + "'s pile.";
        }

        public string RemoveChipsPile(MessageAddress address)//string characterName, string channelId)
        {
            ChipPile characterPile = GetChipPile(address, false);
            if(characterPile == null)
            {
                return "No pile of " + BotMain.CurrencyPlaceholder + "s was found for " + TextFormat.GetCharacterUserTags(address.character) + ".";
            }
            else
            {
                int amt = characterPile.Chips;
                //string channelKey = address.GetChannelKey();
                int before = ChipPiles.Count;
                ChipPiles.Remove(characterPile);
                    //.RemoveAll(a => a.Character.ToLower() == address.character.ToLower() && a.ChannelId.ToLower() == channelKey.ToLower());
                int after = ChipPiles.Count;
                if(after < before)
                    return TextFormat.GetCharacterUserTags(address.character) + "'s pile of " + BotMain.CurrencyPlaceholder + "s was removed. (" + amt + ")";
                else
                    return "Error: " + BotMain.CurrencyPlaceholderCapital + " pile for " + TextFormat.GetCharacterUserTags(address.character) + " was found but not removed! (" + amt + ")";
            }
        }

        public string RemoveAllChipsPiles(MessageAddress address)// string characterName, string channelId)
        {
            string channelPiles = ListAllChipPiles(address);
            string channelKey = address.GetChannelKey();
            int pilesCount = ChipPiles.Count(a => a.ChannelId.ToLower() == channelKey.ToLower());
            ChipPiles.RemoveAll(a => a.ChannelId.ToLower() == channelKey.ToLower());

            return TextFormat.GetCharacterUserTags(address.character) + " removed all the channel piles of " + BotMain.CurrencyPlaceholder + "s. (" + pilesCount + "): " + channelPiles;
        }

        public string BetChips(MessageAddress address, int amount, bool all)
        {
            ChipPile characterPile = GetChipPile(address);
            ChipPile potPile = GetChipPile(new MessageAddress() {
                channel = address.channel, guild = address.guild, character = PotPlayerAlias });
                //PotPlayerAlias, channelId);
            int moved = MoveChipsFromPile(characterPile, potPile, amount, all);

            string allInString = characterPile.Chips == 0 && moved > 0 ? " [color=red][b](ALL IN)[/b][/color]" : "";
            return TextFormat.GetCharacterUserTags(address.character) + " added " + moved + " " + BotMain.CurrencyPlaceholder + "s to the pot." + allInString;
        }

        public string GiveChips(MessageAddress fromAddress, string targetCharacterName, int amount, bool all, bool overrideEmpty = false)
            //string characterName, string targetCharacterName, string channelId, int amount, bool all, bool overrideEmpty = false)
        {
            ChipPile characterPile = GetChipPile(fromAddress);
            MessageAddress targetCharacterAddress = new MessageAddress() { character = targetCharacterName, channel = fromAddress.channel, guild = fromAddress.guild };
            ChipPile targetPile = GetChipPile(targetCharacterAddress, false);

            bool specialCharacterTarget = SpecialCharacterName(targetCharacterName);
            if (targetPile == null)
            {
                if (specialCharacterTarget || overrideEmpty)
                {
                    targetPile = GetChipPile(targetCharacterAddress, true);
                }
                else
                {
                    return TextFormat.GetCharacterUserTags(targetCharacterName) + " must have a " + BotMain.CurrencyPlaceholder + "s pile (by typing [b]!register[/b]) before giving them " + BotMain.CurrencyPlaceholder + "s.";
                }
            }

            int moved = MoveChipsFromPile(characterPile, targetPile, amount, all);

            string characterString = SpecialCharacterName(fromAddress.character) ? Utils.GetCharacterStringFromSpecialName(fromAddress.character) : TextFormat.GetCharacterUserTags(fromAddress.character);
            string allInString = characterPile.Chips == 0 && moved > 0 ? " [color=red][b](100%)[/b][/color]" : "";
            string targetString = specialCharacterTarget ? Utils.GetCharacterStringFromSpecialName(targetCharacterName) : TextFormat.GetCharacterUserTags(targetCharacterName);
            return characterString + " gave " + moved + " " + BotMain.CurrencyPlaceholder + "s to " + targetString + allInString + ".";
        }

        public string TakeChips(MessageAddress sourceAddress, string targetCharacterName, int amount, bool all)
        {
            ChipPile characterPile = GetChipPile(sourceAddress);
            MessageAddress targetCharacterAddress = new MessageAddress() { character = targetCharacterName, channel = sourceAddress.channel, guild = sourceAddress.guild };
            ChipPile targetPile = GetChipPile(targetCharacterAddress, false);

            if (targetPile == null)
            {
                return TextFormat.GetCharacterUserTags(targetCharacterName) + " must have a " + BotMain.CurrencyPlaceholder + "s pile before taking their " + BotMain.CurrencyPlaceholder + "s.";
            }

            int moved = MoveChipsFromPile(targetPile, characterPile, amount, all);

            string allInString = characterPile.Chips == 0 && moved > 0 ? " [color=red][b](100%)[/b][/color]" : "";
            return TextFormat.GetCharacterUserTags(sourceAddress.character) + " took " + moved + " " + BotMain.CurrencyPlaceholder + "s from " + TextFormat.GetCharacterUserTags(targetCharacterName) + " " + allInString;
        }

        public string ClaimPot(MessageAddress address, double portion, int actualNumber = -1)
        {
            ChipPile claimingPile = GetChipPile(address);
            MessageAddress potAddress = new MessageAddress() { character = PotPlayerAlias, channel = address.channel, guild = address.guild };
            ChipPile potPile = GetChipPile(potAddress);
            if(potPile == null || claimingPile == null)
            {
                return "Error: unable to locate " + BotMain.CurrencyPlaceholder + "s pile.";
            }

            int amount = (int) Math.Round(potPile.Chips * portion);

            if(actualNumber > 0)
            {
                amount = actualNumber;
            }

            int moved = MoveChipsFromPile(potPile, claimingPile, amount, false);
            string fractionString = portion == .5 ? "half " : (portion == .333333 ? "a third of " : "");
            if (actualNumber > 0 && potPile.Chips > 0)
                fractionString = "some of ";

            string charString = TextFormat.GetCharacterUserTags(address.character);
            if (SpecialCharacterName(address.character))
            {
                charString = Utils.GetCharacterStringFromSpecialName(address.character);
            }

            string remaining = "";
            if (potPile.Chips > 0)
                remaining = " (" + potPile.Chips + " " + BotMain.CurrencyPlaceholder + "s remain)";
            return charString + " claimed " + fractionString + "the pot ([b]" + moved + " " + BotMain.CurrencyPlaceholder + "s[/b])." + remaining;
        }

        private int MoveChipsFromPile(ChipPile sourcePile, ChipPile destinationPile, int amount, bool all)
        {
            int creditedAmount = 0;
            if (sourcePile == null || destinationPile == null)
                return creditedAmount;

            if(amount >= sourcePile.Chips || all)
            {
                creditedAmount = sourcePile.Chips;
                sourcePile.Chips = 0;
            }
            else
            {
                sourcePile.Chips -= amount;
                creditedAmount = amount;
            }

            destinationPile.Chips += creditedAmount;
            return creditedAmount;
        }

        public string DisplayChipPile(MessageAddress address, ChipPile existing = null)
        {
            if(existing == null)
                existing = GetChipPile(address);

            string charString = GetCharacterNameString(address.character);

            return charString + " " + BotMain.CurrencyPlaceholder + "s pile: [b]" + existing.Chips + "[/b]";
        }

        public ChipPile GetChipPile(MessageAddress address, bool createNew = true)
        {
            string channelKey = address.GetChannelKey();
            ChipPile h = ChipPiles.FirstOrDefault(a => a.ChannelId != null && a.Character != null &&
                a.ChannelId.ToLower() == channelKey.ToLower() && a.Character.ToLower() == address.character.ToLower());

            if (h == null && createNew)
            {
                ChannelSettings thisChannelSettings = botMain.GetChannelSettings(address);
                bool specialAccount = SpecialCharacterName(address.character);
                int startingChipsAmount = thisChannelSettings.StartingChips;

                h = new ChipPile();
                h.ChannelId = channelKey;
                h.Character = address.character;
                h.Chips = specialAccount? 0 : startingChipsAmount;
                ChipPiles.Add(h);
            }

            return h;
        }

        public CharacterData GetCharacterData(MessageAddress address, bool createNew = true)
        {
            CharacterData h = null;
            string channelKey = address.GetChannelKey();
            //xxxxxxxxxxxGetCharacterData_combineguild
            //var x = CharacterDatas.Where(a => a.Channel == null || a.Character == null).ToList();

            if(!string.IsNullOrEmpty(address.character) && !string.IsNullOrEmpty(channelKey))
                h = CharacterDatas.FirstOrDefault(a => a.Channel != null && a.Character != null && a.Channel.ToLower() == channelKey.ToLower() && a.Character.ToLower() == address.character.ToLower());

            if (h == null && createNew)
            {
                bool specialAccount = SpecialCharacterName(address.character);

                h = new CharacterData();
                h.Channel = channelKey;
                h.Character = address.character;
                h.SpecialName = specialAccount;
                h.DiceUnlocked = false;
                h.Inventory = new List<InventoryItem>();
                h.LastSlotsSpin = 0;
                CharacterDatas.Add(h);
            }

            return h;
        }

        public bool SpecialCharacterName(string characterName)
        {
            if (characterName.Contains(JackpotSuffix))
                return true;

            return characterName == PotPlayerAlias || characterName == DealerPlayerAlias || characterName == DiscardPlayerAlias || characterName == BurnCardsPlayerAlias || characterName == HousePlayerAlias;
        }

        public string GetCharacterNameString(string characterName)
        {
            return SpecialCharacterName(characterName) ? Utils.GetCharacterStringFromSpecialName(characterName) : TextFormat.GetCharacterUserTags(characterName);
        }

        public string GetDeckKey(MessageAddress address, DeckType deckType, string deckTypeId)
        {
            return address.GetChannelKey() + "_" + deckType + "_" + deckTypeId;
        }

        public string GetPotionSearchString(string[] terms, string optionalRemoveTerm = null)
        {
            List<string> remainingTerms = terms.Where(a => a != "noflavor" && a != "nonsfw" && a != "requirensfw" && a != "s" && a != "secret").ToList();
            if (!string.IsNullOrEmpty(optionalRemoveTerm))
                remainingTerms = terms.Where(b => b.ToLower() != optionalRemoveTerm.ToLower()).ToList();

            string searchTerm = string.Join(" ", remainingTerms);
            return searchTerm;
        }

        public GeneratePotionResult GeneratePotion(string[] terms, ChannelSettings channelSettings, MessageAddress address, bool includeOrigin)//, out string privateMessage)
        {
            GeneratePotionResult result = new GeneratePotionResult();

            bool allowFlavor = true;
            bool allowLewd = true;
            bool requireLewd = false;
            bool secret = false;
            if (terms != null && terms.Length > 0)
            {
                if (terms.Contains("noflavor"))
                    allowFlavor = false;
                if (terms.Contains("nonsfw"))
                    allowLewd = false;
                if (terms.Contains("requirensfw"))
                    requireLewd = true;
                if (terms.Contains("secret") || terms.Contains("s"))
                    secret = true;
            }

            result.AllowFlavor = allowFlavor;
            result.AllowNsfw = allowLewd;
            result.RequireNsfw = requireLewd;
            result.Secret = secret;

            if (!channelSettings.AllowNsfw)
            {
                allowLewd = false;
                requireLewd = false;
            }

            if (!allowLewd && requireLewd)
            {
                result.OutputString = "Error: AllowNsfw must be enabled in channel settings if you want to require a nsfw " + channelSettings.PotionCommandsAlias + ".";
            }
            else
            {
                string searchTerm = GetPotionSearchString(terms);
                result.SpecificPotionSearch = searchTerm;

                ChannelSettings settings = botMain.GetChannelSettings(address);

                Potion potion = null;

                List<Enchantment> channelPotions = botMain.GetChannelPotions(address);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    potion = GetSpecificPotion(channelPotions, settings.UseDefaultPotions, searchTerm);
                    if (potion != null)
                        result.SpecificPotionGenerated = true;
                }
                else
                {
                    potion = GetRandomPotion(channelPotions, settings.UseDefaultPotions, allowFlavor, allowLewd, requireLewd);
                }

                string output = PotionGenerator.GetPotionGenerationOutputString(potion, includeOrigin);
                if (channelSettings.PotionCommandsAlias != null)
                {
                    output.Replace("potion", channelSettings.PotionCommandsAlias);
                    output.Replace("Potion", TextFormat.CapitalizeFirst(channelSettings.PotionCommandsAlias));
                }
                result.Result = potion;

                bool outputHandled = false;

                if (potion != null && potion.enchantment != null && potion.enchantment.Flag != EnchantmentFlag.NONE)
                {
                    if (potion.enchantment.Flag == EnchantmentFlag.WhisperPotionRevealFake)
                    {
                        secret = true;
                        Potion secondPotion = GetSpecificPotion(null, true, "fluid flavor");
                        if (!allowFlavor)
                            secondPotion = GetSpecificPotion(null, true, "lust");
                        if (!allowLewd)
                            secondPotion = GetSpecificPotion(null, true, "skin color");

                        secondPotion.strength = 1;
                        string outputFake = PotionGenerator.GetPotionGenerationOutputString(secondPotion, includeOrigin);
                        if (channelSettings.PotionCommandsAlias != null)
                        {
                            outputFake.Replace("potion", channelSettings.PotionCommandsAlias);
                            outputFake.Replace("Potion", TextFormat.CapitalizeFirst(channelSettings.PotionCommandsAlias));
                        }
                        SetPotionHeld(address, potion);

                        //privateMessage =
                        //output = outputFake;
                        //outputMessage = output;

                        result.PrivateMessage = output + "\nThe channel has been sent a fake " + channelSettings.PotionCommandsAlias + ". This is your real " + channelSettings.PotionCommandsAlias + " result and it can be shown with !showpotion ";
                        output = outputFake;
                        outputHandled = true;
                    }
                    else if (potion.enchantment.Flag == EnchantmentFlag.RequireRollBondage)
                    {
                        string tableOutput = GetRollTableResult(botMain.SavedTables, "bondage", address, 0, true, true, 0, 0, 0, DiceBot.MaximumSecondaryTableRolls);
                        output += "\n" + tableOutput;
                    }
                    else if (potion.enchantment.Flag == EnchantmentFlag.RequireRollHumiliation)
                    {
                        string tableOutput = GetRollTableResult(botMain.SavedTables, "humiliation", address, 0, true, true, 0, 0, 0, DiceBot.MaximumSecondaryTableRolls);
                        output += "\n" + tableOutput;
                    }
                    else if (potion.enchantment.Flag == EnchantmentFlag.RequireRollSexToy)
                    {
                        string tableOutput = GetRollTableResult(botMain.SavedTables, "sextoy", address, 0, true, true, 0, 0, 0, DiceBot.MaximumSecondaryTableRolls);
                        output += "\n" + tableOutput;
                    }
                    else if (potion.enchantment.Flag == EnchantmentFlag.RequireRollPunishment)
                    {
                        string tableOutput = GetRollTableResult(botMain.SavedTables, "punishment", address, 0, true, true, 0, 0, 0, DiceBot.MaximumSecondaryTableRolls);
                        output += "\n" + tableOutput;
                    }
                    result.OutputString = output;
                }

                if (secret && !outputHandled)
                {
                    SetPotionHeld(address, potion);

                    result.PrivateMessage = output + "\nThis " + channelSettings.PotionCommandsAlias + " is now a secret and can be shown in the same channel with !show" + channelSettings.PotionCommandsAlias + " ";
                    result.OutputString = "A " + channelSettings.PotionCommandsAlias + " was generated in secret and sent to " + TextFormat.GetCharacterUserTags(address.character) + ".";
                }
                else
                    result.OutputString = output;//.PrivateMessage = output;
            }

            //result.OutputString = outputMessage;

            return result;
        }

        public Potion GetRandomPotion(List<Enchantment> channelPotions, bool useDefaultPotions, bool allowFlavor, bool allowNsfw, bool requireNsfw)//todo: make this generator more customizable, and able to store generator(s) to use in the data files
        {
            return PotionGenerator.GeneratePotion(channelPotions, useDefaultPotions, allowFlavor, allowNsfw, requireNsfw);
        }

        public Potion GetSpecificPotion(List<Enchantment> channelPotions, bool useDefaultPotions, string searchTerm)
        {
            return PotionGenerator.GeneratePotionWithSpecificEffect(channelPotions, true, true, false, searchTerm);
        }


        #region game sessions
        public string JoinGame(MessageAddress address, IGame gameType)
        {
            GameSession sesh = GetGameSession(address, gameType, true);
            if (sesh != null)
            {
                sesh.Players.Add(address.character);
                return TextFormat.GetCharacterUserTags(address.character) + " joined " + sesh.CurrentGame.GetGameName() + " successfully.";
            }

            return "";
        }

        public string LeaveGame(MessageAddress address, IGame gameType)
        {
            GameSession sesh = GetGameSession(address, gameType, false);
            if (sesh != null)
            {
                sesh.Players.RemoveAll(a => a == address.character);
                string leftGameNote = sesh.CurrentGame.PlayerLeftGame(botMain, sesh, address.character);

                if (sesh.Players.Count() == 0)
                {
                    leftGameNote += "\n" + CancelGame(address, gameType);
                }

                return TextFormat.GetCharacterUserTags(address.character) + " left " + sesh.CurrentGame.GetGameName() + " successfully. " + leftGameNote;
            }

            return "";
        }

        public string StartGame(MessageAddress address,  string characterExecuting, IGame gameType, BotMain botMain, bool keepSession, bool endSession)
        {
            GameSession sesh = GetGameSession(address, gameType, false);

            bool keepingSession = keepSession || (gameType.KeepSessionDefault() && !endSession);
            string output = "";
            if(sesh != null)
            {
                string runGameString = sesh.RunGame(characterExecuting, this, botMain);

                string endingString = "\n" + sesh.CurrentGame.GetEndingDisplay();
                if(sesh.State == GameState.Finished)
                {
                    if (keepingSession)
                    {
                        sesh.State = GameState.Unstarted;

                        if(sesh.Ante > 0)
                        {
                            List<string> leavePlayers = new List<string>();
                            foreach(string characterName in sesh.Players)
                            {
                                MessageAddress mAdd = new MessageAddress() { character = characterName, channel = address.channel, guild = address.guild };
                                ChipPile p = GetChipPile(mAdd, false);

                                if(sesh.Ante > p.Chips)
                                {
                                    leavePlayers.Add(characterName);
                                }
                            }

                            if(leavePlayers.Count > 0)
                            {
                                string leavingCharacters = Utils.PrintList(leavePlayers) + " removed for being below the minimum ante.";
                                endingString += leavingCharacters;
                                foreach(string chara in leavePlayers)
                                {
                                    MessageAddress mAdd = new MessageAddress() { character = chara, channel = address.channel, guild = address.guild };
                                    LeaveGame(mAdd, gameType);
                                }
                            }
                        }

                        endingString += " [i](game session kept)[/i]";
                    }
                    else
                    {
                        RemoveGameSession(address, gameType);
                        endingString += " [i](game session ended)[/i]";
                    }
                }
                if(sesh.State == GameState.GameInProgress)
                {
                    endingString += " [i](game session in progress)[/i]";
                }

                output = runGameString + endingString;
            }
            else
            {
                output = "Game session not found.";
            }

            return output;
        }

        public string CancelGame(MessageAddress address, IGame gameType)
        {
            GameSession sesh = GetGameSession(address, gameType, false);
            string output = "";
            if (sesh != null)
            {
                if (Wager.WagerGameSupport.IsCurrencyWager(gameType))
                {
                    // Return any committed stakes before tearing the table down.
                    Wager.WagerGameSupport.RefundAll(this, sesh);
                    RemoveGameSession(address, gameType);
                    output = "The " + sesh.CurrentGame.GetGameName() + " game is off.";
                }
                else
                {
                    RemoveGameSession(address, gameType);
                    output = "Session for " + sesh.CurrentGame.GetGameName() + " cancelled.";
                }
            }
            else
            {
                output = "Game session not found.";
            }

            return output;
        }

        public void UpdateAllGames()
        {
            double currentTime = DoubleTime.GetCurrentTimestampSeconds();
            for(int i = 0; i < GameSessions.Count; i++)
            {
                GameSession session = GameSessions[i];
                if(session != null)
                {
                    session.UpdateGame(botMain, currentTime);
                }
            }
        }

        public GameSession GetGameSession(MessageAddress address, IGame gameType, bool createNew = true)
        {
            //string channelKey = address.GetChannelKey();
            string addressChannel = address.channel == null ? null : address.channel.ToLower();
            string addressGuild = address.guild == null ? null : address.guild.ToLower();
            GameSession rtn = GameSessions.FirstOrDefault(a => a.ChannelId.ToLower() == addressChannel
                && a.GuildId == addressGuild
                && a.CurrentGame.GetGameName() == gameType.GetGameName());
            if (rtn == null && createNew)
            {
                rtn = new GameSession();
                rtn.ChannelId = address.channel;// channelKey;
                rtn.GuildId = address.guild;
                rtn.CurrentGame = gameType;
                rtn.Players = new List<string>();
                rtn.State = GameState.Unstarted;
                rtn.CreationTime = DoubleTime.GetCurrentTimestampSeconds();

                GameSessions.Add(rtn);
            }

            return rtn;
        }

        public string RemoveGameSession(MessageAddress address, IGame gameType)
        {
            GameSession g = GetGameSession(address, gameType, false);
            if(g != null)
            {
                string channelKey = address.GetChannelKey();
                GameSessions.RemoveAll(a => a.CurrentGame.GetGameName() == gameType.GetGameName() && a.GetChannelKey() == channelKey);

                return "Game session removed for " + gameType.GetGameName() + ".";
            }

            return "Game session not found for " + gameType.GetGameName() + ".";
        }

        public string IssueGameCommand(MessageAddress address, GameSession session, string[] terms, string[] rawTerms)
        {
            string rtn = session.IssueGameCommand(this, botMain, address, terms, rawTerms);

            return rtn;
        }

        #endregion

        #region countdown timers

        public bool CountdownFinishedOrNotStarted(MessageAddress address, string countdownId)
        {
            CountdownTimer timer = GetCountdownTimer(address, countdownId);
            if(timer == null)
            {
                return true;
            }
            else if (timer.TimerFinished())
            {
                timer.StopTimer();
                CountdownTimers.Remove(timer);
                return true;
            }

            return false;
        }

        public CountdownTimer GetCountdownTimer(MessageAddress address, string countdownId)
        {
            string channelKey = address.GetChannelKey();
            return CountdownTimers.FirstOrDefault(a => a.ChannelId.ToLower() == channelKey.ToLower() && a.TimerId == countdownId);
        }

        public double GetSecondsRemainingOnCountdownTimer(MessageAddress address, string countdownId)
        {
            CountdownTimer timer = GetCountdownTimer(address, countdownId);
            double secondsRemain = 0;

            if (timer != null)
                secondsRemain = timer.GetSecondsRemaining();
            return secondsRemain;
        }

        public void StartCountdownTimer(MessageAddress address, string countdownId, int targetMiliseconds)
        {
            CountdownTimer timer = GetCountdownTimer(address, countdownId);
            if(timer == null)
            {
                CountdownTimer t = new CountdownTimer()
                {
                    ChannelId = address.GetChannelKey(),
                    CharacterId = address.character,
                    TimerId = countdownId,
                    FinishedMs = targetMiliseconds
                };
                t.StartTimer();
                CountdownTimers.Add(t);
            }
        }

        #endregion
    }

    public class DrawCardResult
    {
        public string OutputString;
        public string TrueDraw;
        public List<DeckCard> Cards;
        public DeckType DeckType;
        public MessageAddress Address;

        public override string ToString()
        {
            return OutputString;
        }
    }

    public class GeneratePotionResult
    {
        public string OutputString;
        public bool SpecificPotionGenerated;
        public string SpecificPotionSearch;
        public bool AllowNsfw;
        public bool RequireNsfw;
        public bool AllowFlavor;
        public bool Secret;
        public string PrivateMessage;
        public Potion Result;
    }

    public enum CardMoveType
    {
        NONE,
        DiscardCard,
        PlayCard,
        ToDeckFromHand,
        ToDeckFromPlay,
        ToDeckFromDiscard,
        ToPlayFromDiscard,
        ToDiscardFromPlay,
        ToHandFromDiscard,
        ToHandFromPlay,
        FromPileToPile
    }

    public enum CardPileId
    {
        NONE,
        Hand,
        Play,
        Discard,
        Burn,
        Dealer,
        Deck,
        HiddenInPlay
    }
}
