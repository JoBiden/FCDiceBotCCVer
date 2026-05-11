using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class EndHand : ChatBotCommand
    {
        public EndHand()
        {
            Name = "endhand";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string deckTypeId = "";
            DeckType deckType = commandController.GetDeckTypeFromCommandTerms(terms, out deckTypeId);
            SavedData.ChannelSettings channelSettings = bot.GetChannelSettings(channel);
            bool reveal = false;
            if (terms != null && terms.Contains("reveal"))
                reveal = true;

            string deckTypeString = Utils.GetDeckTypeStringHidePlaying(deckType, deckTypeId);
            string rtn = bot.DiceBot.EndHand(channel, reveal, channelSettings.CardPrintSetting, deckType, deckTypeId);
            bot.SendMessageInChannel("[i]" + rtn + "[/i]", channel);
        }
    }
}
