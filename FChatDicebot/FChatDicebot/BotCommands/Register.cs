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
    public class Register : ChatBotCommand
    {
        public Register()
        {
            Name = "register";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            if (thisChannel.AllowChips)
            {
                string messageString = "";

                ChipPile existing = bot.DiceBot.GetChipPile(address, false);
                if(existing != null)
                {
                    messageString = TextFormat.GetCharacterUserTags(address.character) + " is already registered.";
                }
                else
                {
                    messageString = TextFormat.GetCharacterUserTags(address.character) + " was registered for a " + BotMain.CurrencyPlaceholder + "s pile.";

                    if(thisChannel.StartingChips > 0)
                        messageString += "\n[b]" + thisChannel.StartingChips + " " + BotMain.CurrencyPlaceholderCapital + "s[/b] were given to " + TextFormat.GetCharacterUserTags(address.character) + " to start.";

                    bot.DiceBot.AddChips(address, 0, false);

                    commandController.SaveChipsToDisk("Register");
                }

                bot.SendMessageInChannel(messageString, address);
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
        }
    }
}
