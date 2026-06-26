using FChatDicebot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class DiceRoll
    {
        public int DiceRolled;
        public int DiceSides;

        public long Total;

        public List<DiceFace> Rolls;

        public bool Error;
        public string ErrorString;

        public bool TextFormat;

        public int KeepHighest;
        public int KeepLowest;
        public int RemoveHighest;
        public int RemoveLowest;
        public int RerollNumber;
        public int CountOver;
        public int CountUnder;

        public bool Explode;
        public bool ExplodeTable;
        public bool FateDice;
        public bool AlsoCountOver;
        public int CountOfOver;
        public bool AlsoCountUnder;
        public int CountOfUnder;
        public int ExplodeThreshold;

        public DiceRollFormat RollFormat;

        public DiceRoll()
        {
            RollFormat = DiceRollFormat.Text;
        }

        public DiceRoll(DiceRollFormat rollFormat)
        {
            RollFormat = rollFormat;
        }

        public DiceRoll(MessageAddress address, BotMain botMain)
        {
            FChatDicebot.SavedData.CharacterData dat = botMain.DiceBot.GetCharacterData(address, true);

            bool goldDice = dat.DiceUnlocked || (botMain.AccountSettings != null && botMain.AccountSettings.FullCosmeticsUnlockCharacters != null &&
                botMain.AccountSettings.FullCosmeticsUnlockCharacters.Count(a => a.ToLower() == address.character.ToLower()) > 0);
            RollFormat = goldDice? DiceRollFormat.GoldEicon6 : DiceRollFormat.OjEicon6;
        }

        public string ResultString(DiceRollFormat rollFormat = DiceRollFormat.Inherit, bool sort = false, bool showTotal = true)
        {
            if (rollFormat == DiceRollFormat.Inherit)
                rollFormat = RollFormat;

            if(Error)
            {
                return "ERROR: " + ErrorString;
            }
            if (DiceRolled > 0)
            {
                string extraOutput = "";
                if (AlsoCountOver)
                    extraOutput += " [sup]{w/ " + CountOfOver + " rolls over " + CountOver + "}[/sup]";
                if (AlsoCountUnder)
                    extraOutput += " [sup]{w/ " + CountOfUnder + " rolls under " + CountUnder + "}[/sup]";
                return "Rolled " + DiceRolled + "d" + DiceSides + " {" + PrintRollsList(Rolls, sort, rollFormat) + "} " + GetConditionsString() + (showTotal ? "= [b]" + Total + "[/b]" + extraOutput : "");
            }
            else
                return "[b]" + Total + "[/b]";
        }

        public void Roll(System.Random r)
        {
            if (Error || DiceRolled == 0)
                return;

            try
            {
                Rolls = new List<DiceFace>();

                int explosionBonusDice = 0;
                int rerollBonusDice = 0;
                bool currentDieIsReroll = false;

                for (int i = 0; (i < DiceRolled + explosionBonusDice + rerollBonusDice && i < DiceBot.MaximumRolls); i++)
                {
                    int rollAmount = r.Next(DiceSides) + 1;
                    DiceFace thisRoll = new DiceFace() { Result = rollAmount };

                    if(RerollNumber != 0)
                    {
                        if(RerollNumber == rollAmount && !currentDieIsReroll)
                        {
                            thisRoll.Crossout = true;
                            rerollBonusDice++;
                            currentDieIsReroll = true;
                        }
                        else
                        {
                            currentDieIsReroll = false;
                        }
                    }

                    if(Explode || ExplodeTable)
                    {
                        if((ExplodeThreshold == 0 && rollAmount == DiceSides) ||
                            (ExplodeThreshold > 1 && rollAmount >= ExplodeThreshold)
                            && (explosionBonusDice <= (DiceBot.MaximumDice * 2)))
                        {
                            if(TextFormat)
                            {
                                thisRoll.Bold = true;
                                thisRoll.UseTableExplosionScore = ExplodeTable;
                            }
                            explosionBonusDice++;
                        }
                    }
                    if (FateDice)
                    {
                        if (DiceSides != 3)
                            thisRoll.Result %= 3;

                        thisRoll.Result -= 2;
                    }
                    Rolls.Add(thisRoll);
                }

                bool useKeptRollsTotal = false;
                List<DiceFace> keptRolls = new List<DiceFace>();

                //these two are not exclusive to the others anymore
                if (CountOver != 0)
                {
                    for (int i = 0; i < Rolls.Count; i++)
                    {
                        if (Rolls[i].Result > CountOver)
                        {
                            Rolls[i].Asterisk = true; //set later with total of kept or normal dice
                            //CountOfOver++;
                            //keptRolls.Add(new DiceFace(){ Result = 1 }); //also would set useKeptRolls = true; before
                        }
                    }
                }
                if (CountUnder != 0)
                {
                    for (int i = 0; i < Rolls.Count; i++)
                    {
                        if (Rolls[i].Result < CountUnder)
                        {
                            Rolls[i].LowerAsterisk = true;
                            //CountOfUnder++; //set later with total of kept or normal dice
                            //keptRolls.Add(new DiceFace() { Result = 1 }); //also would set useKeptRolls = true; before
                        }
                    }
                }

                List<DiceFace> currentKeptRolls = Rolls;

                if (RerollNumber != 0)
                {
                    keptRolls = Rolls.Where(a => !a.Crossout).ToList();
                    currentKeptRolls = keptRolls;//this is handled a little badly rn, but the value totalled at the end is keptRolls and this currentKeptRolls is just used along the way to track rerolled dice correctly
                    useKeptRollsTotal = true;
                }


                if(KeepHighest != 0)
                {
                    List<DiceFace> highestRollsValues = new List<DiceFace>();

                    int index = 0;
                    foreach(DiceFace j in currentKeptRolls)
                    {
                        if (highestRollsValues.Count < KeepHighest)
                        {
                            highestRollsValues.Add(j);
                        }
                        else
                        {
                            int min = highestRollsValues.Min(a => a.Result);
                            if(min < j.Result)
                            {
                                DiceFace item = highestRollsValues.FirstOrDefault(a => a.Result == min);
                                int indexOfLowValue = highestRollsValues.IndexOf(item);
                                highestRollsValues.RemoveAt(indexOfLowValue);
                                highestRollsValues.Add(j);
                            }
                        }
                        index++;
                    }

                    useKeptRollsTotal = true;
                    keptRolls = highestRollsValues;
                    currentKeptRolls = keptRolls;
                    foreach (DiceFace d in keptRolls)
                    {
                        d.Highlight = true;
                    }
                }
                else if(KeepLowest != 0)
                {
                    List<DiceFace> highestRollsValues = new List<DiceFace>();

                    int index = 0;
                    foreach (DiceFace j in currentKeptRolls)
                    {
                        if (highestRollsValues.Count < KeepLowest)
                        {
                            highestRollsValues.Add(j);
                        }
                        else
                        {
                            int max = highestRollsValues.Max(a => a.Result);
                            if (max > j.Result)
                            {
                                DiceFace item = highestRollsValues.FirstOrDefault(a => a.Result == max);
                                int indexOfLowValue = highestRollsValues.IndexOf(item);
                                highestRollsValues.RemoveAt(indexOfLowValue);
                                highestRollsValues.Add(j);
                            }
                        }
                        index++;
                    }

                    useKeptRollsTotal = true;
                    keptRolls = highestRollsValues;
                    currentKeptRolls = keptRolls;
                    foreach (DiceFace d in keptRolls)
                    {
                        d.Highlight = true;
                    }
                }
                else if (RemoveHighest != 0)
                {
                    List<DiceFace> highestRollsValues = new List<DiceFace>();

                    int index = 0;
                    foreach (DiceFace j in currentKeptRolls)
                    {
                        if (highestRollsValues.Count < RemoveHighest)
                        {
                            highestRollsValues.Add(j);
                        }
                        else
                        {
                            int min = highestRollsValues.Min(a => a.Result);
                            if (min < j.Result)
                            {
                                DiceFace item = highestRollsValues.FirstOrDefault(a => a.Result == min);
                                int indexOfLowValue = highestRollsValues.IndexOf(item);
                                highestRollsValues.RemoveAt(indexOfLowValue);
                                highestRollsValues.Add(j);
                            }
                        }
                        index++;
                    }

                    useKeptRollsTotal = true;

                    keptRolls = new List<DiceFace>(currentKeptRolls);
                    while (highestRollsValues.Count > 0)
                    {
                        int value = highestRollsValues[0].Result;
                        DiceFace item = highestRollsValues.FirstOrDefault(a => a.Result == value);
                        int indexOfValue = keptRolls.IndexOf(item);
                        keptRolls.RemoveAt(indexOfValue);
                        highestRollsValues.RemoveAt(0);
                    }

                    currentKeptRolls = keptRolls;
                    foreach (DiceFace d in keptRolls)
                    {
                        d.Highlight = true;
                    }
                }
                else if (RemoveLowest != 0)
                {
                    List<DiceFace> highestRollsValues = new List<DiceFace>();

                    int index = 0;
                    foreach (DiceFace j in currentKeptRolls)
                    {
                        if (highestRollsValues.Count < RemoveLowest)
                        {
                            highestRollsValues.Add(j);
                        }
                        else
                        {
                            int max = highestRollsValues.Max(a => a.Result);
                            if (max > j.Result)
                            {
                                DiceFace item = highestRollsValues.FirstOrDefault(a => a.Result == max);
                                int indexOfHighValue = highestRollsValues.IndexOf(item);
                                highestRollsValues.RemoveAt(indexOfHighValue);
                                highestRollsValues.Add(j);
                            }
                        }
                        index++;
                    }

                    useKeptRollsTotal = true;

                    keptRolls = new List<DiceFace>(currentKeptRolls);
                    while (highestRollsValues.Count > 0)
                    {
                        int value = highestRollsValues[0].Result;
                        DiceFace item = highestRollsValues.FirstOrDefault(a => a.Result == value);
                        int indexOfValue = keptRolls.IndexOf(item);
                        keptRolls.RemoveAt(indexOfValue);
                        highestRollsValues.RemoveAt(0);
                    }

                    currentKeptRolls = keptRolls;
                    foreach (DiceFace d in keptRolls)
                    {
                        d.Highlight = true;
                    }
                }

                List<DiceFace> countedRolls = Rolls;
                if (useKeptRollsTotal)
                    countedRolls = keptRolls;

                if (CountOver != 0)
                    CountOfOver = countedRolls.Count(a => a.Result > CountOver);
                if (CountUnder != 0)
                    CountOfUnder = countedRolls.Count(a => a.Result < CountUnder);

                if (CountOfOver > 0 && !AlsoCountOver)
                    Total = CountOfOver;
                if (CountOfUnder > 0 && !AlsoCountUnder)
                    Total = CountOfUnder;
                else
                    Total = GetTotalOfRolls(countedRolls);
            }
            catch(Exception exc)
            {
                Error = true;
                ErrorString = "exception occured: " + exc.ToString();
            }
        }

        public int GetTotalOfRolls(List<DiceFace> usedList)
        {
            if (usedList == null || usedList.Count == 0)
                return 0;

            if (CountOver > 0 && !AlsoCountOver)
            {
                return CountOfOver;
            }
            if (CountUnder > 0 && !AlsoCountUnder)
                return CountOfUnder;

            if (ExplodeTable)
            {
                int currentTotal = usedList.Where(a => !a.UseTableExplosionScore).Sum(b => b.Result);
                int explosionTableAmount = GetTableExplodeAmount(DiceSides, DiceRolled);
                currentTotal += usedList.Count(a => a.UseTableExplosionScore) * explosionTableAmount;
                return currentTotal;
            }
            else
            {
                return usedList.Sum(a => a.Result);
            }
        }

        public string PrintRollsList(List<DiceFace> rollsList, bool sort, DiceRollFormat rollFormat = DiceRollFormat.OjEicon6)
        {
            if (rollsList == null || rollsList.Count == 0)
                return "";

            List<DiceFace> usedList = new List<DiceFace>(rollsList);

            if(sort)
            {
                usedList = usedList.OrderBy(a => a.Result).ToList();
            }

            string rtnString = "";

            bool formatsRequired = Rolls.Where(a => a.Highlight || a.Bold || a.Crossout).Count() > 0;

            string shownDice = "";
            List<int> diceSidesList = new List<int>() { 4, 6, 8, 10, 12, 20 };
            bool powerTenDie = DiceSides % 10 == 0;

            //only used when there are highlights or crossouts due to keeping certain dice or rerolling AND format is not text
            if (formatsRequired && rollFormat != DiceRollFormat.Text && (powerTenDie || diceSidesList.Contains(DiceSides)) &&  DiceRolled <= 10)
            {
                foreach (DiceFace i in usedList)
                {
                    string addition = GetDiceResult(rollFormat, true, FateDice, DiceSides, i.Result);

                    shownDice += addition;
                }
                shownDice += " ";
            }
            int count = 0;
            int digits = DiceSides.ToString().Length - 1;

            bool showEiconInMainString = rollFormat != DiceRollFormat.Text && !formatsRequired;

            foreach (DiceFace i in usedList)
            {
                string addition = GetDiceResult(rollFormat, showEiconInMainString, FateDice, DiceSides, i.Result);

                if (rtnString.Length > 0)
                    rtnString += ", ";

                if(i.Crossout)
                {
                    addition = "[s]" + addition + "[/s]";
                }
                if (i.Asterisk)
                    addition += "[sup]*[/sup]";
                if (i.LowerAsterisk)
                    addition += "[sub]*[/sub]";

                if (i.Highlight)
                {
                    addition = "[color=yellow]" + addition + "[/color]";
                }
                else if (i.Bold)
                {
                    addition = "[b]" + addition + "[/b]";
                }

                rtnString += addition;
                count++;
            }

            return shownDice + rtnString;
        }

        public string GetDiceResult( DiceRollFormat rollFormat, bool showEicon, bool fateDice, int sides, int dieRoll)
        {
            string addition = dieRoll.ToString();
            if (fateDice)
            {
                switch (dieRoll)
                {
                    case -1: addition = "[color=red]-[/color]"; break;
                    case 0: addition = "o"; break;
                    case 1: addition = "[color=green]+[/color]"; break;
                }
            }
            if (showEicon)
            {
                if (rollFormat == DiceRollFormat.GoldEicon6)
                {
                    if (sides == 4)
                    {
                        addition = FChatDicebot.TextFormat.Emoji("d4gold-" + dieRoll);
                    }
                    else if (sides == 6)
                    {
                        addition = FChatDicebot.TextFormat.Emoji("db-d6gold-" + dieRoll);
                        //addition = FChatDicebot.TextFormat.Emoji("dbgoldd6-" + dieRoll);
                    }
                    else if (sides == 8)
                    {
                        addition = FChatDicebot.TextFormat.Emoji("d8gold-" + dieRoll);
                    }
                    else if (sides == 12)
                    {
                        addition = FChatDicebot.TextFormat.Emoji("d12gold-" + dieRoll);
                    }
                    else if (sides == 20)
                    {
                        addition = FChatDicebot.TextFormat.Emoji("d20gold-" + dieRoll);
                    }
                    else if (sides % 10 == 0) //check d-10 string
                    {
                        addition = "";
                        //d10s/100/1000/10000 etc
                        string roll10String = GetRollD10String(dieRoll, sides);
                        int startIndex = 0;
                        if(roll10String.StartsWith("10") && roll10String.Length == sides.ToString().Length)
                        {
                            addition += FChatDicebot.TextFormat.Emoji("db-d10gold-10");
                            //addition += FChatDicebot.TextFormat.Emoji("dbgoldd10-10");
                            startIndex = 2;
                        }
                        for (int i = startIndex; i < roll10String.Length; i++ )
                        {
                            addition += FChatDicebot.TextFormat.Emoji("db-d10gold-" + roll10String[i]);
                            //addition += FChatDicebot.TextFormat.Emoji("dbgoldd10-" + roll10String[i]);
                        }
                    }
                }
                else
                {
                    if (sides == 4)
                    {
                        addition = FChatDicebot.TextFormat.Emoji("d4-" + dieRoll);
                    }
                    else if (sides == 6)
                    {
                        addition = FChatDicebot.TextFormat.Emoji("db-d6-" + dieRoll);
                        //addition = FChatDicebot.TextFormat.Emoji("dbd6-" + dieRoll);
                    }
                    else if (sides == 8)
                    {
                        addition = FChatDicebot.TextFormat.Emoji("d8-" + dieRoll);
                    }
                    else if (sides == 12)
                    {
                        addition = FChatDicebot.TextFormat.Emoji("d12-" + dieRoll);
                    }
                    else if (sides == 20)
                    {
                        addition = FChatDicebot.TextFormat.Emoji("d20-" + dieRoll);
                    }
                    else if (sides % 10 == 0) //check d-10 string
                    {
                        addition = "";
                        //d10s/100/1000/10000 etc
                        string roll10String = GetRollD10String(dieRoll, sides);
                        int startIndex = 0;
                        if (roll10String.StartsWith("10") && roll10String.Length == sides.ToString().Length)
                        {
                            addition += FChatDicebot.TextFormat.Emoji("db-d10-10");
                            //addition += FChatDicebot.TextFormat.Emoji("dbredd10-10");
                            startIndex = 2;
                        }
                        for (int i = startIndex; i < roll10String.Length; i++)
                        {
                            addition += FChatDicebot.TextFormat.Emoji("db-d10-" + roll10String[i]);
                            //addition += FChatDicebot.TextFormat.Emoji("dbredd10-" + roll10String[i]);
                        }
                    }
                }
            }
            return addition;
        }

        public string GetRollD10String(int roll, int sides)
        {
            string numberString = roll.ToString();

            // Pad the string with zeros if needed to reach the desired length
            if(numberString.Length < sides.ToString().Length)
                numberString = numberString.PadLeft(sides.ToString().Length - 1, '0');

            return numberString;
        }

        public int GetTableExplodeAmount(int dieSides, int dieAmount)
        {
            int amountExplode = dieSides;
            if (dieAmount > 1)
            {
                if (dieSides <= 3)
                    amountExplode = Math.Max(dieSides - 2, 0);
                else
                    amountExplode = dieSides - 1;
            }
            else
            {
                if (dieSides <= 1)
                    amountExplode = 0;
                else if (dieSides == 2)
                    amountExplode = 1;
                else if (dieSides == 3)
                    amountExplode = 2;
                else
                    amountExplode = (int)Math.Floor(((double)dieSides - 4) * 1.5) + 4;//4 = 4, 6 = 7, 8 = 10, 10 = 13, etc
            }
            return amountExplode;
        }

        public string GetConditionsString()
        {
            string explodeStr = "";

            if(Rolls.Count >= DiceBot.MaximumRolls)
            {
                explodeStr += "(Maximum roll count reached.)";
            }

            if (Explode || ExplodeTable)
            {
                string explosionNumberPrint = (ExplodeThreshold > 0 ? ExplodeThreshold + "+" : DiceSides.ToString());
                if (ExplodeTable)
                {
                    int explodeAmount = GetTableExplodeAmount(DiceSides, DiceRolled);
                    explodeStr += "(exploding on " + explosionNumberPrint + " for " + explodeAmount + ") ";
                }
                else
                    explodeStr += "(exploding on " + explosionNumberPrint + ") ";
            }

            string outputString = explodeStr;
            if (RerollNumber > 0)
                outputString += "(re-rolling once on " + RerollNumber + ") ";
            if (KeepHighest > 0)
                outputString += "(keeping highest " + KeepHighest + ") ";
            if (KeepLowest > 0)
                outputString += "(keeping lowest " + KeepLowest + ") ";
            if (RemoveHighest > 0)
                outputString += "(removing highest " + RemoveHighest + ") ";
            if (RemoveLowest > 0)
                outputString += "(removing lowest " + RemoveLowest + ") ";
            if (CountOver > 0 && !AlsoCountOver)
                outputString += "(# of rolls over " + CountOver + ") ";
            if (CountUnder > 0 && !AlsoCountUnder)
                outputString += "(# of rolls under " + CountUnder + ") ";

            return outputString;
        }
    }

    public enum DiceRollFormat
    {
        Text,
        OjEicon6,
        GoldEicon6,
        AllEiconDb,
        Inherit
    }

    public class DiceFace
    {
        public int Result;
        public bool Highlight;
        public bool Asterisk;
        public bool LowerAsterisk;
        public bool Crossout;
        public bool Bold;
        public bool UseTableExplosionScore;
    }
}
