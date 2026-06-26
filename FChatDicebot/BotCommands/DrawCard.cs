using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands
{
    public class DrawCard : ChatBotCommand
    {
        public DrawCard()
        {
            Name = "drawcard";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            CardCommandOptions options = new CardCommandOptions(commandController, terms, address.character);
            options.characterDrawName = commandController.GetCharacterDrawNameFromCommandTerms(address.character, terms);

            //TODO: (?done?) when changing secret draw options, this should get removed and merged into CardCommandOptions
            if (terms != null && terms.Length >= 1 && (terms.Contains("reveal")))
                options.secretDraw = false;
            else
                options.secretDraw = true;

            int numberDrawn = Utils.GetNumberFromInputs(terms);
            if (numberDrawn > 1)
                options.cardsS = "s";
            if (numberDrawn == 0 && terms.Contains("0"))
            {
                bot.SendMessageInChannel("Error: You cannot specify 0 cards to draw.", address);
                return;
            }

            string deckTypeString = Utils.GetDeckTypeStringHidePlaying(options.deckType, options.deckTypeId);
            string cardDrawingCharacterString = Utils.GetCharacterStringFromSpecialName(options.characterDrawName);

            ChannelSettings thisChannel = thisChannel = bot.GetChannelSettings(address);
            SavedDeck existingDeck = bot.SavedDecks.FirstOrDefault(a => a.DeckId == options.deckTypeId);

            string errorMessage = "";
            if (existingDeck != null && Utils.GetNsfwError(thisChannel, existingDeck, out errorMessage))
            {
                //sendMessage set in error method
                bot.SendMessageInChannel(errorMessage, address);
                return;
            }

            //string trueDraw = "";
            MessageAddress drawCardsAddress = new MessageAddress() { character = options.characterDrawName, channel = address.channel, guild = address.guild };
            DrawCardResult drawOutput = bot.DiceBot.DrawCards(numberDrawn, options.jokers, options.deckDraw, options.deckType, options.deckTypeId, drawCardsAddress, options.secretDraw, options.fromExtraDeckType, options.extraDeckTypeId);
            string channelMessageOutput = "[i]" + cardDrawingCharacterString + ": " + deckTypeString + "Card" + options.cardsS + " drawn:[/i] " + drawOutput;
            if (options.secretDraw && !(options.characterDrawName == DiceBot.DealerPlayerAlias || options.characterDrawName == DiceBot.BurnCardsPlayerAlias || options.characterDrawName == DiceBot.DiscardPlayerAlias))
            {
                string playerMessageOutput = "[i]" + cardDrawingCharacterString + ": " + deckTypeString + "Card" + options.cardsS + " drawn:[/i] " + drawOutput.TrueDraw;
                bot.SendPrivateMessage(playerMessageOutput, address);
            }

            bot.SendMessageInChannel(channelMessageOutput, address);
        }
    }
}
