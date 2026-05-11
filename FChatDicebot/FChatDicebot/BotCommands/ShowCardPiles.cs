using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class ShowCardPiles : ChatBotCommand
    {
        public ShowCardPiles()
        {
            Name = "showcardpiles";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            bool allPiles = false;
            bool revealHand = false;
            bool secretOutput = false;
            string characterDrawName = commandController.GetCharacterDrawNameFromCommandTerms(characterName, terms);
            SavedData.ChannelSettings channelSettings = bot.GetChannelSettings(channel);

            if (terms.Contains("reveal"))
                revealHand = true;

            if (terms.Contains("all"))
                allPiles = true;

            if (terms.Contains("s") || terms.Contains("secret"))
                secretOutput = true;

            string customDeckName = "";
            DeckType deckType = commandController.GetDeckTypeFromCommandTerms(terms, out customDeckName);

            string deckTypeString = Utils.GetDeckTypeStringHidePlaying(deckType, customDeckName);
            Hand h = bot.DiceBot.GetHand(channel, deckType, customDeckName, characterName);

            string displayName = characterDrawName;
            if (displayName.Contains(DiceBot.PlaySuffix))
                displayName = displayName.Replace(DiceBot.PlaySuffix, "");
            if (displayName.Contains(DiceBot.HiddenPlaySuffix))
                displayName = displayName.Replace(DiceBot.HiddenPlaySuffix, "");

            string outputString = "[i]Card piles for " + deckTypeString;

            bool showHandSize = !revealHand;
            if (secretOutput)
                showHandSize = false;
            outputString += "\n[user]" + displayName + "[/user]'s " + h.GetCollectionName() + ": [/i]" + h.Print(showHandSize, channelSettings.CardPrintSetting);

            Hand hiddenplay = bot.DiceBot.GetHand(channel, deckType, customDeckName, characterName + DiceBot.HiddenPlaySuffix);

            if(!secretOutput && h != null)
            {
                string privateOutput = "[i]" + deckTypeString + " " + h.GetCollectionName() + ": [/i]" + h.Print(false, channelSettings.CardPrintSetting);
                if(hiddenplay != null && hiddenplay.CardsCount() > 0)
                {
                    privateOutput += "\n[i]" + deckTypeString + " " + h.GetCollectionName() + ": [/i]" + hiddenplay.Print(false, channelSettings.CardPrintSetting);
                }
                bot.SendPrivateMessage(privateOutput, characterName);
            }

            Hand play = bot.DiceBot.GetHand(channel, deckType, customDeckName, characterName + DiceBot.PlaySuffix);
            outputString += "\n[i]cards in play: [/i]" + play.Print(false, channelSettings.CardPrintSetting);
            if(hiddenplay != null && (hiddenplay.CardsCount() > 0 || allPiles))
            {
                outputString += "\n[i]hidden cards in play: [/i]" + hiddenplay.Print(showHandSize, channelSettings.CardPrintSetting);
            }
            Hand discarded = bot.DiceBot.GetHand(channel, deckType, customDeckName, DiceBot.DiscardPlayerAlias);
            if(discarded != null && (discarded.CardsCount() > 0 || allPiles))
            {
                outputString += "\n[i]discarded cards: [/i]" + discarded.Print(false, channelSettings.CardPrintSetting);
            }

            if (characterDrawName == DiceBot.BurnCardsPlayerAlias || allPiles)
            {
                Hand hNew = bot.DiceBot.GetHand(channel, deckType, customDeckName, DiceBot.BurnCardsPlayerAlias);
                outputString += "\n[i]burned cards: [/i]" + hNew.Print(showHandSize, channelSettings.CardPrintSetting);
            }
            if (characterDrawName == DiceBot.DealerPlayerAlias || allPiles)
            {
                Hand hNew = bot.DiceBot.GetHand(channel, deckType, customDeckName, DiceBot.DealerPlayerAlias);
                outputString += "\n[i]dealer's hand: [/i]" + hNew.Print(showHandSize, channelSettings.CardPrintSetting);
            }

            if(secretOutput)
            {
                bot.SendPrivateMessage(outputString, characterName);
                bot.SendMessageInChannel("Sent information for card piles to " + Utils.GetCharacterUserTags(characterName), channel);
            }
            else
            {
                bot.SendMessageInChannel(outputString, channel);
            }
        }
    }
}
