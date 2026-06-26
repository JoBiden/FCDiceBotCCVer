using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands
{
    public class TestDiscordEmote : ChatBotCommand
    {
        public TestDiscordEmote()
        {
            Name = "testdiscordemote";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings chan = bot.GetChannelSettings(address);
            string responseMessage = "";
            string channelNameLow = address.channel.ToLower();

            if (terms.Contains("a"))
            {
                responseMessage = ":heart:";
            }
            else if (terms.Contains("b"))
            {
                responseMessage = ":Slots2:";
            }
            else if (terms.Contains("c"))
            {
                responseMessage = ":TenDice1:"; //does not work
            }
            else if (terms.Contains("d"))
            {
                responseMessage = ":RegularDice2:";
            }
            else if (terms.Contains("e"))
            {
                responseMessage = ":regulardice2:";
            }
            else if (terms.Contains("f"))
            {
                responseMessage = "<:TenDiceGold10: 1345919928250794115 >"; //does not work
            }
            else if (terms.Contains("g"))
            {
                responseMessage = "<:TenDice4:1345917227291119698>"; //works
            }
            else if (terms.Contains("h"))
            {
                responseMessage = "<:TenDiceGold10:1345919928250794115>"; //does work
            }
            else
            {
                responseMessage = ":fire:";
            }

            bot.SendMessageInChannel(responseMessage, address);
        }
    }
}
