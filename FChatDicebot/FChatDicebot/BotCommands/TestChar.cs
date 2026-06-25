using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class TestChar : ChatBotCommand
    {
        public TestChar()
        {
            Name = "testchar";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            int numberCharacters = 0;
            string output = "";

            if (terms.Length > 2)
            {
                output = "Failed: requires exactly 1 or 2 terms [# chars] [char to duplicate or nothing]";
            }
            else
            {
                if (terms.Length > 0)
                {
                    int.TryParse(terms[0], out numberCharacters);
                }

                if (numberCharacters <= 0)
                {
                    output = "Failed: requires exactly 1 or 2 terms [# chars] [char to duplicate or nothing]";
                }
                else if (terms.Length < 2)
                {
                    output = "[b][ADMIN] [/b]" + Utils.GetStringOfNLength(numberCharacters);
                }
                else if (terms.Length == 2)
                {
                    output = "[b][ADMIN] [/b]" + Utils.GetStringOfNLength(numberCharacters, terms[1]);
                }
            }

            if (!commandController.MessageCameFromChannel(address))
            {
                bot.SendPrivateMessage(output, address);
            }
            else
            {
                bot.SendMessageInChannel(output, address);
            }
        }
    }
}
