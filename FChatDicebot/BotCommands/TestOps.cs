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
    public class TestOps : ChatBotCommand
    {
        public TestOps()
        {
            Name = "testops";
            RequireBotAdmin = true;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            if(command.ops == null)
            {
                bot.RequestChannelOpListAndQueueFurtherRequest(new UserGeneratedCommand(){
                    channel = address.GetChannelKey(),
                    terms = terms,
                    rawTerms = rawTerms,
                    ops = null,
                    characterName = address.character,
                    commandName = Name
                });
            }
            else
            {
                Console.WriteLine("Channeloprequest completing");
                string[] opsList = command.ops;
                string output = "";
                if (opsList == null)
                {
                    output = "opslist was null";
                }
                else
                {
                    output = Utils.PrintList(opsList);
                    output += " " + TextFormat.GetCharacterUserTags(address.character) + " is an op? " + opsList.Contains(address.character);
                }

                bot.SendMessageInChannel("[b][ADMIN] [/b]" + output, address);
            }
        }
    }
}