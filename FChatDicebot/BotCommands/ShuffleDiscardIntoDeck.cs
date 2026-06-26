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
    public class ShuffleDiscardIntoDeck : ChatBotCommand
    {
        public ShuffleDiscardIntoDeck()
        {
            Name = "shufflediscardintodeck";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string rtn = "";
            terms.Concat(new string[] { "all" });
            MoveCards.Run(bot, commandController, rawTerms, terms, address, command, CardPileId.Discard, CardPileId.Deck);
            rtn += "Transferred Discard pile into deck.";
            SavedData.ChannelSettings channelSettings = bot.GetChannelSettings(address);

            bool fullShuffle = false;
            if (terms != null && terms.Length >= 1 && terms.Contains("eh"))
                fullShuffle = true;

            string customDeckName = "";
            DeckType deckType = commandController.GetDeckTypeFromCommandTerms(terms, out customDeckName);

            string deckTypeString = Utils.GetDeckTypeStringHidePlaying(deckType, address.character);

            bot.DiceBot.ShuffleDeck(bot.DiceBot.random, address, channelSettings.CardPrintSetting, deckType, fullShuffle, customDeckName);
            bot.SendMessageInChannel("[i]" + deckTypeString + rtn + " Channel deck shuffled. " + (fullShuffle ? "Hands emptied." : "") + "[/i]", address);

        }
    }
}
