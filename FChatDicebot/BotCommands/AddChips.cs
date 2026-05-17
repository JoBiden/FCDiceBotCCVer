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
    public class AddChips : ChatBotCommand
    {
        public AddChips()
        {
            Name = "addchips";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            bool characterIsAdmin = Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, characterName);
            bool characterIsTrustedForChannel = Utils.IsCharacterTrusted(bot.AccountSettings.TrustedCharacters, characterName, channel);

            if (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && command.ops == null)
            {
                bot.RequestChannelOpListAndQueueFurtherRequest(command);
            }
            else if ((thisChannel.ChipsClearance == ChipsClearanceLevel.DicebotAdmin && !characterIsAdmin && !characterIsTrustedForChannel) ||
                (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && !command.ops.Contains(characterName) && !characterIsAdmin))
            {
                bot.SendMessageInChannel(Utils.GetCharacterUserTags(characterName) + " cannot perform [" + Name + "] under the current chip settings for this channel.", channel);
            }
            else if (thisChannel.AllowChips)
            {
                bool pot = false;

                int chipAmount = Utils.GetNumberFromInputs(terms);

                if (terms != null && terms.Length >= 1 && terms.Contains("pot"))
                    pot = true;

                string messageString = "";
                if (chipAmount <= 0)
                {
                    messageString = "Error: You must specify a number of chips above 0 to add.";
                }
                else
                {
                    messageString = bot.DiceBot.AddChips(characterName, channel, chipAmount, pot);

                    commandController.SaveChipsToDisk("AddChips");
                }

                bot.SendMessageInChannel(messageString, channel);
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", channel);
            }
        }
    }
}
