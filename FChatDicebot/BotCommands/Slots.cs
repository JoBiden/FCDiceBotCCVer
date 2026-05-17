using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.SavedData;
using FChatDicebot.DiceFunctions;

namespace FChatDicebot.BotCommands
{
    public class Slots : ChatBotCommand
    {
        public Slots()
        {
            Name = "slots";
            RequireBotAdmin = false;
            RequireChannelAdmin = false;
            RequireChannel = true;
            LockCategory = CommandLockCategory.ChannelScores;
        }
        //!slots x10 test jackpot
        //!slots x10 test fix2 fix3
        //!slots x10 test fail
        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, string characterName, string channel, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = bot.GetChannelSettings(channel);

            if (thisChannel.AllowChips)
            {
                string slotsName = commandController.GetNonNumberWordFromCommandTerms(terms);

                SavedSlotsSetting savedSlots = Utils.GetSlotsFromId(bot.SavedSlots, slotsName);

                if (thisChannel.AllowSlots)
                {
                    CharacterData thisCharacterData = bot.DiceBot.GetCharacterData(characterName, channel);
                    double slotsTimeSinceLastSpin = Utils.GetCurrentTimestampSeconds() - thisCharacterData.LastSlotsSpin;

                    if (slotsTimeSinceLastSpin > BotMain.SlotsSpinCooldownSeconds || thisChannel.RemoveSlotsCooldown)
                    {
                        //get bet #
                        int betMultiplier = 1;
                        foreach(string s in terms)
                        {
                            if(s.StartsWith("x") || s.EndsWith("x"))
                            {
                                string newS = s.Replace("x", "");
                                int tempMultiplier = 0;
                                int.TryParse(newS, out tempMultiplier);
                                if (tempMultiplier > 0)
                                    betMultiplier = tempMultiplier;
                            }
                        }

                        SlotsTestCommand testCommand = null;
                        if(terms.Contains("test"))
                        {
                            //test mode - set outcome (admin only)
                            if (Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, characterName))
                            {
                                testCommand = new SlotsTestCommand();
                                if(terms.Contains("jackpot"))
                                {
                                    testCommand.Jackpot = true;
                                }
                                if (terms.Contains("fail"))
                                {
                                    testCommand.Fail = true;
                                }
                                if (terms.Contains("fix2"))
                                {
                                    testCommand.Fix2 = true;
                                }
                                if (terms.Contains("fix3"))
                                {
                                    testCommand.Fix3 = true;
                                }
                            }
                            else
                            {
                                bot.SendMessageInChannel("Failed: Forbidden command for non-admin character " + Utils.GetCharacterUserTags(characterName) + ".", channel);
                            }
                        }

                        if(betMultiplier == 1 && terms.Count() > 0)
                        {
                            int tempBet = Utils.GetNumberFromInputs(terms);
                            if (tempBet >= 1)
                                betMultiplier = tempBet;
                        }

                        if (betMultiplier <= 1)
                            betMultiplier = 1;

                        string errorMessage = "";
                        if(betMultiplier > thisChannel.SlotsMultiplierLimit)
                        {
                            errorMessage = "The multiplier given (" + betMultiplier + ") is higher than this channel's maximum slots multiplier (" + thisChannel.SlotsMultiplierLimit + ").";
                        }

                        SlotsSetting usedSlots = null;
                        //find possibilities from channel settings for slots
                        if (savedSlots != null)
                            usedSlots = savedSlots.SlotsSetting;
                        else
                            usedSlots = commandController.GetDefaultSlotsSetting(thisChannel.DefaultSlotsFruit);

                        ChipPile thisCharacterPile = bot.DiceBot.GetChipPile(characterName, channel);

                        string sendMessage = "";
                        int usedNumber = usedSlots.MinimumBet * betMultiplier;
                        
                        if(!string.IsNullOrEmpty(errorMessage))
                        {
                            sendMessage = errorMessage;
                        }
                        else if(thisCharacterPile.Chips < usedNumber)
                        {
                            sendMessage = "You don't have enough chips to spin. Held: (" + thisCharacterPile.Chips + ") required: (" + usedNumber + ")";
                        }
                        else
                        {
                            //spin slots for 3 results
                            sendMessage = bot.DiceBot.SpinSlots(usedSlots, characterName, channel, betMultiplier, testCommand);
                            //get graphics for results
                            thisCharacterData.LastSlotsSpin = Utils.GetCurrentTimestampSeconds();
                            thisCharacterData.TimesSlotsSpun += 1;
                            commandController.SaveChipsToDisk("Slots");
                            commandController.SaveCharacterDataToDisk();
                        }

                        bot.SendMessageInChannel(sendMessage, channel);
                    }
                    else
                    {
                        bot.SendMessageInChannel("You cannot spin the slots for another " + Utils.PrintTimeFromSeconds(BotMain.SlotsSpinCooldownSeconds - slotsTimeSinceLastSpin), channel);
                    }
                }
                else
                {
                    bot.SendMessageInChannel("This channel's settings for " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + " do not allow slots.", channel);
                }
            }
            else
            {
                bot.SendMessageInChannel(Name + " is currently not allowed in this channel under " + Utils.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel. (chips must be allowed)", channel);
            }
        }
    }

    public class SlotsTestCommand
    {
        public bool Jackpot;
        public bool Fix2;
        public bool Fix3;
        public bool Fail;
    }
}
