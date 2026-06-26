using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FChatDicebot.BotCommands.Base;
using FChatDicebot.Model;
using FChatDicebot.SavedData;
using Newtonsoft.Json;

namespace FChatDicebot.BotCommands
{
    public class DeleteJobsList : ChatBotCommand
    {
        public DeleteJobsList()
        {
            Name = "deletejobslist";
            RequireBotAdmin = false;
            RequireChannelAdmin = true;
            RequireChannel = true;
            LockCategory = CommandLockCategory.SavedTables;
        }

        public override void Run(BotMain bot, BotCommandController commandController, string[] rawTerms, string[] terms, MessageAddress address, UserGeneratedCommand command)
        {
            SavedJobsList jobsList = Utils.GetJobsListFromChannel(bot.SavedJobsLists, address.GetChannelKey());

            bool characterIsAdmin = Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, address.character);

            string sendMessage = "";
            if (jobsList == null)
            {
                sendMessage = "Failed: No JobsList found for channel " + address.GetChannelKey();
            }
            else
            {
                if (jobsList.OriginCharacter.ToLower() != address.character.ToLower() && !characterIsAdmin)
                {
                    sendMessage = "Only " + jobsList.OriginCharacter + " can delete their own saved jobs list.";
                }
                else
                {
                    bot.SavedJobsLists.Remove(jobsList);

                    sendMessage = "[b]" + jobsList.Channel + "[/b] jobs list deleted by " + TextFormat.GetCharacterUserTags(address.character);
                    bot.BotCommandController.SaveJobsListDataToDisk();
                }
            }

            bot.SendMessageInChannel(sendMessage, address);
        }
    }
}
