using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;

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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            Potion p = bot.DiceBot.GetPotionHeld(address);

            ChannelSettings channelSettings = bot.GetChannelSettings(address);

            string sendMessage = "";

            if (p == null)
            {
                bot.SendMessageInChannel(TextFormat.GetCharacterUserTags(address.character) + " is not holding a potion.", address);
            }
            else if (Utils.GetNsfwError(channelSettings, p.enchantment, out sendMessage))
            {
                //sendMessage set in error method
            }
            else
            {
                sendMessage = bot.DiceBot.PotionGenerator.GetPotionGenerationOutputString(p, false);

                sendMessage = "Showing potion held by " + TextFormat.GetCharacterIconTags(address.character) + "\n" + sendMessage;
            }
            bot.SendMessageInChannel(sendMessage, address);

        }
    }
}
