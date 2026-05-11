using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;

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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            CardCommandOptions options = new CardCommandOptions(commandController, terms, characterName);
            options.characterDrawName = commandController.GetCharacterDrawNameFromCommandTerms(characterName, terms);

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
                bot.SendMessageInChannel("Error: You cannot specify 0 cards to draw.", channel);
                return;
            }

            string deckTypeString = Utils.GetDeckTypeStringHidePlaying(options.deckType, options.deckTypeId);
            string cardDrawingCharacterString = Utils.GetCharacterStringFromSpecialName(options.characterDrawName);

            string trueDraw = "";
            string drawOutput = bot.DiceBot.DrawCards(numberDrawn, options.jokers, options.deckDraw, channel, options.deckType, options.deckTypeId, options.characterDrawName, options.secretDraw, options.fromExtraDeckType, options.extraDeckTypeId, out trueDraw);
            string channelMessageOutput = "[i]" + cardDrawingCharacterString + ": " + deckTypeString + "Card" + options.cardsS + " drawn:[/i] " + drawOutput;
            if (options.secretDraw && !(options.characterDrawName == DiceBot.DealerPlayerAlias || options.characterDrawName == DiceBot.BurnCardsPlayerAlias || options.characterDrawName == DiceBot.DiscardPlayerAlias))
            {
                string playerMessageOutput = "[i]" + cardDrawingCharacterString + ": " + deckTypeString + "Card" + options.cardsS + " drawn:[/i] " + trueDraw;
                bot.SendPrivateMessage(playerMessageOutput, characterName);
            }

            bot.SendMessageInChannel(channelMessageOutput, channel);
        }
    }
}
