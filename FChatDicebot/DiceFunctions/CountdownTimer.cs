using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot.DiceFunctions
{
    public class CountdownTimer
    {
        public string TimerId;
        public string ChannelId;
        public string CharacterId;

        public Stopwatch Timer = new Stopwatch();
        public int FinishedMs;

        public void StartTimer()
        {
            Timer.Start();
        }

        public void StopTimer()
        {
            Timer.Stop();
        }

        public int GetMsRemaining()
        {
            int rtn = FinishedMs - (int)Timer.ElapsedMilliseconds;
            if (rtn < 0)
            {
                StopTimer();
                rtn = 0;
            }

            return rtn;
        }

        public double GetSecondsRemaining()
        {
            return ((double) GetMsRemaining()) / 1000;
        }

        public double GetMinutesRemaining()
        {
            return ((double)GetMsRemaining()) / 60000;
        }

        public bool TimerFinished()
        {
            return Timer.ElapsedMilliseconds >= FinishedMs;
        }

    }
}



