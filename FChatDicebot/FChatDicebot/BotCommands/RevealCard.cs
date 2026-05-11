using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class RevealCard : ChatBotCommand
    {
        public RevealCard()
        {
            Name = "revealcard";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            List<string> newTerms = new List<string>();
            newTerms.AddRange(terms.Where( a=> a != "s" && a != "secret"));
            newTerms.Add("reveal");
            string[] replacementTerms = newTerms.ToArray();
            string deckTypeId = "";
            DeckType deckType = commandController.GetDeckTypeFromCommandTerms(terms, out deckTypeId);
            Hand h = bot.DiceBot.GetHand(channel, deckType, deckTypeId, characterName + DiceBot.HiddenPlaySuffix);
            if (h == null || h.CardsCount() == 0)
                bot.SendMessageInChannel("(No secret cards inn play to reveal)", channel);
            else
            {
                MoveCards.Run(bot, commandController, rawTerms, replacementTerms, characterName, channel, command, CardPileId.HiddenInPlay, CardPileId.Play);
            }
        }
    }
}
