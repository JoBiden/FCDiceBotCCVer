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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
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
                    if (characterName == deleteDeck.OriginCharacter)
                    {
                        bot.SavedDecks.Remove(deleteDeck);

                        sendMessage = "[b]" + deleteDeck.DeckId + "[/b] deleted by [user]" + characterName + "[/user]";

                        Utils.WriteToFileAsData(bot.SavedDecks, Utils.GetTotalFileName(BotMain.FileFolder, BotMain.SavedDecksFileName));
                    }
                    else
                    {
                        sendMessage = "Only " + deleteDeck.OriginCharacter + " can delete their own saved deck.";
                    }
                }
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
