using FChatDicebot.BotCommands.Base;
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
        public const string DiceBotCharacter = "Dice Bot";

        public List<Deck> ChannelDecks;
        public List<Hand> Hands;
        public List<ChipPile> ChipPiles;
        public List<CharacterData> CharacterDatas;
        public List<GameSession> GameSessions;
        public List<CountdownTimer> CountdownTimers;
        public PotionGenerator PotionGenerator;
        public List<ItemOwned> ItemsOwned;
        public List<ChannelDiceRoll> LastRolls;
        public List<VcChipOrder> VcChipOrders;

        public List<IGame> PossibleGames;

        //characters that might be useful for printouts
        //⚀⚁⚂⚃⚄⚅ 🎲

        public DiceBot(BotMain sourceBot)
        {
            botMain = sourceBot;
            random = new System.Random();

            ChannelDecks = new List<Deck>();
            Hands = new List<Hand>();
            ChipPiles = new List<ChipPile>();
            CharacterDatas = new List<CharacterData>();
            GameSessions = new List<GameSession>();
            CountdownTimers = new List<CountdownTimer>();
            PotionGenerator = new DiceFunctions.PotionGenerator(random);
            ItemsOwned = new List<ItemOwned>();
            LastRolls = new List<ChannelDiceRoll>();
            VcChipOrders = new List<VcChipOrder>();

            PossibleGames = new List<IGame>() { new HighRoll(), new Poker(), new BottleSpin(), new Roulette(), new KingsGame(), new LiarsDice(), new Pokergame(), new SlamRoll(), new Blackjack(), new RockPaperScissors() };

            //LoadChipPilesFromDisk(BotMain.FileFolder, BotMain.SavedChipsFileName);
            //LoadCharacterDataFromDisk(BotMain.FileFolder, BotMain.CharacterDataFileName);
            //LoadVcChipOrderDataFromDisk(BotMain.FileFolder, BotMain.VcChipOrdersFileName);

            //Utils.AddToLog(" ", "FINISHED DICE BOT LOAD :: Chip Piles found " + ChipPiles.Count() + "... Character Datas found " + CharacterDatas.Count());
            //Console.WriteLine("FINISHED DICE BOT LOAD :: Chip Piles found " + ChipPiles.Count() + "... Character Datas found " + CharacterDatas.Count() );
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

        public VcChipOrder GetVcChipOrder(string character, string channel)
        {
            return VcChipOrders.FirstOrDefault(a => a.Character == character && a.ChannelId == channel);
        }

        public bool AddVcChipOrder(int amount, string character, string channel, string vcTransactionId)
        {
            bool contains = VcChipOrders.Count(a => a.TransactionId == vcTransactionId) > 0;

            VcChipOrders.Add(new VcChipOrder() {
                ChannelId = channel,
                Character = character,
                Chips = amount,
                Created = VelvetcuffConnection.ConvertToSecondsTimestamp(DateTime.UtcNow),
                LastCheckedTime =  VelvetcuffConnection.ConvertToSecondsTimestamp(DateTime.UtcNow),
                TransactionId = vcTransactionId,
                CheckedCount = 0,
                OrderStatus = 0
            });
            return !contains;
        }

        public string RollFitD(int diceNumber, string channel, string character)
        {
            int actualRolled = diceNumber <= 0? 2 : diceNumber;
            DiceRoll roll = new DiceRoll()
            {
                DiceRolled = actualRolled,
                DiceSides = 6,
                TextFormat = true
            };
            roll.Roll(random);

            int success = roll.Rolls.Count(a => a == 4 || a == 5);
            int critical = roll.Rolls.Count(a => a == 6);
            int fail = roll.Rolls.Count(a => a < 4);

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

            RecordDiceRoll(roll, character, channel);

            string resultString = string.Format("{0} \nFitD [b]{1}[/b] dice, {2}{3}{4}", roll.ResultString(), diceNumber, failString, successString, criticalString);

            return resultString;
        }

        public string GetRollResult(string[] inputCommands, string character, string channel, bool debugOutput)
        {
            string allCommands = Utils.CombineStringArray(inputCommands);

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
                DiceRoll d = ParseRollFromCommand(s);

                d.Roll(random);

                finishedRolls.Add(d);

                if (totalResultString != "")
                    totalResultString += ", ";
                totalResultString += d.ResultString();
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

                    //OOP first */
                    string applyOperatorErrorString = "";
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
                RecordDiceRoll(finishedRolls.Last(), character, channel);

            return totalResultString;
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
                    error = "integer overflow";
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

        public DiceRoll ParseRollFromCommand(string command)
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
                || command.IndexOf('d') == 0 || command.IndexOf('d') == command.Length -1)
                return new DiceRoll() { Error = true, ErrorString = "invalid dice input" };

            int numberSides = 0;
            int numberDice = 0;
            bool explode = false;

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

            int explodeThreshold = ParseOutDieCommand("!", ref splitDiceSidesAndCommands, out parseError);
            //note: no error for ! on purpose: it is possible to parse out nothing for the number.
            splitDiceSidesAndCommands = splitDiceSidesAndCommands.Replace("!0", "").Replace("!", "");

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
            
            int countOver = ParseOutDieCommand("co", ref splitDiceSidesAndCommands, out parseError);

            if (!string.IsNullOrEmpty(parseError))
                return new DiceRoll() { Error = true, ErrorString = parseError };

            int countUnder = ParseOutDieCommand("cu", ref splitDiceSidesAndCommands, out parseError);

            if (!string.IsNullOrEmpty(parseError))
                return new DiceRoll() { Error = true, ErrorString = parseError };

            bool success2 = int.TryParse(splitDiceSidesAndCommands, out numberSides);


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
            if ((keepHighest >= numberDice) ||
                (keepLowest >= numberDice) ||
                (removeHighest >= numberDice) ||
                (removeLowest >= numberDice)
                )
                return new DiceRoll() { Error = true, ErrorString = "number of dice to choose must be less than total number of dice" };


            return new DiceRoll()
            {
                DiceRolled = numberDice,
                DiceSides = numberSides,
                Error = false,
                Explode = explode,
                ExplodeThreshold = explodeThreshold,
                TextFormat = true,
                KeepHighest = keepHighest,
                KeepLowest = keepLowest,
                RemoveHighest = removeHighest,
                RemoveLowest = removeLowest,
                RerollNumber = rerollNumber,
                CountOver = countOver,
                CountUnder = countUnder
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
        
        public void RecordDiceRoll(DiceRoll diceRoll, string character, string channel)
        {
            LastRolls.RemoveAll(a => a.Channel == channel);
            LastRolls.Add(new ChannelDiceRoll() { Channel = channel, Character = character, DiceRoll = diceRoll });
        }

        public DiceRoll GetLastDiceRoll(string channel)
        {
            ChannelDiceRoll cdr = LastRolls.FirstOrDefault(a => a.Channel == channel);
            return cdr == null? null : cdr.DiceRoll;
        }

        public string SpinSlots(SlotsSetting slotsSetting, string characterName, string channel, int betMultiplier, FChatDicebot.BotCommands.SlotsTestCommand testCommand)
        {
            ChipPile characterChips = GetChipPile(characterName, channel);
            ChipPile currentJackpot = GetChipPile(slotsSetting.Name + JackpotSuffix, channel);
            if (currentJackpot.Chips < slotsSetting.StartingJackpotAmount)
                currentJackpot.Chips = slotsSetting.StartingJackpotAmount;

            int betAmount = betMultiplier * slotsSetting.MinimumBet;
            if(betAmount < slotsSetting.MinimumBet)
            {
                betAmount = slotsSetting.MinimumBet;
            }
            int rewardMultiplier = betMultiplier;

            if(characterChips == null)
                return "Error: Chips pile not found for " + Utils.GetCharacterUserTags(characterName);
            if (betAmount <= 0)
                return "Error: Slots requires a bet greater than 0.";
            if (characterChips.Chips < betAmount)
                return "Error: " + Utils.GetCharacterUserTags(characterName) + " does not have sufficient chips (" + betAmount + ") in their chips pile (" + characterChips.Chips + ")";

            characterChips.Chips -= betAmount;

            var result = slotsSetting.GetSpinResult(random, betAmount, rewardMultiplier, currentJackpot.Chips, testCommand);

            characterChips.Chips += result.Winnings;
            currentJackpot.Chips = result.NewJackpotAmount;

            string newChips = DisplayChipPile(channel, characterName, characterChips);
            string betBonusString = betMultiplier > 1 ? "(x" + betMultiplier + ")" : "";
            string slotsSpinText = "[eicon]dbslots1[/eicon][eicon]dbslots2[/eicon]\n" + Utils.GetCharacterIconTags(characterName) + " is spinning the [b]" + slotsSetting.Name + "[/b] slot machine! " + betBonusString + "\n[sub]Putting in " + betAmount + " chips and pulling the lever...[/sub] " + result.GetJackpotString();
            return slotsSpinText + "\n" + result.ToString();
        }

        public string DrawCards(int numberDraws, bool includeJoker, bool fromDeck, string channelId, DeckType deckType, string character, bool secretDraw, out string trueDraw)
        {
            if (numberDraws <= 0)
                numberDraws = 1;

            if (numberDraws > MaximumDrawSize)
                numberDraws = MaximumDrawSize;

            string totalDrawString = "";
            trueDraw = "";

            string customDeckName = Utils.GetCustomDeckName(character);
            Deck relevantDeck = GetDeck(channelId, deckType, customDeckName);//just for checking help
            
            string helpOutput = (relevantDeck.GetNumberCardsDrawn() == 0 && secretDraw)? " [sub]note: you can use the 'reveal' parameter to reveal your draws in the channel.[/sub]" : "";

            Hand thisCharacterHand = GetHand(channelId, deckType, character);

            int actualDraws = 0;

            for(int i = 0; i < numberDraws; i++)
            {
                if (!string.IsNullOrEmpty(totalDrawString))
                    totalDrawString += ", ";

                if(thisCharacterHand.CardsCount() >= MaximumHandSize)
                {
                    totalDrawString += "(maximum hand size reached!)";
                    break;
                }

                DeckCard d = DrawCard(random, includeJoker, fromDeck, channelId, deckType, customDeckName);
                
                if(d!= null)
                {
                    thisCharacterHand.AddCard(d, random);
                    actualDraws++;

                    totalDrawString += d.ToString();
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

            trueDraw += "\n[i]Current " + collectionName + ": [/i]" + thisCharacterHand.ToString();
            totalDrawString += "\n[i]Current " + collectionName + ": [/i]" + thisCharacterHand.ToString(secretDraw) + helpOutput;

            return totalDrawString;
        }

        public string ViewEntireDeck(string channelId, DeckType deckType, string customDeckName)
        {
            Deck d = GetDeck(channelId, deckType, customDeckName);
            return d.GetDeckList();
        }

        public string DiscardCards(List<int> discardsList, bool all, string channelId, DeckType deckType, string character, out int actualDiscards)
        {
            return MoveCards(discardsList, all, false, channelId, deckType, character, CardPileId.Hand, CardPileId.Discard, out actualDiscards);   
        }

        public string DiscardCardsFromPlay(List<int> discardsList, bool all, string channelId, DeckType deckType, string character, out int actualDiscards)
        {
            return MoveCards(discardsList, all, false, channelId, deckType, character, CardPileId.Play, CardPileId.Discard, out actualDiscards);
        }

        public string PlayCards(List<int> playCardsList, bool all, string channelId, DeckType deckType, string character, out int actualPlayedCount)
        {
            return MoveCards(playCardsList, all, false, channelId, deckType, character, CardPileId.Hand, CardPileId.Play, out actualPlayedCount);   
        }

        public string PlayCardsFromDiscard(List<int> playCardsList, bool all, string channelId, DeckType deckType, string character, out int actualPlayedCount)
        {
            return MoveCards(playCardsList, all, false, channelId, deckType, character, CardPileId.Discard, CardPileId.Play, out actualPlayedCount);
        }

        public string MoveCardsFromTo(List<int> playCardsList, bool all,  bool secret, string channelId, DeckType deckType, string character,
            CardPileId fromPile, CardPileId toPile, out int actualPlayedCount)
        {
            return MoveCards(playCardsList, all, secret, channelId, deckType, character, fromPile, toPile, out actualPlayedCount);
        }

        private string MoveCards(List<int> moveCardsList, bool all, bool secret, string channelId, DeckType deckType, string character, 
            CardPileId fromPile, CardPileId toPile,  out int actualMovedCount)
        {
            actualMovedCount = 0;

            CardCollection moveFromPile = null;
            CardCollection moveToPile = null;
            switch(fromPile)
            {
                case CardPileId.Hand:
                    moveFromPile = GetHand(channelId, deckType, character);
                    break;
                case CardPileId.Play:
                    moveFromPile = GetHand(channelId, deckType, character + PlaySuffix);
                    break;
                case CardPileId.Burn:
                    moveFromPile = GetHand(channelId, deckType, BurnCardsPlayerAlias);
                    break;
                case CardPileId.Discard:
                    moveFromPile = GetHand(channelId, deckType, DiscardPlayerAlias);
                    break;
                case CardPileId.Dealer:
                    moveFromPile = GetHand(channelId, deckType, DealerPlayerAlias);
                    break;
                case CardPileId.Deck:
                    moveFromPile = GetDeck(channelId, deckType, null);
                    break;
                case CardPileId.HiddenInPlay:
                    moveFromPile = GetHand(channelId, deckType, character + HiddenPlaySuffix);
                    break;
            }

            switch (toPile)
            {
                case CardPileId.Hand:
                    moveToPile = GetHand(channelId, deckType, character);
                    break;
                case CardPileId.Play:
                    moveToPile = GetHand(channelId, deckType, character + PlaySuffix);
                    break;
                case CardPileId.Burn:
                    moveToPile = GetHand(channelId, deckType, BurnCardsPlayerAlias);
                    break;
                case CardPileId.Discard:
                    moveToPile = GetHand(channelId, deckType, DiscardPlayerAlias);
                    break;
                case CardPileId.Dealer:
                    moveToPile = GetHand(channelId, deckType, DealerPlayerAlias);
                    break;
                case CardPileId.Deck:
                    moveToPile = GetDeck(channelId, deckType, null);
                    break;
                case CardPileId.HiddenInPlay:
                    moveToPile = GetHand(channelId, deckType, character + HiddenPlaySuffix);
                    break;
            }

            if(moveToPile == null)
            {
                return "(Error: moveto pile not found)";
            }
            else if (moveToPile == null)
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
                        totalOutputString += d.ToString();
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

            totalOutputString += "\n[i]Current " + moveFromPile.GetCollectionName() + ": [/i]" + moveFromPile.ToString(true);
            totalOutputString += "\n[i]Current " + moveToPile.GetCollectionName() + ": [/i]" + moveToPile.ToString(true);
            
            return totalOutputString;
        }

        public string TakeCardsFromPlay(List<int> drawFromPlayList, bool all, string channelId, DeckType deckType, string character, out int movedCount)
        {
            return MoveCards(drawFromPlayList, all, false, channelId, deckType, character, CardPileId.Play, CardPileId.Hand, out movedCount);
        }

        public string TakeCardsFromDiscard(List<int> drawFromDiscardList, bool all, string channelId, DeckType deckType, string character, out int movedCount)
        {
            return MoveCards(drawFromDiscardList, all, false, channelId, deckType, character, CardPileId.Discard, CardPileId.Hand, out movedCount);
        }

        public string GetRollTableResult(List<SavedRollTable> savedTables, string tableName, string character, string channel, int rollModifier, bool includeLabel, bool includeSecondaryRolls, int dataX, int dataY, int dataZ, int callDepth)
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

                string returnString = res.ResultString;
                string addition = "";

                if(res.TriggeredRolls != null && res.TriggeredRolls.Count > 0 && callDepth > 0)
                {
                    if(callDepth > 0)
                    {
                        foreach (TableRollTrigger trt in res.TriggeredRolls)
                        {
                            trt.TableId = trt.TableId.Replace("#x", dataX.ToString());
                            trt.TableId = trt.TableId.Replace("#y", dataY.ToString());
                            trt.TableId = trt.TableId.Replace("#z", dataZ.ToString());

                            if(trt.TableId.StartsWith("!"))
                            {
                                if(trt.TableId.StartsWith("!roll"))
                                {
                                    string commandName = "";
                                    string[] commandTerms = botMain.SeparateCommandTerms(trt.TableId, out commandName);

                                    string rollResult = GetRollResult(commandTerms, character, channel, false);

                                    addition += "\n" + rollResult;
                                }
                                else if (trt.TableId.StartsWith("!generatepotion"))
                                {
                                    string commandName = "";
                                    string[] commandTerms = botMain.SeparateCommandTerms(trt.TableId, out commandName);

                                    string privateMessage = "";
                                    string rollResult = GeneratePotion(commandTerms, character, channel, out privateMessage); //don't send private message?

                                    addition += "\n" + rollResult;
                                }
                                else if(trt.TableId.StartsWith("!drawcard"))
                                {
                                    string commandName = "";
                                    string[] rawTerms = botMain.SeparateCommandTerms(trt.TableId, out commandName);

                                    string[] terms = Utils.LowercaseStrings(rawTerms);

                                    CardCommandOptions options = new CardCommandOptions(botMain.BotCommandController, terms, character);

                                    int numberDrawn = Utils.GetNumberFromInputs(terms);
                                    if (numberDrawn > 1)
                                        options.cardsS = "s";

                                    string customDeckName = Utils.GetCustomDeckName(character);
                                    string deckTypeString = Utils.GetDeckTypeStringHidePlaying(options.deckType, customDeckName);
                                    string cardDrawingCharacterString = Utils.GetCharacterStringFromSpecialName(options.characterDrawName);

                                    string trueDraw = "";
                                    string messageOutput = "[i]" + cardDrawingCharacterString + ": " + deckTypeString + "Card" + options.cardsS + " drawn:[/i] " + DrawCards(numberDrawn, options.jokers, options.deckDraw, channel, options.deckType, options.characterDrawName, options.secretDraw, out trueDraw);

                                    if (options.secretDraw && !(options.characterDrawName == DiceBot.DealerPlayerAlias || options.characterDrawName == DiceBot.BurnCardsPlayerAlias || options.characterDrawName == DiceBot.DiscardPlayerAlias))
                                    {
                                        string playerMessageOutput = "[i]" + cardDrawingCharacterString + ": " + deckTypeString + "Card" + options.cardsS + " drawn:[/i] " + trueDraw;
                                        botMain.SendPrivateMessage(playerMessageOutput, character);
                                    }

                                    addition += "\n" + messageOutput;
                                }
                            }
                            else
                            {
                                int totalRollBonus = trt.RollBonus;
                                if(!string.IsNullOrEmpty(trt.VariableRollBonus))
                                {
                                    switch(trt.VariableRollBonus.ToLower())
                                    {
                                        case "x":
                                            totalRollBonus += dataX;
                                            break;
                                        case "y":
                                            totalRollBonus += dataY;
                                            break;
                                        case "z":
                                            totalRollBonus += dataZ;
                                            break;
                                    }
                                }
                                addition += "\n" + GetRollTableResult(savedTables, trt.TableId.ToLower(), character, channel, totalRollBonus, includeLabel, includeSecondaryRolls, dataX, dataY, dataZ, callDepth - 1);
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

        public DeckCard DrawCard(System.Random random, bool includeJoker, bool fromDeck, string channelId, DeckType deckType, string customDeckName)
        {
            DeckCard rtnCard = new DeckCard();

            if (fromDeck)
            {
                Deck thisDeck = GetDeck(channelId, deckType, customDeckName);
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

        public void ShuffleDeck(Random r, string channelId, DeckType deckType, bool fullShuffle, string customDeckName)
        {
            Deck relevantDeck = GetDeck(channelId, deckType, customDeckName);

            if (relevantDeck != null)
            {
                if (fullShuffle)
                {
                    EndHand(channelId, false, deckType);
                    relevantDeck.ShuffleFullDeck(r);
                }
                else
                    relevantDeck.ShuffleRemainingDeck(r);
            }
        }

        public void ResetDeck(bool jokers, int deckCopies, string channelId, DeckType deckType, string customDeckName)
        {
            Deck relevantDeck = GetDeck(channelId, deckType, customDeckName);

            if(relevantDeck != null)
            {
                EndHand(channelId, false, deckType, true);
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

        public Deck GetDeck(string channelId, DeckType deckType, string customDeckName)
        {
            string deckKey = GetDeckKey(channelId, deckType);
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

        public string EndHand(string channelId, bool reveal, DeckType deckType, bool deleteCards = false)
        {
            List<Hand> thisChannelHands = GetChannelHands(channelId, deckType);

            int totalMoved = 0;
            int totalPiles = 0;

            Hand burnedCards = GetHand(channelId, deckType, BurnCardsPlayerAlias);
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

                    for (int i = 0; i < currentHandSize; i++)
                    {
                        DeckCard d = h.GetCardAtIndex(i);
                        if (!string.IsNullOrEmpty(allCardsMoved))
                            allCardsMoved += ", ";
                        allCardsMoved += d.ToString();
                        cardsMoved++;
                        burnedCards.AddCard(d, random);
                    }

                    h.Reset();
                }
            }

            if (!reveal)
                allCardsMoved = totalMoved.ToString();

            string sss = totalPiles != 1 ? "s" : "";
            return "Hand Ended. Moved " + allCardsMoved + " cards from " + totalPiles + " pile" + sss + " to burn pile.";
        }

        public List<ItemOwned> GetItemsOwned(string character)
        {
            return ItemsOwned.Where(a => a.character == character).ToList();
        }

        public Potion GetPotionHeld(string character)
        {
            List<ItemOwned> items = GetItemsOwned(character);
            if(items.Count > 0)
            {
                ItemOwned own = items.FirstOrDefault(b => b.item.GetItemCategory() == ItemCategory.Potion);
                if (own != null)
                    return (Potion) own.item;
            }
            return null;
        }

        public void SetPotionHeld(string character, Potion potion)
        {
            List<ItemOwned> items = GetItemsOwned(character);
            if (items.Count > 0)
            {
                ItemOwned own = items.FirstOrDefault(b => b.item.GetItemCategory() == ItemCategory.Potion);
                if (own != null)
                {
                    ItemsOwned.Remove(own);
                }
            }

            ItemsOwned.Add(new ItemOwned() { character = character, item = potion });
        }

        public Hand GetHand(string channelId, DeckType deckType, string character)
        {
            string deckKey = GetDeckKey(channelId, deckType);
            Hand h = Hands.FirstOrDefault(a => a.Id == deckKey && a.Character == character);
            if (h == null)
            {
                h = new Hand();
                h.Id = deckKey;
                h.Character = character;

                if (character == DiscardPlayerAlias)
                    h.SetCollectionName(BotMain.DiscardCollectionName);
                if (character.Contains(PlaySuffix))
                    h.SetCollectionName(BotMain.InPlayCollectionName);
                if (character.Contains(HiddenPlaySuffix))
                    h.SetCollectionName(BotMain.HiddenInPlayCollectionName);
                if (character == BurnCardsPlayerAlias)
                    h.SetCollectionName(BotMain.BurnCollectionName);

                Hands.Add(h);
            }

            return h;
        }

        public List<Hand> GetChannelHands(string channelId, DeckType deckType)
        {
            string deckKey = GetDeckKey(channelId, deckType);
            return Hands.Where(a => a.Id == deckKey).ToList();
        }

        private List<ChipPile> GetChannelChipPiles(string channelId)
        {
            return ChipPiles.Where(a => a.ChannelId == channelId).ToList();
        }

        public string ListAllChipPiles(string channelName)
        {
            string rtnString = "";
            List<ChipPile> channelPiles = GetChannelChipPiles(channelName);

            if(channelPiles.Count == 0)
            {
                rtnString = "No piles found.";
            }
            else
            {
                foreach(ChipPile p in channelPiles)
                {
                    if(!SpecialCharacterName(p.Character))
                    {
                        if (!string.IsNullOrEmpty(rtnString))
                            rtnString += ", ";
                        string charString = GetCharacterNameString(p.Character);
                        rtnString += charString + ": " + p.Chips;
                    }
                }
            }

            return rtnString;
        }

        public string AddChips(string characterName, string channelId, int amount, bool pot)
        {
            ChipPile characterPile = GetChipPile(characterName, channelId);
            ChipPile potPile = GetChipPile(PotPlayerAlias, channelId);
            string characterNameUsed = Utils.GetCharacterUserTags(characterName);
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

            string addedAmount = amount + " chips were added to ";
            if (amount < 0)
                addedAmount = (amount * -1) + " chips were removed from ";

            return addedAmount + characterNameUsed + "'s pile.";
        }

        public string RemoveChipsPile(string characterName, string channelId)
        {
            ChipPile characterPile = GetChipPile(characterName, channelId);
            int amt = characterPile.Chips;
            ChipPiles.RemoveAll(a => a.Character == characterName && a.ChannelId == channelId);

            return Utils.GetCharacterUserTags( characterName) + " removed their pile of chips. (" + amt + ")";
        }

        public string RemoveAllChipsPiles(string characterName, string channelId)
        {
            string channelPiles = ListAllChipPiles(channelId);
            int pilesCount = ChipPiles.Count(a => a.ChannelId == channelId);
            ChipPiles.RemoveAll(a => a.ChannelId == channelId);

            return Utils.GetCharacterUserTags(characterName) + " removed all the channel piles of chips. (" + pilesCount + "): " + channelPiles;
        }

        public string BetChips(string characterName, string channelId, int amount, bool all)
        {
            ChipPile characterPile = GetChipPile(characterName, channelId);
            ChipPile potPile = GetChipPile(PotPlayerAlias, channelId);
            int moved = MoveChipsFromPile(characterPile, potPile, amount, all);

            string allInString = characterPile.Chips == 0 && moved > 0 ? " [color=red][b](ALL IN)[/b][/color]" : "";
            return Utils.GetCharacterUserTags(characterName) + " added " + moved + " chips to the pot." + allInString;
        }

        public string GiveChips(string characterName, string targetCharacterName, string channelId, int amount, bool all, bool overrideEmpty = false)
        {
            ChipPile characterPile = GetChipPile(characterName, channelId);
            ChipPile targetPile = GetChipPile(targetCharacterName, channelId, false);

            bool specialCharacterTarget = SpecialCharacterName(targetCharacterName);
            if (targetPile == null)
            {
                if (specialCharacterTarget || overrideEmpty)
                {
                    targetPile = GetChipPile(targetCharacterName, channelId, true);
                }
                else
                {
                    return Utils.GetCharacterUserTags(targetCharacterName) + " must have a chips pile (by typing [b]!register[/b]) before giving them chips.";
                }
            }

            int moved = MoveChipsFromPile(characterPile, targetPile, amount, all);

            string characterString = SpecialCharacterName(characterName)? Utils.GetCharacterStringFromSpecialName(characterName) : Utils.GetCharacterUserTags(characterName);
            string allInString = characterPile.Chips == 0 && moved > 0 ? " [color=red][b](100%)[/b][/color]" : "";
            string targetString = specialCharacterTarget? Utils.GetCharacterStringFromSpecialName(targetCharacterName) : Utils.GetCharacterUserTags(targetCharacterName);
            return characterString + " gave " + moved + " chips to " + targetString + " " + allInString;
        }

        public string TakeChips(string characterName, string targetCharacterName, string channelId, int amount, bool all)
        {
            ChipPile characterPile = GetChipPile(characterName, channelId);
            ChipPile targetPile = GetChipPile(targetCharacterName, channelId, false);

            if (targetPile == null)
            {
                return Utils.GetCharacterUserTags(targetCharacterName) + " must have a chips pile before taking their chips.";
            }

            int moved = MoveChipsFromPile(targetPile, characterPile, amount, all);

            string allInString = characterPile.Chips == 0 && moved > 0 ? " [color=red][b](100%)[/b][/color]" : "";
            return Utils.GetCharacterUserTags(characterName) + " took " + moved + " chips from " + Utils.GetCharacterUserTags(targetCharacterName) + " " + allInString;
        }

        public string ClaimPot(string characterName, string channelId, double portion)
        {
            ChipPile claimingPile = GetChipPile(characterName, channelId);
            ChipPile potPile = GetChipPile(PotPlayerAlias, channelId);

            int amount = (int) Math.Round(potPile.Chips * portion);

            int moved = MoveChipsFromPile(potPile, claimingPile, amount, false);
            string fractionString = portion == .5 ? "half " : (portion == .33 ? "a third of " : "");

            string charString = Utils.GetCharacterUserTags(characterName);
            if (SpecialCharacterName(characterName))
            {
                charString = Utils.GetCharacterStringFromSpecialName(characterName);
            }

            return charString + " claimed " + fractionString + "the pot ([b]" + moved + " chips[/b]).";
        }

        private int MoveChipsFromPile(ChipPile sourcePile, ChipPile destinationPile, int amount, bool all)
        {
            int creditedAmount = 0;
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

        public string DisplayChipPile(string channelName, string character, ChipPile existing = null)
        {
            if(existing == null)
                existing = GetChipPile(character, channelName);

            string charString = GetCharacterNameString(character);

            return charString + " chips pile: [b]" + existing.Chips + "[/b]";
        }

        public ChipPile GetChipPile(string characterName, string channelId, bool createNew = true)
        {
            ChipPile h = ChipPiles.FirstOrDefault(a => a.ChannelId == channelId && a.Character.ToLower() == characterName.ToLower());

            if (h == null && createNew)
            {
                ChannelSettings thisChannelSettings = botMain.GetChannelSettings(channelId);
                bool specialAccount = SpecialCharacterName(characterName);
                int startingChipsAmount = thisChannelSettings.StartingChips;

                h = new ChipPile();
                h.ChannelId = channelId;
                h.Character = characterName;
                h.Chips = specialAccount? 0 : startingChipsAmount;
                ChipPiles.Add(h);
            }

            return h;
        }

        public CharacterData GetCharacterData(string characterName, string channelId, bool createNew = true)
        {
            CharacterData h = CharacterDatas.FirstOrDefault(a => a.Channel == channelId && a.Character.ToLower() == characterName.ToLower());

            if (h == null && createNew)
            {
                bool specialAccount = SpecialCharacterName(characterName);
                
                h = new CharacterData();
                h.Channel = channelId;
                h.Character = characterName;
                h.SpecialName = specialAccount;
                h.DiceUnlocked = false;
                h.Inventory = new List<InventoryItem>();
                h.LastSlotsSpin = DateTime.UtcNow - TimeSpan.FromHours(2);
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
            return SpecialCharacterName(characterName) ? Utils.GetCharacterStringFromSpecialName(characterName) : Utils.GetCharacterUserTags(characterName);
        }

        public string GetDeckKey(string channelId, DeckType deckType)
        {
            return channelId + "_" + deckType;
        }

        public string GeneratePotion(string[] terms, string characterName, string channel, out string privateMessage)
        {
            bool allowFlavor = true;
            bool allowLewd = true;
            bool requireLewd = false;
            bool secret = false;
            if (terms != null && terms.Length > 0)
            {
                if (terms.Contains("noflavor"))
                    allowFlavor = false;
                if (terms.Contains("nolewd"))
                    allowLewd = false;
                if (terms.Contains("requirelewd"))
                    requireLewd = true;
                if (terms.Contains("secret") || terms.Contains("s"))
                    secret = true;
            }

            string outputMessage = "";
            privateMessage = "";
            if (!allowLewd && requireLewd)
            {
                outputMessage = "Error: Lewd must be allowed if you want to require a lewd potion.";
            }
            else
            {
                List<string> remainingTerms = terms.Where(a => a != "noflavor" && a != "nolewd" && a != "requirelewd" && a != "s" && a != "secret").ToList();
                string searchTerm = string.Join(" ", remainingTerms);

                Potion potion = null;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    potion = GetSpecificPotion(searchTerm);
                }
                else
                {
                    potion = GetRandomPotion(allowFlavor, allowLewd, requireLewd);
                }

                string output = PotionGenerator.GetPotionGenerationOutputString(potion);

                bool outputHandled = false;

                if (potion != null && potion.enchantment != null && potion.enchantment.Flag != EnchantmentFlag.NONE)
                {
                    if (potion.enchantment.Flag == EnchantmentFlag.WhisperPotionRevealFake)
                    {
                        secret = true;
                        Potion secondPotion = GetSpecificPotion("fluid flavor");
                        if (!allowFlavor)
                            secondPotion = GetSpecificPotion("lust");
                        if (!allowLewd)
                            secondPotion = GetSpecificPotion("skin color");

                        secondPotion.strength = 1;
                        string outputFake = PotionGenerator.GetPotionGenerationOutputString(secondPotion);

                        SetPotionHeld(characterName, potion);

                        privateMessage = output + "\nThe channel has been sent a fake potion. This is your real potion result and it can be shown with !showpotion ";
                        output = outputFake;
                        outputMessage = output;
                        outputHandled = true;
                    }
                    else if (potion.enchantment.Flag == EnchantmentFlag.RequireRollBondage)
                    {
                        string tableOutput = GetRollTableResult(botMain.SavedTables, "bondage", characterName, channel, 0, true, true, 0, 0, 0, DiceBot.MaximumSecondaryTableRolls);
                        output += "\n" + tableOutput;
                    }
                    else if (potion.enchantment.Flag == EnchantmentFlag.RequireRollHumiliation)
                    {
                        string tableOutput = GetRollTableResult(botMain.SavedTables, "humiliation", characterName, channel, 0, true, true, 0, 0, 0, DiceBot.MaximumSecondaryTableRolls);
                        output += "\n" + tableOutput;
                    }
                    else if (potion.enchantment.Flag == EnchantmentFlag.RequireRollSexToy)
                    {
                        string tableOutput = GetRollTableResult(botMain.SavedTables, "sextoy", characterName, channel, 0, true, true, 0, 0, 0, DiceBot.MaximumSecondaryTableRolls);
                        output += "\n" + tableOutput;
                    }
                    else if (potion.enchantment.Flag == EnchantmentFlag.RequireRollPunishment)
                    {
                        string tableOutput = GetRollTableResult(botMain.SavedTables, "punishment", characterName, channel, 0, true, true, 0, 0, 0, DiceBot.MaximumSecondaryTableRolls);
                        output += "\n" + tableOutput;
                    }
                }

                if (secret && !outputHandled)
                {
                    SetPotionHeld(characterName, potion);

                    privateMessage = output + "\nThis potion is now a secret and can be shown in channels with !showpotion ";
                    outputMessage = "A potion was generated in secret and sent to " + Utils.GetCharacterUserTags(characterName) + ".";
                }
                else
                    outputMessage = output;
            }
            return outputMessage;
        }

        public Potion GetRandomPotion(bool allowFlavor, bool allowKinky, bool requireKinky)//todo: make this generator more customizable, and able to store generator(s) to use in the data files
        {
            return PotionGenerator.GeneratePotion(allowFlavor, allowKinky, requireKinky); 
        }

        public Potion GetSpecificPotion(string searchTerm)//todo: make this generator more customizable, and able to store generator(s) to use in the data files
        {
            return PotionGenerator.GeneratePotionWithSpecificEffect(true, true, false, searchTerm);
        }


        #region game sessions
        public string JoinGame(string characterName, string channelId, IGame gameType)
        {
            GameSession sesh = GetGameSession(channelId, gameType, true);
            if (sesh != null)
            {
                sesh.Players.Add(characterName);
                return Utils.GetCharacterUserTags(characterName) + " joined " + sesh.CurrentGame.GetGameName() + " successfully.";
            }

            return "";
        }

        public string LeaveGame(string characterName, string channelId, IGame gameType)
        {
            GameSession sesh = GetGameSession(channelId, gameType, false);
            if (sesh != null)
            {
                sesh.Players.RemoveAll(a => a == characterName);
                string leftGameNote = sesh.CurrentGame.PlayerLeftGame(botMain, sesh, characterName);
                return Utils.GetCharacterUserTags(characterName) + " left " + sesh.CurrentGame.GetGameName() + " successfully.";
            }

            return "";
        }

        public string StartGame(string channelId,  string characterExecuting, IGame gameType, BotMain botMain, bool keepSession, bool endSession)
        {
            GameSession sesh = GetGameSession(channelId, gameType, false);

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
                                ChipPile p = GetChipPile(characterName, channelId, false);

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
                                    LeaveGame(chara, channelId, gameType);
                                }
                            }
                        }

                        endingString += " [i](game session kept)[/i]";
                    }
                    else
                    {
                        RemoveGameSession(channelId, gameType);
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

        public string CancelGame(string channelId, IGame gameType)
        {
            GameSession sesh = GetGameSession(channelId, gameType, false);
            string output = "";
            if (sesh != null)
            {
                RemoveGameSession(channelId, gameType);
                output = "Session for " + sesh.CurrentGame.GetGameName() + " cancelled.";
            }
            else
            {
                output = "Game session not found.";
            }

            return output;
        }

        public GameSession GetGameSession(string channelId, IGame gameType, bool createNew = true)
        {
            GameSession rtn = GameSessions.FirstOrDefault(a => a.ChannelId == channelId && a.CurrentGame.GetGameName() == gameType.GetGameName());
            if (rtn == null && createNew)
            {
                rtn = new GameSession();
                rtn.ChannelId = channelId;
                rtn.CurrentGame = gameType;
                rtn.Players = new List<string>();
                rtn.State = GameState.Unstarted;

                GameSessions.Add(rtn);
            }

            return rtn;
        }

        public string RemoveGameSession(string channelId, IGame gameType)
        {
            GameSession g = GetGameSession(channelId, gameType, false);
            if(g != null)
            {
                GameSessions.RemoveAll(a => a.CurrentGame.GetGameName() == gameType.GetGameName() && a.ChannelId == channelId);

                return "Game session removed for " + gameType.GetGameName() + ".";
            }

            return "Game session not found for " + gameType.GetGameName() + ".";
        }

        public string IssueGameCommand(string character, string channel, GameSession session, string[] terms, string[] rawTerms)
        {
            string rtn = session.IssueGameCommand(this, botMain, character, channel, terms, rawTerms);

            return rtn;
        }

        #endregion

        #region countdown timers

        public bool CountdownFinishedOrNotStarted(string channel, string countdownId)
        {
            CountdownTimer timer = GetCountdownTimer(channel, countdownId);
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

        public CountdownTimer GetCountdownTimer(string channel, string countdownId)
        {
            return CountdownTimers.FirstOrDefault(a => a.ChannelId == channel && a.TimerId == countdownId);
        }

        public double GetSecondsRemainingOnCountdownTimer(string channel, string countdownId)
        {
            CountdownTimer timer = GetCountdownTimer(channel, countdownId);
            double secondsRemain = 0;

            if (timer != null)
                secondsRemain = timer.GetSecondsRemaining();
            return secondsRemain;
        }

        public void StartCountdownTimer(string channel, string countdownId, string characterId, int targetMiliseconds)
        {
            CountdownTimer timer = GetCountdownTimer(channel, countdownId);
            if(timer == null)
            {
                CountdownTimer t = new CountdownTimer()
                {
                    ChannelId = channel,
                    CharacterId = characterId,
                    TimerId = countdownId,
                    FinishedMs = targetMiliseconds
                };
                t.StartTimer();
                CountdownTimers.Add(t);
            }
        }

        #endregion
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
