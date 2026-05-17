using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands
{
    //NOTE: admin only used to manually edit settings for every channel at once
    public class RestrictChips : ChatBotCommand
    {
        public RestrictChips()
        {
            Name = "restrictchips";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedChannels;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            var channelSettings = bot.GetChannelSettings(channel);

            string clearance = "";
            if (channelSettings.ChipsClearance == ChipsClearanceLevel.DicebotAdmin)
            {
                channelSettings.ChipsClearance = ChipsClearanceLevel.ChannelOp;
                clearance = "[b]Channel Ops[/b] - AddChips is enabled, but only for Channel Ops";
            }
            else
            {
                channelSettings.ChipsClearance = ChipsClearanceLevel.DicebotAdmin;
                clearance = "[b]Restricted[/b] - AddChips is disabled, even for Channel Ops";
            }

            commandController.SaveChannelSettingsToDisk();

            string output = "(Channel setting updated) chips clearance set to " + clearance;

            bot.SendMessageInChannel(output, channel);
        }
    }

}