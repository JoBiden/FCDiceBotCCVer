using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;
using FChatDicebot.Model;
using FChatDicebot.SavedData;

namespace FChatDicebot.BotCommands.Base
{
    public class AddPlayerToGame
    {
        public static void Run(BotMain bot, BotCommandController commandController,
            string[] rawTerms, string[] terms, MessageAddress address,
            UserGeneratedCommand command, string addedCharacterName, string originCommandName)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            string messageString = "";

            if (thisChannel == null)
                messageString = "Error: channel settings not found or created";
            else if (!thisChannel.AllowGames)
            {
                messageString = "Joining games is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.";
            }
            else
            {
                int ante = Utils.GetNumberFromInputs(terms);
                if (ante < 0)
                    ante = 0;

                IGame gametype = commandController.GetGameTypeForCommand(bot.DiceBot, address, terms, out string errorString);
                CharacterData chardat = bot.DiceBot.GetCharacterData(new MessageAddress(address, addedCharacterName), true);
                double dungeonDelveTimeSinceLast = DoubleTime.GetCurrentTimestampSeconds() - chardat.LastDungeonDelve;

                if (!string.IsNullOrEmpty(errorString))
                    messageString = errorString;
                else if (gametype == null)
                    messageString = "Error: gametype not found";
                //else if (chardat == null)
                //    messageString = "Failed: no character data found for " + TextFormat.GetCharacterUserTags(addedCharacterName);
                else if (gametype != null && gametype.GetType() == typeof(DungeonDelve) && !thisChannel.AllowRPG)
                {
                    messageString = "Dungeon Delve " + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel (AllowRPG).";
                }
                else if (gametype != null && gametype.GetType() == typeof(DungeonDelve) && dungeonDelveTimeSinceLast < thisChannel.DungeonDelveCooldownSeconds)
                {
                    messageString = TextFormat.GetCharacterUserTags(addedCharacterName) + " cannot join another Dungeon Delve for " + DoubleTime.PrintTimeFromSeconds(thisChannel.DungeonDelveCooldownSeconds - dungeonDelveTimeSinceLast) + ".";
                }
                else if (ante > 0 && !thisChannel.AllowChips)
                {
                    messageString = "Games" + " [b]with " + BotMain.CurrencyPlaceholder + "s[/b] are currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.";
                }
                else
                {
                    GameSession sesh = bot.DiceBot.GetGameSession(address, gametype);

                    if (sesh != null)
                    {
                        ChipPile characterChips = bot.DiceBot.GetChipPile(address);

                        if (!gametype.AllowAnte() && ante > 0)
                        {
                            messageString = sesh.CurrentGame.GetGameName() + " cannot be played with ante. Try joining the game without an ante amount.";
                        }
                        else if (sesh.Players.Contains(addedCharacterName))
                        {
                            messageString = TextFormat.GetCharacterUserTags(addedCharacterName) + " is already in " + sesh.CurrentGame.GetGameName() + ".";
                        }
                        else if (sesh.Players.Count >= sesh.CurrentGame.GetMaxPlayers())
                        {
                            messageString = TextFormat.GetCharacterUserTags(addedCharacterName) + " cannot join " + sesh.CurrentGame.GetGameName() + " because it is already at the [b]maximum amount of players[/b].";
                        }
                        else if (ante > 0 && characterChips.Chips < ante)
                        {
                            messageString = TextFormat.GetCharacterUserTags(addedCharacterName) + " cannot make a bet for " + ante + " because they do not have enough " + BotMain.CurrencyPlaceholder + "s. [i](" + characterChips.Chips + " " + BotMain.CurrencyPlaceholder + "s owned)[/i]";
                        }
                        else
                        {
                            string timerString = "";

                            double secondsRemain = 0;// bot.DiceBot.GetSecondsRemainingOnCountdownTimer(address, gametype.GetGameName());
                            if (sesh.CurrentGame.GetType() == typeof(Roulette) && (thisChannel.LastRouletteSpinTime + thisChannel.SinglePlayerGameCooldownSeconds - DoubleTime.GetCurrentTimestampSeconds()) > 0)
                            {
                                secondsRemain = thisChannel.LastRouletteSpinTime + thisChannel.SinglePlayerGameCooldownSeconds - DoubleTime.GetCurrentTimestampSeconds();
                            }
                            else if (sesh.CurrentGame.GetType() == typeof(Blackjack) && (thisChannel.LastBlackjackGameTime + thisChannel.SinglePlayerGameCooldownSeconds - DoubleTime.GetCurrentTimestampSeconds()) > 0)
                            {
                                secondsRemain = thisChannel.LastBlackjackGameTime + thisChannel.SinglePlayerGameCooldownSeconds - DoubleTime.GetCurrentTimestampSeconds();
                            }
                            else if (sesh.CurrentGame.GetType() == typeof(DungeonDelve) && (thisChannel.LastDungeonDelveTime + thisChannel.SinglePlayerGameCooldownSeconds - DoubleTime.GetCurrentTimestampSeconds()) > 0)
                            {
                                secondsRemain = thisChannel.LastDungeonDelveTime + thisChannel.SinglePlayerGameCooldownSeconds - DoubleTime.GetCurrentTimestampSeconds();
                            }

                            if (secondsRemain > 0)
                                timerString = "[i] (game can begin in " + secondsRemain.ToString("N0") + " seconds)[/i]";

                            bool anteProblem = false;
                            if (gametype.UsesFlatAnte())
                            {
                                bool anteJustSet = false;
                                if (!sesh.AnteSet) //leave as separate if
                                {
                                    sesh.AnteSet = true;
                                    anteJustSet = true;
                                    sesh.Ante = ante > 0 ? ante : 0;
                                }

                                if (anteJustSet && ante > 0 && characterChips.Chips < sesh.Ante)
                                {
                                    messageString = TextFormat.GetCharacterUserTags(addedCharacterName) + " does not have [b]" + ante + " " + BotMain.CurrencyPlaceholder + "s[/b] to start a game with an ante this high. [i](" + characterChips.Chips + " " + BotMain.CurrencyPlaceholder + "s owned)[/i]";
                                    bot.DiceBot.RemoveGameSession(address, sesh.CurrentGame);
                                    anteProblem = true;
                                }
                                else if (sesh.AnteSet && ante > 0 && sesh.Ante != ante)
                                {
                                    messageString = "The ante for " + sesh.CurrentGame.GetGameName() + " has already been set to something else. Use [b]!joingame " + sesh.CurrentGame.GetGameName() + "[/b] without an ante to match it.";
                                    anteProblem = true;
                                }
                                else if (characterChips.Chips < sesh.Ante)
                                {
                                    messageString = TextFormat.GetCharacterUserTags(addedCharacterName) + " cannot join " + sesh.CurrentGame.GetGameName() + " because they have less than the ante amount of [b]" + sesh.Ante + " " + BotMain.CurrencyPlaceholder + "s.[/b] [i](" + characterChips.Chips + " " + BotMain.CurrencyPlaceholder + "s owned)[/i]";
                                    anteProblem = true;
                                }
                            }

                            if (!anteProblem)
                            {
                                string outMessage = "";
                                bool successfullyAddedGameData = gametype.AddGameDataForPlayerJoin(addedCharacterName, sesh, bot, terms, ante, out outMessage);

                                if (!successfullyAddedGameData)
                                {
                                    messageString = outMessage;
                                }
                                else
                                {
                                    messageString = bot.DiceBot.JoinGame(new MessageAddress()
                                    {
                                        character = addedCharacterName,
                                        channel = address.channel,
                                        guild = address.guild
                                    }, gametype);
                                    //addedCharacterName, channel, gametype);
                                    messageString += "\n" + sesh.Players.Count + " players ready. [i](min " + sesh.CurrentGame.GetMinPlayers() + ", max " + sesh.CurrentGame.GetMaxPlayers() + ")[/i]" +
                                        (string.IsNullOrEmpty(outMessage) ? "" : "\n" + outMessage);

                                    if (!string.IsNullOrEmpty(timerString))
                                    {
                                        messageString += timerString;
                                    }
                                    else if (sesh.HasEnoughPlayersToStart())
                                    {
                                        messageString += "[b] (Ready to start!)[/b]";
                                    }
                                }
                            }
                        }//end too many players or player already joined
                    }
                    else
                    {
                        messageString = "Failed: Game session for " + gametype.GetGameName() + " not found or created.";
                    }//end game session null
                }//end dungeon delve cd/legality and ante checks
            }

            if(!string.IsNullOrEmpty(messageString))
                bot.SendMessageInChannel(messageString, address);
        }
    }
}
