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
    public class SetChannelDescription : ChatBotCommand
    {
        public SetChannelDescription()
        {
            Name = "setchanneldescription";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = true;
            RequireBotIsChannelAdmin = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string newStatus = Utils.GetFullStringOfInputs(rawTerms);

            SavedData.ChannelSettings thisChannel = bot.GetChannelSettings(address);
            bool fromChannel = commandController.MessageCameFromChannel(address);

            if (fromChannel && thisChannel != null && !thisChannel.AllowSettingChannelDescription && !Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, address.character))
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
                return;
            }
            bot.SetChannelDescription(address.GetChannelKey(), newStatus);


            if (!fromChannel)
            {
                bot.SendPrivateMessage("[b][ADMIN] Channel Description updated.[/b]", address);
            }
            else
            {
                bot.SendMessageInChannel("[b][ADMIN] Channel Description updated.[/b]", address);
            }
        }
    }
}
