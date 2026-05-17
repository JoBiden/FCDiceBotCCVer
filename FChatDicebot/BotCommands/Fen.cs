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

namespace FChatDicebot.BotCommands
{
    public class Fen : ChatBotCommand
    {
        public Fen()
        {
            Name = "fen";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.NONE;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            if (thisChannel.AllowChess)
            {
                bool useEicons = thisChannel.AllowChessEicons && (channel.ToLower() == BotMain.ChessClubChannelId || channel.ToLower() == BotMain.TestDicebotChannelId);
            
                string fenString = Utils.GetFullStringOfInputs(rawTerms);

                string sendMessage = "";
                try
                {
                    Console.WriteLine("Hello, World!");
                    if(fenString.ToLower() == "test" || fenString.ToLower() == "hello world")
                    {
                        fenString = "rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 1 2";
                    }

                    ChessFENPosition position = new ChessFENPosition();
                    position.loadFromFEN(fenString);
                    string bbcode = position.ToBBCode(useEicons);
                    Console.WriteLine(bbcode);
                    sendMessage = "Position loaded: \n" + bbcode;
                }
                catch(Exception exc)
                {
                    sendMessage = "ERROR: " + exc.ToString();
                }

                bot.SendMessageInChannel(sendMessage, channel);
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", channel);
            }
        }
    }
}

// See https://aka.ms/new-console-template for more information

