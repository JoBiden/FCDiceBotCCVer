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
    public class RevealPotion : ChatBotCommand
    {
        public RevealPotion()
        {
            Name = "revealpotion";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.CharacterInventories;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {

            Potion p = bot.DiceBot.GetPotionHeld(characterName, channel);

            if(p == null)
            {
                bot.SendMessageInChannel(Utils.GetCharacterUserTags(characterName) + " is not holding a potion.", channel);
            }
            else
            {

                string output = bot.DiceBot.PotionGenerator.GetPotionGenerationOutputString(p, false);

                output = "Showing potion held by " + Utils.GetCharacterIconTags(characterName) + "\n" + output;

                bot.SendMessageInChannel(output, channel);
            }

        }
    }
}
