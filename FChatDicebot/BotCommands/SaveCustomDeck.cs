using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class SaveCustomDeck : ChatBotCommand
    {
        public SaveCustomDeck()
        {
            Name = "savecustomdeck";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string saveJson = Utils.GetFullStringOfInputs(rawTerms);
            string sendMessage = "";

            try
            {
                SavedData.ChannelSettings channelSettings = bot.GetChannelSettings(address);
                //accept JSON format deck
                SavedDeck d = JsonConvert.DeserializeObject<SavedDeck>(saveJson);

                FChatDicebot.DiceFunctions.Deck newDeck = new DiceFunctions.Deck(DiceFunctions.DeckType.Custom);

                newDeck.CreateFromDeckList(d.DeckList);

                string newDeckId = Utils.SanitizeInput(d.DeckId).Trim().Replace(" ", "_").ToLower();

                d.DeckId = newDeckId;
                d.OriginCharacter = address.character;

                var thisCharacterDecks = bot.SavedDecks.Where(a => a.OriginCharacter == address.character);

                SavedDeck existingDeck = Utils.GetDeckFromId(bot.SavedDecks, newDeckId);

                if (newDeck == null)
                {
                    sendMessage = "Error: Deck could not be created from input.";
                }
                else if (channelSettings != null && Utils.GetNsfwError(channelSettings, newDeck, out sendMessage))
                {
                    //sendMessage set in error method
                }
                else if (thisCharacterDecks.Count() >= BotMain.MaximumSavedTablesPerCharacter && existingDeck == null)
                {
                    sendMessage = "Failed: A character can only save up to " + BotMain.MaximumSavedTablesPerCharacter + " decks at one time. Delete or overwrite old decks.";
                }
                else if (existingDeck != null && existingDeck.OriginCharacter != address.character)
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
                    PrintSetting printSetting = new PrintSetting() { FourColorPlayingCards = false, SortCards = false, TarotIcons = false };

                    SavedDeck newSavedDeck = new SavedDeck()
                    {
                        DeckList = newDeck.GetDeckList(printSetting),
                        DeckId = newDeckId,
                        OriginCharacter = address.character
                    };

                    if (existingDeck != null)
                    {
                        existingDeck.Copy(newSavedDeck);
                    }
                    else
                    {
                        bot.SavedDecks.Add(newSavedDeck);
                    }

                    commandController.SaveCustomDecksToDisk();

                    bot.DiceBot.ResetDeck(false, 1, address, printSetting, DeckType.Custom, newDeckId);
                    sendMessage = "[b]Success[/b]. Deck saved by " + TextFormat.GetCharacterUserTags(address.character) + ". Draw from this deck using !drawcard deck:" + newDeckId;
                }
            }
            catch (Exception)
            {
                sendMessage = "Failed to parse deck entry data. Make sure the Json is correctly formatted.";
            }

            SendMessageToChannelOrUser(bot, commandController, address, sendMessage);
        }
    }
}
