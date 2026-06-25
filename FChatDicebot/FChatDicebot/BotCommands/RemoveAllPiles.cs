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
    public class RemoveAllPiles : ChatBotCommand
    {
        public RemoveAllPiles()
        {
            Name = "removeallpiles";
            RequireBotAdmin = true;
            RequireChannelAdmin = true;
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
                //bot.SendMessageInChannel("This command has been deactivated to prevent accidents. Use this command for each pile you need removed: !removePile NAME", address);
                string messageString = bot.DiceBot.RemoveAllChipsPiles(address);

                commandController.SaveChipsToDisk("RemoveAllPiles");

                bot.SendMessageInChannel(messageString, address);
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
        }
    }
}
