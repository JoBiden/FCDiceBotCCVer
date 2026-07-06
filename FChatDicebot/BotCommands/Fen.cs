using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using Newtonsoft.Json;
using FChatDicebot.DiceFunctions;

using System.Runtime.InteropServices;
using System.Xml;
using FChatDicebot.BotCommands.ChessFEN;
using FChatDicebot.Model;

namespace FChatDicebot.BotCommands
{
    public class Fen : ChatBotCommand
    {
        public Fen()
        {
            Name = "fen";
            Aliases = new string[] { };
            Category = "Dicebot";
            ShortDescription = "Display a chess board from FEN notation";
            LongDescription = "Renders a chess board position from FEN (Forsyth-Edwards Notation) text. 'starting' or 'default' shows the standard opening position. Add 'setdescription' to set this channel's description to the rendered board (requires the bot to be a channel admin, or you to be a bot admin).";
            Usage = "!fen {FEN string}\nor\n!fen starting\nor\n!fen {FEN string} setdescription";
            RelatedCommands = new string[] { "joingame", "gamecommand" };
            CooldownDuration = null;
            CooldownAppliesTo = null;
            IdentifierCategory = null;
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(address);

            if (thisChannel == null || thisChannel.AllowChess)
            {
                string channelKey = address.GetChannelKey() == null? "" : address.GetChannelKey().ToLower();
                bool useEicons = thisChannel == null || (thisChannel.AllowChessEicons && !(bot.AccountSettings.AllowedChessEiconChannels == null) && bot.AccountSettings.AllowedChessEiconChannels.Contains(address.GetChannelKey().ToLower()));
                    //(channelKey == BotMain.ChessClubChannelId || channelKey == BotMain.TestDicebotChannelId));

                string fenString = Utils.GetFullStringOfInputs(rawTerms);
                bool setDescriptionOutput = false;

                if (fenString.Contains("setdescription"))
                {
                    fenString = fenString.Replace("setdescription ", "").Replace(" setdescription", "");
                    setDescriptionOutput = true;
                }
                fenString = fenString.Trim();

                string sendMessage = "";
                try
                {
                    if (fenString.ToLower() == "test" || fenString.ToLower() == "hello world")
                    {
                        fenString = "rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 1 2";
                    }
                    else if (fenString.ToLower() == "starting" || fenString.ToLower() == "default" || fenString.ToLower() == "start")
                    {
                        fenString = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
                    }

                    bool discord = Utils.IsDiscordMessage(command);
                    ChessFENPosition position = new ChessFENPosition(discord);
                    position.loadFromFEN(fenString);
                    string bbcode = position.ToBBCode(useEicons);
                    Console.WriteLine(bbcode);
                    sendMessage = "Position loaded: \n" + bbcode;
                }
                catch(Exception exc)
                {
                    sendMessage = "ERROR: " + exc.ToString();
                }

                //bot.SendMessageInChannel(sendMessage, address);

                //if (fromChannel)
                //    Bot.SendMessageInChannel("Failed: " + TextFormat.GetCharacterUserTags(Bot.AccountSettings.CharacterName) + " needs to be a channel/ guild op to use this command (" + command.commandName + ").", address);
                //else
                //    Bot.SendPrivateMessage("Failed: " + TextFormat.GetCharacterUserTags(Bot.AccountSettings.CharacterName) + " needs to be a channel/ guild op to use this command (" + command.commandName + ").", address);
                if (!commandController.MessageCameFromChannel(address))
                {
                    bot.SendPrivateMessage(sendMessage, address);
                }
                else
                {
                    if (setDescriptionOutput)
                    {

                        if (thisChannel != null && !thisChannel.AllowSettingChannelDescription && !Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, address.character))
                        {
                            bot.SendMessageInChannel(Name + " setting channel description is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
                            return;
                        }
                        else
                        {
                            bot.SetChannelDescription(address.GetChannelKey(), sendMessage);
                            bot.SendMessageInChannel("Set channel description to chess position! [sub](if it fails, make sure " + TextFormat.GetCharacterUserTags(bot.AccountSettings.CharacterName) + " is a channel admin)[/sub]", address);
                        }
                    }
                    else
                    {
                        bot.SendMessageInChannel(sendMessage, address);
                    }
                }
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
        }
    }
}

// See https://aka.ms/new-console-template for more information

