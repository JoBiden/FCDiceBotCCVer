using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class SlotsSetting
    {
        public List<SlotRoll> SlotsEntry = new List<SlotRoll>();

        public string Name;
        public string Description;
        public int StartingJackpotAmount;
        public double RerollMatch; //0-1 odds to auto reroll this result once, higher meaning more odds to reroll it .22 in 1.34, .20 in 1.33, higher earlier
        public double RerollJackpot; //0-1 odds to auto reroll this result once, higher meaning more odds to reroll it .47 in 1.34, .45 in 1.33, higher earlier
        public double AutoLossRoll; //0-1 odds to automatically lose by getting 3 symbols that don't match
        public int MinimumBet;
        public List<int> PenaltyTriggerIndexes;
        public List<int> DoubleSetTriggerIndexes;
 
        public List<List<double>> JackpotAccumulationBonusMultipliers;
        public bool FixSlot2;
        public bool FixSlot3;

        public SlotsSpinResult GetSpinResult(System.Random rnd, int chipsBet, int rewardMultiplier, int currentJackpot, FChatDicebot.BotCommands.SlotsTestCommand testCommand)
        {
            SlotsSpinResult result = new SlotsSpinResult();

            //reset all the ids to be sequential
            for(int i = 0; i < SlotsEntry.Count; i++)
            {
                SlotsEntry[i].Id = i;
            }

            SlotRoll roll1 = GetSlotsRoll(rnd, null);
            SlotRoll roll2 = GetSlotsRoll(rnd, new List<SlotRoll> { roll1 });

            if (FixSlot2 || (testCommand != null && testCommand.Fix2))
                roll2 = roll1;
            SlotRoll roll3 = GetSlotsRoll(rnd, new List<SlotRoll> { roll1, roll2 });
            if (FixSlot3 || (testCommand != null && testCommand.Fix3))
                roll3 = roll1;

            double autoLossSeed = rnd.NextDouble();
            if (autoLossSeed <= AutoLossRoll || (testCommand != null && testCommand.Fail))
            {
                Console.WriteLine("auto loss seed triggered " + autoLossSeed + " " + AutoLossRoll);
                roll1 = GetUnmatchingSlotsRoll(rnd, null);
                roll2 = GetUnmatchingSlotsRoll(rnd, new List<SlotRoll> { roll1 });
                roll3 = GetUnmatchingSlotsRoll(rnd, new List<SlotRoll> { roll1, roll2 });
            }

            if(testCommand != null && testCommand.Jackpot)
            {
                roll1 = SlotsEntry.FirstOrDefault(a => a.WinsJackpot);
                roll2 = SlotsEntry.FirstOrDefault(a => a.WinsJackpot);
                roll3 = SlotsEntry.FirstOrDefault(a => a.WinsJackpot);
            }

            result.Winnings = 0;

            int originaljackpot = currentJackpot;
            int amountAdded = roll1.AmountAddedToJackpot + roll2.AmountAddedToJackpot + roll3.AmountAddedToJackpot;

            foreach(List<double> multipliers in JackpotAccumulationBonusMultipliers)
            {
                if(currentJackpot < (int) (Math.Floor(StartingJackpotAmount * multipliers[0])))
                {
                    amountAdded = (int) (Math.Floor(amountAdded *  multipliers[1]));
                    break;
                }
            }

            amountAdded = (amountAdded / 5) * 5; //round to 5s for int

            amountAdded = amountAdded * rewardMultiplier;

            result.AmountAddedToJackpot = amountAdded;
            result.NewJackpotAmount = currentJackpot + amountAdded;
            result.WonJackpot = false;
            result.RewardMultiplierApplied = rewardMultiplier;

            if (roll1.Id == roll2.Id && roll1.Id == roll3.Id)
            {
                if(roll1.WinsJackpot)
                {
                    result.Winnings += originaljackpot;
                    result.AmountAddedToJackpot = 0;
                    result.NewJackpotAmount = StartingJackpotAmount;
                    result.WonJackpot = true;
                }
                else
                {
                    result.Winnings = (int)((double)roll1.Match3Winnings);
                }
            }
            else
            {
                List<SlotRoll> allRolls = new List<SlotRoll>(){roll1, roll2, roll3};

                var groupedRolls = allRolls.GroupBy(roll => roll.Id);

                var oneSameIds = groupedRolls.Where(group => group.Count() == 1);
                var twoSameIds = groupedRolls.FirstOrDefault(group => group.Count() == 2);
                var threeSameIds = groupedRolls.FirstOrDefault(group => group.Count() == 3);

                
                bool doubleSetTrigger = false;
                if(DoubleSetTriggerIndexes != null)
                {
                    foreach (var qq in oneSameIds)
                    {
                        var thisElement = qq.First();
                        if (DoubleSetTriggerIndexes.Contains(thisElement.Id))
                            doubleSetTrigger = true;
                    }
                }

                bool penaltyTrigger = false;
                if(PenaltyTriggerIndexes != null)
                {
                    foreach (var qq in oneSameIds)
                    {
                        var thisElement = qq.First();
                        if (PenaltyTriggerIndexes.Contains(thisElement.Id))
                            penaltyTrigger = true;
                    }
                }

                if(twoSameIds != null && twoSameIds.Count() > 0)
                {
                    var rollWithTwo = twoSameIds.First();
                    int relevantId = rollWithTwo.Id;

                    SlotRoll entry = SlotsEntry.FirstOrDefault(a => a.Id == relevantId);

                    if (penaltyTrigger)
                    {
                        result.LossMessage = entry.PenaltyMessage;
                    }
                    else if (doubleSetTrigger)
                    {
                        result.Winnings = entry.Match2Winnings;
                    }
                    else
                    {
                        result.LossMessage = "[i](There was no high roll with the set)[/i]";
                    }
                }
            }

            if(result.WonJackpot && rewardMultiplier > 1)
            {
                result.Winnings = (1 * originaljackpot) + (rewardMultiplier - 1) * StartingJackpotAmount;
            }
            else
            {
                result.Winnings = result.Winnings * rewardMultiplier;
            }

            result.RawSpin = roll1.Printout + roll2.Printout + roll3.Printout;

            return result;
        }

        public SlotRoll GetSlotsRoll(Random rnd, List<SlotRoll> existingRolls)
        {
            int entries = SlotsEntry.Count;
            int rollAmount = rnd.Next(0, entries);

            if (existingRolls != null && existingRolls.Count == 2 && existingRolls[0].Id == existingRolls[1].Id)
            {
                double rerollSeed = rnd.NextDouble();
                if (!existingRolls[0].WinsJackpot && rollAmount == existingRolls[0].Id && rerollSeed < RerollMatch)
                    rollAmount = rnd.Next(0, entries);//reroll if there's a match y% of the time
                else if (existingRolls[0].WinsJackpot && rollAmount == existingRolls[0].Id && rerollSeed < RerollJackpot)
                    rollAmount = rnd.Next(0, entries);//another reroll for jackpot x% of the time
            }

            return SlotsEntry[rollAmount];

        }

        public SlotRoll GetUnmatchingSlotsRoll(Random rnd, List<SlotRoll> existingRolls)
        {
            int entries = SlotsEntry.Count;
            int rollAmount = rnd.Next(0, entries);
            if(existingRolls != null)
            {
                rollAmount = rnd.Next(0, entries - existingRolls.Count());

                List<int> rollIds = existingRolls.Select(a => a.Id).ToList();
                int safety = 100;
                while(rollIds.Contains(rollAmount) && safety > 0)
                {
                    safety--;
                    if(rollAmount < SlotsEntry.Count() - 1)
                        rollAmount++;
                }
            }

            return SlotsEntry[rollAmount];
        }

        public string GetSlotsEntryList()
        {
            string rtnString = "";
            if (SlotsEntry != null && SlotsEntry.Count > 0)
            {
                foreach (SlotRoll t in SlotsEntry)
                {
                    if (!string.IsNullOrEmpty(rtnString))
                        rtnString += "\n";
                    string reward = t.WinsJackpot ? "[color=yellow]JACKPOT[/color]" : t.Match3Winnings.ToString();
                    string penalty = string.IsNullOrEmpty(t.PenaltyMessage)? "" : " (has [color=red]penalty[/color])";
                    string match2Reward = t.Match2Winnings == 0? "" : " OR x2 = [color=green]$" + t.Match2Winnings + "[/color] [i](with third high roll)[/i] , adds " + t.AmountAddedToJackpot + " to jackpot when rolled.";
                    rtnString += t.Printout + "x3 = [color=green]$" + reward + "[/color]" + match2Reward + penalty;
                }
            }
            return rtnString;
        }

        public string PrintInformation(bool showAdminInfo)
        {
            string entries = GetSlotsEntryList();

            string baseString = "[color=yellow]" + Name + "[/color]\n[i]" + Description + "[/i]\nMinimum Bet: [color=green]$" + MinimumBet + "[/color] Starting Jackpot Amount: [color=green]$" + StartingJackpotAmount + "[/color]";

            string additional = (FixSlot2 || FixSlot3) ? "\n[i](slots currently fixed for higher winning odds)[/i]" : "";
            string adminInfo = "RerollMatch " + RerollMatch + " RerollJackpot " + RerollJackpot;
            if (showAdminInfo)
                additional = additional + "\n" + adminInfo;

            string highRolls = DoubleSetTriggerIndexes == null ? "" : "\n2-set high roll indexes: " + string.Join(", ", DoubleSetTriggerIndexes);
            string penaltyRolls = PenaltyTriggerIndexes == null ? "" : "\n2-set penalty indexes: " + string.Join(", ", PenaltyTriggerIndexes);

            string jackpotBonusThresholds = "";
            if (JackpotAccumulationBonusMultipliers != null)
            {
                foreach(List<double> doublo in JackpotAccumulationBonusMultipliers)
                {
                    if(!string.IsNullOrEmpty(jackpotBonusThresholds))
                    {
                        jackpotBonusThresholds += ", ";
                    }
                    jackpotBonusThresholds += "x" + doublo[1].ToString("F1") + " until $" + (doublo[0] * StartingJackpotAmount).ToString("F0");
                }
                jackpotBonusThresholds = "\nJackpot bonus thresholds: " + jackpotBonusThresholds;
            }

            return baseString + "\n3-Set Rewards:\n" + entries + highRolls + penaltyRolls + additional + jackpotBonusThresholds;
        }
    }

    public class SlotsSpinResult
    {
        public string RawSpin;
        public int Winnings;
        public int AmountAddedToJackpot;
        public int NewJackpotAmount;
        public bool WonJackpot;
        public string LossMessage;
        public int RewardMultiplierApplied;

        public override string ToString()
        {
            string winString = "[i]...you lost this spin.[/i]";
            if (!string.IsNullOrEmpty(LossMessage))
                winString += " " + LossMessage;
            string rewardString = "";
            if(Winnings > 0)
            {
                if(WonJackpot)
                {
                    winString = "[eicon]fireworks[/eicon][b][color=yellow]J[/color] [color=orange]A[/color] [color=yellow]C[/color] [color=orange]K[/color] [color=yellow]P[/color] [color=orange]O[/color] [color=yellow]T[/color][/b][eicon]fireworks[/eicon]";
                }
                else
                {
                    winString = "[eicon]confetti[/eicon][color=green]W[/color][color=cyan]I[/color][color=white]N[/color][color=pink]N[/color][color=red]E[/color][color=orange]R[/color][eicon]confetti[/eicon]";
                }

                rewardString = "[color=green]$" + Winnings.ToString() + "!!![/color]" + (RewardMultiplierApplied > 1 ? "[color=yellow] (x" + RewardMultiplierApplied + " bonus!)[/color=yellow]" : "");
            }


            return RawSpin + " " + winString + " " + rewardString;
        }

        public string GetJackpotString()
        {
            if (WonJackpot)
                return "";
            else
                return "[sub]The jackpot ticks up to $" + NewJackpotAmount + ".[/sub]";
        }
    }
}
