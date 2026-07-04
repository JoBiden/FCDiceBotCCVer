using FChatDicebot.SavedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class SlotsSetting : ICustomUserContent
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
        public List<int> RewardTriggerIndexes;
        public List<int> DoubleSetTriggerIndexes;

        public List<List<double>> JackpotAccumulationBonusMultipliers;
        public List<List<double>> JackpotOddsIncreaseMultipliers;

        public List<List<double>> ScoreBasedRewards;
        public bool FixSlot2;
        public bool FixSlot3;
        public bool Nsfw;

        public SlotsSpinResult GetSpinResult(System.Random rnd, int chipsBet, int rewardMultiplier, int currentJackpot, FChatDicebot.BotCommands.SlotsTestCommand testCommand)
        {
            if(ScoreBasedRewards != null && ScoreBasedRewards.Count > 0)
            {
                return GetScoreBasedSpinResult(rnd, chipsBet, rewardMultiplier, currentJackpot, testCommand);
            }

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

            amountAdded = MultiplyJackpotAddition(currentJackpot, amountAdded);

            //double jackpotChanceMultiplier = 1;
            //if(JackpotOddsIncreaseMultipliers != null && JackpotOddsIncreaseMultipliers.Count() > 0)
            //{
            //    foreach (List<double> multipliers in JackpotOddsIncreaseMultipliers)
            //    {
            //        if (currentJackpot < (int)(Math.Floor(StartingJackpotAmount * multipliers[0])))
            //        {
            //            jackpotChanceMultiplier = multipliers[1];
            //            break;
            //        }
            //    }
            //}

            //amountAdded = (amountAdded / 5) * 5; //round to 5s for int

            amountAdded = amountAdded * rewardMultiplier;

            result.AmountAddedToJackpot = amountAdded;
            result.NewJackpotAmount = currentJackpot + amountAdded;
            result.WonJackpot = false;
            result.RewardMultiplierApplied = rewardMultiplier;

            List<SlotRoll> allRolls = new List<SlotRoll>() { roll1, roll2, roll3 };

            var groupedRolls = allRolls.GroupBy(roll => roll.Id);

            var oneSameIds = groupedRolls.Where(group => group.Count() == 1);
            var twoSameIds = groupedRolls.FirstOrDefault(group => group.Count() == 2);
            var threeSameIds = groupedRolls.FirstOrDefault(group => group.Count() == 3);

            //if (twoSameIds != null && twoSameIds.Count() > 0 && twoSameIds.First().WinsJackpot)
            //{
            //    while(jackpotChanceMultiplier > 1)
            //    {
            //        jackpotChanceMultiplier--;

            //        //reroll #3 until you run out of chances or jackpots
            //        SlotRoll newRoll = GetSlotsRoll(rnd, new List<SlotRoll>());

            //        if(newRoll.WinsJackpot)
            //        {
            //            if(!roll1.WinsJackpot)
            //                roll1 = newRoll;
            //            if(!roll2.WinsJackpot)
            //                roll2 = newRoll;
            //            if(!roll3.WinsJackpot)
            //                roll3 = newRoll;
            //            break;
            //        }
            //    }
            //}

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
                bool rewardTrigger = false;
                if (PenaltyTriggerIndexes != null || RewardTriggerIndexes != null)
                {
                    foreach (var qq in oneSameIds)
                    {
                        var thisElement = qq.First();
                        if (PenaltyTriggerIndexes != null && PenaltyTriggerIndexes.Contains(thisElement.Id))
                            penaltyTrigger = true;
                        if (RewardTriggerIndexes != null && RewardTriggerIndexes.Contains(thisElement.Id))
                            rewardTrigger = true;
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
                    else if (rewardTrigger)
                    {
                        result.LossMessage = entry.GiftMessage;
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

        public SlotsSpinResult GetScoreBasedSpinResult(System.Random rnd, int chipsBet, int rewardMultiplier, int currentJackpot, FChatDicebot.BotCommands.SlotsTestCommand testCommand)
        {
            SlotsSpinResult result = new SlotsSpinResult();

            //reset all the ids to be sequential
            for (int i = 0; i < SlotsEntry.Count; i++)
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

            if (testCommand != null && testCommand.Jackpot)
            {
                roll1 = SlotsEntry.FirstOrDefault(a => a.WinsJackpot);
                roll2 = SlotsEntry.FirstOrDefault(a => a.WinsJackpot);
                roll3 = SlotsEntry.FirstOrDefault(a => a.WinsJackpot);
            }


            int originaljackpot = currentJackpot;
            int amountAdded = roll1.AmountAddedToJackpot + roll2.AmountAddedToJackpot + roll3.AmountAddedToJackpot;

            amountAdded = MultiplyJackpotAddition(currentJackpot, amountAdded);

            result.Winnings = 0;
            result.WonJackpot = false;
            result.NewJackpotAmount = currentJackpot + amountAdded;

            if (amountAdded > 0 && roll1.Id == roll2.Id && roll1.Id == roll3.Id)
            {
                if (roll1.WinsJackpot)
                {
                    result.Winnings += originaljackpot;
                    result.AmountAddedToJackpot = 0;
                    result.NewJackpotAmount = StartingJackpotAmount;
                    result.WonJackpot = true;

                    return result;
                }
            }
            else
            {
                int totalScore = roll1.ScoreAdded + roll2.ScoreAdded + roll3.ScoreAdded;

                ScoreBasedRewards = ScoreBasedRewards.OrderByDescending(a => a[0]).ToList();
                foreach(List<double> tier in ScoreBasedRewards)
                {
                    if(tier[0] <= totalScore)
                    {
                        result.Winnings = (int) Math.Floor(tier[1]);
                        break;
                    }
                }

                result.LossMessage = "[sub](total points: " + totalScore + ")[/sub]";
            }

            amountAdded = amountAdded * rewardMultiplier;

            result.AmountAddedToJackpot = amountAdded;
            result.RewardMultiplierApplied = rewardMultiplier;
            result.Winnings = result.Winnings * rewardMultiplier;

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

        public int MultiplyJackpotAddition(int currentJackpot, int currentAmountAdded)
        {
            if (JackpotAccumulationBonusMultipliers != null && JackpotAccumulationBonusMultipliers.Count > 0)
            {
                foreach (List<double> multipliers in JackpotAccumulationBonusMultipliers)
                {
                    if (currentJackpot < (int)(Math.Floor(StartingJackpotAmount * multipliers[0])))
                    {
                        currentAmountAdded = (int)(Math.Floor(currentAmountAdded * multipliers[1]));
                        break;
                    }
                }
            }
            return currentAmountAdded;
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
                if (ScoreBasedRewards != null && ScoreBasedRewards.Count > 0)
                {
                    foreach (SlotRoll t in SlotsEntry)
                    {
                        if (!string.IsNullOrEmpty(rtnString))
                            rtnString += "\n";
                        string reward = t.WinsJackpot ? "[color=yellow]JACKPOT[/color]" : t.ScoreAdded.ToString();
                        //string penalty = string.IsNullOrEmpty(t.PenaltyMessage) ? "" : " (has [color=red]penalty[/color])";
                        //string giftMsg = string.IsNullOrEmpty(t.GiftMessage) ? "" : " (has [color=green]gift[/color])";
                        //string match2Reward = t.Match2Winnings == 0 ? "" : " OR x2 = [color=green]$" + t.Match2Winnings + "[/color] [i](with third high roll)[/i] , adds " + t.AmountAddedToJackpot + " to jackpot when rolled.";
                        rtnString += t.Printout + " Points: [color=green]" + reward + "[/color]";
                        // + match2Reward + penalty + giftMsg;
                    }
                }
                else
                {
                    foreach (SlotRoll t in SlotsEntry)
                    {
                        if (!string.IsNullOrEmpty(rtnString))
                            rtnString += "\n";
                        string reward = t.WinsJackpot ? "[color=yellow]JACKPOT[/color]" : t.Match3Winnings.ToString();
                        string penalty = string.IsNullOrEmpty(t.PenaltyMessage) ? "" : " (has [color=red]penalty[/color])";
                        string giftMsg = string.IsNullOrEmpty(t.GiftMessage) ? "" : " (has [color=green]gift[/color])";
                        string match2Reward = t.Match2Winnings == 0 ? "" : " OR x2 = [color=green]$" + t.Match2Winnings + "[/color] [i](with third high roll)[/i] , adds " + t.AmountAddedToJackpot + " to jackpot when rolled.";
                        rtnString += t.Printout + "x3 = [color=green]$" + reward + "[/color]" + match2Reward + penalty + giftMsg;
                    }
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
            string rewardRolls = PenaltyTriggerIndexes == null ? "" : "\n2-set reward indexes: " + string.Join(", ", RewardTriggerIndexes);

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

            string jackpotScoreThresholds = "";
            if (JackpotOddsIncreaseMultipliers != null)
            {
                foreach (List<double> doublo in JackpotOddsIncreaseMultipliers)
                {
                    if (!string.IsNullOrEmpty(jackpotScoreThresholds))
                    {
                        jackpotScoreThresholds += ", ";
                    }
                    jackpotScoreThresholds += "x" + doublo[1].ToString("F1") + " until $" + (doublo[0] * StartingJackpotAmount).ToString("F0");
                }
                jackpotScoreThresholds = "\nJackpot score thresholds: " + jackpotScoreThresholds;
            }

            if (ScoreBasedRewards != null && ScoreBasedRewards.Count() > 0)
            {
                highRolls = "\n";
                penaltyRolls = "";
                rewardRolls = "";

                foreach (List<double> dt in ScoreBasedRewards)
                {
                    if (!string.IsNullOrEmpty(rewardRolls))
                        rewardRolls += "\n";

                    rewardRolls += "[color=yellow]" + dt[0] + " points[/color] = [color=green]$" + dt[1] + "[/color]";
                }
            }

            return baseString + "\nSpin Rewards:\n" + entries + highRolls + penaltyRolls + rewardRolls + additional + jackpotBonusThresholds + jackpotScoreThresholds;
        }

        public bool IsNsfw()
        {
            return Nsfw;
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

            string rewardString = "";
            if(Winnings > 0)
            {
                if(WonJackpot)
                {
                    winString = FChatDicebot.TextFormat.Emoji("fireworks") + "[b][color=yellow]J[/color] [color=orange]A[/color] [color=yellow]C[/color] [color=orange]K[/color] [color=yellow]P[/color] [color=orange]O[/color] [color=yellow]T[/color][/b]" + FChatDicebot.TextFormat.Emoji("fireworks");
                }
                else
                {
                    winString = FChatDicebot.TextFormat.Emoji("confetti") + "[color=green]W[/color][color=cyan]I[/color][color=white]N[/color][color=pink]N[/color][color=red]E[/color][color=orange]R[/color]" + FChatDicebot.TextFormat.Emoji("confetti");
                }

                rewardString = " " + (RewardMultiplierApplied > 1 ? "[color=yellow](x" + RewardMultiplierApplied + " bonus!)[/color=yellow] = " : "") + "[color=green]$" + Winnings.ToString() + "!!![/color]";
            }

            if (!string.IsNullOrEmpty(LossMessage))
                rewardString += " " + LossMessage;

            return RawSpin + " " + winString + rewardString;
        }

        public string GetJackpotString(string currency)
        {
            if (WonJackpot || NewJackpotAmount == 0)
                return "";
            else
                return "[sub]The jackpot ticks up to " + NewJackpotAmount + " " + currency + ".[/sub]";
        }
    }
}
