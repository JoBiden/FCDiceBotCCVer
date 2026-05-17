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
    public class StartGame : ChatBotCommand
    {
        public StartGame()
        {
            Name = "startgame";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            if (thisChannel.AllowGames)
            {
                string messageString = "";

                IGame gametype = commandController.GetGameTypeForCommand(bot.DiceBot, channel, terms, out messageString);

                bool keepSession = terms.Contains("keepsession") || terms.Contains("keepgame");
                bool endSession = terms.Contains("endsession") || terms.Contains("endgame");

                if(gametype != null) //gametype can be set above after being null so this should no longer be 'else'
                {
                    GameSession sesh = bot.DiceBot.GetGameSession(channel, gametype);

                    if (sesh != null)
                    {
                        ChipPile potChips = bot.DiceBot.GetChipPile(DiceBot.PotPlayerAlias, channel);

                        if(sesh.Ante > 0 && potChips.Chips > 0)
                        {
                            messageString = "The pot already has chips in it. The pot must be empty before starting a game with ante.";
                        }
                        else if (sesh.CurrentGame.GetMinPlayers() > sesh.Players.Count)
                        {
                            messageString = "Cannot start " + sesh.CurrentGame.GetGameName() + " because there are currently less than [b]" + sesh.CurrentGame.GetMinPlayers() + " players[/b].";
                        }
                        else if (sesh.State == GameState.GameInProgress)
                        {
                            messageString = "Cannot start " + sesh.CurrentGame.GetGameName() + " because the game is already in progress. Please finish the current round first.";
                        }
                        else if (sesh.CurrentGame.GetType() == typeof(Roulette) && !bot.DiceBot.CountdownFinishedOrNotStarted(channel, sesh.CurrentGame.GetGameName()))
                        {
                            double secondsRemain = bot.DiceBot.GetSecondsRemainingOnCountdownTimer(channel, gametype.GetGameName());

                            messageString = "Cannot start " + sesh.CurrentGame.GetGameName() + " because the starting countdown is not yet finished. [b]" + secondsRemain.ToString("N3") + " seconds [/b]remain.";
                        }
                        else
                        {
                            messageString = sesh.CurrentGame.GetStartingDisplay();
                            messageString += "\n" + bot.DiceBot.StartGame(channel, characterName, gametype, bot, keepSession, endSession);

                            if (gametype.GetMinimumMsBetweenGames() > 0)
                            {
                                bot.DiceBot.StartCountdownTimer(channel, gametype.GetGameName(), characterName, gametype.GetMinimumMsBetweenGames());
                            }

                            commandController.SaveChipsToDisk("StartGame");
                        }
                    }
                    else
                    {
                        messageString = "Error: Game session for " + gametype.GetGameName() + " not found or created.";
                    }
                }

                bot.SendMessageInChannel(messageString, channel);
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", channel);
            }
        }
    }
}
