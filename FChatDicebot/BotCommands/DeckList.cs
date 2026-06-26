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
    public class DeckList : ChatBotCommand
    {
        public DeckList()
        {
            Name = "decklist";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string customDeckName = "";
            DeckType deckType = commandController.GetDeckTypeFromCommandTerms(terms, out customDeckName);

            string deckTypeString = Utils.GetDeckTypeStringHidePlaying(deckType, customDeckName);

            var channelSettings = bot.GetChannelSettings(address);

            Deck a = bot.DiceBot.GetDeck(address, deckType, customDeckName);
            string sendString = "";
            if (a != null && Utils.GetNsfwError(channelSettings, a, out sendString))
            {
                //sendMessage set in error method
            }
            else if (a != null)
            {
                PrintSetting printset = new PrintSetting();
                if(channelSettings.CardPrintSetting != null)
                    printset = new PrintSetting() { SortCards = false, FourColorPlayingCards = channelSettings.CardPrintSetting.FourColorPlayingCards, TarotIcons = channelSettings.CardPrintSetting.TarotIcons };

                sendString = "[i]" + deckTypeString + "Channel deck contents: [/i]" + a.Print(false, printset);
                //sendString = Utils.LimitStringToNCharacters(sendString, BotMain.MaximumCharsInMessageChannel);
            }
            else
                sendString = "[i]Error: " + deckTypeString + " deck not found[/i]";

            bot.SendMessageInChannel(sendString, address);
        }
    }
}
