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
    public class StartGame : ChatBotCommand
    {
        public StartGame()
        {
            Name = "startgame";
            Aliases = new string[] { };
            Category = "Dicebot";
            ShortDescription = "Start the current game session once enough players have joined";
            LongDescription = "Starts the game session for the given game type once the minimum player count has been met. Add 'keepsession' to keep the session open for another round after this one finishes, or 'endsession' to close it out.";
            Usage = "!startgame {game name}\nor\n!startgame {game name} keepsession\nor\n!startgame {game name} endsession";
            RelatedCommands = new string[] { "joingame", "gamestatus", "gamecommand" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
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

                IGame gametype = commandController.GetGameTypeForCommand(bot.DiceBot, address, terms, out messageString);

                bool keepSession = terms.Contains("keepsession") || terms.Contains("keepgame");
                bool endSession = terms.Contains("endsession") || terms.Contains("endgame");

                if(gametype != null) //gametype can be set above after being null so this should no longer be 'else'
                {
                    GameSession sesh = bot.DiceBot.GetGameSession(address, gametype);

                    if (sesh != null)
                    {
                        ChipPile potChips = bot.DiceBot.GetChipPile( new MessageAddress() { character = DiceBot.PotPlayerAlias, channel = address.channel, guild = address.guild });

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
                        else if (sesh.CurrentGame.GetType() == typeof(Roulette) && (thisChannel.LastRouletteSpinTime + thisChannel.SinglePlayerGameCooldownSeconds - DoubleTime.GetCurrentTimestampSeconds()) > 0)
                        {
                            double secondsRemain = thisChannel.LastRouletteSpinTime + thisChannel.SinglePlayerGameCooldownSeconds - DoubleTime.GetCurrentTimestampSeconds();
                            messageString = "Cannot start " + sesh.CurrentGame.GetGameName() + " because the starting countdown is not yet finished. [b]" + secondsRemain.ToString("N0") + " seconds [/b]remain.";
                        }
                        else if (sesh.CurrentGame.GetType() == typeof(Blackjack) && (thisChannel.LastBlackjackGameTime + thisChannel.SinglePlayerGameCooldownSeconds - DoubleTime.GetCurrentTimestampSeconds()) > 0)
                        {
                            double secondsRemain = thisChannel.LastBlackjackGameTime + thisChannel.SinglePlayerGameCooldownSeconds - DoubleTime.GetCurrentTimestampSeconds();
                            messageString = "Cannot start " + sesh.CurrentGame.GetGameName() + " because the starting countdown is not yet finished. [b]" + secondsRemain.ToString("N0") + " seconds [/b]remain.";
                        }
                        //else if (sesh.CurrentGame.GetType() == typeof(Roulette) && !bot.DiceBot.CountdownFinishedOrNotStarted(address, sesh.CurrentGame.GetGameName()))
                        //{
                        //    double secondsRemain = bot.DiceBot.GetSecondsRemainingOnCountdownTimer(address, gametype.GetGameName());

                        //    messageString = "Cannot start " + sesh.CurrentGame.GetGameName() + " because the starting countdown is not yet finished. [b]" + secondsRemain.ToString("N3") + " seconds [/b]remain.";
                        //}
                        else
                        {
                            messageString = sesh.CurrentGame.GetStartingDisplay();
                            messageString += "\n" + bot.DiceBot.StartGame(address, address.character, gametype, bot, keepSession, endSession);

                            //if (gametype.GetMinimumMsBetweenGames() > 0)
                            //{
                                //int minimumMs = thisChannel.SinglePlayerGameCooldownSeconds * 1000;
                                //bot.DiceBot.StartCountdownTimer(address, gametype.GetGameName(), minimumMs);// gametype.GetMinimumMsBetweenGames());
                            //}

                            if (gametype.GetType() == typeof(Roulette))
                            {
                                thisChannel.LastRouletteSpinTime = DoubleTime.GetCurrentTimestampSeconds();
                            }
                            else if (gametype.GetType() == typeof(Blackjack))
                            {
                                thisChannel.LastBlackjackGameTime = DoubleTime.GetCurrentTimestampSeconds();
                            }
                            else if (gametype.GetType() == typeof(DungeonDelve))
                            {
                                thisChannel.LastDungeonDelveTime = DoubleTime.GetCurrentTimestampSeconds();
                            }

                            commandController.SaveChipsToDisk("StartGame");
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
