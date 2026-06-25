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
    public class ShuffleDeck : ChatBotCommand
    {
        public ShuffleDeck()
        {
            Name = "shuffledeck";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            bool fullShuffle = false;
            if (terms != null && terms.Length >= 1 && terms.Contains("eh"))
                fullShuffle = true;

            SavedData.ChannelSettings channelSettings = bot.GetChannelSettings(address);

            string customDeckName = "";
            DeckType deckType = commandController.GetDeckTypeFromCommandTerms(terms, out customDeckName);

            string deckTypeString = Utils.GetDeckTypeStringHidePlaying(deckType, address.character);

            bot.DiceBot.ShuffleDeck(bot.DiceBot.random, address, channelSettings.CardPrintSetting, deckType, fullShuffle, customDeckName);
            bot.SendMessageInChannel("[i]" + deckTypeString + "Channel deck shuffled. " + (fullShuffle ? "Hands emptied." : "") + "[/i]", address);
        }
    }
}
