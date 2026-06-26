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

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = null;
            if(commandController.MessageCameFromChannel(address))
                thisChannel = bot.GetChannelSettings(address);

            bool fromChannel = commandController.MessageCameFromChannel(address);

            if (!fromChannel || (thisChannel != null && thisChannel.AllowTableInfo))
            {
                string sendMessage = "(no deck found)";

                //string deckTypeId = "";
                string deckTypeId = terms[0];
                //DeckType deckType = commandController.GetDeckTypeFromCommandTerms(terms, out deckTypeId);

                //CardCommandOptions options = new CardCommandOptions(commandController, terms, address.character);

                SavedDeck deck = bot.SavedDecks.FirstOrDefault(a => a.DeckId == deckTypeId);// options.deckTypeId);

                if (deck != null && Utils.GetNsfwError(thisChannel, deck, out sendMessage))
                {
                    //sendMessage set in error method
                }
                else if (deck != null)
                {
                    sendMessage = "Deck id [b]" + deck.DeckId + "[/b] created by [user]" + deck.OriginCharacter + "[/user]";

                    string potiondesc = "\n" + deck.DeckId + " JSON [sub](note: OriginCharacter field is auto-populated on potion save and does nothing when set in !savecustomdeck)[/sub]:\n";
                    potiondesc += JsonConvert.SerializeObject(deck);

                    sendMessage += potiondesc;
                }

                if (fromChannel)
                {
                    bot.SendMessageInChannel(sendMessage, address);
                }
                else
                    bot.SendPrivateMessage(sendMessage, address);
            }
            else
            {
                if(fromChannel)
                    bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }

        }
    }
}
