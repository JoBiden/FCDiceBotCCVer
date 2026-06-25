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
using System.Text.RegularExpressions;

namespace FChatDicebot.BotCommands
{
    public class ShowChips : ChatBotCommand
    {
        public ShowChips()
        {
            Name = "showchips";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = false;
            LockCategory = CommandLockCategory.ChannelScores;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            bool fromChannel = true;
            //string channelKeyUsed = address.GetChannelKey();
            MessageAddress channelAddressToExamine = new MessageAddress() { channel = address.channel, character = address.character, guild = address.guild };
            string errorString = "";
            if(string.IsNullOrEmpty(channelAddressToExamine.channel))
            {
                fromChannel = false;
                channelAddressToExamine.channel = Utils.GetChannelIdFromInputs(rawTerms, out errorString);
            }
            ChannelSettings thisChannel = bot.GetChannelSettings(channelAddressToExamine);

            bool characterIsAdmin = Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, address.character);

            if(thisChannel == null)
            {
                string channelErrorResponse = "Failed: channel \"" + channelAddressToExamine.GetChannelKey() + "\" was not found. " + errorString;

                if(!fromChannel)
                {
                    if (channelAddressToExamine.channel == null || channelAddressToExamine.channel.Length < 20)
                    {
                        channelErrorResponse = "Failed: when used as a private message this command requires a channel code paste from /code . " + errorString;
                    }
                    bot.SendPrivateMessage(channelErrorResponse, address);
                }
                else
                {
                    bot.SendPrivateMessage(channelErrorResponse, address);
                }
            }
            else if (!thisChannel.AllowChips)
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.", address);
            }
            else
            {
                bool characterClearance = !thisChannel.OnlyOpViewOthersChips || characterIsAdmin || (command.ops != null && command.ops.Contains(command.characterName));

                bool all = false;
                bool pot = false;
                bool secret = false;

                bool otherUserName = false;
                bool nameNotFound = false;

                string userNameUsed = address.character;
                string tempUserName = "";
                if(terms != null && terms.Length >= 1)
                {
                    int indexOfSecret = -1;
                    for (int i = 0; i < terms.Length; i++ )
                    {
                        if (terms[i] == "secret" || terms[i] == "s")
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

                    tempUserName = Utils.GetUserNameFromFullInputs(usedTermsForname);
                    channelAddressToExamine.character = tempUserName;
                }

                ChipPile testFoundPile = bot.DiceBot.GetChipPile(channelAddressToExamine, false);

                int startFromNumber = 0;
                int topCount = 0;

                if (testFoundPile == null)
                {
                    if (terms.Contains("all"))
                    {
                        all = true;
                    }
                    else if (terms.Contains("pot"))
                    {
                        pot = true;
                    }
                    else
                    {
                        bool found = false;

                        foreach (string term in terms)
                        {
                            Match topMatch = Regex.Match(term, @"^top(\d+)$", RegexOptions.IgnoreCase);

                            if (topMatch.Success)
                            {
                                topCount = int.Parse(topMatch.Groups[1].Value);
                                found = true;
                                break;
                            }

                            Match fromMatch = Regex.Match(term, @"^from(\d+)$", RegexOptions.IgnoreCase);

                            if (fromMatch.Success)
                            {
                                startFromNumber = int.Parse(fromMatch.Groups[1].Value);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                            nameNotFound = true;
                    }

                    //otherUserName = true;

                    //int startFromNumber = 0;

                    //if (terms.Contains("all"))
                    //    all = true;
                    //else if (terms.Contains("pot"))
                    //    pot = true;
                    //else if (terms.Contains("top10"))
                    //    top10 = true;
                    //else if (terms.Contains("top50"))
                    //    top50 = true;
                    //else if (terms.Contains("top100"))
                    //    top100 = true;
                    //else
                    //    nameNotFound = true;
                }
                else if (!string.IsNullOrEmpty(tempUserName) && terms != null && terms.Length > 0)
                    //found with the temp name therefore they entered another user's name and it resolved
                {
                    otherUserName = true;
                    userNameUsed = tempUserName;
                }

                bool requiresClearance = thisChannel.OnlyOpViewOthersChips && (otherUserName || all || topCount > 0 || startFromNumber > 0);

                string messageString = "";
                if (requiresClearance && !characterClearance && command.ops == null)
                {
                    bot.RequestChannelOpListAndQueueFurtherRequest(command);
                }
                else
                {
                    if (requiresClearance && !characterClearance && command.ops != null)
                    {
                        messageString = "Failed: You cannot view other users' " + BotMain.CurrencyPlaceholder + "s with this channel's current settings.";
                    }
                    else if (!characterClearance && thisChannel.OnlyOpViewOthersChips && !string.IsNullOrEmpty(channelAddressToExamine.character) && (userNameUsed.ToLower() != channelAddressToExamine.character.ToLower()))
                    {
                        messageString = "Failed: You cannot view another user's " + BotMain.CurrencyPlaceholder + "s with this channel's current settings.";
                    }
                    else if (all)
                    {
                        messageString = bot.DiceBot.ListAllChipPiles(channelAddressToExamine);
                    }
                    else if (topCount > 0)
                    {
                        messageString = bot.DiceBot.ListTopNChipPiles(channelAddressToExamine, topCount);
                    }
                    else if (startFromNumber > 0)
                    {
                        messageString = bot.DiceBot.ListAllChipPiles(channelAddressToExamine, startFromNumber);
                    }
                    else
                    {
                        if (pot)
                            channelAddressToExamine.character = DiceBot.PotPlayerAlias;
                        else
                            channelAddressToExamine.character = userNameUsed;

                        messageString = bot.DiceBot.DisplayChipPile(channelAddressToExamine);
                    }

                    if ((all || topCount > 0 || startFromNumber > 0) || secret || !fromChannel)
                    {
                        bot.SendPrivateMessage(messageString, address);
                    }
                    else
                    {
                        bot.SendMessageInChannel(messageString, address);
                    }
                }


            }
        }
    }
}
