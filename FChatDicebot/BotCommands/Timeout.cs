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
    public class Timeout : ChatBotCommand
    {
        public Timeout()
        {
            Name = "timeout";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.CharacterInventories;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {

            string targetUserName = "";// address.character;
            string output = "";

            int timeoutNumber = -1;
            int count = 0;
            if (rawTerms.Count() <= 1)
            {
                output = "Failed: No name found for user to timeout.";
            }
            else {
                while (timeoutNumber < 0 && count < rawTerms.Count())
                {
                    int.TryParse(rawTerms[count], out timeoutNumber);
                    if (timeoutNumber == 0 && rawTerms[count] != "0")
                        timeoutNumber = -1;
                    //if(timeoutNumber >= 0)

                    count++;
                }
                if (timeoutNumber < 0)
                {
                    output = "Failed: No number found for timeout seconds length.";
                }
                else
                {
                    string[] newTerms = new string[rawTerms.Count() - 1];
                    for (int i = 0; i < rawTerms.Count(); i++)
                    {
                        if (i >= count)
                            newTerms[i - 1] = rawTerms[i];
                        else if (i == count - 1)
                            continue;
                        else
                            newTerms[i] = rawTerms[i];
                    }

                    if (rawTerms != null && rawTerms.Length > 0)
                    {
                        targetUserName = Utils.GetUserNameFromFullInputs(newTerms);
                    }

                    MessageAddress targetUserAddress = new MessageAddress() { character = targetUserName, channel = address.channel, guild = address.guild };


                    CharacterData data = bot.DiceBot.GetCharacterData(targetUserAddress, false);

                    if (data == null)
                    {
                        output = "Failed: could not find character name.";
                    }
                    else if (data.Character == address.character)
                    {
                        output = "Failed: you cannot timeout yourself.";
                    }
                    else
                    {
                        data.TimeoutDuration = timeoutNumber;
                        data.TimeoutStartTime = DoubleTime.GetCurrentTimestampSeconds();
                        bot.BotCommandController.SaveCharacterDataToDisk();
                        if(timeoutNumber == 0)
                            output = TextFormat.GetCharacterUserTags(targetUserName) + " is no longer timed out from using bot commands in this channel.";
                        else
                            output = TextFormat.GetCharacterUserTags(targetUserName) + " was timed out from using bot commands in this channel for " + timeoutNumber + " second" + TextFormat.SIfPlural(timeoutNumber) + ".";
                    }
                }
            }

            bot.SendMessageInChannel(output, address);
        }
    }
}
