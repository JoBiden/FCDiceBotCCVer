using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;

namespace FChatDicebot.BotCommands
{
    public class CoinFlip : ChatBotCommand
    {
        public CoinFlip()
        {
            Name = "coinflip";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            double seed = bot.DiceBot.random.NextDouble();

            string result = "[color=yellow]Flipping a coin! ... ... ... The result is:[/color] [color=orange][b]HEADS[/b][/color] [eicon]dbheads[/eicon]";
            if(seed >= 0.5)
                result = "[color=yellow]Flipping a coin! ... ... ... The result is:[/color] [color=blue][b]TAILS[/b][/color] [eicon]dbtails[/eicon]";

            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(result, characterName);
            }
            else
            {
                bot.SendMessageInChannel(result, channel);
            }

        }
    }
}
