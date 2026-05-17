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
    public class DeckJson : ChatBotCommand
    {
        public DeckJson()
        {
            Name = "deckjson";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = null;
            if(commandController.MessageCameFromChannel(channel))
                thisChannel = bot.GetChannelSettings(channel);

            bool fromChannel = commandController.MessageCameFromChannel(channel);

            if (!fromChannel || (thisChannel != null && thisChannel.AllowTableInfo))
            {
                string sendMessage = "(no deck found)";

                string deckTypeId = "";
                DeckType deckType = commandController.GetDeckTypeFromCommandTerms(terms, out deckTypeId);

                CardCommandOptions options = new CardCommandOptions(commandController, terms, characterName);

                SavedDeck deck = bot.SavedDecks.FirstOrDefault(a => a.DeckId == options.deckTypeId);

                if (deck != null)
                {
                    sendMessage = "Deck id [b]" + deck.DeckId + "[/b] created by [user]" + deck.OriginCharacter + "[/user]";

                    string potiondesc = "\n" + deck.DeckId + " JSON [sub](note: OriginCharacter field is auto-populated on potion save and does nothing when set in !savecustomdeck)[/sub]:\n";
                    potiondesc += JsonConvert.SerializeObject(deck);

                    sendMessage += potiondesc;
                }

                if (fromChannel)
                {
                    bot.SendMessageInChannel(sendMessage, channel);
                }
                else
                    bot.SendPrivateMessage(sendMessage, characterName);
            }
            else
            {
                if(fromChannel)
                    bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", channel);
            }
            
        }
    }
}
