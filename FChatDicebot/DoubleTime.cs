using Discord;
using Discord.WebSocket;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FChatDicebot
{
    public static class DoubleTime
    {
        private static DateTime Origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime ConvertFromSecondsTimestamp(double timestamp)
        {
            return Origin.AddSeconds(timestamp);
        }

        public static double GetCurrentTimestampMiliseconds()
        {
            return ConvertToMilisecondsimestamp(DateTime.UtcNow);
        }

        public static double GetCurrentTimestampSeconds()
        {
            return ConvertToSecondsTimestamp(DateTime.UtcNow);
        }

        public static double ConvertToSecondsTimestamp(DateTime date)
        {
            TimeSpan diff = date.ToUniversalTime() - Origin;
            return Math.Floor(diff.TotalSeconds);
        }

        public static double ConvertToMilisecondsimestamp(DateTime date)
        {
            TimeSpan diff = date.ToUniversalTime() - Origin;
            return Math.Floor(diff.TotalMilliseconds);
        }

        public static string PrintTimeFromSeconds(double seconds)
        {
            if (seconds < 0)
                seconds = 0;
            string output = seconds.ToString("F1") + " seconds";
            if (seconds > 60)
                output = (seconds / 60).ToString("F1") + " minutes";
            if (seconds > 60 * 60)
                output = (seconds / (60 * 60)).ToString("F2") + " hours";
            if (seconds > 60 * 60 * 24)
                output = (seconds / (60 * 60 * 24)).ToString("F2") + " days";
            return output;
        }

        public static string PrintTimeAllUnitsFromSeconds(double seconds, string separator)
        {
            int days = (int)Math.Floor((seconds / (60 * 60 * 24)));
            double remainder = seconds - (days * (60 * 60 * 24));
            int hours = (int)Math.Floor(remainder / (60 * 60));
            remainder = remainder - (hours * (60 * 60));
            int minutes = (int)Math.Floor(remainder / (60));
            remainder = remainder - (minutes * 60);

            string rtn = "";
            if (days > 0) rtn += days + " day" + getS(days) + separator;
            if (hours > 0) rtn += hours + " hour" + getS(hours) + separator;
            if (minutes > 0) rtn += minutes + " minute" + getS(minutes) + separator;
            if (remainder > 0) rtn += remainder.ToString("F1") + " seconds " + separator;

            return rtn;
        }

        private static string getS(int num)
        {
            return num > 1 ? "s" : "";
        }
    }
}
