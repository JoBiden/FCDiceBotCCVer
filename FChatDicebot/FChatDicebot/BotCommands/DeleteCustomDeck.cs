using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using FChatDicebot.SavedData;
using Newtonsoft.Json;

namespace FChatDicebot.BotCommands
{
    public class DeleteCustomDeck : ChatBotCommand
    {
        public DeleteCustomDeck()
        {
            Name = "deletecustomdeck";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            string sendMessage = "";
            if(terms.Length != 1)
            {
                sendMessage = "Failed: requires one term (deckId)";
            }
            else
            {
                string deckTypeId = terms[0];

                SavedDeck deleteDeck = Utils.GetDeckFromId(bot.SavedDecks, deckTypeId);

                if (deleteDeck == null)
                {
                    sendMessage = "Failed: No decks found named " + deckTypeId + "";
                }
                else
                {
                    if (address.character == deleteDeck.OriginCharacter)
                    {
                        bot.SavedDecks.Remove(deleteDeck);

                        sendMessage = "[b]" + deleteDeck.DeckId + "[/b] deleted by " + TextFormat.GetCharacterUserTags(address.character);

                        Utils.WriteToFileAsData(bot.SavedDecks, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedDecksFileName));
                    }
                    else
                    {
                        sendMessage = "Only " + deleteDeck.OriginCharacter + " can delete their own saved deck.";
                    }
                }
            }

            if (!commandController.MessageCameFromChannel(address))
            {
                bot.SendPrivateMessage(sendMessage, address);
            }
            else
            {
                bot.SendMessageInChannel(sendMessage, address);
            }
        }
    }
}
