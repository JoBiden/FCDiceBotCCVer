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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            bool debugInfo = false;
            if (terms.Contains("debug") && Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, address.character))
            {
                debugInfo = true;
            }

            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            string slotsName = commandController.GetNonNumberWordFromCommandTerms(terms);

            SavedSlotsSetting savedSlots = Utils.GetSlotsFromId(bot.SavedSlots, slotsName);

            bool fromChannel = commandController.MessageCameFromChannel(address);

            if (!fromChannel || (thisChannel != null && thisChannel.AllowTableInfo && thisChannel.AllowSlots))
            {
                SlotsSetting usedSlots = null;
                //find possibilities from channel settings for slots
                if (savedSlots != null)
                    usedSlots = savedSlots.SlotsSetting;
                else if (thisChannel != null)
                    usedSlots = commandController.GetDefaultSlotsSetting(thisChannel.DefaultSlotsType);
                else
                    usedSlots = commandController.GetDefaultSlotsSetting(SlotsType.Default);

                //spin slots for 3 results
                string sendMessage = usedSlots.PrintInformation(debugInfo);
                //get graphics for results

                if (thisChannel != null && Utils.GetNsfwError(thisChannel, usedSlots, out sendMessage))
                {
                    //sendMessage set in error method
                    SendMessageToChannelOrUser(bot, commandController, address, sendMessage);
                }
                else if (!commandController.MessageCameFromChannel(address))
                {
                    bot.SendPrivateMessage(sendMessage, address);
                }
                else
                {
                    bot.SendMessageInChannel(sendMessage, address);
                }
            }
            else
            {
                bot.SendMessageInChannel("This channel's settings for " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + " do not allow one of slots or showing table info.", address);
            }
        }
    }
}
