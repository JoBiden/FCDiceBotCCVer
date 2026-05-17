using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class TestSetHand : ChatBotCommand
    {
        public TestSetHand()
        {
            Name = "testsethand";
            RequireBotAdmin = true;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            Hand characterHand = bot.DiceBot.GetHand(channel, DeckType.Playing, null, characterName);
            bool dealerHand = false;
            if(terms.Contains("dealer"))
            {
                dealerHand = true;
                characterHand = bot.DiceBot.GetHand(channel, DeckType.Playing, null, DiceBot.DealerPlayerAlias);
            }

            characterHand.Reset();

            foreach(string s in terms)
            {
                int cardNumber = -1;
                int cardSuit = -1;
                string remainingNumber = s;
                if(s.Contains("♥") )
                {
                    cardSuit = 0;
                    remainingNumber = remainingNumber.Replace("♥","");
                }
                else if(s.Contains("♦"))
                {
                    cardSuit = 1;
                    remainingNumber = remainingNumber.Replace("♦","");
                }
                else if(s.Contains("♠"))
                {
                    cardSuit = 3;
                    remainingNumber = remainingNumber.Replace("♠","");
                }
                else if(s.Contains("♣"))
                {
                    cardSuit = 2;
                    remainingNumber = remainingNumber.Replace("♣","");
                }

                if (cardSuit >= 0)
                {
                    int.TryParse(remainingNumber, out cardNumber);

                    if(cardNumber >= 0)
                    {
                        characterHand.AddCard(new DeckCard() { joker = false, description = "", cardState = "", number = cardNumber, suit = cardSuit }, bot.DiceBot.random);
                    }
                }
            }

            if(dealerHand)
            {
                bot.SendMessageInChannel("The dealer's new hand: " + characterHand.Print(false, null), channel);
            }
            else
            {
                bot.SendPrivateMessage("new hand: " + characterHand.Print(false, null), characterName);
                bot.SendMessageInChannel(Utils.GetCharacterUserTags(characterName) + " new hand: " + characterHand.Print(false, null), channel);
            }
        }
    }
}
