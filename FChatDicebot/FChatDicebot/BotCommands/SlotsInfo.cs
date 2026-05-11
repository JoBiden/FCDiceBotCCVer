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
    public class SlotsInfo : ChatBotCommand
    {
        public SlotsInfo()
        {
            Name = "slotsinfo";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            bool debugInfo = false;
            if (terms.Contains("debug") && Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, characterName))
            {
                debugInfo = true;
            }

            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            string slotsName = commandController.GetNonNumberWordFromCommandTerms(terms);

            SavedSlotsSetting savedSlots = Utils.GetSlotsFromId(bot.SavedSlots, slotsName);

            bool fromChannel = commandController.MessageCameFromChannel(channel);

            if (!fromChannel || (thisChannel != null && thisChannel.AllowTableInfo && thisChannel.AllowSlots))
            {
                SlotsSetting usedSlots = null;
                //find possibilities from channel settings for slots
                if (savedSlots != null)
                    usedSlots = savedSlots.SlotsSetting;
                else if (thisChannel != null)
                    usedSlots = commandController.GetDefaultSlotsSetting(thisChannel.DefaultSlotsFruit);
                else
                    usedSlots = commandController.GetDefaultSlotsSetting(false);

                //spin slots for 3 results
                string sendMessage = usedSlots.PrintInformation(debugInfo);
                //get graphics for results

                bot.SendMessageInChannel(sendMessage, channel);
            }
            else
            {
                bot.SendMessageInChannel("This channel's settings for " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + " do not allow one of slots or showing table info.", channel);
            }
        }
    }
}
