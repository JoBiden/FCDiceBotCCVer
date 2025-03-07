﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class ShuffleDeck : ChatBotCommand
    {
        public ShuffleDeck()
        {
            Name = "shuffledeck";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelDecks;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            bool fullShuffle = false;
            if (terms != null && terms.Length >= 1 && terms.Contains("eh"))
                fullShuffle = true;

            DeckType deckType = commandController.GetDeckTypeFromCommandTerms(terms);

            string deckTypeString = Utils.GetDeckTypeStringHidePlaying(deckType, characterName);

            string customDeckName = Utils.GetCustomDeckName(characterName);
            bot.DiceBot.ShuffleDeck(bot.DiceBot.random, channel, deckType, fullShuffle, customDeckName);
            bot.SendMessageInChannel("[i]" + deckTypeString + "Channel deck shuffled. " + (fullShuffle ? "Hands emptied." : "") + "[/i]", channel);
        }
    }
}
