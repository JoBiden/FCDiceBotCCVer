using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class ResetDeck : ChatBotCommand
    {
        public ResetDeck()
        {
            Name = "resetdeck";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            bool jokers = false;
            int deckCopies = 1;
            if (terms != null && terms.Length >= 1)
            {
                if (terms.Contains("j"))
                    jokers = true;
                if (terms.Contains("x2") || terms.Contains("double"))
                    deckCopies = 2;
                if (terms.Contains("x3") || terms.Contains("triple"))
                    deckCopies = 3;
                if (terms.Contains("x4") || terms.Contains("quadrouple"))
                    deckCopies = 4;
                if (terms.Contains("x5") || terms.Contains("quintuple"))
                    deckCopies = 5;
                if (terms.Contains("x6") || terms.Contains("sextuple"))
                    deckCopies = 6;
            }

            string customDeckName ="";
            DeckType deckType = commandController.GetDeckTypeFromCommandTerms(terms, out customDeckName);
            SavedData.ChannelSettings channelSettings = bot.GetChannelSettings(channel);
            
            string deckTypeString = Utils.GetDeckTypeStringHidePlaying(deckType, customDeckName);
            string deckMultiplier = deckCopies > 1 ? " (x" + deckCopies + " decks combined)" : "";

            bot.DiceBot.ResetDeck(jokers, deckCopies, channel, channelSettings.CardPrintSetting, deckType, customDeckName);
            bot.SendMessageInChannel("[i]" + deckTypeString + "Channel deck reset." + deckMultiplier + (jokers ? " (contains jokers)" : "") + "[/i]", channel);
        }
    }
}
