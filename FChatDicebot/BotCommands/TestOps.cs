using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;

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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            if(command.ops == null)
            {
                bot.RequestChannelOpListAndQueueFurtherRequest(new UserGeneratedCommand(){
                    channel = channel,
                    terms = terms,
                    rawTerms = rawTerms,
                    ops = null,
                    characterName = characterName,
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
                    output += " " + Utils.GetCharacterUserTags(characterName) + " is an op? " + opsList.Contains(characterName);
                }

                bot.SendMessageInChannel("[b][ADMIN] [/b]" + output, channel);
            }
        }
    }
}