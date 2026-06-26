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
    public class MyDecks : ChatBotCommand
    {
        public MyDecks()
        {
            Name = "mydecks";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            var thisCharacterDecks = bot.SavedDecks.Where(a => a.OriginCharacter == address.character);

            string sendMessage = "No decks found for " + TextFormat.GetCharacterUserTags(address.character);
            bool fromChannel = commandController.MessageCameFromChannel(address);
            bool includeNsfw = fromChannel ? bot.GetChannelSettings(address).AllowNsfw : true;

            if (thisCharacterDecks.Count() > 0)
            {
                string tablesList = "";
                foreach (SavedDeck savedDeck in thisCharacterDecks)
                {
                    if(!savedDeck.Nsfw || includeNsfw)
                    {
                        if (!string.IsNullOrEmpty(tablesList))
                            tablesList += ", ";

                        tablesList += savedDeck.DeckId;
                    }
                }
                sendMessage = "Decks found for " + TextFormat.GetCharacterUserTags(address.character) + ": " + tablesList;
            }

            SendMessageToChannelOrUser(bot, commandController, address, sendMessage);
        }
    }
}
