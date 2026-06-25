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
    public class CancelGame : ChatBotCommand
    {
        public CancelGame()
        {
            Name = "cancelgame";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            if (thisChannel.AllowGames)
            {
                string messageString = "";

                IGame gametype = commandController.GetGameTypeFromCommandTerms(bot.DiceBot, terms);
                bool adminCancel = terms.Contains("override");

                if (gametype == null)
                {
                    //check game sessions and see if this channel has a session for anything
                    var gamesPresent = bot.DiceBot.GameSessions.Where(a => a.GetChannelKey().ToLower() == address.GetChannelKey().ToLower());
                    if (gamesPresent.Count() == 0)
                    {
                        messageString = "Error: No game sessions are active in this channel to cancel.";
                    }
                    else if (gamesPresent.Count() > 1)
                    {
                        messageString = "Error: You must specify a game type if more than one game session exists in the channel.";
                    }
                    else if (gamesPresent.Count() == 1)
                    {
                        GameSession sesh = gamesPresent.First();
                        gametype = sesh.CurrentGame;
                    }
                }
                if (gametype != null)
                {
                    GameSession sesh = bot.DiceBot.GetGameSession(address, gametype, false);

                    if (sesh != null)
                    {
                        ChipPile potChips = bot.DiceBot.GetChipPile(new MessageAddress() { character = DiceBot.PotPlayerAlias, channel = address.channel, guild = address.guild });// DiceBot.PotPlayerAlias, address);

                        if(!adminCancel && sesh.Ante > 0 && potChips.Chips > 0)
                        {
                            messageString = "The pot already has " + BotMain.CurrencyPlaceholder + "s in it. The pot must be empty before cancelling a game with ante.";
                        }
                        else
                        {
                            messageString = bot.DiceBot.CancelGame(address, gametype);
                        }
                    }
                    else
                    {
                        messageString = "Error: Game session for " + gametype.GetGameName() + " not found or created.";
                    }
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
