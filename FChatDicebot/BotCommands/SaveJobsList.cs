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
    public class SaveJobsList : ChatBotCommand
    {
        public SaveJobsList()
        {
            Name = "savejobslist";
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
            bool usedCustomChannel = !string.IsNullOrEmpty(channelName);

            if (!string.IsNullOrEmpty(channelIdError) && !fromChannel)
            {
                SendMessageToChannelOrUser(bot, commandController, address, channelIdError);
                return;
            }
            else if (!fromChannel || (thisChannel != null && thisChannel.AllowTableInfo))
            {
                string tableName = commandController.GetTableNameFromCommandTerms(terms);

                if (fromChannel && string.IsNullOrEmpty(channelName))
                    channelName = address.GetChannelKey();
            }

            //secondary check for required ops
            if (command.ops == null)
            {
                //get the channel Id to check...
                bot.RequestChannelOpListAndQueueFurtherRequest(command, channelName);
                return;
            }
            else if (command.ops != null && !command.ops.Contains(command.characterName) && !Utils.IsCharacterAdmin(bot.AccountSettings.AdminCharacters, command.characterName))
            {
                SendMessageToChannelOrUser(bot, commandController, address, "Error: You are not a channel OP of the requested channel!");
                return;
            }

            string saveJson = Utils.GetFullStringOfInputs(rawTerms);
            if (usedCustomChannel)//new channel used
                saveJson = Utils.GetFullStringOfInputsAfterTermX(rawTerms, 1);
            string sendMessage = "save jobs list error";

            try
            {
                FChatDicebot.DiceFunctions.JobsList newJobsList = JsonConvert.DeserializeObject<FChatDicebot.DiceFunctions.JobsList>(saveJson);

                MessageAddress tempAddress = address;
                if (!Utils.IsDiscordMessage(command) && usedCustomChannel)
                    tempAddress = new MessageAddress() { channel = channelName, character = address.character, guild = address.guild };

                ChannelSettings settings = bot.GetChannelSettings(tempAddress);
                if(newJobsList == null)
                {
                    SendMessageToChannelOrUser(bot, commandController, address, "Failed: could not parse jobs list JSON.");
                    return;
                }
                else if (settings != null && Utils.GetNsfwError(settings, newJobsList, out sendMessage))
                {
                    SendMessageToChannelOrUser(bot, commandController, address, sendMessage);
                    return;
                }

                //List<Enchantment> allEnchantments = bot.DiceBot.PotionGenerator.GetAllEnchantments(bot, true, address);
                //var thisCharacterEnchantments = allEnchantments.Where(a => a.CreatedBy == address.character);
                var thisCharacterTotalJobsLists = bot.GetCharacterTotalJobsLists(address.character);

                SavedJobsList existingJobsList = Utils.GetJobsListFromChannel(bot.SavedJobsLists, tempAddress.GetChannelKey());// allEnchantments.FirstOrDefault(a => a.suffix.ToLower() == lowerSuf || a.prefix.ToLower() == lowerPre);

                if (thisCharacterTotalJobsLists.Count() >= BotMain.MaximumSavedJobListsPerCharacter && existingJobsList == null)
                {
                    sendMessage = "Failed: A character can only save up to " + BotMain.MaximumSavedJobListsPerCharacter + " job lists at one time. Delete or overwrite old job lists.";
                }
                //else if (existingJobsList != null && existingJobsList.OriginCharacter != address.character)
                //{
                //    sendMessage = "Failed: Channel job list is already created by another character.";
                //}
                else if (newJobsList.JobTiers == null || newJobsList.JobTiers.Count == 0)
                {
                    sendMessage = "Failed: A jobs list must contain more than 0 job tiers.";
                }
                else if (newJobsList.JobTiers.Where(a => a.Jobs != null).SelectMany(a => a.Jobs).Count() > BotMain.MaximumSavedJobEntries)
                {
                    sendMessage = "Failed: A jobs list cannot contain more than " + BotMain.MaximumSavedJobEntries + " entries of jobs.";
                }
                else if (newJobsList.JobTiers.Sum(a => a.Probability) < 0.99 || newJobsList.JobTiers.Sum(a => a.Probability) > 1.01)
                {
                    sendMessage = "Failed: A jobs list's job tiers must add up to 1.0 total probability.";
                }
                else if (newJobsList.JobTiers.Count > BotMain.MaximumJobTiers)
                {
                    sendMessage = "Failed: A jobs list cannot contain more than " + BotMain.MaximumJobTiers + " job tiers.";
                }
                //else if (newJobsList.Entries == null || newJobsList.Entries.Count == 0)
                //{
                //    sendMessage = "Failed: A jobs list must contain more than 0 entries of jobs.";
                //}
                //else if (newJobsList.Entries.Count > BotMain.MaximumSavedJobEntries)
                //{
                //    sendMessage = "Failed: A jobs list cannot contain more than " + BotMain.MaximumSavedJobEntries + " entries of jobs.";
                //}
                //else if (newJobsList.TierProbabilities == null || newJobsList.TierProbabilities.Count == 0)
                //{
                //    sendMessage = "Failed: A jobs list must contain more than 0 entries of Tier Probabilities.";
                //}
                //else if (newJobsList.TierProbabilities.Count > BotMain.MaximumJobTiers)
                //{
                //    sendMessage = "Failed: A jobs list cannot contain more than " + BotMain.MaximumJobTiers + " job tiers.";
                //}
                else if (string.IsNullOrEmpty(newJobsList.Name) || newJobsList.Name.Length < 4)
                {
                    sendMessage = "Failed: A jobs list must include a name of at least 4 characters in length.";
                }
                else
                {
                    foreach (TierEntry tierEntry in newJobsList.JobTiers)
                    {
                        tierEntry.RareMultiplier = Utils.Clamp(tierEntry.RareMultiplier, 0, 10000);
                        tierEntry.RareProbability = Utils.Clamp(tierEntry.RareProbability, 0, 1);
                        tierEntry.Probability = Utils.Clamp(tierEntry.Probability, 0, 1);
                        tierEntry.TierColor = Utils.LimitStringToNCharacters(tierEntry.TierColor, BotMain.MaximumCharactersEiconName);
                        tierEntry.Tier = Utils.Clamp(tierEntry.Tier, 0, BotMain.MaximumJobTiers);

                        foreach (JobEntry jobEntry in tierEntry.Jobs)
                        {
                            jobEntry.Description = jobEntry.Description == null ? null : Utils.LimitStringToNCharacters(jobEntry.Description, BotMain.MaximumCharactersPotionDescription);
                            jobEntry.Name = jobEntry.Name == null ? null : Utils.LimitStringToNCharacters(jobEntry.Name, BotMain.MaximumCharactersPotionName);
                            //jobEntry.Tier = tierEntry.Tier;// jobEntry.Tier < 0 ? 0 : Math.Min(jobEntry.Tier, BotMain.MaximumJobTiers);
                            //jobEntry.
                        }
                    }


                    newJobsList.Name = newJobsList.Name == null ? null : Utils.LimitStringToNCharacters(newJobsList.Name, BotMain.MaximumCharactersPotionName);

                    if (existingJobsList != null)
                    {
                        existingJobsList.OverwriteJobsList(newJobsList);
                        existingJobsList.OriginCharacter = address.character;
                    }
                    else
                    {
                        SavedJobsList newSavedJobsList = new SavedJobsList()
                        {
                            Channel = tempAddress.GetChannelKey(),
                            DefaultList = true,
                            JobsList = newJobsList,
                            OriginCharacter = tempAddress.character
                        };
                        //SavedPotion newSavedPotion = new SavedPotion()
                        //{
                        //    Channel = address.GetChannelKey(),
                        //    DefaultPotion = false,
                        //    Enchantment = newPotion,
                        //    OriginCharacter = address.character
                        //};

                        bot.SavedJobsLists.Add(newSavedJobsList);
                    }

                    commandController.SaveJobsListDataToDisk();

                    sendMessage = "[b]Success[/b]. Jobs list saved by " + TextFormat.GetCharacterUserTags(tempAddress.character) + ". This will now be the default jobs list used for !work";
                }
            }
            catch (Exception)
            {
                sendMessage = "Exception (196) on saving jobs list. Failed to parse jobs list entry data. Make sure the Json is correctly formatted. ";// + e.Message;
            }

            SendMessageToChannelOrUser(bot, commandController, address, sendMessage);
        }
    }
}
