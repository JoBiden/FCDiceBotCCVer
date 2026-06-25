using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FChatDicebot
{
    class Program
    {
        static void Main(string[] args)
        {
            BotMain m = new BotMain();

            if (BotMain._debug)
                Console.WriteLine("Created Bot. Starting Run...");
            List<string> lowerArgs = new List<string>();
            if (args != null && args.Count() > 0)
            {
                foreach (string s in args)
                {
                    lowerArgs.Add(s.ToLower());
                }
            }
            RunMode runMode = RunMode.FlistPlusDiscord;

            if (BotMain._testVersion)
                runMode = RunMode.Default;
            else if (lowerArgs.Contains("-flist"))
                runMode = RunMode.FListOnly;
            else if (lowerArgs.Contains("-discord"))
                runMode = RunMode.DiscordOnly;
            else if (lowerArgs.Contains("-both") || lowerArgs.Contains("-flistplusdiscord"))
                runMode = RunMode.FlistPlusDiscord;
            else if (lowerArgs.Contains("-none") || lowerArgs.Contains("-noserver"))
                runMode = RunMode.NONE;

            m.Run(runMode);

            if (BotMain._debug)
                Console.WriteLine("Run loop exited.");

            Console.ReadLine();
        }
    }
}
