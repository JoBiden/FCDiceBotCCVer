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
    public class JobsListJson : ChatBotCommand
    {
        public JobsListJson()
        {
            Name = "jobslistjson";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = false;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            ChannelSettings thisChannel = null;
            bool fromChannel = commandController.MessageCameFromChannel(address);

            if (fromChannel)
                thisChannel = bot.GetChannelSettings(address);

            string channelName = commandController.GetChannelFromInputs(rawTerms, out string channelIdError);
            //todo: channel stuff error handling and such, remove channel id from terms (first term)

            string outputMessage = "";

            if (!string.IsNullOrEmpty(channelIdError) && !fromChannel)
            {
                outputMessage = channelIdError;
            }
            else if (!fromChannel || (thisChannel != null && thisChannel.AllowTableInfo))
            {
                string tableName = commandController.GetTableNameFromCommandTerms(terms);

                if (fromChannel && string.IsNullOrEmpty(channelName))
                    channelName = address.GetChannelKey();

                SavedJobsList infoJobsList = Utils.GetJobsListFromChannel(bot.SavedJobsLists, channelName);// address.GetChannelKey());

                string sendMessage = "Table \'" + tableName + "\' not found.";
                if (infoJobsList != null && Utils.GetNsfwError(thisChannel, infoJobsList.JobsList, out sendMessage))
                {
                    //sendMessage set in error method
                }
                if (infoJobsList != null)
                {
                    sendMessage = "Channel id [b]" + infoJobsList.Channel + "[/b] jobs list created by " + TextFormat.GetCharacterUserTags(infoJobsList.OriginCharacter);

                    if (infoJobsList.JobsList != null)
                    {
                        string tabledesc = "\n" + " JSON:\n";
                        //tabledesc += JsonConvert.SerializeObject(infoJobsList.JobsList);
                        tabledesc += JsonConvert.SerializeObject(infoJobsList.JobsList, Formatting.None, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DefaultValueHandling = DefaultValueHandling.Ignore
                        });

                        sendMessage += tabledesc;
                    }
                    else
                    {
                        sendMessage += "\n (JobsList contents not found)";
                    }
                }

                outputMessage = sendMessage;
            }
            else
            {
                outputMessage = Name + " is currently not allowed in this channel under " + TextFormat.GetCharacterUserTags(DiceBot.DiceBotCharacter) + "'s settings for this channel.";
            }


            //output:
            if (fromChannel)
                bot.SendMessageInChannel(outputMessage, address);
            else
                bot.SendPrivateMessage(outputMessage, address);

        }
    }
}
