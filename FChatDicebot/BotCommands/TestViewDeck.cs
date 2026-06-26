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
    public class TestViewDeck : ChatBotCommand
    {
        public TestViewDeck()
        {
            Name = "testviewdeck";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string customDeckName = "";
            DeckType deckType = commandController.GetDeckTypeFromCommandTerms(terms, out customDeckName);
            SavedData.ChannelSettings channelSettings = bot.GetChannelSettings(address);

            string deckTypeString = Utils.GetDeckTypeStringHidePlaying(deckType, address.character);

            string deckList = bot.DiceBot.ViewEntireDeck(address, channelSettings.CardPrintSetting, deckType, customDeckName);
            bot.SendMessageInChannel("[i]" + deckTypeString + "Channel deck list. :[/i]" + deckList, address);
        }
    }
}
