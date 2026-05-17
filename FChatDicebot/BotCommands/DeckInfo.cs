using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class DeckInfo : ChatBotCommand
    {
        public DeckInfo()
        {
            Name = "deckinfo";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string deckTypeId = "";
            DeckType deckType = commandController.GetDeckTypeFromCommandTerms(terms, out deckTypeId);

            string customDeckName = deckTypeId;// Utils.GetCustomDeckName(characterName);
            string deckTypeString = Utils.GetDeckTypeStringHidePlaying(deckType, customDeckName);

            Deck a = bot.DiceBot.GetDeck(channel, deckType, customDeckName);

            string sendString = "";
            if(a != null)
            {
                string cardsString = a.GetCardsRatio();
                string jokersString = a.ContainsJokers() ? " [i](contains jokers)[/i] " : "";
                sendString = "[i]" + deckTypeString + "Channel deck cards remaining: [/i]" + cardsString + jokersString;
            }
            else
            {
                sendString = "[i]Error: " + deckTypeString + " deck not found[/i]";
            }
            bot.SendMessageInChannel(sendString, channel);
        }
    }
}
