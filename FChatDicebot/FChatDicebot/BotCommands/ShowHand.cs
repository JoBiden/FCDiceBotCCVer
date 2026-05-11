using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class ShowHand : ChatBotCommand
    {
        public ShowHand()
        {
            Name = "showhand";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string characterDrawName = commandController.GetCharacterDrawNameFromCommandTerms(characterName, terms);

            string customDeckName = "";
            DeckType deckType = commandController.GetDeckTypeFromCommandTerms(terms, out customDeckName);

            SavedData.ChannelSettings channelSettings = bot.GetChannelSettings(channel);
            string deckTypeString = Utils.GetDeckTypeStringHidePlaying(deckType, customDeckName);

            Hand h = bot.DiceBot.GetHand(channel, deckType, customDeckName, characterDrawName);

            string displayName = characterDrawName;
            if (displayName.Contains(DiceBot.PlaySuffix))
                displayName = displayName.Replace(DiceBot.PlaySuffix, "");

            string outputString = "[i]" + deckTypeString + "Showing [user]" + displayName + "[/user]'s " + h.GetCollectionName() + ": [/i]" + h.Print(false, channelSettings.CardPrintSetting);
            if (characterDrawName == DiceBot.BurnCardsPlayerAlias)
                outputString = "[i]" + deckTypeString + "Showing burned cards: [/i]" + h.Print(false, channelSettings.CardPrintSetting);
            else if (characterDrawName == DiceBot.DealerPlayerAlias)
                outputString = "[i]" + deckTypeString + "Showing the dealer's hand: [/i]" + h.Print(false, channelSettings.CardPrintSetting);
            else if (characterDrawName == DiceBot.DiscardPlayerAlias)
                outputString = "[i]" + deckTypeString + "Showing discarded cards: [/i]" + h.Print(false, channelSettings.CardPrintSetting);

            bot.SendMessageInChannel(outputString, channel);
        }
    }
}
