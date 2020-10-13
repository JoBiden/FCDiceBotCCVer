﻿using System;
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

            m.Run();

            if (BotMain._debug)
                Console.WriteLine("Run loop exited.");

            Console.ReadLine();
        }
    }
}
