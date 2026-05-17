using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands.Base
{
    public class MoveCards
    {
        public static void Run(BotMain bot, BotCommandController commandController, 
            string[] rawTerms, string[] terms, string characterName, string channel, 
            UserGeneratedCommand command, CardPileId moveFrom, CardPileId moveTo)// CardMoveType moveType)
        {
            CardCommandOptions options = new CardCommandOptions(commandController, terms, characterName);
            if (moveFrom != CardPileId.Hand) //!(moveType == CardMoveType.DiscardCard || moveType == CardMoveType.PlayCard))
                options.redraw = false;
            if ((moveTo == CardPileId.HiddenInPlay || moveFrom == CardPileId.HiddenInPlay) && !terms.Contains("reveal"))
                options.secretDraw = true;

            //string customDeckString = Utils.GetCustomDeckName(characterName);
            string deckTypeString = Utils.GetDeckTypeStringHidePlaying(options.deckType, options.deckTypeId);
            string cardDrawingCharacterString = Utils.GetCharacterStringFromSpecialName(options.characterDrawName);

            SavedData.ChannelSettings channelSettings = bot.GetChannelSettings(channel);

            int numberDiscards = 0;

            string actionString = "";
            actionString = bot.DiceBot.MoveCardsFromTo(options.moveCardsList, options.all, options.secretDraw, channel, options.deckType, options.deckTypeId, options.characterDrawName, moveFrom, moveTo, out numberDiscards);

            if(numberDiscards > 1 || numberDiscards == 0)
            {
                options.cardsS = "s";
            }
            string moveTypeText = deckTypeString + "Card" + options.cardsS + " moved " + Utils.GetCardMoveTypeString(moveFrom, moveTo);
            string messageOutput = "[i]" + cardDrawingCharacterString + ": " + moveTypeText + ":[/i] " + actionString;
                //Utils.GetCardMoveTypeString(moveType) + "[/i] " + actionString;
            string trueDraw = "";
            string privateMessageDraw = "";
            if (options.redraw)
            {
                messageOutput += "\n [i]Redrawn:[/i] " + bot.DiceBot.DrawCards(numberDiscards, options.jokers, options.deckDraw, channel, options.deckType, options.deckTypeId, options.characterDrawName, options.secretDraw, options.fromExtraDeckType, options.extraDeckTypeId, out trueDraw);
                privateMessageDraw = "[i]Redrawn:[/i] " + trueDraw;
            }

            if (options.secretDraw && options.redraw && !(options.characterDrawName == DiceBot.DealerPlayerAlias || options.characterDrawName == DiceBot.BurnCardsPlayerAlias || options.characterDrawName == DiceBot.DiscardPlayerAlias))
            {
                bot.SendPrivateMessage(privateMessageDraw, characterName);
            }
            else if (moveFrom == CardPileId.Hand || moveTo == CardPileId.Hand || moveFrom == CardPileId.HiddenInPlay || moveTo == CardPileId.HiddenInPlay)
            {
                string privateOutput = "";
                Hand thisCharacterHand = bot.DiceBot.GetHand(channel, options.deckType, options.deckTypeId, characterName);
                if(thisCharacterHand != null)
                {
                    privateOutput += "[i]After " + moveTypeText + " Current [b]Hand[/b]: [/i]" + thisCharacterHand.Print(false, channelSettings.CardPrintSetting);
                }
                Hand thisCharacterHidden = bot.DiceBot.GetHand(channel, options.deckType, options.deckTypeId, characterName + DiceBot.HiddenPlaySuffix);
                if (thisCharacterHidden != null && thisCharacterHidden.CardsCount() > 0)
                {
                    if (!string.IsNullOrEmpty(privateOutput))
                        privateOutput += "\n";
                    privateOutput += "[i]After " + moveTypeText + " Current [b]Hidden Cards[/b] in Play: [/i]" + thisCharacterHidden.Print(false, channelSettings.CardPrintSetting);
                }
                if(!string.IsNullOrEmpty(privateOutput))
                {
                    bot.SendPrivateMessage(privateOutput, characterName);
                }
            }

            if (options.secretDraw)
            {
                string redrawSecretString = options.redraw ? " (and redrew)" : "";
                string newMessageOutput = "[i]" + cardDrawingCharacterString + ": " + deckTypeString + numberDiscards + " Card" + options.cardsS + " moved " + Utils.GetCardMoveTypeString(moveFrom, moveTo) + " (secret)" + redrawSecretString + "[/i] ";
                bot.SendMessageInChannel(newMessageOutput, channel);
            }
            else
            {
                bot.SendMessageInChannel(messageOutput, channel);
            }
        }
    }

    public class CardCommandOptions
    {
        public string characterDrawName;
        public DeckType deckType;
        public string deckTypeId;

        public DeckType fromExtraDeckType;
        public string extraDeckTypeId;

        public bool secretDraw;

        public bool all;

        public bool redraw;
        public bool jokers;
        public bool deckDraw;

        public List<int> moveCardsList = new List<int>();
        public string cardsS;

        public CardCommandOptions(BotCommandController commandController, string[] terms, string characterName)
        {
            deckDraw = true;
            all = false;
            redraw = false;
            secretDraw = false;
            characterDrawName = characterName;// commandController.GetCharacterDrawNameFromCommandTerms(characterName, terms);
            if (characterDrawName == DiceBot.DiscardPlayerAlias)
                characterDrawName = characterName;

            if (terms != null && terms.Length >= 1 && terms.Contains("all"))
                all = true;
            if (terms != null && terms.Length >= 1 && terms.Contains("redraw"))
                redraw = true;
            //if (terms != null && terms.Length >= 1 && (terms.Contains("reveal")))
            //    secretDraw = false;
            if (terms != null && terms.Length >= 1 && (terms.Contains("s") || terms.Contains("secret")))
                secretDraw = true;
            if (terms != null && terms.Length >= 1 && terms.Contains("j"))
                jokers = true;
            if (terms != null && terms.Length >= 1 && terms.Contains("nodeck"))
                deckDraw = false;

            List<int> discardsTemp = Utils.GetAllNumbersFromInputs(terms);

            //decrease all the numbers by 1 to match array indexes, rather than the card position for a player
            if (discardsTemp.Count > 0)
            {
                foreach (int i in discardsTemp)
                {
                    moveCardsList.Add(i - 1);
                }
            }

            if (moveCardsList.Count > 1)
                cardsS = "s";

            deckType = commandController.GetDeckTypeFromCommandTerms(terms, out deckTypeId);
            fromExtraDeckType = commandController.GetExtraDeckTypeFromCommandTerms(terms, out extraDeckTypeId);
        }

        public CardCommandOptions(string character, DeckType deckType)
        {
            characterDrawName = character;
            this.deckType = deckType;
        }

    }

    public enum CardRevealState
    {
        NONE,
        ForcedSecret,
        Default,
        ForcedReveal,
        ForcedRevealFull
    }
}
