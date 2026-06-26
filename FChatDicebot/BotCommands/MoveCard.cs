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
    public class MoveCard : ChatBotCommand
    {
        public MoveCard()
        {
            Name = "movecard";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            CardPileId fromPile = CardPileId.NONE;
            CardPileId toPile = CardPileId.NONE;
            bool firstFound = false;
            #region findPiles
            foreach(string s in terms)
            {
                if(s == "hand")
                {
                    if(firstFound)
                        toPile = CardPileId.Hand;
                    else
                    {
                        fromPile = CardPileId.Hand;
                        firstFound = true;
                    }
                }
                else if (s == "play")
                {
                    if (firstFound)
                        toPile = CardPileId.Play;
                    else
                    {
                        fromPile = CardPileId.Play;
                        firstFound = true;
                    }
                }
                else if (s == "hiddeninplay" || s == "hidden")
                {
                    if (firstFound)
                        toPile = CardPileId.HiddenInPlay;
                    else
                    {
                        fromPile = CardPileId.HiddenInPlay;
                        firstFound = true;
                    }
                }
                else if (s == "deck")
                {
                    if (firstFound)
                        toPile = CardPileId.Deck;
                    else
                    {
                        fromPile = CardPileId.Deck;
                        firstFound = true;
                    }
                }
                else if (s == "discard")
                {
                    if (firstFound)
                        toPile = CardPileId.Discard;
                    else
                    {
                        fromPile = CardPileId.Discard;
                        firstFound = true;
                    }
                }
                else if (s == "burn")
                {
                    if (firstFound)
                        toPile = CardPileId.Burn;
                    else
                    {
                        fromPile = CardPileId.Burn;
                        firstFound = true;
                    }
                }
                else if (s == "dealer")
                {
                    if (firstFound)
                        toPile = CardPileId.Dealer;
                    else
                    {
                        fromPile = CardPileId.Dealer;
                        firstFound = true;
                    }
                }
            }
#endregion

            if (fromPile == CardPileId.Deck)
            {
                bot.SendMessageInChannel("Error: cannot move cards from 'deck' with this command. Try 'drawcard'.", address);
                return;
            }
            if (fromPile == CardPileId.NONE || toPile == CardPileId.NONE)
            {
                bot.SendMessageInChannel("Error: please specify a FROM pile and then a TO pile by name (hand/play/discard/hidden/deck/burn/dealer).", address);
                return;
            }
            if (fromPile == toPile)
            {
                bot.SendMessageInChannel("Error: cannot move cards to the same pile.", address);
                return;
            }

            MoveCards.Run(bot, commandController, rawTerms, terms, address, command, fromPile, toPile);
        }
    }
}
