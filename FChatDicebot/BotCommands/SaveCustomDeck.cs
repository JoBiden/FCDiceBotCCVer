using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            string saveJson = Utils.GetFullStringOfInputs(rawTerms);
            string sendMessage = "";

            try
            {
                SavedData.ChannelSettings channelSettings = bot.GetChannelSettings(channel);
                //accept JSON format deck
                SavedDeck d = JsonConvert.DeserializeObject<SavedDeck>(saveJson);

                FChatDicebot.DiceFunctions.Deck newDeck = new DiceFunctions.Deck(DiceFunctions.DeckType.Custom);

                newDeck.CreateFromDeckList(d.DeckList);

                string newDeckId = Utils.SanitizeInput(d.DeckId).Trim().Replace(" ", "_").ToLower();

                d.DeckId = newDeckId;
                d.OriginCharacter = characterName;

                var thisCharacterDecks = bot.SavedDecks.Where(a => a.OriginCharacter == characterName);

                SavedDeck existingDeck = Utils.GetDeckFromId(bot.SavedDecks, newDeckId);

                if (newDeck == null)
                {
                    sendMessage = "Error: Deck could not be created from input.";
                }
                else if (thisCharacterDecks.Count() >= BotMain.MaximumSavedTablesPerCharacter && existingDeck == null)
                {
                    sendMessage = "Failed: A character can only save up to " + BotMain.MaximumSavedTablesPerCharacter + " decks at one time. Delete or overwrite old decks.";
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

                    bot.DiceBot.ResetDeck(false, 1, channel, channelSettings.CardPrintSetting, DeckType.Custom, newDeckId);
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
