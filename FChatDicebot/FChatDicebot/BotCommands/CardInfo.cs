using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands
{
    public class CardInfo : ChatBotCommand
    {
        public CardInfo()
        {
            Name = "cardinfo";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string cardName = Utils.GetFullStringOfInputs(rawTerms);

            List<SavedDeck> possibleDecks = bot.SavedDecks.Where(a => a.DeckList.Contains(cardName)).ToList();
            SavedData.ChannelSettings channelSettings = bot.GetChannelSettings(channel);

            string allReturned = "";

            if(possibleDecks != null && possibleDecks.Count >= 1)
            {
                foreach (SavedDeck saved in possibleDecks)
                {
                    if (!string.IsNullOrEmpty(allReturned))
                        allReturned += ", ";

                    int startIndex = saved.DeckList.IndexOf(cardName);
                    string relevant = saved.DeckList.Substring(startIndex);

                    string[] remainingCards = relevant.Split(',');

                    if(remainingCards[0].Contains('|'))
                    {
                        string[] thisSplit = remainingCards[0].Split('|');
                        DeckCard d = new DeckCard() { specialName = thisSplit[0], description = thisSplit[1] };
                        allReturned += d.FullDescription(channelSettings.CardPrintSetting);
                    }
                    else
                    {
                        DeckCard d = new DeckCard() { specialName = remainingCards[0] };
                        allReturned += d.FullDescription(channelSettings.CardPrintSetting);
                    }

                    allReturned += " (" + saved.DeckId + ")";
                }
            }
            else
            {
                allReturned = "The card '" + cardName + "' was not found.";
            }

            bot.SendMessageInChannel("[i]Card Info: [/i]" + allReturned, channel);
        }
    }
}
