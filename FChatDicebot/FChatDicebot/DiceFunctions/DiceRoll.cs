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
        public List<int> Rolls;
        public List<int> HighlightedIndexes;
        public List<int> CrossoutIndexes;
        public List<int> BoldedIndexes;

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

        public DiceRoll(string playerName, string channelId, DiceBot diceBot)
        {
            FChatDicebot.SavedData.CharacterData dat = diceBot.GetCharacterData(playerName, channelId, true);

            RollFormat = dat.DiceUnlocked? DiceRollFormat.GoldEicon6 : DiceRollFormat.OjEicon6;
        }

        public string ResultString(DiceRollFormat rollFormat = DiceRollFormat.Inherit, bool showTotal = true)
        {
            if (rollFormat == DiceRollFormat.Inherit)
                rollFormat = RollFormat;

            if(Error)
            {
                return "ERROR: " + ErrorString;
            }
            if (DiceRolled > 0)
                return "Rolled " + DiceRolled + "d" + DiceSides + " {" + PrintRollsList(Rolls, rollFormat) + "} " + GetConditionsString() + (showTotal ?  "= [b]" + Total + "[/b]" : "");
            else
                return "[b]" + Total + "[/b]";
        }

        public void Roll(System.Random r)
        {
            if (Error || DiceRolled == 0)
                return;

            try
            {
                Rolls = new List<int>();
                BoldedIndexes = new List<int>();
                CrossoutIndexes = new List<int>();
                HighlightedIndexes = new List<int>();

                int explosionBonusDice = 0;
                int rerollBonusDice = 0;
                bool currentDieIsReroll = false;

                for (int i = 0; (i < DiceRolled + explosionBonusDice + rerollBonusDice && i < DiceBot.MaximumRolls); i++)
                {
                    int rollAmount = r.Next(DiceSides) + 1;
                    Rolls.Add(rollAmount);

                    if(RerollNumber != 0)
                    {
                        if(RerollNumber == rollAmount && !currentDieIsReroll)
                        {
                            CrossoutIndexes.Add(i);
                            rerollBonusDice++;
                            currentDieIsReroll = true;
                        }
                        else
                        {
                            currentDieIsReroll = false;
                        }
                    }

                    if(Explode)
                    {
                        if((ExplodeThreshold == 0 && rollAmount == DiceSides) ||
                            (ExplodeThreshold > 1 && rollAmount >= ExplodeThreshold)
                            && (explosionBonusDice <= (DiceBot.MaximumDice * 2)))
                        {
                            if(TextFormat)
                            {
                                BoldedIndexes.Add(i);
                            }
                            explosionBonusDice++;
                        }
                    }
                }

                bool useKeptRollsTotal = false;
                List<int> keptRolls = new List<int>();

                if(RerollNumber != 0)
                {
                    for (int i = 0; i < Rolls.Count; i++ )
                    {
                        if (CrossoutIndexes.Contains(i))
                        {
                        }
                        else
                        {
                            keptRolls.Add(Rolls[i]);
                        }
                    }

                    useKeptRollsTotal = true;
                }
                else if(CountOver != 0)
                {
                    List<int> countedRollsIndexes = new List<int>();
                    for (int i = 0; i < Rolls.Count; i++)
                    {
                        if (Rolls[i] > CountOver)
                        {
                            countedRollsIndexes.Add(i);
                            keptRolls.Add(1);
                        }
                    }

                    useKeptRollsTotal = true;
                    HighlightedIndexes = countedRollsIndexes;
                }
                else if (CountUnder != 0)
                {
                    List<int> countedRollsIndexes = new List<int>();

                    for (int i = 0; i < Rolls.Count; i++)
                    {
                        if (Rolls[i] < CountUnder)
                        {
                            countedRollsIndexes.Add(i);
                            keptRolls.Add(1);
                        }
                    }

                    useKeptRollsTotal = true;
                    HighlightedIndexes = countedRollsIndexes;
                }
                else if(KeepHighest != 0)
                {
                    List<int> highestRollsIndexes = new List<int>();
                    List<int> highestRollsValues = new List<int>();

                    int index = 0;
                    foreach(int j in Rolls)
                    {
                        if(highestRollsIndexes.Count < KeepHighest)
                        {
                            highestRollsIndexes.Add(index);
                            highestRollsValues.Add(j);
                        }
                        else
                        {
                            int min = highestRollsValues.Min();
                            if(min < j)
                            {
                                int indexOfLowValue = highestRollsValues.IndexOf(min);
                                highestRollsIndexes.RemoveAt(indexOfLowValue);
                                highestRollsValues.RemoveAt(indexOfLowValue);
                                highestRollsIndexes.Add(index);
                                highestRollsValues.Add(j);
                            }
                        }
                        index++;
                    }

                    useKeptRollsTotal = true;
                    keptRolls = highestRollsValues;
                    HighlightedIndexes = highestRollsIndexes;
                }
                else if(KeepLowest != 0)
                {
                    List<int> highestRollsIndexes = new List<int>();
                    List<int> highestRollsValues = new List<int>();

                    int index = 0;
                    foreach (int j in Rolls)
                    {
                        if (highestRollsIndexes.Count < KeepLowest)
                        {
                            highestRollsIndexes.Add(index);
                            highestRollsValues.Add(j);
                        }
                        else
                        {
                            int max = highestRollsValues.Max();
                            if (max > j)
                            {
                                int indexOfLowValue = highestRollsValues.IndexOf(max);
                                highestRollsIndexes.RemoveAt(indexOfLowValue);
                                highestRollsValues.RemoveAt(indexOfLowValue);
                                highestRollsIndexes.Add(index);
                                highestRollsValues.Add(j);
                            }
                        }
                        index++;
                    }

                    useKeptRollsTotal = true;
                    keptRolls = highestRollsValues;
                    HighlightedIndexes = highestRollsIndexes;
                }
                else if (RemoveHighest != 0)
                {
                    List<int> highestRollsIndexes = new List<int>();
                    List<int> highestRollsValues = new List<int>();

                    int index = 0;
                    foreach (int j in Rolls)
                    {
                        if (highestRollsIndexes.Count < RemoveHighest)
                        {
                            highestRollsIndexes.Add(index);
                            highestRollsValues.Add(j);
                        }
                        else
                        {
                            int min = highestRollsValues.Min();
                            if (min < j)
                            {
                                int indexOfLowValue = highestRollsValues.IndexOf(min);
                                highestRollsIndexes.RemoveAt(indexOfLowValue);
                                highestRollsValues.RemoveAt(indexOfLowValue);
                                highestRollsIndexes.Add(index);
                                highestRollsValues.Add(j);
                            }
                        }
                        index++;
                    }

                    useKeptRollsTotal = true;

                    keptRolls = Utils.CopyList(Rolls);
                    while (highestRollsValues.Count > 0)
                    {
                        int value = highestRollsValues[0];
                        int indexOfValue = keptRolls.IndexOf(value);
                        keptRolls.RemoveAt(indexOfValue);
                        highestRollsValues.RemoveAt(0);
                    }
                    
                    HighlightedIndexes = highestRollsIndexes;
                }
                else if (RemoveLowest != 0)
                {
                    List<int> highestRollsIndexes = new List<int>();
                    List<int> highestRollsValues = new List<int>();

                    int index = 0;
                    foreach (int j in Rolls)
                    {
                        if (highestRollsIndexes.Count < RemoveLowest)
                        {
                            highestRollsIndexes.Add(index);
                            highestRollsValues.Add(j);
                        }
                        else
                        {
                            int max = highestRollsValues.Max();
                            if (max > j)
                            {
                                int indexOfLowValue = highestRollsValues.IndexOf(max);
                                highestRollsIndexes.RemoveAt(indexOfLowValue);
                                highestRollsValues.RemoveAt(indexOfLowValue);
                                highestRollsIndexes.Add(index);
                                highestRollsValues.Add(j);
                            }
                        }
                        index++;
                    }

                    useKeptRollsTotal = true;
                    keptRolls = Utils.CopyList(Rolls);
                    while (highestRollsValues.Count > 0)
                    {
                        int value = highestRollsValues[0];
                        int indexOfValue = keptRolls.IndexOf(value);
                        keptRolls.RemoveAt(indexOfValue);
                        highestRollsValues.RemoveAt(0);
                    }

                    HighlightedIndexes = highestRollsIndexes;
                }
                
                if (useKeptRollsTotal)
                    Total = keptRolls.Sum();
                else
                    Total = Rolls.Sum();
            }
            catch(Exception exc)
            {
                Error = true;
                ErrorString = "exception occured: " + exc.ToString();
            }
        }

        public string PrintRollsList(List<int> rollsList, DiceRollFormat rollFormat = DiceRollFormat.OjEicon6)
        {
            if (rollsList == null || rollsList.Count == 0)
                return "";

            string rtnString = "";

            bool noFormats = (CrossoutIndexes == null || CrossoutIndexes.Count() == 0) &&
                (HighlightedIndexes == null || HighlightedIndexes.Count() == 0) &&
                (BoldedIndexes == null || BoldedIndexes.Count() == 0);
            string shownDice = "";
            bool powerTenDie = DiceSides.ToString().Count(a => a == '1') == 1 && DiceSides.ToString().Count(a => a == '0') > 0;
            //show vanity dice before the main roll: the main roll will show the crossed out/ highlighted/ etc dice
            if (!noFormats && rollFormat != DiceRollFormat.Text && (DiceSides == 6 || (powerTenDie)) && DiceRolled <= 10)
            {
                foreach (int i in rollsList)
                {
                    string addition = GetDiceResult(rollFormat, true, DiceSides, i);

                    shownDice += addition;
                }
                shownDice += " ";
            }
            int count = 0;
            int digits = DiceSides.ToString().Length - 1;
            bool showEicon = rollFormat != DiceRollFormat.Text && ((DiceSides == 6 && DiceRolled <= 10) || ((powerTenDie) && DiceRolled <= (10 / digits))) && noFormats;
            foreach(int i in rollsList)
            {
                string addition = GetDiceResult(rollFormat, showEicon,DiceSides, i);
                
                if (addition.Length < 15 && rtnString.Length > 0)
                    rtnString += ", ";
                else if (addition.Length > 15 && powerTenDie && rtnString.Length > 0)
                {
                    rtnString += ", ";
                }

                if(CrossoutIndexes != null && CrossoutIndexes.Contains(count))
                {
                    addition = "[s]" + addition + "[/s]";
                }

                if (HighlightedIndexes != null && HighlightedIndexes.Contains(count))
                {
                    addition = "[color=yellow]" + addition + "[/color]";
                }
                else if (BoldedIndexes != null && BoldedIndexes.Contains(count))
                {
                    addition = "[b]" + addition + "[/b]";
                }

                rtnString += addition;
                count++;
            }

            return shownDice + rtnString;
        }

        public string GetDiceResult( DiceRollFormat rollFormat, bool showEicon, int sides, int dieRoll)
        {
            string addition = dieRoll.ToString();
            if (showEicon)
            {
                if (rollFormat == DiceRollFormat.GoldEicon6)
                {
                    if(sides == 6)
                    {
                        addition = "[eicon]dbgoldd6-" + dieRoll + "[/eicon]";
                    }
                    else
                    {
                        addition = "";
                        //d10s/100/1000/10000 etc
                        string roll10String = GetRollD10String(dieRoll, sides);
                        int startIndex = 0;
                        if(roll10String.StartsWith("10") && roll10String.Length == sides.ToString().Length)
                        {
                            addition += "[eicon]dbgoldd10-10[/eicon]";
                            startIndex = 2;
                        }
                        for (int i = startIndex; i < roll10String.Length; i++ )
                        {
                            addition += "[eicon]dbgoldd10-" + roll10String[i] + "[/eicon]";
                        }
                    }
                }
                else
                {
                    if(sides == 6)
                    {
                        if (dieRoll == 1)
                        {
                            addition = "[eicon]dboj-dice1[/eicon]";
                        }
                        else
                        {
                            addition = "[eicon]oj-dice" + dieRoll + "[/eicon]";
                        }
                    }
                    else
                    {
                        addition = "";
                        //d10s/100/1000/10000 etc
                        string roll10String = GetRollD10String(dieRoll, sides);
                        int startIndex = 0;
                        if (roll10String.StartsWith("10") && roll10String.Length == sides.ToString().Length)
                        {
                            addition += "[eicon]dbredd10-10[/eicon]";
                            startIndex = 2;
                        }
                        for (int i = startIndex; i < roll10String.Length; i++)
                        {
                            addition += "[eicon]dbredd10-" + roll10String[i] + "[/eicon]";
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
        
        public string GetConditionsString()
        {
            string explodeStr = "";

            if(Rolls.Count >= DiceBot.MaximumRolls)
            {
                explodeStr += "(Maximum roll count reached.)";
            }

            if (Explode)
            {
                string explosionNumberPrint = (ExplodeThreshold > 0 ? ExplodeThreshold + "+" : DiceSides.ToString());
                explodeStr += "(exploding on " + explosionNumberPrint + ") ";
            }
            if (RerollNumber > 0)
                return explodeStr + "(re-rolling once on " + RerollNumber + ") ";
            if (CountOver > 0)
                return explodeStr + "(# of rolls over " + CountOver + ") ";
            if (CountUnder > 0)
                return explodeStr + "(# of rolls under " + CountUnder + ") ";
            if (KeepHighest > 0)
                return explodeStr + "(keeping highest " + KeepHighest + ") ";
            if (KeepLowest > 0)
                return explodeStr + "(keeping lowest " + KeepLowest + ") ";
            if (RemoveHighest > 0)
                return explodeStr + "(removing highest " + RemoveHighest + ") ";
            if (RemoveLowest > 0)
                return explodeStr + "(removing lowest " + RemoveLowest + ") ";

            return explodeStr;
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
}
