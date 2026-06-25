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
    public class RemovePile : ChatBotCommand
    {
        public RemovePile()
        {
            Name = "removepile";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            bool characterIsAdmin = Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, address.character);

            if (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && command.ops == null)
            {
                bot.RequestChannelOpListAndQueueFurtherRequest(command);
            }
            else if ((thisChannel.ChipsClearance == ChipsClearanceLevel.DicebotAdmin && !characterIsAdmin) ||
                (thisChannel.ChipsClearance == ChipsClearanceLevel.ChannelOp && !command.ops.Contains(address.character) && !characterIsAdmin))
            {
                bot.SendMessageInChannel(TextFormat.GetCharacterUserTags(address.character) + " cannot perform [" + Name + "] under the current chip settings for this channel.", address);
            }
            else if (thisChannel.AllowChips)
            {
                string targetedName = address.character;
                if(rawTerms != null && rawTerms.Length > 0)
                {
                    targetedName = Utils.GetUserNameFromFullInputs(rawTerms);
                }

                MessageAddress removedAddress = new MessageAddress() { character = targetedName, channel = address.channel, guild = address.guild };

                string messageString = bot.DiceBot.RemoveChipsPile(removedAddress);

                commandController.SaveChipsToDisk("RemovePile");

                bot.SendMessageInChannel(messageString, address);
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
        }
    }
}
