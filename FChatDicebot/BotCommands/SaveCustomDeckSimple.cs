using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;

namespace FChatDicebot.BotCommands
{
    public class SaveCustomDeckSimple : ChatBotCommand
    {
        public SaveCustomDeckSimple()
        {
            Name = "savecustomdecksimple";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string saveEntryList = Utils.GetFullStringOfInputs(rawTerms);
            string sendMessage = "";

            try
            {
                SavedData.ChannelSettings channelSettings = bot.GetChannelSettings(channel);
                //accept comma separated list of card names {card text}
                FChatDicebot.DiceFunctions.Deck newDeck = Utils.CreateDeckFromInput(saveEntryList);

                string newDeckId = Utils.SanitizeInput(characterName).Trim().ToLower().Replace(" ", "_") + "_deck";

                var thisCharacterDecks = bot.SavedDecks.Where(a => a.OriginCharacter == characterName);

                SavedDeck existingDeck = Utils.GetDeckFromId(bot.SavedDecks, newDeckId);

                if(newDeck == null)
                {
                    sendMessage = "Error: Deck could not be created from input.";
                }
                else if (thisCharacterDecks.Count() >= BotMain.MaximumSavedTablesPerCharacter && existingDeck == null)
                {
                    sendMessage = "Failed: A character can only save up to 3 decks at one time. Delete or overwrite old decks.";
                }
                else if (existingDeck != null && existingDeck.OriginCharacter != characterName)
                {
                    sendMessage = "Failed: This deck name is taken by a different character.";
                }
                else if (newDeckId.Length < 2)
                {
                    sendMessage = "Failed: Deck name too short.";
                }
                else if (newDeck.GetTotalCards() <= 0)
                {
                    sendMessage = "Failed: No card entries found for this deck.";
                }
                else if (newDeck.GetTotalCards() > BotMain.MaximumCardsInDeck)
                {
                    sendMessage = "Failed: Deck contains more than " + BotMain.MaximumCardsInDeck + " cards.";
                }
                else
                {
                    SavedDeck newSavedDeck = new SavedDeck()
                    {
                        DeckList = newDeck.GetDeckList(channelSettings.CardPrintSetting),
                        DeckId = newDeckId,
                        OriginCharacter = characterName
                    };

                    if (existingDeck != null)
                    {
                        existingDeck.Copy(newSavedDeck);
                    }
                    else
                    {
                        bot.SavedDecks.Add(newSavedDeck);
                    }

                    Utils.WriteToFileAsData(bot.SavedDecks, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedDecksFileName));

                    sendMessage = "[b]Success[/b]. Deck saved by [user]" + characterName + "[/user]. Draw from this deck using !drawcard deck:" + newDeckId;
                }
            }
            catch (Exception)
            {
                sendMessage = "Failed to parse deck entry data. Make sure the Json is correctly formatted.";
            }

            if (!commandController.MessageCameFromChannel(channel))
            {
                bot.SendPrivateMessage(sendMessage, characterName);
            }
            else
            {
                bot.SendMessageInChannel(sendMessage, channel);
            }
        }
    }
}
