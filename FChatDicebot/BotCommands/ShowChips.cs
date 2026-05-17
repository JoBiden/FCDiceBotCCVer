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
    public class ShowChips : ChatBotCommand
    {
        public ShowChips()
        {
            Name = "showchips";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            if (thisChannel.AllowChips)
            {
                bool all = false;
                bool pot = false;
                bool secret = false;

                string userNameUsed = characterName;
                if(terms != null && terms.Length >= 1)
                {
                    int indexOfSecret = -1;
                    for (int i = 0; i < terms.Length; i++ )
                    {
                        if (terms[i] == "secret")
                        {
                            secret = true;
                            indexOfSecret = i;
                            break;
                        }
                    }

                    string[] usedTermsForname = rawTerms;
                    if(indexOfSecret >= 0)
                    {

                        string removeSecret = rawTerms[indexOfSecret];
                        usedTermsForname = Utils.GetRemainingTermsAfterRemovingOneTerm(rawTerms, removeSecret);
                    }

                    string userName = Utils.GetUserNameFromFullInputs(usedTermsForname);
                    ChipPile foundPile = bot.DiceBot.GetChipPile(userName, channel, false);

                    if(foundPile == null)
                    {
                        if (terms.Contains("all"))
                            all = true;

                        if (terms.Contains("pot"))
                            pot = true;
                    }
                    else
                    {
                        userNameUsed = userName;
                    }
                }
                
                string messageString = "";
                if (all)
                {
                    messageString = bot.DiceBot.ListAllChipPiles(channel);
                }
                else
                {
                    if (pot)
                        messageString = bot.DiceBot.DisplayChipPile(channel, DiceBot.PotPlayerAlias);
                    else
                        messageString = bot.DiceBot.DisplayChipPile(channel, userNameUsed);
                }

                if(all || secret)
                {
                    bot.SendPrivateMessage(messageString, characterName);
                }
                else
                {
                    bot.SendMessageInChannel(messageString, channel);
                }

            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", channel);
            }
        }
    }
}
