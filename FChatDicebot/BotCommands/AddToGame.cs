using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class AddToGame : ChatBotCommand
    {
        public AddToGame()
        {
            Name = "addtogame";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            //get player name to remove
            string allInputs = Utils.GetUserNameFromFullInputs(rawTerms);

            if (allInputs.Contains("_noreplace_"))
            {
                allInputs.Replace("_noreplace_", "");
            }
            else
            {
                foreach (string s in bot.DiceBot.PossibleGames.Select(a => a.GetGameName()))
                {
                    allInputs = allInputs.Replace(s, "");
                }
            }

            allInputs = allInputs.Trim();

            AddPlayerToGame.Run(bot, commandController, rawTerms, terms, characterName, channel, command, allInputs, Name);
        }
    }
}
