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
    public class ShowDecks : ChatBotCommand
    {
        public ShowDecks()
        {
            Name = "showdecks";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = null;
            if (commandController.MessageCameFromChannel(channel))
                thisChannel = bot.GetChannelSettings(channel);

            bool fromChannel = commandController.MessageCameFromChannel(channel);

            if (!fromChannel || ( thisChannel != null && thisChannel.AllowGames ))
            {
                string sendMessage = "_";
                List<SavedDeck> relevantDecks = bot.SavedDecks;
                if(!terms.Contains("all"))
                {
                    relevantDecks = relevantDecks.Where(a => a.OriginCharacter == characterName).ToList();
                }

                if (relevantDecks.Count == 0)
                {

                    sendMessage = "No decks found.";
                    if(!terms.Contains("all"))
                    {
                        sendMessage = "No decks found created by " + Utils.GetCharacterUserTags(characterName) + ".";
                    }
                }
                else
                {
                    sendMessage = "Decks found:";

                    List<string> defaultDecks = new List<string>() { "playing", "tarot", "manythings", "uno", "rumble", "rumbleextra" };
                    string defaultDeckString = string.Join(", ", defaultDecks);
                    sendMessage += "\nDefault Decks: " + defaultDeckString + "";
                        
                    relevantDecks = relevantDecks.OrderBy(a => a.OriginCharacter).ToList();

                    string decksMessage = "";
                    string currentCharacter = "";
                    foreach (SavedDeck savedDeck in relevantDecks)
                    {
                        if(currentCharacter != savedDeck.OriginCharacter)
                        {
                            currentCharacter = savedDeck.OriginCharacter;

                            if(!string.IsNullOrEmpty(decksMessage))
                            {
                                sendMessage += decksMessage;
                                decksMessage = "";
                            }
                            sendMessage += "\n" + Utils.GetCharacterUserTags(savedDeck.OriginCharacter) + ": ";
                        }
                        if(!string.IsNullOrEmpty(decksMessage))
                        {
                            decksMessage += ", ";
                        }

                        decksMessage += savedDeck.DeckId;
                    }

                    sendMessage += decksMessage;
                }


                if (!fromChannel)
                {
                    bot.SendPrivateMessage(sendMessage, characterName);
                }
                else
                {
                    bot.SendMessageInChannel(sendMessage, channel);
                }
            }
            else
            {
                if(fromChannel)
                    bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", channel);
            }
            
        }
    }
}
